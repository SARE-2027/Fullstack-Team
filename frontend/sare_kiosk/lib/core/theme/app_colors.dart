import 'package:flutter/material.dart';

/// SARE "Precision Glass Cockpit" & "Fresh-Verify" design tokens.
///
/// Engineered for the 10.1" (1280×800) cart-mounted touchscreen:
/// - Deep obsidian-titanium & crisp porcelain-alabaster surfaces stay quiet so
///   functional color signals read instantly from arm's length.
/// - Electric cobalt (`brand`) drives primary actions, focus rings, and optical
///   viewfinders.
/// - Emerald (`verified`) is reserved strictly for sensor-fusion confirmation
///   (±15g load-cell pass, verified item badges, and the exit gate pass).
/// - Amber (`warning`) signals calibration or timer hold.
/// - Crimson (`danger`) signals weight discrepancy (> ±15g fraud alert).
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

  // ── Neutral surfaces (anodized obsidian / porcelain alabaster) ───────────
  /// Primary module & panel background.
  final Color surface;

  /// Recessed input wells, secondary chips, and nested telemetry slots.
  final Color surfaceAlt;

  /// Elevated hover / pressed / floating HUD layer.
  final Color surfaceRaised;

  /// Precision hairline separation.
  final Color border;

  /// Emphasized instrument bezel for interactive bounds.
  final Color borderStrong;

  // ── Text hierarchy (WCAG ≥ 4.5:1 verified across both themes) ────────────
  final Color textSecondary;
  final Color textFaint;

  // ── Brand (Electric Sapphire / Royal Cobalt) ─────────────────────────────
  /// Brand accent for text & iconography — tuned for ≥ 4.5:1 contrast.
  final Color brand;

  /// Deep cobalt fill for primary CTAs and active selection badges.
  final Color brandDeep;
  final Color onBrand;
  final Color brandGradientStart;
  final Color brandGradientEnd;

  /// Ambient optical glow behind primary CTAs, focus rings, and cockpit wash.
  final Color brandGlow;

  // ── Sensor-Fusion Verification Semantics ─────────────────────────────────
  /// Load-cell weight verified (±15g gate passed) — "GO".
  final Color verified;

  /// Attention — calibration hold, paused auto-reset, degraded sensor.
  final Color warning;

  /// Weight discrepancy (> ±15g fraud alert) or destructive action — "STOP".
  final Color danger;

  /// High-contrast foreground on top of a [danger] fill.
  final Color onDanger;

  /// Informational telemetry accent (scanning in progress, hints).
  final Color info;

  /// Modal backdrop scrim with progressive depth dimming.
  final Color scrim;

  LinearGradient get brandGradient => LinearGradient(
        begin: Alignment.topLeft,
        end: Alignment.bottomRight,
        colors: [brandGradientStart, brandGradientEnd],
      );

  LinearGradient get verifiedGradient => LinearGradient(
        begin: Alignment.topLeft,
        end: Alignment.bottomRight,
        colors: [
          verified.withValues(alpha: 0.20),
          verified.withValues(alpha: 0.06),
        ],
      );

  LinearGradient get dangerGradient => LinearGradient(
        begin: Alignment.topLeft,
        end: Alignment.bottomRight,
        colors: [
          danger.withValues(alpha: 0.22),
          danger.withValues(alpha: 0.07),
        ],
      );

  /// Dark mode palette — "Obsidian Titanium Cockpit" (default for the 10.1"
  /// supermarket touchscreen under bright overhead store lighting).
  static const dark = AppColorTheme(
    surface: Color(0xFF111620),
    surfaceAlt: Color(0xFF171E2B),
    surfaceRaised: Color(0xFF1F2838),
    border: Color(0xFF253042),
    borderStrong: Color(0xFF384761),
    textSecondary: Color(0xFFA3B1C6),
    textFaint: Color(0xFF7D8CA3),
    brand: Color(0xFF60A5FA),
    brandDeep: Color(0xFF2563EB),
    onBrand: Color(0xFFFFFFFF),
    brandGradientStart: Color(0xFF3B82F6),
    brandGradientEnd: Color(0xFF1D4ED8),
    brandGlow: Color(0x3D2563EB),
    verified: Color(0xFF34D399),
    warning: Color(0xFFFBBF24),
    danger: Color(0xFFF87171),
    onDanger: Color(0xFF280808),
    info: Color(0xFF38BDF8),
    scrim: Color(0xBD04070D),
  );

  /// Light mode palette — "Porcelain Alabaster Studio" (high-contrast daylight
  /// mode with crisp machined silver borders).
  static const light = AppColorTheme(
    surface: Color(0xFFFFFFFF),
    surfaceAlt: Color(0xFFEEF2F7),
    surfaceRaised: Color(0xFFE3E9F2),
    border: Color(0xFFD8E0EC),
    borderStrong: Color(0xFFB4C0D3),
    textSecondary: Color(0xFF4A5568),
    textFaint: Color(0xFF64748B),
    brand: Color(0xFF1D4ED8),
    brandDeep: Color(0xFF2563EB),
    onBrand: Color(0xFFFFFFFF),
    brandGradientStart: Color(0xFF2563EB),
    brandGradientEnd: Color(0xFF1D4ED8),
    brandGlow: Color(0x2E1D4ED8),
    verified: Color(0xFF047857),
    warning: Color(0xFFB45309),
    danger: Color(0xFFDC2626),
    onDanger: Color(0xFFFFFFFF),
    info: Color(0xFF0369A1),
    scrim: Color(0x6B0B111E),
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
      brandGradientStart:
          Color.lerp(brandGradientStart, other.brandGradientStart, t)!,
      brandGradientEnd:
          Color.lerp(brandGradientEnd, other.brandGradientEnd, t)!,
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
