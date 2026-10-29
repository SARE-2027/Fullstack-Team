import 'dart:io' show Platform;
import 'dart:math' as math;

import 'package:flutter/material.dart';
import 'package:flutter/physics.dart';

/// Central motion & Apple WWDC fluid physics engine for the SARE kiosk.
///
/// Implements the exact mathematical models from Apple's *Designing Fluid
/// Interfaces* (WWDC 2018):
/// - Damping ratio + response spring parameterization ([AppleSpringCurve]).
/// - Progressive rubber-band boundary resistance ([rubberband]).
/// - Exponential-decay momentum projection ([projectMomentum]).
///
/// Ambient (infinitely repeating) animations are automatically disabled:
/// - Under `flutter test`, so widget tests can always `pumpAndSettle`.
/// - When the OS requests reduced motion (`MediaQuery.disableAnimations`).
abstract final class KioskMotion {
  static final bool _isRunningInTest =
      Platform.environment.containsKey('FLUTTER_TEST');

  /// Critically damped default UI spring (`dampingRatio: 1.0`, `response: 0.34`).
  /// Smooth, immediate velocity handoff with zero overshoot.
  static const Curve springCritical = AppleSpringCurve(
    dampingRatio: 1.0,
    response: 0.34,
  );

  /// Snappy tactile spring (`dampingRatio: 0.86`, `response: 0.28`) for
  /// press releases, selection rings, and quick state switches.
  static const Curve springSnappy = AppleSpringCurve(
    dampingRatio: 0.86,
    response: 0.28,
  );

  /// Momentum-carrying physical spring (`dampingRatio: 0.74`, `response: 0.42`)
  /// reserved for load-cell needle deflection, celebratory bursts, and
  /// flick-released gestures.
  static const Curve springMomentum = AppleSpringCurve(
    dampingRatio: 0.74,
    response: 0.42,
  );

  /// Returns `true` when ambient (looping) animations are permitted.
  static bool ambientEnabled(BuildContext context) {
    if (_isRunningInTest) return false;
    if (MediaQuery.maybeDisableAnimationsOf(context) ?? false) return false;
    return true;
  }

  /// Returns `true` when the user or environment requests reduced motion.
  static bool reducedMotion(BuildContext context) {
    return MediaQuery.maybeDisableAnimationsOf(context) ?? false;
  }

  /// Apple's progressive rubber-band resistance formula for drag boundaries:
  /// `result = (overshoot * dim * c) / (dim + c * |overshoot|)`
  static double rubberband(
    double overshoot,
    double dimension, {
    double resistance = 0.55,
  }) {
    if (dimension <= 0 || overshoot == 0) return 0;
    final sign = overshoot < 0 ? -1.0 : 1.0;
    final absOvershoot = overshoot.abs();
    final damped = (absOvershoot * dimension * resistance) /
        (dimension + resistance * absOvershoot);
    return sign * damped;
  }

  /// Projects a gesture's resting position from its release velocity (px/s)
  /// using Apple's exponential scroll deceleration formula.
  static double projectMomentum(
    double initialVelocityPxPerSec, {
    double decelerationRate = 0.998,
  }) {
    return (initialVelocityPxPerSec / 1000.0) *
        decelerationRate /
        (1.0 - decelerationRate);
  }
}

/// Spring curve parameterized by Apple's designer-friendly [dampingRatio]
/// and [response] (in seconds) rather than raw mass/stiffness/damping.
@immutable
class AppleSpringCurve extends Curve {
  const AppleSpringCurve({
    this.dampingRatio = 1.0,
    this.response = 0.35,
    this.initialVelocity = 0.0,
  });

  /// Controls overshoot: `1.0` = critically damped (no bounce), `< 1.0` = underdamped.
  final double dampingRatio;

  /// Controls how quickly the spring reaches its target, in seconds.
  final double response;

  /// Normalized initial velocity handed off from a gesture.
  final double initialVelocity;

  @override
  double transformInternal(double t) {
    if (t <= 0.0) return 0.0;
    if (t >= 1.0) return 1.0;

    final omega = (2.0 * math.pi) / math.max(response, 0.05);
    final stiffness = omega * omega;
    final damping = 2.0 * dampingRatio * omega;

    final sim = SpringSimulation(
      SpringDescription(
        mass: 1.0,
        stiffness: stiffness,
        damping: damping,
      ),
      0.0,
      1.0,
      initialVelocity,
    );

    // Evaluate over normalized [0, 1] window with smooth tail clamping so
    // Flutter's Curve contract (f(0)=0, f(1)=1) holds strictly.
    final raw = sim.x(t * response * 2.2);
    if (t > 0.85) {
      final blend = (t - 0.85) / 0.15;
      return raw + (1.0 - raw) * (blend * blend);
    }
    return raw;
  }
}
