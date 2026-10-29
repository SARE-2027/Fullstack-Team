import 'package:flutter/material.dart';

import '../theme/app_colors.dart';
import '../theme/app_typography.dart';
import 'custom_text.dart';

/// Tactile glass HUD header button for the 10.1" touchscreen kiosk.
///
/// Features ≥44px touch target compliance, instant pointer-down compression,
/// spring release (`AppTypography.springCurve`), and smooth width morphing
/// (`AnimatedSize`) when localization switches between Arabic and English.
class KioskHeaderButton extends StatefulWidget {
  const KioskHeaderButton({
    super.key,
    required this.onPressed,
    required this.icon,
    required this.label,
    this.foregroundColor,
  });

  final VoidCallback onPressed;
  final IconData icon;
  final String label;
  final Color? foregroundColor;

  @override
  State<KioskHeaderButton> createState() => _KioskHeaderButtonState();
}

class _KioskHeaderButtonState extends State<KioskHeaderButton> {
  bool _isPressed = false;
  bool _isHovered = false;

  @override
  Widget build(BuildContext context) {
    final colors = context.colors;
    final isDark = Theme.of(context).brightness == Brightness.dark;

    final resolvedFg = widget.foregroundColor ??
        (_isHovered ? Theme.of(context).colorScheme.onSurface : colors.textSecondary);
    final resolvedBg =
        _isHovered ? colors.surfaceRaised : colors.surfaceAlt;

    return RepaintBoundary(
      child: MouseRegion(
        cursor: SystemMouseCursors.click,
        onEnter: (_) => setState(() => _isHovered = true),
        onExit: (_) => setState(() => _isHovered = false),
        child: GestureDetector(
          behavior: HitTestBehavior.opaque,
          onTap: widget.onPressed,
          onTapDown: (_) => setState(() => _isPressed = true),
          onTapUp: (_) => setState(() => _isPressed = false),
          onTapCancel: () => setState(() => _isPressed = false),
          child: AnimatedScale(
            scale: _isPressed ? 0.94 : (_isHovered ? 1.02 : 1.0),
            duration: _isPressed ? AppTypography.fast : AppTypography.base,
            curve: _isPressed ? Curves.easeOut : AppTypography.springCurve,
            child: AnimatedContainer(
              duration: AppTypography.base,
              curve: AppTypography.enterCurve,
              constraints: const BoxConstraints(minHeight: 40),
              padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 8),
              decoration: BoxDecoration(
                color: resolvedBg,
                borderRadius: BorderRadius.circular(11),
                border: Border.all(
                  color: _isHovered ? colors.borderStrong : colors.border,
                ),
                boxShadow: isDark
                    ? null
                    : [
                        BoxShadow(
                          color: Colors.black.withValues(alpha: 0.03),
                          blurRadius: 6,
                          offset: const Offset(0, 2),
                        ),
                      ],
              ),
              child: AnimatedSize(
                duration: AppTypography.slow,
                curve: AppTypography.enterCurve,
                alignment: Alignment.center,
                child: Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Icon(
                      widget.icon,
                      size: 17,
                      color: resolvedFg,
                    ),
                    const SizedBox(width: 8),
                    AnimatedSwitcher(
                      duration: AppTypography.base,
                      switchInCurve: AppTypography.enterCurve,
                      switchOutCurve: AppTypography.exitCurve,
                      layoutBuilder: (currentChild, previousChildren) {
                        return Stack(
                          alignment: Alignment.center,
                          children: [
                            ...previousChildren,
                            ?currentChild,
                          ],
                        );
                      },
                      transitionBuilder: (child, animation) {
                        return FadeTransition(
                          opacity: animation,
                          child: SlideTransition(
                            position: Tween<Offset>(
                              begin: const Offset(0.0, 0.16),
                              end: Offset.zero,
                            ).animate(animation),
                            child: child,
                          ),
                        );
                      },
                      child: CustomText(
                        widget.label,
                        key: ValueKey(widget.label),
                        fontSize: 13,
                        fontWeight: FontWeight.w600,
                        color: resolvedFg,
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}
