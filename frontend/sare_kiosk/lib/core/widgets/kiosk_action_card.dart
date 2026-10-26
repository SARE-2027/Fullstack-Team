import 'package:flutter/material.dart';

import '../theme/app_colors.dart';
import '../theme/app_typography.dart';
import 'custom_text.dart';

/// Reusable action & selection card for the kiosk workflow.
///
/// Used for Welcome options (Guest vs Membership) and other two-choice
/// moments. The primary variant carries the brand accent; both variants lift
/// slightly on hover and crossfade text on localization changes.
class KioskActionCard extends StatefulWidget {
  const KioskActionCard({
    super.key,
    required this.title,
    required this.description,
    required this.icon,
    required this.actionButton,
    this.isPrimary = false,
    this.padding = const EdgeInsets.all(24),
    this.borderRadius = 20.0,
  });

  final String title;
  final String description;
  final IconData icon;
  final Widget actionButton;
  final bool isPrimary;
  final EdgeInsetsGeometry padding;
  final double borderRadius;

  @override
  State<KioskActionCard> createState() => _KioskActionCardState();
}

class _KioskActionCardState extends State<KioskActionCard> {
  bool _hovered = false;

  @override
  Widget build(BuildContext context) {
    final colors = context.colors;
    final theme = Theme.of(context);

    final accent = theme.colorScheme.primary;
    final resolvedBorder = _hovered
        ? (widget.isPrimary ? accent : colors.borderStrong)
        : (widget.isPrimary ? accent.withValues(alpha: 0.55) : colors.border);
    final resolvedBorderWidth = widget.isPrimary ? 1.6 : 1.2;

    return MouseRegion(
      cursor: SystemMouseCursors.click,
      onEnter: (_) => setState(() => _hovered = true),
      onExit: (_) => setState(() => _hovered = false),
      child: AnimatedContainer(
        duration: AppTypography.base,
        curve: AppTypography.enterCurve,
        transform: Matrix4.translationValues(0, _hovered ? -3 : 0, 0),
        padding: widget.padding,
        decoration: BoxDecoration(
          color: colors.surface,
          borderRadius: BorderRadius.circular(widget.borderRadius),
          border: Border.all(
            color: resolvedBorder,
            width: resolvedBorderWidth,
          ),
          boxShadow: [
            if (widget.isPrimary)
              BoxShadow(
                color: colors.brandGlow.withValues(alpha: _hovered ? 1 : 0.55),
                blurRadius: _hovered ? 26 : 18,
                offset: const Offset(0, 8),
                spreadRadius: -6,
              ),
          ],
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                AnimatedContainer(
                  duration: AppTypography.base,
                  curve: AppTypography.enterCurve,
                  padding: const EdgeInsets.all(11),
                  decoration: BoxDecoration(
                    gradient:
                        widget.isPrimary ? colors.brandGradient : null,
                    color: widget.isPrimary
                        ? null
                        : colors.surfaceAlt,
                    borderRadius: BorderRadius.circular(14),
                    border: widget.isPrimary
                        ? null
                        : Border.all(color: colors.border),
                  ),
                  child: Icon(
                    widget.icon,
                    size: 22,
                    color: widget.isPrimary
                        ? colors.onBrand
                        : colors.textSecondary,
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: AnimatedSwitcher(
                    duration: AppTypography.base,
                    layoutBuilder: (current, previous) => Stack(
                      alignment: AlignmentDirectional.centerStart,
                      children: [...previous, ?current],
                    ),
                    transitionBuilder: (child, animation) => FadeTransition(
                      opacity: animation,
                      child: child,
                    ),
                    child: CustomText(
                      widget.title,
                      key: ValueKey(widget.title),
                      fontSize: 18,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 12),
            AnimatedSwitcher(
              duration: AppTypography.base,
              layoutBuilder: (current, previous) => Stack(
                alignment: AlignmentDirectional.topStart,
                children: [...previous, ?current],
              ),
              transitionBuilder: (child, animation) => FadeTransition(
                opacity: animation,
                child: child,
              ),
              child: CustomText(
                widget.description,
                key: ValueKey(widget.description),
                fontSize: 13,
                color: colors.textSecondary,
                maxLines: 2,
              ),
            ),
            const Spacer(),
            const SizedBox(height: 18),
            Row(
              children: [
                Expanded(child: widget.actionButton),
              ],
            ),
          ],
        ),
      ),
    );
  }
}
