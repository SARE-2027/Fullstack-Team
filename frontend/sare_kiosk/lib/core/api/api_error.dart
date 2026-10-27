/// Structured error returned by the .NET API via [GlobalExceptionHandler].
class ApiError {
  const ApiError({
    required this.message,
    this.code,
    this.details,
    this.errors,
  });

  final String message;
  final String? code;
  final String? details;
  final List<String>? errors;

  factory ApiError.fromJson(Map<String, dynamic>? json, {required String fallbackMessage}) {
    if (json == null) {
      return ApiError(message: fallbackMessage);
    }

    // .NET ValidationProblem shape and custom AppException shape
    final errors = <String>[];
    if (json['errors'] is Map) {
      for (final entry in (json['errors'] as Map).values) {
        if (entry is List) {
          for (final e in entry) {
            errors.add(e.toString());
          }
        } else {
          errors.add(entry.toString());
        }
      }
    } else if (json['errors'] is List) {
      for (final e in (json['errors'] as List)) {
        errors.add(e.toString());
      }
    }

    final message = (json['message'] ?? json['title'] ?? json['error'] ?? json['detail'] ?? fallbackMessage).toString();
    final code = (json['code'] ?? json['errorCode'] ?? json['error_code'])?.toString();
    final details = (json['details'] ?? json['detail'])?.toString();

    return ApiError(
      message: message,
      code: code,
      details: details,
      errors: errors.isEmpty ? null : errors,
    );
  }

  String get displayMessage {
    if (errors != null && errors!.isNotEmpty) {
      return errors!.join('\n');
    }
    return message;
  }

  bool get isNotFound => code == 'NOT_FOUND' || message.toLowerCase().contains('not found');
  bool get isConflict => code == 'CONFLICT' || code == 'EVENT_ID_REUSED';
  bool get isValidation => code == 'VALIDATION_FAILED' || code == 'VALIDATION_ERROR';
  bool get isUnauthorized => code == 'UNAUTHORIZED' || code == 'INVALID_CART_TOKEN';
}

class ApiException implements Exception {
  const ApiException({
    required this.statusCode,
    required this.error,
    this.rawBody,
  });

  final int statusCode;
  final ApiError error;
  final String? rawBody;

  @override
  String toString() => 'ApiException($statusCode): ${error.displayMessage}';
}
