import 'package:flutter/material.dart';

/// Shared type & motion contract for the SARE kiosk.
///
/// Typography pairs Poppins (Latin) with Tajawal (Arabic) — both bundled
/// offline so the Raspberry Pi never depends on a network font fetch.
abstract final class AppTypography {
  static const String englishFontFamily = 'Poppins';
  static const String arabicFontFamily = 'Tajawal';

  // ── Motion contract ──────────────────────────────────────────────────────
  // One shared set of durations & curves so every component feels like the
  // same physical product. Enter fast-and-out, exit quicker-and-in.
  static const Duration fast = Duration(milliseconds: 140);
  static const Duration base = Duration(milliseconds: 220);
  static const Duration slow = Duration(milliseconds: 320);

  static const Curve enterCurve = Curves.easeOutCubic;
  static const Curve exitCurve = Curves.easeInCubic;

  static String fontFamilyFor(String localeCode) =>
      localeCode == 'ar' ? arabicFontFamily : englishFontFamily;

  /// Kiosk-tuned text scale for the 10.1" 1280×800 touchscreen: glanceable
  /// from arm's length, generous hierarchy jumps between roles.
  static TextTheme createTextTheme(Brightness brightness) {
    final baseColor =
        brightness == Brightness.dark ? const Color(0xFFF2F5F9) : const Color(0xFF10141A);
    final secondaryColor =
        brightness == Brightness.dark ? const Color(0xFFA6B0BC) : const Color(0xFF566070);

    const fontFamily = englishFontFamily;

    return TextTheme(
      displayLarge: TextStyle(
        fontFamily: fontFamily,
        color: baseColor,
        fontSize: 42,
        fontWeight: FontWeight.w800,
        letterSpacing: -0.5,
        height: 1.1,
      ),
      displayMedium: TextStyle(
        fontFamily: fontFamily,
        color: baseColor,
        fontSize: 34,
        fontWeight: FontWeight.w800,
        letterSpacing: -0.4,
        height: 1.15,
      ),
      displaySmall: TextStyle(
        fontFamily: fontFamily,
        color: baseColor,
        fontSize: 28,
        fontWeight: FontWeight.w800,
        letterSpacing: -0.3,
        height: 1.2,
      ),
      headlineMedium: TextStyle(
        fontFamily: fontFamily,
        color: baseColor,
        fontSize: 24,
        fontWeight: FontWeight.w700,
        letterSpacing: -0.2,
        height: 1.25,
      ),
      headlineSmall: TextStyle(
        fontFamily: fontFamily,
        color: baseColor,
        fontSize: 20,
        fontWeight: FontWeight.w700,
        letterSpacing: -0.2,
        height: 1.3,
      ),
      titleLarge: TextStyle(
        fontFamily: fontFamily,
        color: baseColor,
        fontSize: 18,
        fontWeight: FontWeight.w700,
        height: 1.3,
      ),
      titleMedium: TextStyle(
        fontFamily: fontFamily,
        color: baseColor,
        fontSize: 16,
        fontWeight: FontWeight.w600,
        height: 1.35,
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
        height: 1.3,
      ),
      labelMedium: TextStyle(
        fontFamily: fontFamily,
        color: secondaryColor,
        fontSize: 12,
        fontWeight: FontWeight.w600,
        letterSpacing: 0.1,
        height: 1.35,
      ),
    );
  }
}
