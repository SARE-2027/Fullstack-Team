import 'exceptions.dart';

abstract class Failure {
  const Failure(this.message);
  final String message;

  factory Failure.fromException(Object e) {
    if (e is ServerException) return ServerFailure(e.message);
    if (e is NetworkException) return NetworkFailure(e.message);
    if (e is HardwareScaleException) return HardwareScaleFailure(e.message);
    if (e is CacheException) return CacheFailure(e.message);
    return UnknownFailure(e.toString());
  }

  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      other is Failure &&
          runtimeType == other.runtimeType &&
          message == other.message;

  @override
  int get hashCode => message.hashCode;

  @override
  String toString() => '$runtimeType: $message';
}

class ServerFailure extends Failure {
  const ServerFailure([super.message = 'Server failure occurred']);
}

class NetworkFailure extends Failure {
  const NetworkFailure([super.message = 'Network connection failure']);
}

class HardwareScaleFailure extends Failure {
  const HardwareScaleFailure([super.message = 'Hardware scale failure']);
}

class CacheFailure extends Failure {
  const CacheFailure([super.message = 'Cache failure']);
}

class UnknownFailure extends Failure {
  const UnknownFailure([super.message = 'An unknown failure occurred']);
}
