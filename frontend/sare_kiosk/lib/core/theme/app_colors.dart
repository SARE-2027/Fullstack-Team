import 'package:flutter/material.dart';

/// Extension on [ThemeData] providing customized design tokens for the
/// SARE Smart Shopping Cart Kiosk, featuring high-contrast monochrome surfaces
/// and functional smart cart status indicators.
@immutable
class AppColorTheme extends ThemeExtension<AppColorTheme> {
  const AppColorTheme({
    required this.textFieldFill,
    required this.textField,
    required this.secondaryText,
    required this.dividerColor,
    required this.containerBorder,
    required this.countButtonBackground,
    required this.weightVerified,
    required this.weightWarning,
    required this.weightDiscrepancy,
    required this.brandPrimary,
    required this.brandPrimaryText,
  });

  // Monochrome Surface & Input Tokens
  final Color textFieldFill;
  final Color textField;
  final Color secondaryText;
  final Color dividerColor;
  final Color containerBorder;
  final Color countButtonBackground;

  // Smart Cart Status Indicators
  final Color weightVerified;
  final Color weightWarning;
  final Color weightDiscrepancy;
  final Color brandPrimary;
  final Color brandPrimaryText;

  /// Dark mode palette (Default for 10.1" Supermarket Touchscreen)
  static const dark = AppColorTheme(
    textFieldFill: Color(0xFF2A2A2A),
    textField: Color(0xFF777777),
    secondaryText: Color(0xFFCCCCCC),
    dividerColor: Color(0xFF1E1E1E),
    containerBorder: Color(0xFF555555),
    countButtonBackground: Color(0xFF424242),
    weightVerified: Color(0xFF10B981), // Emerald 500
    weightWarning: Color(0xFFF59E0B), // Amber 500
    weightDiscrepancy: Color(0xFFEF4444), // Coral Red 500
    brandPrimary: Color(0xFF2563EB), // Royal Blue 600
    brandPrimaryText: Colors.white,
  );

  /// Light mode palette (High-contrast crisp daytime option)
  static const light = AppColorTheme(
    textFieldFill: Color(0xFFF3F4F5),
    textField: Color(0xFFAAAAAA),
    secondaryText: Color(0xFF666666),
    dividerColor: Color(0xFFDDDDDD),
    containerBorder: Color(0xFF888888),
    countButtonBackground: Color(0xFFEEEEEE),
    weightVerified: Color(0xFF059669), // Emerald 600
    weightWarning: Color(0xFFD97706), // Amber 600
    weightDiscrepancy: Color(0xFFDC2626), // Coral Red 600
    brandPrimary: Color(0xFF1D4ED8), // Royal Blue 700
    brandPrimaryText: Colors.white,
  );

  @override
  AppColorTheme copyWith({
    Color? textFieldFill,
    Color? textField,
    Color? secondaryText,
    Color? dividerColor,
    Color? containerBorder,
    Color? countButtonBackground,
    Color? weightVerified,
    Color? weightWarning,
    Color? weightDiscrepancy,
    Color? brandPrimary,
    Color? brandPrimaryText,
  }) {
    return AppColorTheme(
      textFieldFill: textFieldFill ?? this.textFieldFill,
      textField: textField ?? this.textField,
      secondaryText: secondaryText ?? this.secondaryText,
      dividerColor: dividerColor ?? this.dividerColor,
      containerBorder: containerBorder ?? this.containerBorder,
      countButtonBackground:
          countButtonBackground ?? this.countButtonBackground,
      weightVerified: weightVerified ?? this.weightVerified,
      weightWarning: weightWarning ?? this.weightWarning,
      weightDiscrepancy: weightDiscrepancy ?? this.weightDiscrepancy,
      brandPrimary: brandPrimary ?? this.brandPrimary,
      brandPrimaryText: brandPrimaryText ?? this.brandPrimaryText,
    );
  }

  @override
  AppColorTheme lerp(ThemeExtension<AppColorTheme>? other, double t) {
    if (other is! AppColorTheme) {
      return this;
    }
    return AppColorTheme(
      textFieldFill: Color.lerp(textFieldFill, other.textFieldFill, t)!,
      textField: Color.lerp(textField, other.textField, t)!,
      secondaryText: Color.lerp(secondaryText, other.secondaryText, t)!,
      dividerColor: Color.lerp(dividerColor, other.dividerColor, t)!,
      containerBorder: Color.lerp(containerBorder, other.containerBorder, t)!,
      countButtonBackground: Color.lerp(
        countButtonBackground,
        other.countButtonBackground,
        t,
      )!,
      weightVerified: Color.lerp(weightVerified, other.weightVerified, t)!,
      weightWarning: Color.lerp(weightWarning, other.weightWarning, t)!,
      weightDiscrepancy:
          Color.lerp(weightDiscrepancy, other.weightDiscrepancy, t)!,
      brandPrimary: Color.lerp(brandPrimary, other.brandPrimary, t)!,
      brandPrimaryText:
          Color.lerp(brandPrimaryText, other.brandPrimaryText, t)!,
    );
  }
}

/// Convenience extension on [BuildContext] for ergonomic token lookup
extension AppColorThemeX on BuildContext {
  AppColorTheme get colors =>
      Theme.of(this).extension<AppColorTheme>() ?? AppColorTheme.dark;
}
