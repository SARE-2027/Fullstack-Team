import 'package:flutter/material.dart';

import '../theme/app_colors.dart';
import '../theme/app_typography.dart';
import 'custom_text.dart';

/// Pill-shaped status indicator for the kiosk header and inline telemetry.
/// Glides width (`AnimatedSize`) and crossfades text (`AnimatedSwitcher`)
/// when language or status updates.
class KioskStatusBadge extends StatelessWidget {
  const KioskStatusBadge({
    super.key,
    required this.label,
    this.leading,
    this.backgroundColor,
    this.borderColor,
    this.textColor,
    this.padding = const EdgeInsets.symmetric(horizontal: 14, vertical: 8),
  });

  final String label;
  final Widget? leading;
  final Color? backgroundColor;
  final Color? borderColor;
  final Color? textColor;
  final EdgeInsetsGeometry padding;

  @override
  Widget build(BuildContext context) {
    final colors = context.colors;
    final resolvedBg = backgroundColor ?? colors.surfaceAlt;
    final resolvedBorder = borderColor ?? colors.border;
    final resolvedFg = textColor ?? colors.textSecondary;

    return RepaintBoundary(
      child: Container(
        padding: padding,
        decoration: BoxDecoration(
          color: resolvedBg,
          borderRadius: BorderRadius.circular(10),
          border: Border.all(color: resolvedBorder),
        ),
        child: AnimatedSize(
          duration: AppTypography.slow,
          curve: Curves.easeInOutCubic,
          alignment: Alignment.center,
          child: Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              if (leading != null) ...[
                leading!,
                const SizedBox(width: 8),
              ],
              AnimatedSwitcher(
                duration: AppTypography.base,
                switchInCurve: AppTypography.enterCurve,
                switchOutCurve: AppTypography.exitCurve,
                transitionBuilder: (child, animation) =>
                    FadeTransition(opacity: animation, child: child),
                child: CustomText(
                  label,
                  key: ValueKey(label),
                  fontSize: 13,
                  fontWeight: FontWeight.w700,
                  color: resolvedFg,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
