import 'dart:math' as math;

import 'package:flutter/material.dart';

import '../theme/app_colors.dart';

/// The SARE brand mark: a barcode-scanner radar sweeping around the cart
/// glyph — the kiosk's one crafted hero element.
///
/// The sweep is a comet of brand blue drawn with a [SweepGradient] shader on
/// a stroked arc, rotating once every ~14s; two quiet "blip" dots and a static
/// inner arc add depth. Pass `animate: false` (tests / reduced motion) to
/// render the static composition.
class KioskBrandMark extends StatefulWidget {
  const KioskBrandMark({
    super.key,
    this.size = 148,
    this.animate = true,
  });

  final double size;
  final bool animate;

  @override
  State<KioskBrandMark> createState() => _KioskBrandMarkState();
}

class _KioskBrandMarkState extends State<KioskBrandMark>
    with SingleTickerProviderStateMixin {
  late final AnimationController _sweep;

  @override
  void initState() {
    super.initState();
    _sweep = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 14000),
    );
    if (widget.animate) {
      _sweep.repeat();
    } else {
      _sweep.value = 0.18;
    }
  }

  @override
  void didUpdateWidget(KioskBrandMark oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (widget.animate && !oldWidget.animate) {
      _sweep.repeat();
    } else if (!widget.animate && oldWidget.animate) {
      _sweep.stop();
    }
  }

  @override
  void dispose() {
    _sweep.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final colors = context.colors;
    final markSize = widget.size * 0.58;

    return RepaintBoundary(
      child: SizedBox(
        width: widget.size,
        height: widget.size,
        child: AnimatedBuilder(
          animation: _sweep,
          builder: (context, _) => Stack(
            alignment: Alignment.center,
            children: [
              Positioned.fill(
                child: CustomPaint(
                  painter: _RadarPainter(
                    progress: _sweep.value,
                    ring: colors.borderStrong,
                    brand: colors.brand,
                    glow: colors.brandGlow,
                  ),
                ),
              ),
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
      ),
    );
  }
}

class _RadarPainter extends CustomPainter {
  _RadarPainter({
    required this.progress,
    required this.ring,
    required this.brand,
    required this.glow,
  });

  final double progress;
  final Color ring;
  final Color brand;
  final Color glow;

  @override
  void paint(Canvas canvas, Size size) {
    final center = Offset(size.width / 2, size.height / 2);
    final radius = size.shortestSide / 2 - 2;

    // Quiet base ring.
    final ringPaint = Paint()
      ..color = ring
      ..style = PaintingStyle.stroke
      ..strokeWidth = 1.2;
    canvas.drawCircle(center, radius, ringPaint);

    // Static depth arc (lower-left, faint).
    final depthPaint = Paint()
      ..color = Colors.white.withValues(alpha: 0.06)
      ..style = PaintingStyle.stroke
      ..strokeWidth = 2
      ..strokeCap = StrokeCap.round;
    canvas.drawArc(
      Rect.fromCircle(center: center, radius: radius * 0.8),
      math.pi * 0.62,
      math.pi * 0.42,
      false,
      depthPaint,
    );

    // Rotating comet sweep.
    final sweepPaint = Paint()
      ..style = PaintingStyle.stroke
      ..strokeWidth = 2.6
      ..strokeCap = StrokeCap.round
      ..shader = SweepGradient(
        colors: [
          brand.withValues(alpha: 0.0),
          brand.withValues(alpha: 0.0),
          brand.withValues(alpha: 0.75),
          brand.withValues(alpha: 0.0),
        ],
        stops: const [0.0, 0.62, 0.86, 1.0],
      ).createShader(Rect.fromCircle(center: center, radius: radius * 0.92));
    canvas.save();
    canvas.translate(center.dx, center.dy);
    canvas.rotate(progress * 2 * math.pi);
    canvas.translate(-center.dx, -center.dy);
    canvas.drawArc(
      Rect.fromCircle(center: center, radius: radius * 0.92),
      0,
      2 * math.pi,
      false,
      sweepPaint,
    );
    canvas.restore();

    // Two radar blips riding the ring, breathing with the sweep.
    for (final (angle, phase) in [(0.35, 0.0), (3.7, 0.5)]) {
      final breath = 0.5 + 0.5 * math.sin((progress * 2 * math.pi) + phase);
      final blipPaint = Paint()
        ..color = brand.withValues(alpha: 0.25 + breath * 0.55)
        ..style = PaintingStyle.fill;
      final blipCenter = Offset(
        center.dx + math.cos(angle) * radius,
        center.dy + math.sin(angle) * radius,
      );
      canvas.drawCircle(blipCenter, 2.2 + breath * 1.1, blipPaint);
    }
  }

  @override
  bool shouldRepaint(covariant _RadarPainter oldDelegate) =>
      progress != oldDelegate.progress ||
      ring != oldDelegate.ring ||
      brand != oldDelegate.brand ||
      glow != oldDelegate.glow;
}
