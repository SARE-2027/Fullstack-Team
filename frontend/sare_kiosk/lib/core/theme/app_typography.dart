import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';

abstract final class AppTypography {
  static const String englishFontFamily = 'Poppins';
  static const String arabicFontFamily = 'Tajawal';

  /// Generates the base TextTheme using Poppins as primary, styled for high-legibility touch kiosk
  static TextTheme createTextTheme(Brightness brightness) {
    final baseColor = brightness == Brightness.dark ? Colors.white : Colors.black;
    final secondaryColor =
        brightness == Brightness.dark ? const Color(0xFFCCCCCC) : const Color(0xFF666666);

    final poppinsTheme = GoogleFonts.poppinsTextTheme();

    return poppinsTheme.copyWith(
      displayLarge: poppinsTheme.displayLarge?.copyWith(
        color: baseColor,
        fontWeight: FontWeight.bold,
        letterSpacing: -0.5,
      ),
      displayMedium: poppinsTheme.displayMedium?.copyWith(
        color: baseColor,
        fontWeight: FontWeight.bold,
      ),
      headlineMedium: poppinsTheme.headlineMedium?.copyWith(
        color: baseColor,
        fontWeight: FontWeight.w600,
      ),
      titleLarge: poppinsTheme.titleLarge?.copyWith(
        color: baseColor,
        fontWeight: FontWeight.w600,
      ),
      titleMedium: poppinsTheme.titleMedium?.copyWith(
        color: baseColor,
        fontWeight: FontWeight.w500,
      ),
      bodyLarge: poppinsTheme.bodyLarge?.copyWith(
        color: baseColor,
      ),
      bodyMedium: poppinsTheme.bodyMedium?.copyWith(
        color: secondaryColor,
      ),
      labelLarge: poppinsTheme.labelLarge?.copyWith(
        color: baseColor,
        fontWeight: FontWeight.w600,
      ),
    );
  }
}
