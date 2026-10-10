import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:sare_kiosk/features/check_in/presentation/screens/welcome_check_in_screen.dart';
import 'package:sare_kiosk/main.dart';

void main() {
  testWidgets('SARE Kiosk app smoke test loads WelcomeCheckInScreen',
      (WidgetTester tester) async {
    tester.view.physicalSize = const Size(1280, 800);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    await tester.pumpWidget(const SareKioskApp());
    await tester.pumpAndSettle();

    expect(find.byType(SareKioskApp), findsOneWidget);
    expect(find.byType(WelcomeCheckInScreen), findsOneWidget);
  });
}
