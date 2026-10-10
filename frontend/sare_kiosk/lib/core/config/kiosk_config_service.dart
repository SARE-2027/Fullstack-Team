import 'dart:convert';
import 'dart:io';

import 'package:flutter/foundation.dart';

import 'kiosk_config.dart';

/// Service responsible for loading and resolving Kiosk device configuration.
///
/// Implements a prioritized resolution cascade:
/// 1. CLI Arguments (`--cart-number=02`, `--cart-id=CART-02`, `--config-path=...`)
/// 2. System Environment Variables (`SARE_CART_NUMBER`, `SARE_CART_ID`, `SARE_API_URL`)
/// 3. External File on Host OS (e.g., `/etc/sare/config.json` on Linux/RPi, or `./sare_config.json`)
/// 4. Safe Default Fallback (`cartNumber: '01'`, `cartId: 'CART-01'`)
class KioskConfigService {
  KioskConfigService._();

  static const List<String> _candidateLinuxPaths = [
    '/etc/sare/config.json',
    '/etc/sare/kiosk_config.json',
  ];

  static const List<String> _candidateLocalPaths = [
    'sare_config.json',
    'kiosk_config.json',
    'config.json',
  ];

  /// Loads the resolved [KioskConfig].
  ///
  /// Never throws an unhandled exception — safely defaults to single-cart
  /// configuration if external files are missing or malformed.
  static Future<KioskConfig> load({List<String> args = const []}) async {
    KioskConfig config = const KioskConfig();

    // 1. Try to read from external host file
    final fileConfig = await _loadFromFile(args);
    if (fileConfig != null) {
      config = fileConfig;
    }

    // 2. Override with system environment variables if present
    config = _applyEnvironmentOverrides(config);

    // 3. Override with CLI arguments if present (highest precedence)
    config = _applyCliOverrides(config, args);

    if (kDebugMode) {
      debugPrint('[KioskConfigService] Active configuration: $config');
    }

    return config;
  }

  /// Attempts to discover and parse a JSON configuration file on the host filesystem.
  static Future<KioskConfig?> _loadFromFile(List<String> args) async {
    try {
      // Check if explicit path was specified via CLI: --config-path=/path/to/file.json
      final explicitPath = _getCliValue(args, 'config-path');
      if (explicitPath != null && explicitPath.isNotEmpty) {
        final explicitFile = File(explicitPath);
        if (await explicitFile.exists()) {
          final content = await explicitFile.readAsString();
          return KioskConfig.fromJson(
            jsonDecode(content) as Map<String, dynamic>,
          );
        }
      }

      // Check standard system and local paths
      final candidates = <String>[
        ..._candidateLocalPaths,
        if (Platform.isLinux) ..._candidateLinuxPaths,
        if (Platform.isWindows && Platform.environment.containsKey('APPDATA'))
          '${Platform.environment['APPDATA']}\\sare\\kiosk_config.json',
      ];

      for (final path in candidates) {
        try {
          final file = File(path);
          if (await file.exists()) {
            final content = await file.readAsString();
            final json = jsonDecode(content);
            if (json is Map<String, dynamic>) {
              if (kDebugMode) {
                debugPrint('[KioskConfigService] Loaded file: $path');
              }
              return KioskConfig.fromJson(json);
            }
          }
        } catch (e) {
          if (kDebugMode) {
            debugPrint('[KioskConfigService] Error reading candidate $path: $e');
          }
        }
      }
    } catch (e) {
      if (kDebugMode) {
        debugPrint('[KioskConfigService] General file load error: $e');
      }
    }
    return null;
  }

  /// Applies environment variable overrides.
  static KioskConfig _applyEnvironmentOverrides(KioskConfig base) {
    try {
      final env = Platform.environment;

      final cartId = env['SARE_CART_ID'] ?? base.cartId;
      final cartNumber = env['SARE_CART_NUMBER'] ?? base.cartNumber;
      final apiUrl = env['SARE_API_URL'] ?? base.apiBaseUrl;
      final mqttBroker = env['SARE_MQTT_BROKER'] ?? base.mqttBroker;
      final locationName = env['SARE_LOCATION_NAME'] ?? base.locationName;

      int mqttPort = base.mqttPort;
      if (env.containsKey('SARE_MQTT_PORT')) {
        mqttPort = int.tryParse(env['SARE_MQTT_PORT']!) ?? base.mqttPort;
      }

      bool isScaleReady = base.isScaleReady;
      if (env.containsKey('SARE_SCALE_READY')) {
        isScaleReady = env['SARE_SCALE_READY']!.toLowerCase() == 'true';
      }

      return base.copyWith(
        cartId: cartId,
        cartNumber: cartNumber,
        apiBaseUrl: apiUrl,
        mqttBroker: mqttBroker,
        mqttPort: mqttPort,
        isScaleReady: isScaleReady,
        locationName: locationName,
      );
    } catch (_) {
      return base;
    }
  }

  /// Applies CLI arguments (--key=value or --key value).
  static KioskConfig _applyCliOverrides(KioskConfig base, List<String> args) {
    if (args.isEmpty) return base;

    final cartId = _getCliValue(args, 'cart-id') ?? base.cartId;
    final cartNumber = _getCliValue(args, 'cart-number') ?? base.cartNumber;
    final apiUrl = _getCliValue(args, 'api-url') ?? base.apiBaseUrl;
    final mqttBroker = _getCliValue(args, 'mqtt-broker') ?? base.mqttBroker;
    final locationName = _getCliValue(args, 'location') ?? base.locationName;

    int mqttPort = base.mqttPort;
    final portRaw = _getCliValue(args, 'mqtt-port');
    if (portRaw != null) {
      mqttPort = int.tryParse(portRaw) ?? base.mqttPort;
    }

    bool isScaleReady = base.isScaleReady;
    final scaleRaw = _getCliValue(args, 'scale-ready');
    if (scaleRaw != null) {
      isScaleReady = scaleRaw.toLowerCase() == 'true';
    }

    return base.copyWith(
      cartId: cartId,
      cartNumber: cartNumber,
      apiBaseUrl: apiUrl,
      mqttBroker: mqttBroker,
      mqttPort: mqttPort,
      isScaleReady: isScaleReady,
      locationName: locationName,
    );
  }

  static String? _getCliValue(List<String> args, String key) {
    final prefix = '--$key=';
    for (int i = 0; i < args.length; i++) {
      final arg = args[i];
      if (arg.startsWith(prefix)) {
        return arg.substring(prefix.length);
      }
      if (arg == '--$key' && i + 1 < args.length) {
        return args[i + 1];
      }
    }
    return null;
  }
}
