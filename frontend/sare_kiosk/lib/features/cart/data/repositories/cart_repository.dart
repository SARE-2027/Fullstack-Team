import 'dart:convert';
import 'dart:math';

import '../../../../core/api/api_client.dart';
import '../../../../core/api/api_error.dart';
import '../../../../core/error/either.dart';
import '../../../../core/error/exceptions.dart';
import '../../../../core/error/failures.dart';
import '../../../session/data/models/session_models.dart';
import 'local_smart_cart_engine.dart';

class CartRepository {
  CartRepository(this._api, [LocalSmartCartEngine? localEngine])
      : _localEngine = localEngine ?? LocalSmartCartEngine();

  final ApiClient _api;
  final LocalSmartCartEngine _localEngine;

  LocalSmartCartEngine get localEngine => _localEngine;

  Future<Result<AddItemResult>> addItem({
    required String sessionId,
    required String barcode,
    int? measuredWeightG,
    String source = 'Scanner',
    String? eventId,
  }) async {
    final normalizedBarcode = barcode.trim();
    final catalogProduct = LocalSmartCartEngine.findByBarcode(normalizedBarcode);
    final effectiveWeightG = measuredWeightG ?? catalogProduct?.weightG ?? 0;

    final cachedSession = _localEngine.getSession(sessionId);
    if (cachedSession != null && cachedSession.isOfflineMode) {
      final localResult = _localEngine.processItemDetection(
        sessionId: sessionId,
        barcode: normalizedBarcode,
        measuredWeightG: effectiveWeightG,
        source: source,
      );
      if (!localResult.success && !localResult.isWeightMismatch) {
        return Left(ServerFailure(localResult.message));
      }
      return Right(localResult);
    }

    final id = eventId ?? _newGuid();
    try {
      final json = await _api.post(
        '/api/cart/$sessionId/items',
        body: {
          'barcode': normalizedBarcode,
          'measuredWeightG': effectiveWeightG,
          'source': source,
          'eventId': id,
        },
      );

      if (json == null) {
        return const Left(ServerFailure('Empty response from server'));
      }
      final result = AddItemResult.fromJson(_normalizeAddItemJson(json));
      if (result.session != null) {
        _localEngine.cacheSession(result.session!);
      }
      return Right(result);
    } on ApiException catch (e) {
      if (e.statusCode == 400 || e.statusCode == 409 || e.statusCode == 404) {
        try {
          if (e.rawBody != null && e.rawBody!.isNotEmpty) {
            final decoded = jsonDecode(e.rawBody!);
            if (decoded is Map<String, dynamic>) {
              if (decoded.containsKey('isWeightMismatch') || decoded.containsKey('IsWeightMismatch')) {
                return Right(AddItemResult.fromJson(_normalizeAddItemJson(decoded)));
              }
            }
          }
        } catch (_) {}

        final bodyLower = (e.rawBody ?? '').toLowerCase();
        final msgLower = e.error.displayMessage.toLowerCase();
        final isWeight = msgLower.contains('weight') || bodyLower.contains('weight');
        if (isWeight) {
          return Right(AddItemResult(
            success: false,
            message: e.error.displayMessage,
            isWeightMismatch: true,
            expectedWeightG: catalogProduct?.weightG,
            measuredWeightG: effectiveWeightG,
            itemNameAr: catalogProduct?.nameAr,
            itemNameEn: catalogProduct?.nameEn,
          ));
        }
        return Left(ServerFailure(e.error.displayMessage));
      }
      return Left(ServerFailure(e.error.displayMessage));
    } on Exception catch (e) {
      if (cachedSession != null) {
        final localResult = _localEngine.processItemDetection(
          sessionId: sessionId,
          barcode: normalizedBarcode,
          measuredWeightG: effectiveWeightG,
          source: source,
        );
        if (!localResult.success && !localResult.isWeightMismatch) {
          return Left(ServerFailure(localResult.message));
        }
        return Right(localResult);
      }
      if (e is NetworkException) return Left(NetworkFailure(e.message));
      return Left(UnknownFailure(e.toString()));
    }
  }

  Future<Result<SessionSummary>> removeItem({
    required String sessionId,
    required String itemId,
  }) async {
    final updated = _localEngine.removeItem(sessionId: sessionId, itemId: itemId);
    if (updated != null) {
      return Right(updated);
    }
    return const Left(ServerFailure('Could not remove item from session.'));
  }

  /// Re-appends a previously removed item to the current session (Undo).
  /// Operates on the live session so items scanned after the removal are kept.
  Future<Result<SessionSummary>> restoreItem({
    required String sessionId,
    required SessionItem item,
  }) async {
    final current = _localEngine.getSession(sessionId);
    if (current == null || !current.isOpen) {
      return const Left(ServerFailure('Active session not found.'));
    }
    final updatedItems = [...current.items, item];
    final updatedTotal =
        updatedItems.fold<int>(0, (sum, i) => sum + i.unitPriceMinor);
    final updated = current.copyWith(
      items: updatedItems,
      itemsCount: updatedItems.length,
      totalMinor: updatedTotal,
      updatedAt: DateTime.now().toUtc(),
    );
    _localEngine.cacheSession(updated);
    return Right(updated);
  }

  Future<Result<CheckoutResult>> checkout(
    String sessionId, {
    String paymentMethod = 'card',
    int itemsCount = 0,
  }) async {
    final cached = _localEngine.getSession(sessionId);
    if (cached != null && cached.isOfflineMode) {
      final res = _localEngine.checkout(sessionId, paymentMethod: paymentMethod);
      if (res != null) return Right(res);
    }
    try {
      final json = await _api.post('/api/cart/$sessionId/checkout');
      if (json == null) throw const ServerException('Empty checkout response');
      return Right(CheckoutResult.fromJson(
        json,
        itemsCount: cached?.itemsCount ?? itemsCount,
        paymentMethod: paymentMethod,
      ));
    } on ApiException catch (e) {
      final fallback = _localEngine.checkout(sessionId, paymentMethod: paymentMethod);
      if (fallback != null) return Right(fallback);
      return Left(ServerFailure(e.error.displayMessage));
    } on Exception catch (e) {
      final fallback = _localEngine.checkout(sessionId, paymentMethod: paymentMethod);
      if (fallback != null) return Right(fallback);
      if (e is NetworkException) return Left(NetworkFailure(e.message));
      return Left(UnknownFailure(e.toString()));
    }
  }

  Map<String, dynamic> _normalizeAddItemJson(Map<String, dynamic> json) {
    if (json.containsKey('success') || json.containsKey('Success')) return json;
    if (json.containsKey('sessionId') || json.containsKey('SessionId')) {
      return {
        'success': true,
        'message': 'Item added',
        'isWeightMismatch': false,
        'session': json,
      };
    }
    return json;
  }

  String _newGuid() {
    final r = Random.secure();
    String h(int n) => n.toRadixString(16).padLeft(2, '0');
    final bytes = List<int>.generate(16, (_) => r.nextInt(256));
    bytes[6] = (bytes[6] & 0x0F) | 0x40;
    bytes[8] = (bytes[8] & 0x3F) | 0x80;
    return '${bytes.sublist(0, 4).map(h).join()}'
        '-${bytes.sublist(4, 6).map(h).join()}'
        '-${bytes.sublist(6, 8).map(h).join()}'
        '-${bytes.sublist(8, 10).map(h).join()}'
        '-${bytes.sublist(10, 16).map(h).join()}';
  }
}
