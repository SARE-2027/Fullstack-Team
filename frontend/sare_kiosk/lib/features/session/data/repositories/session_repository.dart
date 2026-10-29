import '../../../../core/api/api_client.dart';
import '../../../../core/api/api_error.dart';
import '../../../../core/error/either.dart';
import '../../../../core/error/exceptions.dart';
import '../../../../core/error/failures.dart';
import '../../../cart/data/repositories/local_smart_cart_engine.dart';
import '../models/session_models.dart';

class SessionRepository {
  SessionRepository(this._api, [LocalSmartCartEngine? localEngine])
      : _localEngine = localEngine ?? LocalSmartCartEngine();

  final ApiClient _api;
  final LocalSmartCartEngine _localEngine;

  LocalSmartCartEngine get localEngine => _localEngine;

  Future<Result<SessionSummary>> startSession({
    required String cartId,
    String? nfcUid,
    bool allowOfflineFallback = true,
  }) async {
    try {
      return await _tryStartRemoteSession(cartId: cartId, nfcUid: nfcUid);
    } on ApiException catch (e) {
      // 1. If cartId had hyphen vs underscore mismatch (e.g., CART-01 vs DbInitializer CART_01), try alternate
      final altCartId = _alternateCartId(cartId);
      if (e.statusCode == 404 && altCartId != cartId) {
        try {
          return await _tryStartRemoteSession(cartId: altCartId, nfcUid: nfcUid);
        } on ApiException catch (eAlt) {
          if (eAlt.statusCode == 409) {
            return getActiveByCart(altCartId);
          }
          if (allowOfflineFallback) {
            return Right(_localEngine.startSession(cartId: cartId, nfcUid: nfcUid));
          }
          return Left(_mapApiException(eAlt));
        } on Exception catch (_) {
          if (allowOfflineFallback) {
            return Right(_localEngine.startSession(cartId: cartId, nfcUid: nfcUid));
          }
        }
      }

      // 2. If cart already has an active session on backend (409 Conflict),
      // close stale session if it has leftover items or mismatched member, then start fresh; otherwise resume.
      if (e.statusCode == 409) {
        final active = await getActiveByCart(cartId);
        if (active.isRight) {
          final existing = active.rightOrNull!;
          final memberMismatch = (nfcUid != null && nfcUid.trim().isNotEmpty) != existing.isMember;
          if (existing.items.isNotEmpty || memberMismatch) {
            await closeSession(existing.sessionId, reason: 'abandoned');
            try {
              return await _tryStartRemoteSession(cartId: cartId, nfcUid: nfcUid);
            } on Exception catch (_) {}
          }
          return Right(_enrichMemberIfNeeded(existing, nfcUid));
        }
        if (altCartId != cartId) {
          final altActive = await getActiveByCart(altCartId);
          if (altActive.isRight) {
            return Right(_enrichMemberIfNeeded(altActive.rightOrNull!, nfcUid));
          }
        }
      }

      // 3. Fallback to legacy endpoint or offline engine when appropriate
      if (e.statusCode == 404 && (nfcUid == null || nfcUid.trim().isEmpty)) {
        try {
          final legacy = await _api.post('/api/cart/session/start', body: {'cartId': cartId, 'userId': null});
          if (legacy != null) {
            final summary = _enrichMemberIfNeeded(SessionSummary.fromJson(legacy), nfcUid);
            _localEngine.cacheSession(summary);
            return Right(summary);
          }
        } on Exception catch (_) {}
      }

      if (allowOfflineFallback) {
        return Right(_localEngine.startSession(cartId: cartId, nfcUid: nfcUid));
      }
      return Left(_mapApiException(e));
    } on Exception catch (e) {
      if (allowOfflineFallback) {
        return Right(_localEngine.startSession(cartId: cartId, nfcUid: nfcUid));
      }
      if (e is NetworkException) return Left(NetworkFailure(e.message));
      return Left(UnknownFailure(e.toString()));
    }
  }

