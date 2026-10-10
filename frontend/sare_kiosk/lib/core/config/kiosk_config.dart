import 'package:flutter/foundation.dart';

/// Immutable configuration data model for the embedded Kiosk device.
///
/// Designed to work seamlessly with:
/// - Single-cart development and evaluation prototypes (zero-setup defaults).
/// - Multi-cart production fleets (configured via CLI arguments, environment
///   variables, or external JSON files on Raspberry Pi / Windows).
@immutable
class KioskConfig {
  const KioskConfig({
    this.cartId = defaultCartId,
    this.cartNumber = defaultCartNumber,
    this.apiBaseUrl = defaultApiBaseUrl,
    this.mqttBroker = defaultMqttBroker,
    this.mqttPort = defaultMqttPort,
    this.isScaleReady = defaultIsScaleReady,
    this.locationName = defaultLocationName,
  });

  /// Standard zero-setup defaults for local development & single-cart demo
  static const String defaultCartId = 'CART-01';
  static const String defaultCartNumber = '01';
  static const String defaultApiBaseUrl = 'http://localhost:5000';
  static const String defaultMqttBroker = 'localhost';
  static const int defaultMqttPort = 1883;
  static const bool defaultIsScaleReady = true;
  static const String defaultLocationName = 'Main Store';

  /// Unique logical identifier of this smart cart (e.g., 'CART-01', 'C12').
  final String cartId;

  /// Visible physical cart label displayed on screen & staff tablet (e.g., '01', '02').
  final String cartNumber;

  /// HTTP endpoint for backend REST API and SignalR Hub.
  final String apiBaseUrl;

  /// Hostname or IP address for the local MQTT broker.
  final String mqttBroker;

  /// Port for MQTT communication.
  final int mqttPort;

  /// Flag indicating whether the electronic scale hardware (HX711) is calibrated and ready.
  final bool isScaleReady;

  /// Name of the store section or store location where this cart operates.
  final String locationName;

  KioskConfig copyWith({
    String? cartId,
    String? cartNumber,
    String? apiBaseUrl,
    String? mqttBroker,
    int? mqttPort,
    bool? isScaleReady,
    String? locationName,
  }) {
    return KioskConfig(
      cartId: cartId ?? this.cartId,
      cartNumber: cartNumber ?? this.cartNumber,
      apiBaseUrl: apiBaseUrl ?? this.apiBaseUrl,
      mqttBroker: mqttBroker ?? this.mqttBroker,
      mqttPort: mqttPort ?? this.mqttPort,
      isScaleReady: isScaleReady ?? this.isScaleReady,
      locationName: locationName ?? this.locationName,
    );
  }

  factory KioskConfig.fromJson(Map<String, dynamic> json) {
    return KioskConfig(
      cartId: json['cart_id']?.toString() ?? defaultCartId,
      cartNumber: json['cart_number']?.toString() ?? defaultCartNumber,
      apiBaseUrl: json['api_base_url']?.toString() ?? defaultApiBaseUrl,
      mqttBroker: json['mqtt_broker']?.toString() ?? defaultMqttBroker,
      mqttPort: json['mqtt_port'] is int
          ? json['mqtt_port'] as int
          : int.tryParse(json['mqtt_port']?.toString() ?? '') ??
              defaultMqttPort,
      isScaleReady: json['is_scale_ready'] is bool
          ? json['is_scale_ready'] as bool
          : (json['is_scale_ready']?.toString().toLowerCase() == 'true' ||
              json['is_scale_ready'] == null),
      locationName: json['location_name']?.toString() ?? defaultLocationName,
    );
  }

  Map<String, dynamic> toJson() => {
        'cart_id': cartId,
        'cart_number': cartNumber,
        'api_base_url': apiBaseUrl,
        'mqtt_broker': mqttBroker,
        'mqtt_port': mqttPort,
        'is_scale_ready': isScaleReady,
        'location_name': locationName,
      };

  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      other is KioskConfig &&
          runtimeType == other.runtimeType &&
          cartId == other.cartId &&
          cartNumber == other.cartNumber &&
          apiBaseUrl == other.apiBaseUrl &&
          mqttBroker == other.mqttBroker &&
          mqttPort == other.mqttPort &&
          isScaleReady == other.isScaleReady &&
          locationName == other.locationName;

  @override
  int get hashCode => Object.hash(
        cartId,
        cartNumber,
        apiBaseUrl,
        mqttBroker,
        mqttPort,
        isScaleReady,
        locationName,
      );

  @override
  String toString() =>
      'KioskConfig(cartNumber: $cartNumber, cartId: $cartId, api: $apiBaseUrl, scaleReady: $isScaleReady)';
}
