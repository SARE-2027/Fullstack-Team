import 'dart:io' show Platform;

import 'package:flutter/material.dart';

/// Central motion policy for the kiosk.
///
/// Ambient (infinitely repeating) animations are disabled:
/// - Under `flutter test`, so widget tests can always `pumpAndSettle`.
/// - When the OS requests reduced motion (accessibility).
abstract final class KioskMotion {
  static final bool _isRunningInTest =
      Platform.environment.containsKey('FLUTTER_TEST');

  static bool ambientEnabled(BuildContext context) {
    if (_isRunningInTest) return false;
    if (MediaQuery.of(context).disableAnimations) return false;
    return true;
  }
}
