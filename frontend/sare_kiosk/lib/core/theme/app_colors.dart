import 'package:flutter/material.dart';

/// SARE "Fresh-Verify" design tokens.
///
/// The entire interface stays quiet — charcoal/porcelain monochrome surfaces —
/// and COLOR MEANS VERIFICATION: emerald = verified / go, amber = attention,
/// red = weight mismatch / stop. Emerald doubles as the brand color because
/// weight verification is the product's core promise.
@immutable
class AppColorTheme extends ThemeExtension<AppColorTheme> {
  const AppColorTheme({
    required this.surface,
    required this.surfaceAlt,
    required this.surfaceRaised,
    required this.border,
    required this.borderStrong,
    required this.textSecondary,
    required this.textFaint,
    required this.brand,
    required this.brandDeep,
    required this.onBrand,
    required this.brandGradientStart,
    required this.brandGradientEnd,
    required this.brandGlow,
    required this.verified,
    required this.warning,
    required this.danger,
    required this.onDanger,
    required this.info,
    required this.scrim,
  });

  // ── Neutral surfaces (quiet monochrome) ──────────────────────────────────
  /// Card / panel background.
  final Color surface;

  /// Inputs, nested chips, secondary panels.
  final Color surfaceAlt;

  /// Hover / pressed raised layer.
  final Color surfaceRaised;

  /// Hairline separation.
  final Color border;

  /// Emphasized outline for interactive bounds.
  final Color borderStrong;

  // ── Text ─────────────────────────────────────────────────────────────────
  final Color textSecondary;
  final Color textFaint;

  // ── Brand / verification semantic colors ─────────────────────────────────
  /// Primary interactive + "verified" state color.
  final Color brand;
  final Color brandDeep;
  final Color onBrand;
  final Color brandGradientStart;
  final Color brandGradientEnd;

  /// Soft glow used behind primary CTAs and focus rings.
  final Color brandGlow;

  /// Weight verified (±15g gate passed) — "go".
  final Color verified;

  /// Attention — session issues, degraded hardware.
  final Color warning;

  /// Weight mismatch / destructive — "stop".
  final Color danger;

  /// Text/icon color on top of a [danger] fill (dark mode uses a light red
  /// fill, so it needs dark text to keep ≥4.5:1 contrast).
  final Color onDanger;

  /// Neutral informational accent (progress, hints).
  final Color info;

  /// Dialog / modal scrim over the scaffold.
  final Color scrim;

  LinearGradient get brandGradient => LinearGradient(
        begin: Alignment.topCenter,
        end: Alignment.bottomCenter,
        colors: [brandGradientStart, brandGradientEnd],
      );

  /// Dark mode palette — default for the 10.1" supermarket touchscreen.
  /// Deep charcoal with a green undertone (never pure black) so emerald
  /// status reads instantly under bright store lighting.
  static const dark = AppColorTheme(
    surface: Color(0xFF141C18),
    surfaceAlt: Color(0xFF1B2620),
    surfaceRaised: Color(0xFF212E27),
    border: Color(0xFF2C3B33),
    borderStrong: Color(0xFF41574B),
    textSecondary: Color(0xFF9FB3A8),
    textFaint: Color(0xFF6E8278),
    brand: Color(0xFF34D399),
    brandDeep: Color(0xFF059669),
    onBrand: Color(0xFF052E20),
    brandGradientStart: Color(0xFF059669),
    brandGradientEnd: Color(0xFF34D399),
    brandGlow: Color(0x332ED596),
    verified: Color(0xFF34D399),
    warning: Color(0xFFFBBF24),
    danger: Color(0xFFF87171),
    onDanger: Color(0xFF2A0B0B),
    info: Color(0xFF38BDF8),
    scrim: Color(0xB30A100D),
  );

