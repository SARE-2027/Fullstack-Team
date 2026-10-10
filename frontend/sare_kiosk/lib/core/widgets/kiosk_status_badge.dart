import 'package:flutter/material.dart';

import '../theme/app_colors.dart';
import 'custom_text.dart';

/// Pill-shaped status indicator badge for the kiosk header and navigation bars.
/// Smoothly glides width (`AnimatedSize`) and crossfades text (`AnimatedSwitcher`)
/// when language or status updates, with theme-interpolating colors (`AnimatedContainer`).
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
    final resolvedBg = backgroundColor ?? colors.textFieldFill;
    final resolvedFg = textColor ?? colors.secondaryText;

    return Container(
      padding: padding,
      decoration: BoxDecoration(
        color: resolvedBg,
        borderRadius: BorderRadius.circular(10),
        border: borderColor != null ? Border.all(color: borderColor!) : null,
      ),
      child: AnimatedSize(
        duration: const Duration(milliseconds: 280),
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
              duration: const Duration(milliseconds: 220),
              switchInCurve: Curves.easeOutCubic,
              switchOutCurve: Curves.easeInCubic,
              transitionBuilder: (child, animation) => FadeTransition(
                opacity: animation,
                child: child,
              ),
              child: CustomText(
                label,
                key: ValueKey(label),
                fontSize: 13,
                fontWeight: FontWeight.bold,
                color: resolvedFg,
              ),
            ),
          ],
        ),
      ),
    );
  }
}
