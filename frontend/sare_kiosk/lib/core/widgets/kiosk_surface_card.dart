import 'package:flutter/material.dart';

import '../theme/app_colors.dart';
import '../theme/app_typography.dart';

/// Precision Glass Cockpit surface card: the kiosk's foundational material.
///
/// Renders a top-lit anodized/porcelain surface gradient, a subtle specular
/// top rim light (`foregroundDecoration`) that catches overhead store lighting,
/// crisp machined borders, and context-aware depth shadows.
class KioskSurfaceCard extends StatelessWidget {
  const KioskSurfaceCard({
    super.key,
    required this.child,
    this.padding = const EdgeInsets.all(18),
    this.borderRadius = 18,
    this.borderColor,
    this.borderWidth = 1.1,
    this.glow,
    this.backgroundColor,
  });

  final Widget child;
  final EdgeInsetsGeometry padding;
  final double borderRadius;

  /// Border color override (e.g. brand or verified accent for active modules).
  final Color? borderColor;

  final double borderWidth;

  /// Outer ambient glow color for emphasized states.
  final Color? glow;

  /// Optional custom surface fill color.
  final Color? backgroundColor;

  @override
  Widget build(BuildContext context) {
    final colors = context.colors;
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final resolvedBorder = borderColor ?? colors.border;
    final baseSurface = backgroundColor ?? colors.surface;

    final cardGradient = LinearGradient(
      begin: Alignment.topCenter,
      end: Alignment.bottomCenter,
      colors: [
        Color.lerp(baseSurface, Colors.white, isDark ? 0.05 : 0.0)!,
        baseSurface,
      ],
    );

    return AnimatedContainer(
      duration: AppTypography.base,
      curve: AppTypography.enterCurve,
      padding: padding,
      decoration: BoxDecoration(
        gradient: cardGradient,
        borderRadius: BorderRadius.circular(borderRadius),
        border: Border.all(color: resolvedBorder, width: borderWidth),
        boxShadow: _shadows(isDark),
      ),
      foregroundDecoration: isDark
          ? BoxDecoration(
              borderRadius: BorderRadius.circular(borderRadius),
              gradient: LinearGradient(
                begin: Alignment.topCenter,
                end: const Alignment(0, 0.38),
                colors: [
                  Colors.white.withValues(alpha: 0.065),
                  Colors.white.withValues(alpha: 0.0),
                ],
              ),
            )
          : null,
      child: child,
    );
  }

  List<BoxShadow> _shadows(bool isDark) {
    if (glow != null) {
      return [
        BoxShadow(
          color: glow!,
          blurRadius: 26,
          offset: const Offset(0, 8),
          spreadRadius: -5,
        ),
      ];
    }
    if (!isDark) {
      return [
        BoxShadow(
          color: const Color(0xFF0D131D).withValues(alpha: 0.055),
          blurRadius: 16,
          offset: const Offset(0, 6),
          spreadRadius: -2,
        ),
      ];
    }
    return [
      BoxShadow(
        color: Colors.black.withValues(alpha: 0.28),
        blurRadius: 18,
        offset: const Offset(0, 8),
        spreadRadius: -6,
      ),
    ];
  }
}
