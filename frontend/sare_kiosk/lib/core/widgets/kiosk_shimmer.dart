import 'package:flutter/material.dart';

import '../theme/app_colors.dart';
import '../utils/kiosk_motion.dart';
import 'kiosk_surface_card.dart';

/// Subtle, GPU-isolated shimmer sweep wrapper for smooth state refreshes and
/// item-update highlights without harsh screen flashes.
class KioskShimmerSweep extends StatefulWidget {
  const KioskShimmerSweep({
    super.key,
    required this.child,
    this.active = false,
    this.triggerKey,
    this.borderRadius = 16,
    this.highlightColor,
  });

  final Widget child;
  final bool active;
  final Object? triggerKey;
  final double borderRadius;
  final Color? highlightColor;

  @override
  State<KioskShimmerSweep> createState() => _KioskShimmerSweepState();
}

class _KioskShimmerSweepState extends State<KioskShimmerSweep>
    with SingleTickerProviderStateMixin {
  late final AnimationController _ctrl;

  @override
  void initState() {
    super.initState();
    _ctrl = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 680),
    );
    if (widget.active) {
      _ctrl.forward(from: 0.0);
    }
  }

  @override
  void didUpdateWidget(covariant KioskShimmerSweep oldWidget) {
    super.didUpdateWidget(oldWidget);
    if ((widget.active && !oldWidget.active) ||
        (widget.triggerKey != null &&
            widget.triggerKey != oldWidget.triggerKey)) {
      _ctrl.forward(from: 0.0);
    }
  }

  @override
  void dispose() {
    _ctrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final colors = context.colors;
    final tint = widget.highlightColor ?? colors.brand;

    return RepaintBoundary(
      child: AnimatedBuilder(
        animation: _ctrl,
        child: widget.child,
        builder: (context, child) {
          if (!_ctrl.isAnimating && _ctrl.value == 0.0) {
            return child!;
          }
          final progress = Curves.easeOutCubic.transform(_ctrl.value);
          if (progress >= 1.0) return child!;

          final slide = -1.2 + (progress * 2.4);
          final alpha = (1.0 - progress) * 0.22;

          return ClipRRect(
            borderRadius: BorderRadius.circular(widget.borderRadius),
            child: Stack(
              children: [
                child!,
                Positioned.fill(
                  child: IgnorePointer(
                    child: DecoratedBox(
                      decoration: BoxDecoration(
                        gradient: LinearGradient(
                          begin: Alignment(slide - 0.55, -0.3),
                          end: Alignment(slide + 0.55, 0.3),
                          colors: [
                            tint.withValues(alpha: 0.0),
                            tint.withValues(alpha: alpha),
                            tint.withValues(alpha: 0.0),
                          ],
                          stops: const [0.0, 0.5, 1.0],
                        ),
                      ),
                    ),
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

/// Calm skeleton shimmer placeholder for cart hydration & session refresh.
class KioskCartShimmerSkeleton extends StatefulWidget {
  const KioskCartShimmerSkeleton({super.key});

  @override
  State<KioskCartShimmerSkeleton> createState() =>
      _KioskCartShimmerSkeletonState();
}

class _KioskCartShimmerSkeletonState extends State<KioskCartShimmerSkeleton>
    with SingleTickerProviderStateMixin {
  late final AnimationController _ctrl;

  @override
  void initState() {
    super.initState();
    _ctrl = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 1200),
    );
  }

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    if (KioskMotion.ambientEnabled(context)) {
      if (!_ctrl.isAnimating) _ctrl.repeat(reverse: true);
    } else {
      _ctrl.value = 0.5;
    }
  }

  @override
  void dispose() {
    _ctrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final colors = context.colors;

    return AnimatedBuilder(
      animation: _ctrl,
      builder: (context, _) {
        final pulse = 0.35 + (_ctrl.value * 0.35);
        final barColor = colors.borderStrong.withValues(alpha: pulse);

        return Padding(
          padding: const EdgeInsets.fromLTRB(24, 14, 24, 18),
          child: Column(
            children: [
              for (int i = 0; i < 3; i++) ...[
                if (i > 0) const SizedBox(height: 10),
                KioskSurfaceCard(
                  padding:
                      const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
                  borderRadius: 16,
                  child: Row(
                    children: [
                      Container(
                        width: 48,
                        height: 48,
                        decoration: BoxDecoration(
                          color: barColor,
                          borderRadius: BorderRadius.circular(14),
                        ),
                      ),
                      const SizedBox(width: 14),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Container(
                              width: 180,
                              height: 14,
                              decoration: BoxDecoration(
                                color: barColor,
                                borderRadius: BorderRadius.circular(6),
                              ),
                            ),
                            const SizedBox(height: 8),
                            Container(
                              width: 110,
                              height: 10,
                              decoration: BoxDecoration(
                                color: barColor.withValues(alpha: pulse * 0.7),
                                borderRadius: BorderRadius.circular(5),
                              ),
                            ),
                          ],
                        ),
                      ),
                      const SizedBox(width: 12),
                      Container(
                        width: 72,
                        height: 22,
                        decoration: BoxDecoration(
                          color: barColor,
                          borderRadius: BorderRadius.circular(8),
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ],
          ),
        );
      },
    );
  }
}

