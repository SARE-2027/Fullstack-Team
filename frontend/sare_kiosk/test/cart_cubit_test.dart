import 'package:bloc_test/bloc_test.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'package:sare_kiosk/core/api/api_client.dart';
import 'package:sare_kiosk/core/storage/session_store.dart';
import 'package:sare_kiosk/features/cart/data/repositories/cart_repository.dart';
import 'package:sare_kiosk/features/cart/data/repositories/local_smart_cart_engine.dart';
import 'package:sare_kiosk/features/cart/presentation/cubit/cart_cubit.dart';
import 'package:sare_kiosk/features/cart/presentation/cubit/cart_state.dart';
import 'package:sare_kiosk/features/session/data/repositories/session_repository.dart';
import 'package:sare_kiosk/features/session/presentation/cubit/session_cubit.dart';

/// Fully offline test stack: every HTTP call fails, so the kiosk runs on the
/// local edge decision engine — the same path it takes when the store Wi-Fi
/// drops mid-shopping.
class _OfflineStack {
  _OfflineStack()
      : engine = LocalSmartCartEngine(),
        api = ApiClient(
          baseUrl: 'http://localhost:5000',
          httpClient: MockClient((request) async {
            throw http.ClientException('backend unreachable');
          }),
        );

  final LocalSmartCartEngine engine;
  final ApiClient api;
  late final SessionCubit sessionCubit = SessionCubit(
    SessionRepository(api, engine),
    SessionStore(),
  );
  late final CartCubit cartCubit = CartCubit(
    CartRepository(api, engine),
    sessionCubit,
  );

  Future<CartCubit> startGuest() async {
    await sessionCubit.startGuestSession('CART-01');
    return cartCubit;
  }

  String get _activeSessionId => sessionCubit.state.session!.sessionId;

  String get firstItemId =>
      engine.getSession(_activeSessionId)!.items.first.id;
}

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  late _OfflineStack stack;

  setUp(() async {
    SharedPreferences.setMockInitialValues({});
    stack = _OfflineStack();
    await stack.startGuest();
  });

  group('CartCubit — addItemByScan', () {
    blocTest<CartCubit, CartState>(
      'verifies then adds an item when the measured weight matches ±15g',
      build: () => stack.cartCubit,
      act: (cubit) => cubit.addItemByScan(
        '6223000123456', // Sunshine Tuna 180g
        measuredWeightG: 180,
      ),
      expect: () => [
        isA<CartState>()
            .having((s) => s.isAdding, 'isAdding', true)
            .having(
                (s) => s.bannerKind, 'bannerKind', WeightBannerKind.verifying),
        isA<CartState>()
            .having((s) => s.isAdding, 'isAdding', false)
            .having(
                (s) => s.bannerKind, 'bannerKind', WeightBannerKind.success)
            .having((s) => s.lastExpectedG, 'lastExpectedG', 180)
            .having((s) => s.lastMeasuredG, 'lastMeasuredG', 180),
      ],
    );

    blocTest<CartCubit, CartState>(
      'flags a weight mismatch and refuses to add the item',
      build: () => stack.cartCubit,
      act: (cubit) => cubit.addItemByScan(
        '6223000123456', // Sunshine Tuna 180g — weighed 95g heavier
        measuredWeightG: 275,
      ),
      expect: () => [
        isA<CartState>()
            .having((s) => s.isAdding, 'isAdding', true)
            .having(
                (s) => s.bannerKind, 'bannerKind', WeightBannerKind.verifying),
        isA<CartState>()
            .having((s) => s.isAdding, 'isAdding', false)
            .having(
                (s) => s.bannerKind, 'bannerKind', WeightBannerKind.mismatch)
            .having((s) => s.lastExpectedG, 'lastExpectedG', 180)
            .having((s) => s.lastMeasuredG, 'lastMeasuredG', 275)
            .having((s) => s.bannerMessage, 'bannerMessage',
                contains('Weight mismatch')),
      ],
    );

    blocTest<CartCubit, CartState>(
      'ignores an empty barcode without any state change',
      build: () => stack.cartCubit,
      act: (cubit) => cubit.addItemByScan('   '),
      expect: () => isEmpty,
    );
  });

  group('CartCubit — remove & undo', () {
    blocTest<CartCubit, CartState>(
      'removeItem keeps the removed item for undo, and undo restores it',
      build: () => stack.cartCubit,
      act: (cubit) async {
        await cubit.addItemByScan(
          '6223000123456',
          measuredWeightG: 180,
        );
        final itemId = stack.firstItemId;
        await cubit.removeItem(itemId);
        await cubit.undoRemove();
      },
      expect: () => [
        // add: verifying
        isA<CartState>()
            .having((s) => s.bannerKind, 'bannerKind', WeightBannerKind.verifying),
        // add: success
        isA<CartState>()
            .having((s) => s.bannerKind, 'bannerKind', WeightBannerKind.success),
        // remove: banner carries the undo payload
        isA<CartState>()
            .having(
                (s) => s.bannerKind, 'bannerKind', WeightBannerKind.removed)
            .having((s) => s.lastRemovedItem, 'lastRemovedItem', isNotNull)
            .having((s) => s.lastRemovedItem!.barcode, 'removedBarcode',
                '6223000123456'),
        // undo: item is back in the basket
        isA<CartState>().having(
            (s) => s.bannerKind, 'bannerKind', WeightBannerKind.restored),
      ],
    );
  });

  group('CartCubit — checkout', () {
    blocTest<CartCubit, CartState>(
      'checkout closes the session and produces a receipt',
      build: () => stack.cartCubit,
      act: (cubit) async {
        await cubit.addItemByScan(
          '6223000123456',
          measuredWeightG: 180,
        );
        await cubit.checkout(paymentMethod: 'card');
      },
      expect: () => [
        isA<CartState>()
            .having((s) => s.bannerKind, 'bannerKind', WeightBannerKind.verifying),
        isA<CartState>()
            .having((s) => s.bannerKind, 'bannerKind', WeightBannerKind.success),
        isA<CartState>().having((s) => s.isAdding, 'isAdding', true),
        isA<CartState>()
            .having((s) => s.isAdding, 'isAdding', false)
            .having(
                (s) => s.bannerKind, 'bannerKind', WeightBannerKind.none),
      ],
    );
  });
}
