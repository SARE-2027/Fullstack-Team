import 'package:flutter/material.dart';

import 'app_colors.dart';
import 'app_typography.dart';

abstract final class AppTheme {
  /// Dark Theme: Primary mode for the 10.1" smart cart touchscreen
  static final ThemeData darkTheme = ThemeData(
    useMaterial3: true,
    brightness: Brightness.dark,
    scaffoldBackgroundColor: Colors.black,
    primaryColor: Colors.white,
    cardColor: const Color(0xFF1E1E1E),
    dividerColor: const Color(0xFF1E1E1E),
    colorScheme: const ColorScheme.dark(
      surface: Color(0xFF1E1E1E),
      primary: Colors.white,
      onPrimary: Colors.black,
      secondary: Color(0xFF2563EB),
      onSecondary: Colors.white,
      error: Color(0xFFEF4444),
      onError: Colors.white,
    ),
    appBarTheme: const AppBarTheme(
      centerTitle: true,
      backgroundColor: Colors.black,
      foregroundColor: Colors.white,
      elevation: 0,
      scrolledUnderElevation: 0,
    ),
    cardTheme: const CardThemeData(
      color: Color(0xFF1E1E1E),
      elevation: 0,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.all(Radius.circular(16)),
        side: BorderSide(color: Color(0xFF333333)),
      ),
    ),
    elevatedButtonTheme: ElevatedButtonThemeData(
      style: ElevatedButton.styleFrom(
        elevation: 0,
        backgroundColor: Colors.white,
        foregroundColor: Colors.black,
        minimumSize: const Size(64, 52),
        shape: const RoundedRectangleBorder(
          borderRadius: BorderRadius.all(Radius.circular(14)),
        ),
        textStyle: const TextStyle(
          fontSize: 16,
          fontWeight: FontWeight.bold,
        ),
      ),
    ),
    outlinedButtonTheme: OutlinedButtonThemeData(
      style: OutlinedButton.styleFrom(
        foregroundColor: Colors.white,
        side: const BorderSide(color: Color(0xFF555555)),
        minimumSize: const Size(64, 52),
        shape: const RoundedRectangleBorder(
          borderRadius: BorderRadius.all(Radius.circular(14)),
        ),
        textStyle: const TextStyle(
          fontSize: 16,
          fontWeight: FontWeight.w600,
        ),
      ),
    ),
    textTheme: AppTypography.createTextTheme(Brightness.dark),
    extensions: const <ThemeExtension<dynamic>>[
      AppColorTheme.dark,
    ],
  );

  /// Light Theme: Crisp high-contrast daytime mode
  static final ThemeData lightTheme = ThemeData(
    useMaterial3: true,
    brightness: Brightness.light,
    scaffoldBackgroundColor: Colors.white,
    primaryColor: Colors.black,
    cardColor: const Color(0xFFF3F4F5),
    dividerColor: const Color(0xFFDDDDDD),
    colorScheme: const ColorScheme.light(
      surface: Color(0xFFF3F4F5),
      primary: Colors.black,
      onPrimary: Colors.white,
      secondary: Color(0xFF1D4ED8),
      onSecondary: Colors.white,
      error: Color(0xFFDC2626),
      onError: Colors.white,
    ),
    appBarTheme: const AppBarTheme(
      centerTitle: true,
      backgroundColor: Colors.white,
      foregroundColor: Colors.black,
      elevation: 0,
      scrolledUnderElevation: 0,
    ),
    cardTheme: const CardThemeData(
      color: Color(0xFFF3F4F5),
      elevation: 0,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.all(Radius.circular(16)),
        side: BorderSide(color: Color(0xFFDDDDDD)),
      ),
    ),
    elevatedButtonTheme: ElevatedButtonThemeData(
      style: ElevatedButton.styleFrom(
        elevation: 0,
        backgroundColor: Colors.black,
        foregroundColor: Colors.white,
        minimumSize: const Size(64, 52),
        shape: const RoundedRectangleBorder(
          borderRadius: BorderRadius.all(Radius.circular(14)),
        ),
        textStyle: const TextStyle(
          fontSize: 16,
          fontWeight: FontWeight.bold,
        ),
      ),
    ),
    outlinedButtonTheme: OutlinedButtonThemeData(
      style: OutlinedButton.styleFrom(
        foregroundColor: Colors.black,
        side: const BorderSide(color: Color(0xFF888888)),
        minimumSize: const Size(64, 52),
        shape: const RoundedRectangleBorder(
          borderRadius: BorderRadius.all(Radius.circular(14)),
        ),
        textStyle: const TextStyle(
          fontSize: 16,
          fontWeight: FontWeight.w600,
        ),
      ),
    ),
    textTheme: AppTypography.createTextTheme(Brightness.light),
    extensions: const <ThemeExtension<dynamic>>[
      AppColorTheme.light,
    ],
  );
}
