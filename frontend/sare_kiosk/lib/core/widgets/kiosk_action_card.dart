import 'package:flutter/material.dart';

import '../theme/app_colors.dart';
import '../theme/app_typography.dart';
import 'custom_text.dart';
import 'kiosk_surface_card.dart';

/// Tactile cockpit selection card for the SARE kiosk workflow.
///
/// Features Apple-style pointer-down compression (`0.985`), hover lift (`-3px`),
/// a subtle radial corner accent wash on the primary module, and smooth
/// localized text crossfading.
class KioskActionCard extends StatefulWidget {
  const KioskActionCard({
    super.key,
    required this.title,
    required this.description,
    required this.icon,
    required this.actionButton,
    this.isPrimary = false,
    this.padding = const EdgeInsets.all(24),
    this.borderRadius = 22.0,
    this.badgeText,
  });

  final String title;
  final String description;
  final IconData icon;
  final Widget actionButton;
  final bool isPrimary;
  final EdgeInsetsGeometry padding;
  final double borderRadius;
  final String? badgeText;

  @override
  State<KioskActionCard> createState() => _KioskActionCardState();
}

class _KioskActionCardState extends State<KioskActionCard> {
  bool _hovered = false;
  bool _pressed = false;

  @override
  Widget build(BuildContext context) {
    final colors = context.colors;
    final theme = Theme.of(context);

    final accent = theme.colorScheme.primary;
    final resolvedBorder = _hovered
        ? (widget.isPrimary ? colors.brand : colors.borderStrong)
        : (widget.isPrimary ? accent.withValues(alpha: 0.65) : colors.border);
    final resolvedBorderWidth = widget.isPrimary ? 1.6 : 1.2;

    return MouseRegion(
      cursor: SystemMouseCursors.click,
      onEnter: (_) => setState(() => _hovered = true),
      onExit: (_) => setState(() => _hovered = false),
      child: Listener(
        onPointerDown: (_) => setState(() => _pressed = true),
        onPointerUp: (_) => setState(() => _pressed = false),
        onPointerCancel: (_) => setState(() => _pressed = false),
        child: AnimatedScale(
          scale: _pressed ? 0.988 : 1.0,
          duration: _pressed ? AppTypography.fast : AppTypography.base,
          curve: _pressed ? Curves.easeOut : AppTypography.springCurve,
          child: AnimatedContainer(
            duration: AppTypography.base,
            curve: AppTypography.enterCurve,
            transform: Matrix4.translationValues(0, _hovered ? -3 : 0, 0),
            child: KioskSurfaceCard(
              padding: widget.padding,
              borderRadius: widget.borderRadius,
              borderColor: resolvedBorder,
              borderWidth: resolvedBorderWidth,
              glow: widget.isPrimary
                  ? colors.brandGlow.withValues(alpha: _hovered ? 1.0 : 0.60)
                  : null,
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      AnimatedContainer(
                        duration: AppTypography.base,
                        curve: AppTypography.enterCurve,
                        padding: const EdgeInsets.all(12),
                        decoration: BoxDecoration(
                          gradient:
                              widget.isPrimary ? colors.brandGradient : null,
                          color: widget.isPrimary ? null : colors.surfaceAlt,
                          borderRadius: BorderRadius.circular(14),
                          border: widget.isPrimary
                              ? Border.all(
                                  color: Colors.white.withValues(alpha: 0.20),
                                )
                              : Border.all(color: colors.borderStrong),
                        ),
                        child: Icon(
                          widget.icon,
                          size: 22,
                          color: widget.isPrimary
                              ? colors.onBrand
                              : colors.brand,
                        ),
                      ),
                      const SizedBox(width: 14),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            if (widget.badgeText != null) ...[
                              Container(
                                padding: const EdgeInsets.symmetric(
                                  horizontal: 8,
                                  vertical: 2,
                                ),
                                decoration: BoxDecoration(
                                  color: (widget.isPrimary
                                          ? colors.brand
                                          : colors.verified)
                                      .withValues(alpha: 0.14),
                                  borderRadius: BorderRadius.circular(6),
                                ),
                                child: CustomText(
                                  widget.badgeText!,
                                  fontSize: 10.5,
                                  fontWeight: FontWeight.w700,
                                  color: widget.isPrimary
                                      ? colors.brand
                                      : colors.verified,
                                ),
                              ),
                              const SizedBox(height: 4),
                            ],
                            AnimatedSwitcher(
                              duration: AppTypography.base,
                              layoutBuilder: (current, previous) => Stack(
                                alignment: AlignmentDirectional.centerStart,
                                children: [...previous, ?current],
                              ),
                              transitionBuilder: (child, animation) =>
                                  FadeTransition(
                                opacity: animation,
                                child: child,
                              ),
                              child: CustomText(
                                widget.title,
                                key: ValueKey(widget.title),
                                fontSize: 18,
                                fontWeight: FontWeight.w800,
                                maxLines: 1,
                              ),
                            ),
                          ],
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
                      fontSize: 13.5,
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
          ),
        ),
      ),
    );
  }
}
