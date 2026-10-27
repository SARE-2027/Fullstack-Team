import 'dart:convert';

import 'package:bloc_test/bloc_test.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'package:sare_kiosk/core/api/api_client.dart';
import 'package:sare_kiosk/core/storage/session_store.dart';
import 'package:sare_kiosk/features/cart/data/repositories/local_smart_cart_engine.dart';
import 'package:sare_kiosk/features/session/data/repositories/session_repository.dart';
import 'package:sare_kiosk/features/session/presentation/cubit/session_cubit.dart';
import 'package:sare_kiosk/features/session/presentation/cubit/session_state.dart';

const _guestSessionJson = {
  'sessionId': 'sess-guest-1',
  'cartId': 'CART-01',
  'status': 'Open',
  'totalMinor': 0,
  'itemsCount': 0,
  'startedAt': '2026-01-01T10:00:00Z',
  'updatedAt': '2026-01-01T10:00:00Z',
  'items': <Object>[],
};

http.Client _onlineClient({Map<String, Object>? sessionJson}) {
  return MockClient((request) async {
    return http.Response(
      jsonEncode(sessionJson ?? _guestSessionJson),
      200,
      headers: {'content-type': 'application/json'},
    );
  });
}

http.Client _offlineClient() {
  return MockClient((request) async {
    throw http.ClientException('backend unreachable');
  });
}

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  setUp(() {
    SharedPreferences.setMockInitialValues({});
  });

  group('SessionCubit — guest check-in', () {
    blocTest<SessionCubit, SessionState>(
      'emits loading then loaded when the backend accepts a guest session',
      build: () {
        final api = ApiClient(
          baseUrl: 'http://localhost:5000',
          httpClient: _onlineClient(),
        );
        return SessionCubit(
          SessionRepository(api, LocalSmartCartEngine()),
          SessionStore(),
        );
      },
      act: (cubit) => cubit.startGuestSession('CART-01'),
      expect: () => [
        isA<SessionState>()
            .having((s) => s.status, 'status', SessionStatusKind.loading)
            .having((s) => s.session, 'session', isNull),
        isA<SessionState>()
            .having((s) => s.status, 'status', SessionStatusKind.loaded)
            .having((s) => s.session!.sessionId, 'sessionId', 'sess-guest-1')
            .having((s) => s.session!.isOpen, 'isOpen', true)
            .having((s) => s.session!.isMember, 'isMember', false),
      ],
    );

    blocTest<SessionCubit, SessionState>(
      'falls back to the offline engine when the backend is unreachable',
      build: () {
        final api = ApiClient(
          baseUrl: 'http://localhost:5000',
          httpClient: _offlineClient(),
        );
        return SessionCubit(
          SessionRepository(api, LocalSmartCartEngine()),
          SessionStore(),
        );
      },
      act: (cubit) => cubit.startGuestSession('CART-01'),
      expect: () => [
        isA<SessionState>().having(
            (s) => s.status, 'status', SessionStatusKind.loading),
        isA<SessionState>()
            .having((s) => s.status, 'status', SessionStatusKind.loaded)
            .having((s) => s.session!.isOfflineMode, 'isOfflineMode', true)
            .having((s) => s.session!.cartId, 'cartId', 'CART-01'),
      ],
    );

    blocTest<SessionCubit, SessionState>(
      'rejects an empty cart identifier without loading',
      build: () {
        final api = ApiClient(
          baseUrl: 'http://localhost:5000',
          httpClient: _onlineClient(),
        );
        return SessionCubit(
          SessionRepository(api, LocalSmartCartEngine()),
          SessionStore(),
        );
      },
      act: (cubit) => cubit.startGuestSession(''),
      expect: () => [
        isA<SessionState>()
            .having((s) => s.status, 'status', SessionStatusKind.failure)
            .having((s) => s.error, 'error', isNotNull),
      ],
    );
  });

  group('SessionCubit — persistence & lifecycle', () {
    blocTest<SessionCubit, SessionState>(
      'hydrate loads a session by id and persists it to the store',
      build: () {
        final api = ApiClient(
          baseUrl: 'http://localhost:5000',
          httpClient: _onlineClient(),
        );
        return SessionCubit(
          SessionRepository(api, LocalSmartCartEngine()),
          SessionStore(),
        );
      },
      act: (cubit) => cubit.hydrate('sess-guest-1'),
      expect: () => [
        isA<SessionState>().having(
            (s) => s.status, 'status', SessionStatusKind.loading),
        isA<SessionState>()
            .having((s) => s.status, 'status', SessionStatusKind.loaded)
            .having((s) => s.session!.sessionId, 'sessionId', 'sess-guest-1'),
      ],
    );

    blocTest<SessionCubit, SessionState>(
      'loadPersisted restores an open session saved across a power flicker',
      build: () {
        SharedPreferences.setMockInitialValues({
          'sare.session_id': 'sess-guest-1',
          'sare.cart_id': 'CART-01',
          'sare.session_updated_ticks': '1767261600000',
        });
        final api = ApiClient(
          baseUrl: 'http://localhost:5000',
          httpClient: _onlineClient(),
        );
        return SessionCubit(
          SessionRepository(api, LocalSmartCartEngine()),
          SessionStore(),
        );
      },
      act: (cubit) => cubit.loadPersisted(),
      expect: () => [
        isA<SessionState>()
            .having((s) => s.status, 'status', SessionStatusKind.loaded)
            .having((s) => s.session!.sessionId, 'sessionId', 'sess-guest-1')
            .having((s) => s.session!.isOpen, 'isOpen', true),
      ],
    );

    blocTest<SessionCubit, SessionState>(
      'loadPersisted clears the store when the saved session is gone',
      build: () {
        SharedPreferences.setMockInitialValues({
          'sare.session_id': 'sess-missing',
          'sare.cart_id': 'CART-01',
        });
        final api = ApiClient(
          baseUrl: 'http://localhost:5000',
          httpClient: MockClient((request) async {
            return http.Response(jsonEncode({'message': 'not found'}), 404);
          }),
        );
        return SessionCubit(
          SessionRepository(api, LocalSmartCartEngine()),
          SessionStore(),
        );
      },
      act: (cubit) => cubit.loadPersisted(),
      expect: () => isEmpty,
    );

    group('closeSession', () {
      late SessionCubit cubit;

      setUp(() async {
        final api = ApiClient(
          baseUrl: 'http://localhost:5000',
          httpClient: _offlineClient(),
        );
        cubit = SessionCubit(
          SessionRepository(api, LocalSmartCartEngine()),
          SessionStore(),
        );
        await cubit.startGuestSession('CART-01');
      });

      blocTest<SessionCubit, SessionState>(
        'resets to the idle state',
        build: () => cubit,
        act: (c) => c.closeSession(reason: 'abandoned'),
        expect: () => [
          isA<SessionState>()
              .having((s) => s.status, 'status', SessionStatusKind.loading),
          isA<SessionState>()
              .having((s) => s.session, 'session', isNull)
              .having((s) => s.status, 'status', SessionStatusKind.idle),
        ],
      );
    });
  });
}
