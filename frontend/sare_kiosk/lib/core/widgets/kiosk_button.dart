import 'package:flutter/material.dart';

import '../theme/app_colors.dart';
import '../theme/app_typography.dart';
import 'custom_text.dart';

enum KioskButtonVariant { primary, secondary, outlined, danger }

/// Large, tactile button optimized for 10.1" supermarket touchscreen kiosks.
///
/// The primary variant carries the brand emerald gradient with a soft glow —
/// it is the single element on any screen allowed to shout. Press feedback is
/// a quick sponge scale; loading swaps the icon slot for a spinner without
/// shifting the layout.
class KioskButton extends StatefulWidget {
  const KioskButton({
    super.key,
    required this.text,
    required this.onPressed,
    this.icon,
    this.variant = KioskButtonVariant.primary,
    this.isLoading = false,
    this.height = 56.0,
    this.width,
    this.borderRadius = 14.0,
  });

  final String text;
  final VoidCallback? onPressed;
  final IconData? icon;
  final KioskButtonVariant variant;
  final bool isLoading;
  final double height;
  final double? width;
  final double borderRadius;

  @override
  State<KioskButton> createState() => _KioskButtonState();
}

class _KioskButtonState extends State<KioskButton> {
  bool _pressed = false;

  bool get _enabled => widget.onPressed != null && !widget.isLoading;

  /// Top-sheen strength per variant — strongest on the filled CTAs.
  double get _sheenAlpha {
    switch (widget.variant) {
      case KioskButtonVariant.primary:
        return 0.16;
      case KioskButtonVariant.danger:
        return 0.14;
      case KioskButtonVariant.secondary:
        return 0.05;
      case KioskButtonVariant.outlined:
        return 0.0;
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final colors = context.colors;

    final Color background;
    final Color foreground;
    final BorderSide? borderSide;
    final LinearGradient? gradient;

    switch (widget.variant) {
      case KioskButtonVariant.primary:
        background = colors.brand;
        foreground = colors.onBrand;
        borderSide = null;
        gradient = colors.brandGradient;
      case KioskButtonVariant.secondary:
        background = colors.surfaceAlt;
        foreground = theme.colorScheme.onSurface;
        borderSide = BorderSide(color: colors.borderStrong);
        gradient = null;
      case KioskButtonVariant.outlined:
        background = Colors.transparent;
        foreground = theme.colorScheme.onSurface;
        borderSide = BorderSide(color: colors.borderStrong, width: 1.4);
        gradient = null;
      case KioskButtonVariant.danger:
        background = colors.danger;
        foreground = colors.onDanger;
        borderSide = null;
        gradient = null;
    }

    final glow = widget.variant == KioskButtonVariant.primary && _enabled
        ? [
            BoxShadow(
              color: colors.brandGlow,
              blurRadius: 20,
              offset: const Offset(0, 6),
              spreadRadius: -4,
            ),
          ]
        : <BoxShadow>[];

    final content = Row(
      mainAxisSize: MainAxisSize.min,
      mainAxisAlignment: MainAxisAlignment.center,
      children: [
        if (widget.isLoading) ...[
          SizedBox(
            width: 22,
            height: 22,
            child: CircularProgressIndicator(
              strokeWidth: 2.5,
              strokeCap: StrokeCap.round,
              valueColor: AlwaysStoppedAnimation<Color>(foreground),
            ),
          ),
          const SizedBox(width: 12),
        ] else if (widget.icon != null) ...[
          AnimatedSwitcher(
            duration: AppTypography.base,
            switchInCurve: AppTypography.enterCurve,
            switchOutCurve: AppTypography.exitCurve,
            transitionBuilder: (child, animation) => ScaleTransition(
              scale: animation,
              child: FadeTransition(opacity: animation, child: child),
            ),
            child: Icon(
              widget.icon,
              key: ValueKey(widget.icon),
              size: 22,
              color: foreground,
            ),
          ),
          const SizedBox(width: 10),
        ],
        Flexible(
          child: FittedBox(
            fit: BoxFit.scaleDown,
            child: AnimatedSwitcher(
              duration: AppTypography.base,
              switchInCurve: AppTypography.enterCurve,
              switchOutCurve: AppTypography.exitCurve,
              transitionBuilder: (child, animation) =>
                  FadeTransition(opacity: animation, child: child),
              child: CustomText(
                widget.text,
                key: ValueKey(widget.text),
                fontSize: 16,
                fontWeight: FontWeight.w700,
                color: foreground,
                maxLines: 1,
              ),
            ),
          ),
        ),
      ],
    );

    return IgnorePointer(
      ignoring: !_enabled,
      child: MouseRegion(
        cursor: _enabled ? SystemMouseCursors.click : MouseCursor.defer,
        child: Listener(
          onPointerDown: (_) => setState(() => _pressed = true),
          onPointerUp: (_) => setState(() => _pressed = false),
          onPointerCancel: (_) => setState(() => _pressed = false),
          child: AnimatedScale(
            scale: _pressed ? 0.97 : 1.0,
            duration: AppTypography.fast,
            curve: AppTypography.enterCurve,
            child: AnimatedContainer(
              duration: AppTypography.base,
              curve: AppTypography.enterCurve,
              decoration: BoxDecoration(
                color: gradient == null ? background : null,
                gradient: gradient,
                borderRadius: BorderRadius.circular(widget.borderRadius),
                border: Border.fromBorderSide(
                  borderSide ?? BorderSide.none,
                ),
                boxShadow: _pressed ? [] : glow,
              ),
              // Rim light: a faint top sheen that makes the button read as
              // physical hardware lit from above.
              foregroundDecoration: BoxDecoration(
                borderRadius: BorderRadius.circular(widget.borderRadius),
                gradient: LinearGradient(
                  begin: Alignment.topCenter,
                  end: const Alignment(0, 0.45),
                  colors: [
                    Colors.white.withValues(alpha: _sheenAlpha),
                    Colors.white.withValues(alpha: 0.0),
                  ],
                ),
              ),
              child: AnimatedOpacity(
                duration: AppTypography.base,
                opacity: _enabled ? 1 : 0.55,
                child: SizedBox(
                  height: widget.height,
                  width: widget.width,
                  child: ElevatedButton(
                    onPressed: widget.isLoading ? null : widget.onPressed,
                    style: ElevatedButton.styleFrom(
                      elevation: 0,
                      backgroundColor: Colors.transparent,
                      shadowColor: Colors.transparent,
                      foregroundColor: foreground,
                      disabledBackgroundColor: Colors.transparent,
                      disabledForegroundColor: foreground,
                      overlayColor: Colors.white.withValues(alpha: 0.10),
                      padding: const EdgeInsets.symmetric(
                        horizontal: 24,
                        vertical: 12,
                      ),
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(widget.borderRadius),
                      ),
                    ),
                    child: content,
                  ),
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}
