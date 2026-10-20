import 'package:flutter_bloc/flutter_bloc.dart';
import '../../../../core/storage/session_store.dart';
import '../../data/models/session_models.dart';
import '../../data/repositories/session_repository.dart';
import 'session_state.dart';

class SessionCubit extends Cubit<SessionState> {
  SessionCubit(this._repo, this._store) : super(const SessionState());

  final SessionRepository _repo;
  final SessionStore _store;

  bool _busy = false;

  Future<bool> startGuestSession(String cartId, {bool allowOfflineFallback = true}) async {
    return _startSession(cartId: cartId, allowOfflineFallback: allowOfflineFallback);
  }

  Future<bool> startMemberSession(String cartId, String nfcUid, {bool allowOfflineFallback = true}) async {
    return _startSession(cartId: cartId, nfcUid: nfcUid, allowOfflineFallback: allowOfflineFallback);
  }

  Future<bool> _startSession({
    required String cartId,
    String? nfcUid,
    bool allowOfflineFallback = true,
  }) async {
    if (_busy) return false;
    if (cartId.isEmpty) {
      emit(state.copyWith(status: SessionStatusKind.failure, error: 'Invalid cart identifier'));
      return false;
    }
    _busy = true;
    emit(state.copyWith(status: SessionStatusKind.loading, clearError: true));
    final result = await _repo.startSession(
      cartId: cartId,
      nfcUid: nfcUid,
      allowOfflineFallback: allowOfflineFallback,
    );
    final ok = result.fold(
      (failure) {
        if (!isClosed) emit(state.copyWith(status: SessionStatusKind.failure, error: failure.message));
        return false;
      },
      (summary) {
        if (isClosed) return true;
        emit(SessionState(session: summary, status: SessionStatusKind.loaded));
        _store.saveSession(
          sessionId: summary.sessionId,
          cartId: summary.cartId,
          updatedAtTicks: summary.updatedAt.millisecondsSinceEpoch.toString(),
        );
        return true;
      },
    );
    _busy = false;
    return ok;
  }

  Future<void> refresh() async {
    if (_busy) return;
    final sid = state.session?.sessionId;
    if (sid == null || sid.isEmpty) return;
    _busy = true;
    emit(state.copyWith(status: SessionStatusKind.loading, clearError: true));
    final result = await _repo.getSession(sid);
    if (isClosed) {
      _busy = false;
      return;
    }
    result.fold(
      (f) => emit(state.copyWith(status: SessionStatusKind.failure, error: f.message)),
      (s) {
        emit(state.copyWith(session: s, status: SessionStatusKind.loaded, clearError: true));
        _store.updateTicks(s.updatedAt.millisecondsSinceEpoch.toString());
      },
    );
    _busy = false;
  }

  Future<void> loadPersisted() async {
    final loaded = await _store.loadSession();
    if (loaded == null) return;
    final result = await _repo.getSession(loaded.sessionId);
    if (isClosed) return;
    result.fold(
      (_) => _store.clearSession(),
      (s) {
        if (s.status == SessionStatusX.open) {
          emit(SessionState(session: s, status: SessionStatusKind.loaded));
        } else {
          _store.clearSession();
        }
      },
    );
  }

  Future<bool> closeSession({String reason = 'abandoned'}) async {
    if (_busy) return false;
    final sid = state.session?.sessionId;
    if (sid == null || sid.isEmpty) return false;
    _busy = true;
    emit(state.copyWith(status: SessionStatusKind.loading, clearError: true));
    final result = await _repo.closeSession(sid, reason: reason);
    final ok = result.fold(
      (f) {
        if (!isClosed) emit(state.copyWith(status: SessionStatusKind.failure, error: f.message));
        return false;
      },
      (s) {
        _store.clearSession();
        if (!isClosed) emit(const SessionState());
        return true;
      },
    );
    _busy = false;
    return ok;
  }

  void setSession(SessionSummary s) {
    if (isClosed) return;
    emit(state.copyWith(session: s, status: SessionStatusKind.loaded, clearError: true));
    _store.updateTicks(s.updatedAt.millisecondsSinceEpoch.toString());
  }

  void setCheckout(CheckoutResult c) {
    if (isClosed) return;
    emit(state.copyWith(checkout: c));
  }

  void clearError() {
    if (isClosed) return;
    emit(state.copyWith(
      clearError: true,
      status: state.session != null ? SessionStatusKind.loaded : SessionStatusKind.idle,
    ));
  }

  Future<void> reset() async {
    await _store.clearSession();
    if (!isClosed) emit(const SessionState());
  }

  Future<void> hydrate(String sessionId) async {
    if (_busy) return;
    if (sessionId.isEmpty) return;
    if (state.session?.sessionId == sessionId) return;
    _busy = true;
    emit(state.copyWith(status: SessionStatusKind.loading, clearError: true));
    final result = await _repo.getSession(sessionId);
    if (isClosed) {
      _busy = false;
      return;
    }
    result.fold(
      (f) => emit(state.copyWith(status: SessionStatusKind.failure, error: f.message)),
      (s) {
        emit(SessionState(session: s, status: SessionStatusKind.loaded));
        _store.saveSession(
          sessionId: s.sessionId,
          cartId: s.cartId,
          updatedAtTicks: s.updatedAt.millisecondsSinceEpoch.toString(),
        );
      },
    );
    _busy = false;
  }

  @override
  Future<void> close() {
    _busy = false;
    return super.close();
  }
}
