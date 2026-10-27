import 'package:flutter/material.dart';

/// Ambient "live telemetry" pulse dot for the load-cell scale readout.
///
/// Breathes with a soft two-phase scale + halo loop. Pass `animate: false`
/// (tests / reduced-motion) to render the static dot only — the animation is
/// the sole infinitely repeating effect in the whole kiosk and is isolated
/// here behind a [RepaintBoundary] so it never triggers a parent repaint.
class KioskPulseDot extends StatefulWidget {
  const KioskPulseDot({
    super.key,
    this.color,
    this.size = 9.0,
    this.animate = true,
  });

  final Color? color;
  final double size;
  final bool animate;

  @override
  State<KioskPulseDot> createState() => _KioskPulseDotState();
}

class _KioskPulseDotState extends State<KioskPulseDot>
    with SingleTickerProviderStateMixin {
  late final AnimationController _controller;

  @override
  void initState() {
    super.initState();
    _controller = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 1900),
    );
    if (widget.animate) {
      _controller.repeat();
    }
  }

  @override
  void didUpdateWidget(KioskPulseDot oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (widget.animate && !oldWidget.animate) {
      _controller.repeat();
    } else if (!widget.animate && oldWidget.animate) {
      _controller.stop();
      _controller.value = 0;
    }
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final dotColor = widget.color ??
        Theme.of(context).colorScheme.primary;

    return RepaintBoundary(
      child: AnimatedBuilder(
        animation: _controller,
        builder: (context, _) {
          // Two-phase breath: scale up with a fading halo, then settle.
          final t = _controller.value;
          final pulse = t < 0.5 ? Curves.easeOutCubic.transform(t * 2) : 0.0;
          final haloOpacity = (1 - (t * 2)).clamp(0.0, 1.0) * 0.45;

          return SizedBox(
            width: widget.size * 3,
            height: widget.size * 3,
            child: Stack(
              alignment: Alignment.center,
              children: [
                if (widget.animate)
                  Container(
                    width: widget.size + pulse * widget.size * 1.6,
                    height: widget.size + pulse * widget.size * 1.6,
                    decoration: BoxDecoration(
                      color: dotColor.withValues(alpha: haloOpacity),
                      shape: BoxShape.circle,
                    ),
                  ),
                Container(
                  width: widget.size,
                  height: widget.size,
                  decoration: BoxDecoration(
                    color: dotColor,
                    shape: BoxShape.circle,
                  ),
                ),
              ],
            ),
          );
        },
      ),
    );
  }
}
