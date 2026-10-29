import 'dart:async';
import 'dart:convert';

import 'package:http/http.dart' as http;

import '../error/exceptions.dart';
import 'api_error.dart';

/// Lightweight HTTP client for the SARE kiosk.
///
/// - JSON in/out only.
/// - Maps transport failures to [NetworkException] and HTTP errors to [ApiException].
/// - Thread-safe: a single client instance is reused across the app.
class ApiClient {
  ApiClient({
    required String baseUrl,
    http.Client? httpClient,
    Duration timeout = const Duration(milliseconds: 1200),
  })  : _baseUrl = baseUrl.endsWith('/')
            ? baseUrl.substring(0, baseUrl.length - 1)
            : baseUrl,
        _client = httpClient ?? http.Client(),
        // ignore: prefer_initializing_formals
        _timeout = timeout;

  final String _baseUrl;
  final http.Client _client;
  final Duration _timeout;

  String get baseUrl => _baseUrl;

  Uri _uri(String path, [Map<String, String>? query]) {
    final normalized = path.startsWith('/') ? path : '/$path';
    final uri = Uri.parse('$_baseUrl$normalized');
    if (query == null || query.isEmpty) return uri;
    return uri.replace(queryParameters: {...uri.queryParameters, ...query});
  }

  Map<String, String> _headers({String? cartToken}) {
    return {
      'Content-Type': 'application/json',
      'Accept': 'application/json',
      if (cartToken != null && cartToken.isNotEmpty) 'X-Cart-Token': cartToken,
    };
  }

  Future<Map<String, dynamic>?> get(
    String path, {
    Map<String, String>? query,
    String? cartToken,
  }) async {
    return _request(
      () => _client.get(_uri(path, query), headers: _headers(cartToken: cartToken)),
    );
  }

  Future<Map<String, dynamic>?> post(
    String path, {
    Object? body,
    Map<String, String>? query,
    String? cartToken,
  }) async {
    return _request(
      () => _client.post(
        _uri(path, query),
        headers: _headers(cartToken: cartToken),
        body: body == null ? null : jsonEncode(body),
      ),
    );
  }

  Future<Map<String, dynamic>?> requestRaw({
    required String method,
    required String path,
    Object? body,
    Map<String, String>? query,
    String? cartToken,
  }) async {
    final uri = _uri(path, query);
    final headers = _headers(cartToken: cartToken);
    Future<http.Response> call() {
      switch (method.toUpperCase()) {
        case 'GET':
          return _client.get(uri, headers: headers);
        case 'POST':
          return _client.post(uri, headers: headers, body: body == null ? null : jsonEncode(body));
        case 'PATCH':
          return _client.patch(uri, headers: headers, body: body == null ? null : jsonEncode(body));
        case 'DELETE':
          return _client.delete(uri, headers: headers, body: body == null ? null : jsonEncode(body));
        default:
          throw ArgumentError('Unsupported method $method');
      }
    }

    return _request(call);
  }

  Future<Map<String, dynamic>?> _request(
    Future<http.Response> Function() call,
  ) async {
    http.Response response;
    try {
      response = await call().timeout(_timeout);
    } on TimeoutException {
      throw const NetworkException('Request timed out. Please check the connection and try again.');
    } on http.ClientException catch (e) {
      throw NetworkException(e.message);
    } catch (e) {
      if (e is NetworkException || e is ApiException) rethrow;
      throw NetworkException(e.toString());
    }

    if (response.statusCode >= 200 && response.statusCode < 300) {
      if (response.body.isEmpty) return null;
      final decoded = jsonDecode(response.body);
      if (decoded is Map<String, dynamic>) return decoded;
      return {'data': decoded};
    }

    final parsed = _tryParseBody(response.body);
    throw ApiException(
      statusCode: response.statusCode,
      error: ApiError.fromJson(parsed, fallbackMessage: _fallbackMessage(response.statusCode)),
      rawBody: response.body,
    );
  }

  Map<String, dynamic>? _tryParseBody(String body) {
    if (body.isEmpty) return null;
    try {
      final decoded = jsonDecode(body);
      if (decoded is Map<String, dynamic>) return decoded;
      return {'message': decoded.toString()};
    } catch (_) {
      return {'message': body};
    }
  }

  String _fallbackMessage(int status) {
    switch (status) {
      case 400:
        return 'Invalid request.';
      case 401:
        return 'Authentication required.';
      case 403:
        return 'Access denied.';
      case 404:
        return 'Requested resource was not found.';
      case 409:
        return 'Conflict with current state. Please synchronize and retry.';
      case 422:
        return 'Validation failed.';
      case 500:
        return 'Server error. Please try again shortly.';
      default:
        return 'Unexpected error ($status).';
    }
  }

  void close() => _client.close();
}
