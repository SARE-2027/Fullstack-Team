import 'package:flutter/material.dart';

import '../theme/app_colors.dart';

/// Serene, Apple-inspired SARE Brand Mark:
/// A soft breathing optical halo framing a depth-lit sapphire squircle with
/// specular top rim lighting.
///
/// The breathing halo is isolated inside its own [RepaintBoundary] so 120 FPS
/// frame budgets (<8.3ms) are never impacted.
class KioskBrandMark extends StatefulWidget {
  const KioskBrandMark({
    super.key,
    this.size = 136,
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
      duration: const Duration(milliseconds: 3200),
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
    final markSize = widget.size * 0.58;

    return RepaintBoundary(
      child: SizedBox(
        width: widget.size,
        height: widget.size,
        child: Stack(
          alignment: Alignment.center,
          children: [
            // Soft outer glass ring
            Container(
              width: widget.size - 8,
              height: widget.size - 8,
              decoration: BoxDecoration(
                shape: BoxShape.circle,
                border: Border.all(
                  color: colors.border.withValues(alpha: 0.75),
                  width: 1.0,
                ),
              ),
            ),
            // Breathing optical halo — isolated inside its own layer
            RepaintBoundary(
              child: AnimatedBuilder(
                animation: _breath,
                builder: (context, _) {
                  final t = Curves.easeInOut.transform(_breath.value);
                  return Container(
                    width: markSize + 24,
                    height: markSize + 24,
                    decoration: BoxDecoration(
                      shape: BoxShape.circle,
                      color: colors.brand.withValues(alpha: 0.04 + (t * 0.05)),
                      border: Border.all(
                        color: colors.brand.withValues(alpha: 0.14 + (t * 0.22)),
                        width: 1.2,
                      ),
                    ),
                  );
                },
              ),
            ),
            // Center mark: depth-lit sapphire squircle with specular top rim
            Container(
              width: markSize,
              height: markSize,
              decoration: BoxDecoration(
                gradient: colors.brandGradient,
                borderRadius: BorderRadius.circular(markSize * 0.30),
                border: Border.all(
                  color: Colors.white.withValues(alpha: 0.24),
                  width: 1.2,
                ),
                boxShadow: [
                  BoxShadow(
                    color: colors.brandGlow,
                    blurRadius: 28,
                    offset: const Offset(0, 10),
                    spreadRadius: -4,
                  ),
                ],
              ),
              foregroundDecoration: BoxDecoration(
                borderRadius: BorderRadius.circular(markSize * 0.30),
                gradient: LinearGradient(
                  begin: Alignment.topCenter,
                  end: const Alignment(0, 0.35),
                  colors: [
                    Colors.white.withValues(alpha: 0.24),
                    Colors.white.withValues(alpha: 0.0),
                  ],
                ),
              ),
              child: Center(
                child: Icon(
                  Icons.shopping_bag_rounded,
                  size: markSize * 0.46,
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
