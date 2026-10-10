class ServerException implements Exception {
  const ServerException([this.message = 'Server encountered an unexpected error']);
  final String message;

  @override
  String toString() => 'ServerException: $message';
}

class NetworkException implements Exception {
  const NetworkException([this.message = 'Network connection failed']);
  final String message;

  @override
  String toString() => 'NetworkException: $message';
}

class HardwareScaleException implements Exception {
  const HardwareScaleException([this.message = 'Smart cart scale sensor communication error']);
  final String message;

  @override
  String toString() => 'HardwareScaleException: $message';
}

class CacheException implements Exception {
  const CacheException([this.message = 'Local cache error']);
  final String message;

  @override
  String toString() => 'CacheException: $message';
}
