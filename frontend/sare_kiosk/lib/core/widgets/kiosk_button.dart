import 'package:flutter/material.dart';

import '../theme/app_colors.dart';
import '../theme/app_typography.dart';
import 'custom_text.dart';

enum KioskButtonVariant { primary, secondary, outlined, danger }

/// Tactile cockpit button engineered for the 10.1" supermarket touchscreen.
///
/// Follows Apple's *Designing Fluid Interfaces* response law:
/// - Instant `<16ms` highlight and scale down (`0.965`) on `onPointerDown`.
/// - Snappy spring release (`AppTypography.springCurve`) on pointer up.
/// - Top specular rim light that reads as physical hardware lit from above.
class KioskButton extends StatefulWidget {
  const KioskButton({
    super.key,
    required this.text,
    required this.onPressed,
    this.icon,
    this.variant = KioskButtonVariant.primary,
    this.isLoading = false,
    this.height = 54.0,
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
  bool _hovered = false;

  bool get _enabled => widget.onPressed != null && !widget.isLoading;

  double get _sheenAlpha {
    switch (widget.variant) {
      case KioskButtonVariant.primary:
        return _pressed ? 0.08 : 0.18;
      case KioskButtonVariant.danger:
        return _pressed ? 0.06 : 0.15;
      case KioskButtonVariant.secondary:
        return 0.06;
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
        background = colors.brandDeep;
        foreground = colors.onBrand;
        borderSide = BorderSide(
          color: Colors.white.withValues(alpha: 0.18),
          width: 1.0,
        );
        gradient = colors.brandGradient;
      case KioskButtonVariant.secondary:
        background = _hovered ? colors.surfaceRaised : colors.surfaceAlt;
        foreground = theme.colorScheme.onSurface;
        borderSide = BorderSide(
          color: _hovered ? colors.brand.withValues(alpha: 0.45) : colors.borderStrong,
        );
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
              blurRadius: _hovered ? 24 : 18,
              offset: const Offset(0, 6),
              spreadRadius: -3,
            ),
          ]
        : <BoxShadow>[];

    final content = Row(
      mainAxisSize: MainAxisSize.min,
      mainAxisAlignment: MainAxisAlignment.center,
      children: [
        if (widget.isLoading) ...[
          SizedBox(
            width: 20,
            height: 20,
            child: CircularProgressIndicator(
              strokeWidth: 2.4,
              strokeCap: StrokeCap.round,
              valueColor: AlwaysStoppedAnimation<Color>(foreground),
            ),
          ),
          const SizedBox(width: 10),
        ] else if (widget.icon != null) ...[
          AnimatedSwitcher(
            duration: AppTypography.base,
            switchInCurve: AppTypography.springCurve,
            switchOutCurve: AppTypography.exitCurve,
            transitionBuilder: (child, animation) => ScaleTransition(
              scale: animation,
              child: FadeTransition(opacity: animation, child: child),
            ),
            child: Icon(
              widget.icon,
              key: ValueKey(widget.icon),
              size: 20,
              color: foreground,
            ),
          ),
          const SizedBox(width: 9),
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
                fontSize: 15.5,
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
        onEnter: (_) => setState(() => _hovered = true),
        onExit: (_) => setState(() => _hovered = false),
        child: Listener(
          onPointerDown: (_) => setState(() => _pressed = true),
          onPointerUp: (_) => setState(() => _pressed = false),
          onPointerCancel: (_) => setState(() => _pressed = false),
          child: AnimatedScale(
            scale: _pressed ? 0.965 : (_hovered && _enabled ? 1.01 : 1.0),
            duration: _pressed ? AppTypography.fast : AppTypography.base,
            curve: _pressed ? Curves.easeOut : AppTypography.springCurve,
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
                boxShadow: _pressed ? const [] : glow,
              ),
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
                opacity: _enabled ? 1.0 : 0.50,
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
                        horizontal: 20,
                        vertical: 10,
                      ),
                      shape: RoundedRectangleBorder(
                        borderRadius:
                            BorderRadius.circular(widget.borderRadius),
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
