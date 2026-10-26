import 'package:flutter/material.dart';

import '../theme/app_colors.dart';
import '../theme/app_typography.dart';
import 'custom_text.dart';

/// Tactile, micro-animated header button for the 10.1" touchscreen kiosk.
/// Features smooth size morphing (`AnimatedSize`) when localization shifts
/// character width, icon scale crossfades (`AnimatedSwitcher`), and sponge
/// press feedback (`AnimatedScale`).
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

    final resolvedFg = widget.foregroundColor ?? colors.textSecondary;
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
            duration: AppTypography.fast,
            curve: AppTypography.enterCurve,
            child: AnimatedContainer(
              duration: AppTypography.base,
              curve: AppTypography.enterCurve,
              padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 8),
              decoration: BoxDecoration(
                color: resolvedBg,
                borderRadius: BorderRadius.circular(10),
                border: Border.all(
                  color: _isHovered ? colors.borderStrong : colors.border,
                ),
              ),
              child: AnimatedSize(
                duration: AppTypography.slow,
                curve: Curves.easeInOutCubic,
                alignment: Alignment.center,
                child: Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Icon(
                      widget.icon,
                      size: 18,
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
                              begin: const Offset(0.0, 0.15),
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