  SessionSummary _enrichMemberIfNeeded(SessionSummary summary, String? nfcUid) {
    if (nfcUid == null || nfcUid.trim().isEmpty) return summary;
    final member = LocalSmartCartEngine.resolveMember(nfcUid);
    final enriched = summary.copyWith(
      userId: summary.userId ?? member.userId,
      userName: (summary.userName == null || summary.userName!.isEmpty)
          ? member.nameEn
          : summary.userName,
      nfcUid: summary.nfcUid ?? member.nfcUid,
    );
    _localEngine.cacheSession(enriched);
    return enriched;
  }

  Future<Result<SessionSummary>> _tryStartRemoteSession({
    required String cartId,
    String? nfcUid,
  }) async {
    final body = {
      'cartId': cartId,
      if (nfcUid != null && nfcUid.trim().isNotEmpty) 'nfcUid': nfcUid.trim(),
    };
    final json = await _api.post('/api/sessions', body: body);
    if (json == null) throw const ServerException('Empty response from server');
    final summary = _enrichMemberIfNeeded(SessionSummary.fromJson(json), nfcUid);
    _localEngine.cacheSession(summary);
    return Right(summary);
  }

  String _alternateCartId(String cartId) {
    if (cartId.contains('-')) return cartId.replaceAll('-', '_');
    if (cartId.contains('_')) return cartId.replaceAll('_', '-');
    return cartId;
  }

  Future<Result<SessionSummary>> getSession(String sessionId) async {
    final cached = _localEngine.getSession(sessionId);
    if (cached != null && cached.isOfflineMode) {
      return Right(cached);
    }
    try {
      final json = await _api.get('/api/sessions/$sessionId');
      if (json == null) throw const ServerException('Session not found');
      final summary = SessionSummary.fromJson(json);
      _localEngine.cacheSession(summary);
      return Right(summary);
    } on ApiException catch (e) {
      if (cached != null) return Right(cached);
      return Left(_mapApiException(e));
    } on Exception catch (e) {
      if (cached != null) return Right(cached);
      if (e is NetworkException) return Left(NetworkFailure(e.message));
      return Left(UnknownFailure(e.toString()));
    }
  }

  Future<Result<SessionSummary>> getActiveByCart(String cartId) async {
    try {
      final json = await _api.get('/api/carts/$cartId/session');
      if (json == null) throw const ServerException('No active session');
      final summary = SessionSummary.fromJson(json);
      _localEngine.cacheSession(summary);
      return Right(summary);
    } on ApiException catch (e) {
      return Left(_mapApiException(e));
    } on Exception catch (e) {
      if (e is NetworkException) return Left(NetworkFailure(e.message));
      return Left(UnknownFailure(e.toString()));
    }
  }

  Future<Result<SessionSummary>> closeSession(String sessionId, {String reason = 'abandoned'}) async {
    final cached = _localEngine.getSession(sessionId);
    if (cached != null && cached.isOfflineMode) {
      final closed = _localEngine.closeSession(sessionId, reason: reason);
      if (closed != null) return Right(closed);
    }
    try {
      final json = await _api.post('/api/sessions/$sessionId/close', body: {});
      if (json == null) throw const ServerException('Failed to close session');
      final summary = SessionSummary.fromJson(json);
      _localEngine.cacheSession(summary);
      return Right(summary);
    } on ApiException catch (e) {
      final localClosed = _localEngine.closeSession(sessionId, reason: reason);
      if (localClosed != null) return Right(localClosed);
      return Left(_mapApiException(e));
    } on Exception catch (e) {
      final localClosed = _localEngine.closeSession(sessionId, reason: reason);
      if (localClosed != null) return Right(localClosed);
      if (e is NetworkException) return Left(NetworkFailure(e.message));
      return Left(UnknownFailure(e.toString()));
    }
  }

  Failure _mapApiException(ApiException e) {
    return ServerFailure(e.error.displayMessage);
  }
}
