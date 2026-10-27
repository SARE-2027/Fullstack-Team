import 'package:flutter/material.dart';

import 'app_colors.dart';
import 'app_typography.dart';

abstract final class AppTheme {
  /// Dark Theme: Primary mode for the 10.1" smart cart touchscreen.
  /// Deep green-charcoal surfaces keep emerald verification states instant.
  static final ThemeData darkTheme = ThemeData(
    useMaterial3: true,
    brightness: Brightness.dark,
    scaffoldBackgroundColor: const Color(0xFF0C1310),
    primaryColor: AppColorTheme.dark.brand,
    cardColor: AppColorTheme.dark.surface,
    dividerColor: AppColorTheme.dark.border,
    splashColor: Colors.white.withValues(alpha: 0.06),
    highlightColor: Colors.white.withValues(alpha: 0.04),
    colorScheme: ColorScheme.dark(
      surface: AppColorTheme.dark.surface,
      onSurface: const Color(0xFFF1F7F3),
      surfaceContainerHighest: AppColorTheme.dark.surfaceAlt,
      primary: AppColorTheme.dark.brand,
      onPrimary: AppColorTheme.dark.onBrand,
      secondary: const Color(0xFF0AAF7C),
      onSecondary: AppColorTheme.dark.onBrand,
      error: AppColorTheme.dark.danger,
      onError: const Color(0xFF2A0B0B),
      outline: AppColorTheme.dark.borderStrong,
    ),
    textTheme: AppTypography.createTextTheme(Brightness.dark),
    splashFactory: InkSparkle.splashFactory,
    appBarTheme: const AppBarTheme(
      centerTitle: true,
      backgroundColor: Colors.transparent,
      foregroundColor: Color(0xFFF1F7F3),
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
        color: Color(0xFFF1F7F3),
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
        backgroundColor: AppColorTheme.dark.brand,
        foregroundColor: AppColorTheme.dark.onBrand,
        disabledBackgroundColor: AppColorTheme.dark.surfaceAlt,
        disabledForegroundColor: AppColorTheme.dark.textFaint,
        minimumSize: const Size(64, 56),
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
        foregroundColor: const Color(0xFFF1F7F3),
        side: BorderSide(color: AppColorTheme.dark.borderStrong),
        minimumSize: const Size(64, 56),
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

  /// Light Theme: Crisp porcelain daytime mode with the same
  /// verification-first color semantics.
  static final ThemeData lightTheme = ThemeData(
    useMaterial3: true,
    brightness: Brightness.light,
    scaffoldBackgroundColor: const Color(0xFFF3F7F4),
    primaryColor: AppColorTheme.light.brand,
    cardColor: AppColorTheme.light.surface,
    dividerColor: AppColorTheme.light.border,
    splashColor: AppColorTheme.light.brandDeep.withValues(alpha: 0.08),
    highlightColor: AppColorTheme.light.brandDeep.withValues(alpha: 0.05),
    colorScheme: ColorScheme.light(
      surface: AppColorTheme.light.surface,
      onSurface: const Color(0xFF11241B),
      surfaceContainerHighest: AppColorTheme.light.surfaceAlt,
      primary: AppColorTheme.light.brand,
      onPrimary: AppColorTheme.light.onBrand,
      secondary: const Color(0xFF059669),
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
      foregroundColor: Color(0xFF11241B),
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
        color: Color(0xFF11241B),
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
        backgroundColor: AppColorTheme.light.brand,
        foregroundColor: AppColorTheme.light.onBrand,
        disabledBackgroundColor: AppColorTheme.light.surfaceAlt,
        disabledForegroundColor: AppColorTheme.light.textFaint,
        minimumSize: const Size(64, 56),
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
        foregroundColor: const Color(0xFF11241B),
        side: BorderSide(color: AppColorTheme.light.borderStrong),
        minimumSize: const Size(64, 56),
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
