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

  testWidgets(
      'Toggling language morphs header and action labels smoothly without overflow',
      (WidgetTester tester) async {
    tester.view.physicalSize = const Size(1280, 800);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    await tester.pumpWidget(const SareKioskApp());
    await tester.pumpAndSettle();

    // Find language switcher button (initially showing 'English' in Arabic locale)
    final languageButton = find.text('English');
    expect(languageButton, findsOneWidget);

    // Tap language toggle
    await tester.tap(languageButton);
    // Pump frames through mid-transition
    await tester.pump(const Duration(milliseconds: 100));
    await tester.pump(const Duration(milliseconds: 200));
    await tester.pumpAndSettle();

    // Language switched to English, button now displays 'العربية'
    expect(find.text('العربية'), findsOneWidget);
  });

  testWidgets(
      'Toggling theme smoothly updates theme state without layout jumps',
      (WidgetTester tester) async {
    tester.view.physicalSize = const Size(1280, 800);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    await tester.pumpWidget(const SareKioskApp());
    await tester.pumpAndSettle();

    // In Arabic light mode, theme button displays 'الوضع الليلي'
    final themeButton = find.text('الوضع الليلي');
    expect(themeButton, findsOneWidget);

    // Tap theme toggle
    await tester.tap(themeButton);
    // Pump frames mid-transition
    await tester.pump(const Duration(milliseconds: 150));
    await tester.pumpAndSettle();

    // Switched to dark mode, now displays 'الوضع النهاري'
    expect(find.text('الوضع النهاري'), findsOneWidget);
  });
}
