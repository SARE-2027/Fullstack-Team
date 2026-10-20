import 'package:equatable/equatable.dart';
import '../../data/models/session_models.dart';

enum SessionStatusKind { idle, loading, loaded, failure }

class SessionState extends Equatable {
  const SessionState({
    this.session,
    this.status = SessionStatusKind.idle,
    this.error,
    this.checkout,
  });

  final SessionSummary? session;
  final SessionStatusKind status;
  final String? error;
  final CheckoutResult? checkout;

  bool get isLoading => status == SessionStatusKind.loading;
  bool get isLoaded => status == SessionStatusKind.loaded && session != null;
  bool get isIdle => status == SessionStatusKind.idle;
  bool get isFailure => status == SessionStatusKind.failure;

  SessionState copyWith({
    SessionSummary? session,
    SessionStatusKind? status,
    String? error,
    CheckoutResult? checkout,
    bool clearError = false,
    bool clearCheckout = false,
    bool clearSession = false,
  }) {
    return SessionState(
      session: clearSession ? null : (session ?? this.session),
      status: status ?? this.status,
      error: clearError ? null : (error ?? this.error),
      checkout: clearCheckout ? null : (checkout ?? this.checkout),
    );
  }

  @override
  List<Object?> get props => [session, status, error, checkout];
}
