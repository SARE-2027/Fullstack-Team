import 'package:flutter/foundation.dart';
import 'package:flutter/services.dart';

abstract final class WindowThemeHelper {
  static const MethodChannel _channel =
      MethodChannel('com.sare.sare_kiosk/window');

  /// Dynamically updates the native host window title bar theme (Windows/Desktop)
  static Future<void> updateTitleBarTheme({required bool isDark}) async {
    if (kIsWeb) return;
    try {
      await _channel.invokeMethod('setDarkMode', isDark);
    } catch (_) {
      // Gracefully ignored on non-desktop platforms or when runner channel is unavailable
    }
  }

  /// Centers the native host window on the current display (Windows/Desktop)
  static Future<void> centerWindow() async {
    if (kIsWeb) return;
    try {
      await _channel.invokeMethod('centerWindow');
    } catch (_) {
      // Gracefully ignored on non-desktop platforms or when runner channel is unavailable
    }
  }
}
