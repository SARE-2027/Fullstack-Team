import 'package:flutter/material.dart';

abstract final class AppTypography {
  static const String englishFontFamily = 'Poppins';
  static const String arabicFontFamily = 'Tajawal';

  /// Generates the base TextTheme using bundled local Poppins as primary,
  /// styled for high-legibility offline 10.1" touch kiosk screens.
  static TextTheme createTextTheme(Brightness brightness) {
    final baseColor =
        brightness == Brightness.dark ? Colors.white : Colors.black;
    final secondaryColor = brightness == Brightness.dark
        ? const Color(0xFFCCCCCC)
        : const Color(0xFF666666);

    const fontFamily = englishFontFamily;

    return TextTheme(
      displayLarge: TextStyle(
        fontFamily: fontFamily,
        color: baseColor,
        fontSize: 57,
        fontWeight: FontWeight.bold,
        letterSpacing: -0.5,
      ),
      displayMedium: TextStyle(
        fontFamily: fontFamily,
        color: baseColor,
        fontSize: 45,
        fontWeight: FontWeight.bold,
      ),
      headlineMedium: TextStyle(
        fontFamily: fontFamily,
        color: baseColor,
        fontSize: 28,
        fontWeight: FontWeight.w600,
      ),
      titleLarge: TextStyle(
        fontFamily: fontFamily,
        color: baseColor,
        fontSize: 22,
        fontWeight: FontWeight.w600,
      ),
      titleMedium: TextStyle(
        fontFamily: fontFamily,
        color: baseColor,
        fontSize: 16,
        fontWeight: FontWeight.w500,
      ),
      bodyLarge: TextStyle(
        fontFamily: fontFamily,
        color: baseColor,
        fontSize: 16,
      ),
      bodyMedium: TextStyle(
        fontFamily: fontFamily,
        color: secondaryColor,
        fontSize: 14,
      ),
      labelLarge: TextStyle(
        fontFamily: fontFamily,
        color: baseColor,
        fontSize: 14,
        fontWeight: FontWeight.w600,
      ),
    );
  }
}
