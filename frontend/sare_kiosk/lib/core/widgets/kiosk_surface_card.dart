import 'package:flutter/material.dart';

import '../theme/app_colors.dart';
import '../theme/app_typography.dart';

/// Premium layered surface: the kiosk's card language.
///
/// Every panel in the product draws through this one component so depth
/// stays consistent: a subtle vertical gradient (lit from above), a soft top
/// sheen via [foregroundDecoration] (the "rim light" that reads as premium
/// hardware), an optional outer glow for selected states, and — in light
/// mode — a quiet drop shadow instead of the sheen.
class KioskSurfaceCard extends StatelessWidget {
  const KioskSurfaceCard({
    super.key,
    required this.child,
    this.padding = const EdgeInsets.all(18),
    this.borderRadius = 18,
    this.borderColor,
    this.borderWidth = 1,
    this.glow,
  });

  final Widget child;
  final EdgeInsetsGeometry padding;
  final double borderRadius;

  /// Border color override (e.g. brand for selected tiles).
  final Color? borderColor;

  final double borderWidth;

  /// Outer glow color for selected/emphasized states (e.g. [AppColorTheme.brandGlow]).
  final Color? glow;

  @override
  Widget build(BuildContext context) {
    final colors = context.colors;
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final resolvedBorder = borderColor ?? colors.border;

    final cardGradient = LinearGradient(
      begin: Alignment.topCenter,
      end: Alignment.bottomCenter,
      colors: [
        Color.lerp(colors.surface, Colors.white, isDark ? 0.045 : 0.0)!,
        colors.surface,
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
        boxShadow: _shadows(context, isDark),
      ),
      foregroundDecoration: isDark
          ? BoxDecoration(
              borderRadius: BorderRadius.circular(borderRadius),
              gradient: LinearGradient(
                begin: Alignment.topCenter,
                end: const Alignment(0, 0.4),
                colors: [
                  Colors.white.withValues(alpha: 0.06),
                  Colors.white.withValues(alpha: 0.0),
                ],
              ),
            )
          : null,
      child: child,
    );
  }

  List<BoxShadow> _shadows(BuildContext context, bool isDark) {
    if (glow != null) {
      return [
        BoxShadow(
          color: glow!,
          blurRadius: 24,
          offset: const Offset(0, 8),
          spreadRadius: -6,
        ),
      ];
    }
    if (!isDark) {
      return [
        BoxShadow(
          color: Colors.black.withValues(alpha: 0.05),
          blurRadius: 14,
          offset: const Offset(0, 5),
        ),
      ];
    }
    return const [];
  }
}