  /// Light mode palette — porcelain with a green hint, crisp daytime option.
  static const light = AppColorTheme(
    surface: Color(0xFFFFFFFF),
    surfaceAlt: Color(0xFFECF3EE),
    surfaceRaised: Color(0xFFE3EDE6),
    border: Color(0xFFD8E4DC),
    borderStrong: Color(0xFFB4CBBE),
    textSecondary: Color(0xFF56705F),
    textFaint: Color(0xFF8AA394),
    brand: Color(0xFF047857),
    brandDeep: Color(0xFF059669),
    onBrand: Color(0xFFFFFFFF),
    brandGradientStart: Color(0xFF059669),
    brandGradientEnd: Color(0xFF0AAF7C),
    brandGlow: Color(0x2E059669),
    verified: Color(0xFF059669),
    warning: Color(0xFFB45309),
    danger: Color(0xFFDC2626),
    onDanger: Color(0xFFFFFFFF),
    info: Color(0xFF0369A1),
    scrim: Color(0x66112219),
  );

  @override
  AppColorTheme copyWith({
    Color? surface,
    Color? surfaceAlt,
    Color? surfaceRaised,
    Color? border,
    Color? borderStrong,
    Color? textSecondary,
    Color? textFaint,
    Color? brand,
    Color? brandDeep,
    Color? onBrand,
    Color? brandGradientStart,
    Color? brandGradientEnd,
    Color? brandGlow,
    Color? verified,
    Color? warning,
    Color? danger,
    Color? onDanger,
    Color? info,
    Color? scrim,
  }) {
    return AppColorTheme(
      surface: surface ?? this.surface,
      surfaceAlt: surfaceAlt ?? this.surfaceAlt,
      surfaceRaised: surfaceRaised ?? this.surfaceRaised,
      border: border ?? this.border,
      borderStrong: borderStrong ?? this.borderStrong,
      textSecondary: textSecondary ?? this.textSecondary,
      textFaint: textFaint ?? this.textFaint,
      brand: brand ?? this.brand,
      brandDeep: brandDeep ?? this.brandDeep,
      onBrand: onBrand ?? this.onBrand,
      brandGradientStart: brandGradientStart ?? this.brandGradientStart,
      brandGradientEnd: brandGradientEnd ?? this.brandGradientEnd,
      brandGlow: brandGlow ?? this.brandGlow,
      verified: verified ?? this.verified,
      warning: warning ?? this.warning,
      danger: danger ?? this.danger,
      onDanger: onDanger ?? this.onDanger,
      info: info ?? this.info,
      scrim: scrim ?? this.scrim,
    );
  }

  @override
  AppColorTheme lerp(ThemeExtension<AppColorTheme>? other, double t) {
    if (other is! AppColorTheme) {
      return this;
    }
    return AppColorTheme(
      surface: Color.lerp(surface, other.surface, t)!,
      surfaceAlt: Color.lerp(surfaceAlt, other.surfaceAlt, t)!,
      surfaceRaised: Color.lerp(surfaceRaised, other.surfaceRaised, t)!,
      border: Color.lerp(border, other.border, t)!,
      borderStrong: Color.lerp(borderStrong, other.borderStrong, t)!,
      textSecondary: Color.lerp(textSecondary, other.textSecondary, t)!,
      textFaint: Color.lerp(textFaint, other.textFaint, t)!,
      brand: Color.lerp(brand, other.brand, t)!,
      brandDeep: Color.lerp(brandDeep, other.brandDeep, t)!,
      onBrand: Color.lerp(onBrand, other.onBrand, t)!,
      brandGradientStart: Color.lerp(brandGradientStart, other.brandGradientStart, t)!,
      brandGradientEnd: Color.lerp(brandGradientEnd, other.brandGradientEnd, t)!,
      brandGlow: Color.lerp(brandGlow, other.brandGlow, t)!,
      verified: Color.lerp(verified, other.verified, t)!,
      warning: Color.lerp(warning, other.warning, t)!,
      danger: Color.lerp(danger, other.danger, t)!,
      onDanger: Color.lerp(onDanger, other.onDanger, t)!,
      info: Color.lerp(info, other.info, t)!,
      scrim: Color.lerp(scrim, other.scrim, t)!,
    );
  }
}

/// Convenience extension on [BuildContext] for ergonomic token lookup.
extension AppColorThemeX on BuildContext {
  AppColorTheme get colors =>
      Theme.of(this).extension<AppColorTheme>() ?? AppColorTheme.dark;
}
