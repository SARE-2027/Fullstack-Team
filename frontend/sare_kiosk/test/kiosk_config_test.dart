import 'dart:convert';
import 'package:flutter_test/flutter_test.dart';
import 'package:sare_kiosk/core/config/kiosk_config_exports.dart';

void main() {
  group('KioskConfig', () {
    test('Default constructor provides single-cart defaults', () {
      const config = KioskConfig();

      expect(config.cartNumber, '01');
      expect(config.cartId, 'CART-01');
      expect(config.apiBaseUrl, 'http://localhost:5000');
      expect(config.mqttBroker, 'localhost');
      expect(config.mqttPort, 1883);
      expect(config.isScaleReady, isTrue);
    });

    test('Custom multi-cart configuration parses properly from JSON', () {
      const jsonString = '''
      {
        "cart_id": "CART-09",
        "cart_number": "09",
        "api_base_url": "http://192.168.1.50:5000",
        "mqtt_broker": "192.168.1.50",
        "mqtt_port": 1883,
        "is_scale_ready": false,
        "location_name": "Bakery Aisle"
      }
      ''';

      final Map<String, dynamic> map = jsonDecode(jsonString);
      final config = KioskConfig.fromJson(map);

      expect(config.cartId, 'CART-09');
      expect(config.cartNumber, '09');
      expect(config.apiBaseUrl, 'http://192.168.1.50:5000');
      expect(config.mqttBroker, '192.168.1.50');
      expect(config.isScaleReady, isFalse);
      expect(config.locationName, 'Bakery Aisle');
    });

    test('CLI arguments override defaults seamlessly', () async {
      final config = await KioskConfigService.load(
        args: [
          '--cart-number=05',
          '--cart-id=CART-05',
          '--api-url=http://sare.local:5000',
          '--scale-ready=true',
        ],
      );

      expect(config.cartNumber, '05');
      expect(config.cartId, 'CART-05');
      expect(config.apiBaseUrl, 'http://sare.local:5000');
      expect(config.isScaleReady, isTrue);
    });
  });
}
