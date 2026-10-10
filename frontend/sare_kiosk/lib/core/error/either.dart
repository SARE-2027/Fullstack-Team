import 'failures.dart';

typedef Result<T> = Either<Failure, T>;

/// A lightweight, functional Either pattern for SARE Kiosk,
/// providing typed Left (Failure) and Right (Success) outcomes without heavy external dependencies.
sealed class Either<L, R> {
  const Either();

  T fold<T>(T Function(L left) onLeft, T Function(R right) onRight);

  Either<L, T> map<T>(T Function(R right) fn) {
    return fold((left) => Left(left), (right) => Right(fn(right)));
  }

  R? getOrNull() => fold((_) => null, (r) => r);

  L? failureOrNull() => fold((l) => l, (_) => null);

  bool get isLeft => this is Left<L, R>;
  bool get isRight => this is Right<L, R>;
}

class Left<L, R> extends Either<L, R> {
  const Left(this.value);
  final L value;

  @override
  T fold<T>(T Function(L left) onLeft, T Function(R right) onRight) =>
      onLeft(value);
}

class Right<L, R> extends Either<L, R> {
  const Right(this.value);
  final R value;

  @override
  T fold<T>(T Function(L left) onLeft, T Function(R right) onRight) =>
      onRight(value);
}
