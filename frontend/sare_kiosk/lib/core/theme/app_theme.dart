import 'package:flutter/material.dart';

import 'app_colors.dart';
import 'app_typography.dart';

abstract final class AppTheme {
  /// Dark Theme: "Obsidian Titanium Cockpit" — default mode for the 10.1"
  /// smart cart touchscreen under bright overhead supermarket lighting.
  static final ThemeData darkTheme = ThemeData(
    useMaterial3: true,
    brightness: Brightness.dark,
    scaffoldBackgroundColor: const Color(0xFF090C12),
    primaryColor: AppColorTheme.dark.brandDeep,
    cardColor: AppColorTheme.dark.surface,
    dividerColor: AppColorTheme.dark.border,
    splashColor: Colors.white.withValues(alpha: 0.06),
    highlightColor: Colors.white.withValues(alpha: 0.04),
    colorScheme: ColorScheme.dark(
      surface: AppColorTheme.dark.surface,
      onSurface: const Color(0xFFF4F7FB),
      surfaceContainerHighest: AppColorTheme.dark.surfaceAlt,
      primary: AppColorTheme.dark.brandDeep,
      onPrimary: AppColorTheme.dark.onBrand,
      secondary: AppColorTheme.dark.brandGradientEnd,
      onSecondary: AppColorTheme.dark.onBrand,
      error: AppColorTheme.dark.danger,
      onError: const Color(0xFF280808),
      outline: AppColorTheme.dark.borderStrong,
    ),
    textTheme: AppTypography.createTextTheme(Brightness.dark),
    splashFactory: InkSparkle.splashFactory,
    appBarTheme: const AppBarTheme(
      centerTitle: true,
      backgroundColor: Colors.transparent,
      foregroundColor: Color(0xFFF4F7FB),
      elevation: 0,
      scrolledUnderElevation: 0,
    ),
    cardTheme: CardThemeData(
      color: AppColorTheme.dark.surface,
      elevation: 0,
      margin: EdgeInsets.zero,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(18),
        side: BorderSide(color: AppColorTheme.dark.border),
      ),
    ),
    dialogTheme: DialogThemeData(
      backgroundColor: AppColorTheme.dark.surface,
      elevation: 0,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(24),
        side: BorderSide(color: AppColorTheme.dark.borderStrong),
      ),
    ),
    snackBarTheme: SnackBarThemeData(
      behavior: SnackBarBehavior.floating,
      backgroundColor: AppColorTheme.dark.surfaceRaised,
      contentTextStyle: const TextStyle(
        fontFamily: AppTypography.englishFontFamily,
        color: Color(0xFFF4F7FB),
        fontSize: 14,
        fontWeight: FontWeight.w600,
      ),
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(14),
        side: BorderSide(color: AppColorTheme.dark.border),
      ),
    ),
    dividerTheme: DividerThemeData(
      color: AppColorTheme.dark.border,
      thickness: 1,
      space: 1,
    ),
    progressIndicatorTheme: ProgressIndicatorThemeData(
      color: AppColorTheme.dark.brand,
      circularTrackColor: AppColorTheme.dark.surfaceAlt,
      linearTrackColor: AppColorTheme.dark.surfaceAlt,
    ),
    textSelectionTheme: TextSelectionThemeData(
      cursorColor: AppColorTheme.dark.brand,
      selectionColor: AppColorTheme.dark.brand.withValues(alpha: 0.30),
      selectionHandleColor: AppColorTheme.dark.brand,
    ),
    elevatedButtonTheme: ElevatedButtonThemeData(
      style: ElevatedButton.styleFrom(
        elevation: 0,
        backgroundColor: AppColorTheme.dark.brandDeep,
        foregroundColor: AppColorTheme.dark.onBrand,
        disabledBackgroundColor: AppColorTheme.dark.surfaceAlt,
        disabledForegroundColor: AppColorTheme.dark.textFaint,
        minimumSize: const Size(64, 54),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
        textStyle: const TextStyle(
          fontFamily: AppTypography.englishFontFamily,
          fontSize: 16,
          fontWeight: FontWeight.w700,
        ),
      ),
    ),
    outlinedButtonTheme: OutlinedButtonThemeData(
      style: OutlinedButton.styleFrom(
        foregroundColor: const Color(0xFFF4F7FB),
        side: BorderSide(color: AppColorTheme.dark.borderStrong),
        minimumSize: const Size(64, 54),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
        textStyle: const TextStyle(
          fontFamily: AppTypography.englishFontFamily,
          fontSize: 16,
          fontWeight: FontWeight.w600,
        ),
      ),
    ),
    textButtonTheme: TextButtonThemeData(
      style: TextButton.styleFrom(
        foregroundColor: AppColorTheme.dark.brand,
        textStyle: const TextStyle(
          fontFamily: AppTypography.englishFontFamily,
          fontSize: 14,
          fontWeight: FontWeight.w700,
        ),
      ),
    ),
    extensions: const <ThemeExtension<dynamic>>[
      AppColorTheme.dark,
    ],
  );

  /// Light Theme: "Porcelain Alabaster Studio" — crisp high-contrast daylight
  /// mode with identical verification-first semantics.
  static final ThemeData lightTheme = ThemeData(
    useMaterial3: true,
    brightness: Brightness.light,
    scaffoldBackgroundColor: const Color(0xFFF3F6FA),
    primaryColor: AppColorTheme.light.brandDeep,
    cardColor: AppColorTheme.light.surface,
    dividerColor: AppColorTheme.light.border,
    splashColor: AppColorTheme.light.brandDeep.withValues(alpha: 0.08),
    highlightColor: AppColorTheme.light.brandDeep.withValues(alpha: 0.05),
    colorScheme: ColorScheme.light(
      surface: AppColorTheme.light.surface,
      onSurface: const Color(0xFF0D131D),
      surfaceContainerHighest: AppColorTheme.light.surfaceAlt,
      primary: AppColorTheme.light.brandDeep,
      onPrimary: AppColorTheme.light.onBrand,
      secondary: AppColorTheme.light.brandDeep,
      onSecondary: AppColorTheme.light.onBrand,
      error: AppColorTheme.light.danger,
      onError: Colors.white,
      outline: AppColorTheme.light.borderStrong,
    ),
    textTheme: AppTypography.createTextTheme(Brightness.light),
    splashFactory: InkSparkle.splashFactory,
    appBarTheme: const AppBarTheme(
      centerTitle: true,
      backgroundColor: Colors.transparent,
      foregroundColor: Color(0xFF0D131D),
      elevation: 0,
      scrolledUnderElevation: 0,
    ),
    cardTheme: CardThemeData(
      color: AppColorTheme.light.surface,
      elevation: 0,
      margin: EdgeInsets.zero,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(18),
        side: BorderSide(color: AppColorTheme.light.border),
      ),
    ),
    dialogTheme: DialogThemeData(
      backgroundColor: AppColorTheme.light.surface,
      elevation: 0,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(24),
        side: BorderSide(color: AppColorTheme.light.borderStrong),
      ),
    ),
    snackBarTheme: SnackBarThemeData(
      behavior: SnackBarBehavior.floating,
      backgroundColor: AppColorTheme.light.surface,
      contentTextStyle: const TextStyle(
        fontFamily: AppTypography.englishFontFamily,
        color: Color(0xFF0D131D),
        fontSize: 14,
        fontWeight: FontWeight.w600,
      ),
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(14),
        side: BorderSide(color: AppColorTheme.light.border),
      ),
    ),
    dividerTheme: DividerThemeData(
      color: AppColorTheme.light.border,
      thickness: 1,
      space: 1,
    ),
    progressIndicatorTheme: ProgressIndicatorThemeData(
      color: AppColorTheme.light.brand,
      circularTrackColor: AppColorTheme.light.surfaceAlt,
      linearTrackColor: AppColorTheme.light.surfaceAlt,
    ),
    textSelectionTheme: TextSelectionThemeData(
      cursorColor: AppColorTheme.light.brand,
      selectionColor: AppColorTheme.light.brand.withValues(alpha: 0.25),
      selectionHandleColor: AppColorTheme.light.brand,
    ),
    elevatedButtonTheme: ElevatedButtonThemeData(
      style: ElevatedButton.styleFrom(
        elevation: 0,
        backgroundColor: AppColorTheme.light.brandDeep,
        foregroundColor: AppColorTheme.light.onBrand,
        disabledBackgroundColor: AppColorTheme.light.surfaceAlt,
        disabledForegroundColor: AppColorTheme.light.textFaint,
        minimumSize: const Size(64, 54),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
        textStyle: const TextStyle(
          fontFamily: AppTypography.englishFontFamily,
          fontSize: 16,
          fontWeight: FontWeight.w700,
        ),
      ),
    ),
    outlinedButtonTheme: OutlinedButtonThemeData(
      style: OutlinedButton.styleFrom(
        foregroundColor: const Color(0xFF0D131D),
        side: BorderSide(color: AppColorTheme.light.borderStrong),
        minimumSize: const Size(64, 54),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
        textStyle: const TextStyle(
          fontFamily: AppTypography.englishFontFamily,
          fontSize: 16,
          fontWeight: FontWeight.w600,
        ),
      ),
    ),
    textButtonTheme: TextButtonThemeData(
      style: TextButton.styleFrom(
        foregroundColor: AppColorTheme.light.brand,
        textStyle: const TextStyle(
          fontFamily: AppTypography.englishFontFamily,
          fontSize: 14,
          fontWeight: FontWeight.w700,
        ),
      ),
    ),
    extensions: const <ThemeExtension<dynamic>>[
      AppColorTheme.light,
    ],
  );
}
