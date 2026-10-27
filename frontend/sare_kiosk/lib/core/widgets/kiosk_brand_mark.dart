import 'dart:math' as math;

import 'package:flutter/material.dart';

import '../theme/app_colors.dart';

/// The SARE brand mark: the cart glyph on a gradient squircle, framed by a
/// quiet instrument ring that breathes — a slow opacity pulse, Apple-style.
///
/// Deliberately opacity-only: no rotation, no per-frame shaders, nothing that
/// can stutter or jitter. The animated halo lives inside its own
/// [RepaintBoundary] so the pulse repaints nothing else. Pass `animate:
/// false` (tests / reduced motion) to render the calm static composition.
class KioskBrandMark extends StatefulWidget {
  const KioskBrandMark({
    super.key,
    this.size = 150,
    this.animate = true,
  });

  final double size;
  final bool animate;

  @override
  State<KioskBrandMark> createState() => _KioskBrandMarkState();
}

class _KioskBrandMarkState extends State<KioskBrandMark>
    with SingleTickerProviderStateMixin {
  late final AnimationController _breath;

  @override
  void initState() {
    super.initState();
    _breath = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 3400),
    );
    if (widget.animate) {
      _breath.repeat(reverse: true);
    } else {
      _breath.value = 0.65;
    }
  }

  @override
  void didUpdateWidget(KioskBrandMark oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (widget.animate && !oldWidget.animate) {
      _breath.repeat(reverse: true);
    } else if (!widget.animate && oldWidget.animate) {
      _breath.stop();
      _breath.value = 0.65;
    }
  }

  @override
  void dispose() {
    _breath.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final colors = context.colors;
    final markSize = widget.size * 0.56;

    return RepaintBoundary(
      child: SizedBox(
        width: widget.size,
        height: widget.size,
        child: Stack(
          alignment: Alignment.center,
          children: [
            // Static instrument ring: hairline circle with four tick marks.
            Positioned.fill(
              child: CustomPaint(
                painter: _InstrumentRingPainter(
                  ring: colors.borderStrong,
                  tick: colors.textFaint,
                ),
              ),
            ),
            // Breathing halo — the only moving part, opacity alone.
            RepaintBoundary(
              child: AnimatedBuilder(
                animation: _breath,
                builder: (context, _) {
                  final t = Curves.easeInOut.transform(_breath.value);
                  return Container(
                    width: markSize + 26,
                    height: markSize + 26,
                    decoration: BoxDecoration(
                      shape: BoxShape.circle,
                      border: Border.all(
                        color: colors.brand
                            .withValues(alpha: 0.08 + (t * 0.24)),
                        width: 1.5,
                      ),
                    ),
                  );
                },
              ),
            ),
            // Center mark: gradient squircle, cart glyph, soft glow.
            Container(
              width: markSize,
              height: markSize,
              decoration: BoxDecoration(
                gradient: colors.brandGradient,
                borderRadius: BorderRadius.circular(markSize * 0.34),
                boxShadow: [
                  BoxShadow(
                    color: colors.brandGlow,
                    blurRadius: 30,
                    offset: const Offset(0, 10),
                    spreadRadius: -4,
                  ),
                ],
              ),
              child: Center(
                child: Icon(
                  Icons.shopping_cart_checkout_rounded,
                  size: markSize * 0.5,
                  color: colors.onBrand,
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

/// Static frame around the mark — painted once, never repainted.
class _InstrumentRingPainter extends CustomPainter {
  _InstrumentRingPainter({required this.ring, required this.tick});

  final Color ring;
  final Color tick;

  @override
  void paint(Canvas canvas, Size size) {
    final center = Offset(size.width / 2, size.height / 2);
    final radius = size.shortestSide / 2 - 2;

    final ringPaint = Paint()
      ..color = ring
      ..style = PaintingStyle.stroke
      ..strokeWidth = 1.2;
    canvas.drawCircle(center, radius, ringPaint);

    // Four quiet tick marks at the cardinal points.
    final tickPaint = Paint()
      ..color = tick
      ..style = PaintingStyle.stroke
      ..strokeWidth = 2
      ..strokeCap = StrokeCap.round;
    for (var i = 0; i < 4; i++) {
      final angle = i * math.pi / 2;
      final outer = Offset(
        center.dx + math.cos(angle) * radius,
        center.dy + math.sin(angle) * radius,
      );
      final inner = Offset(
        center.dx + math.cos(angle) * (radius - 5),
        center.dy + math.sin(angle) * (radius - 5),
      );
      canvas.drawLine(inner, outer, tickPaint);
    }
  }

  @override
  bool shouldRepaint(covariant _InstrumentRingPainter oldDelegate) =>
      ring != oldDelegate.ring || tick != oldDelegate.tick;
}
