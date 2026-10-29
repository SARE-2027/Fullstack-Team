import 'package:flutter/material.dart';

import '../utils/kiosk_motion.dart';

/// Shared optical typography & Apple spring motion contract for the SARE kiosk.
///
/// Typography pairs Poppins (Latin) with Tajawal (Arabic) — both bundled
/// offline so the Raspberry Pi 5 never depends on a network font fetch.
/// Follows Apple WWDC *The Details of UI Typography*: negative tracking on
/// large display figures, neutral tracking on body copy, and inverse leading.
abstract final class AppTypography {
  static const String englishFontFamily = 'Poppins';
  static const String arabicFontFamily = 'Tajawal';

  // ── Motion contract (Apple WWDC Fluid Interfaces) ────────────────────────
  static const Duration fast = Duration(milliseconds: 130);
  static const Duration base = Duration(milliseconds: 240);
  static const Duration slow = Duration(milliseconds: 360);

  /// Critically damped Apple spring for entries and continuous re-targeting.
  static const Curve enterCurve = KioskMotion.springCritical;

  /// Snappy tactile spring for button presses and selection rings.
  static const Curve springCurve = KioskMotion.springSnappy;

  /// Fast ease-in exit so dismissals clear the stage without lingering.
  static const Curve exitCurve = Curves.easeInCubic;

  static String fontFamilyFor(String localeCode) =>
      localeCode == 'ar' ? arabicFontFamily : englishFontFamily;

  /// Kiosk-tuned optical text scale for the 10.1" 1280×800 touchscreen:
  /// glanceable from arm's length, tabular numerals, and strict hierarchy.
  static TextTheme createTextTheme(Brightness brightness) {
    final baseColor = brightness == Brightness.dark
        ? const Color(0xFFF4F7FB)
        : const Color(0xFF0D131D);
    final secondaryColor = brightness == Brightness.dark
        ? const Color(0xFFA3B1C6)
        : const Color(0xFF4A5568);

    const fontFamily = englishFontFamily;
    const tabular = <FontFeature>[FontFeature.tabularFigures()];

    return TextTheme(
      displayLarge: TextStyle(
        fontFamily: fontFamily,
        color: baseColor,
        fontSize: 42,
        fontWeight: FontWeight.w800,
        letterSpacing: -0.9,
        height: 1.06,
        fontFeatures: tabular,
      ),
      displayMedium: TextStyle(
        fontFamily: fontFamily,
        color: baseColor,
        fontSize: 34,
        fontWeight: FontWeight.w800,
        letterSpacing: -0.65,
        height: 1.10,
        fontFeatures: tabular,
      ),
      displaySmall: TextStyle(
        fontFamily: fontFamily,
        color: baseColor,
        fontSize: 28,
        fontWeight: FontWeight.w800,
        letterSpacing: -0.45,
        height: 1.15,
        fontFeatures: tabular,
      ),
      headlineMedium: TextStyle(
        fontFamily: fontFamily,
        color: baseColor,
        fontSize: 24,
        fontWeight: FontWeight.w700,
        letterSpacing: -0.35,
        height: 1.20,
        fontFeatures: tabular,
      ),
      headlineSmall: TextStyle(
        fontFamily: fontFamily,
        color: baseColor,
        fontSize: 20,
        fontWeight: FontWeight.w700,
        letterSpacing: -0.25,
        height: 1.25,
      ),
      titleLarge: TextStyle(
        fontFamily: fontFamily,
        color: baseColor,
        fontSize: 18,
        fontWeight: FontWeight.w700,
        letterSpacing: -0.15,
        height: 1.28,
      ),
      titleMedium: TextStyle(
        fontFamily: fontFamily,
        color: baseColor,
        fontSize: 16,
        fontWeight: FontWeight.w600,
        letterSpacing: -0.1,
        height: 1.32,
      ),
      bodyLarge: TextStyle(
        fontFamily: fontFamily,
        color: baseColor,
        fontSize: 15,
        fontWeight: FontWeight.w400,
        height: 1.45,
      ),
      bodyMedium: TextStyle(
        fontFamily: fontFamily,
        color: secondaryColor,
        fontSize: 14,
        fontWeight: FontWeight.w400,
        height: 1.45,
      ),
      labelLarge: TextStyle(
        fontFamily: fontFamily,
        color: baseColor,
        fontSize: 14,
        fontWeight: FontWeight.w700,
        letterSpacing: 0.1,
        height: 1.28,
        fontFeatures: tabular,
      ),
      labelMedium: TextStyle(
        fontFamily: fontFamily,
        color: secondaryColor,
        fontSize: 12,
        fontWeight: FontWeight.w600,
        letterSpacing: 0.15,
        height: 1.32,
        fontFeatures: tabular,
      ),
    );
  }
}
