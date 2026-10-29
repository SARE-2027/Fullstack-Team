import 'package:flutter_test/flutter_test.dart';
import 'package:sare_kiosk/core/utils/kiosk_motion.dart';

void main() {
  group('KioskMotion — Apple WWDC Fluid Interfaces Physics', () {
    test('AppleSpringCurve satisfies Curve boundary invariants f(0)=0, f(1)=1', () {
      const critical = KioskMotion.springCritical;
      const snappy = KioskMotion.springSnappy;
      const momentum = KioskMotion.springMomentum;

      expect(critical.transform(0.0), 0.0);
      expect(critical.transform(1.0), 1.0);
      expect(snappy.transform(0.0), 0.0);
      expect(snappy.transform(1.0), 1.0);
      expect(momentum.transform(0.0), 0.0);
      expect(momentum.transform(1.0), 1.0);

      // Underdamped momentum spring should rise rapidly and briefly overshoot
      // or approach 1.0 faster than linear at mid-flight.
      expect(momentum.transform(0.5), greaterThan(0.75));
    });

    test('rubberband resists progressively and preserves sign', () {
      expect(KioskMotion.rubberband(0, 200), 0);
      final pos = KioskMotion.rubberband(100, 200);
      final neg = KioskMotion.rubberband(-100, 200);

      expect(pos, greaterThan(0));
      expect(pos, lessThan(100));
      expect(neg, closeTo(-pos, 1e-6));

      // Progressive resistance: doubling overshoot yields less than double displacement.
      final doubleOvershoot = KioskMotion.rubberband(200, 200);
      expect(doubleOvershoot, lessThan(pos * 2));
    });

    test('projectMomentum projects resting endpoint along velocity direction', () {
      final forward = KioskMotion.projectMomentum(800);
      final backward = KioskMotion.projectMomentum(-800);

      expect(forward, greaterThan(0));
      expect(backward, closeTo(-forward, 1e-6));
    });
  });
}

