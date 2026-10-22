import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'package:sare_kiosk/features/cart/presentation/screens/cart_screen.dart';
import 'package:sare_kiosk/features/check_in/presentation/screens/welcome_check_in_screen.dart';
import 'package:sare_kiosk/features/checkout/presentation/screens/checkout_screen.dart';
import 'package:sare_kiosk/features/receipt/presentation/screens/receipt_screen.dart';
import 'package:sare_kiosk/main.dart';

void _setKioskViewport(WidgetTester tester) {
  tester.view.physicalSize = const Size(1280, 800);
  tester.view.devicePixelRatio = 1.0;
  addTearDown(tester.view.resetPhysicalSize);
  addTearDown(tester.view.resetDevicePixelRatio);
}

void main() {
  setUp(() {
    SharedPreferences.setMockInitialValues({});
  });

  testWidgets('SARE Kiosk app smoke test loads WelcomeCheckInScreen', (tester) async {
    _setKioskViewport(tester);

    await tester.pumpWidget(const SareKioskApp());
    await tester.pumpAndSettle();

    expect(find.byType(SareKioskApp), findsOneWidget);
    expect(find.byType(WelcomeCheckInScreen), findsOneWidget);
  });

  testWidgets('Toggling language morphs header and action labels smoothly without overflow', (tester) async {
    _setKioskViewport(tester);

    await tester.pumpWidget(const SareKioskApp());
    await tester.pumpAndSettle();

    final languageButton = find.text('English');
    expect(languageButton, findsOneWidget);

    await tester.tap(languageButton);
    await tester.pump(const Duration(milliseconds: 100));
    await tester.pump(const Duration(milliseconds: 200));
    await tester.pumpAndSettle();

    expect(find.text('العربية'), findsOneWidget);
  });

  testWidgets('Toggling theme smoothly updates theme state without layout jumps', (tester) async {
    _setKioskViewport(tester);

    await tester.pumpWidget(const SareKioskApp());
    await tester.pumpAndSettle();

    final themeButton = find.text('الوضع الليلي');
    expect(themeButton, findsOneWidget);

    await tester.tap(themeButton);
    await tester.pump(const Duration(milliseconds: 150));
    await tester.pumpAndSettle();

    expect(find.text('الوضع النهاري'), findsOneWidget);
  });

  testWidgets(
    'Full Guest Journey: Check-in -> Scan valid item -> Fraud alert -> Checkout -> Receipt -> Idle',
    (tester) async {
      _setKioskViewport(tester);

      await tester.pumpWidget(const SareKioskApp());
      await tester.pumpAndSettle();

      // Switch to English for deterministic label assertions
      await tester.tap(find.text('English'));
      await tester.pumpAndSettle();

      // 1. Tap "Start Shopping as Guest" button
      final startGuestButtons = find.text('Start Shopping as Guest');
      await tester.tap(startGuestButtons.last);
      await tester.pumpAndSettle();

      expect(find.byType(CartScreen), findsOneWidget);
      expect(find.text('Guest Shopper'), findsOneWidget);
      expect(find.text('Your cart is empty'), findsOneWidget);

      // 2. Simulate scanning a valid item (+ Scan (Match)) on Sunshine Tuna 180g
      final validScanButtons = find.text('+ Scan (Match)');
      expect(validScanButtons, findsWidgets);
      await tester.tap(validScanButtons.first);
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 300));

      expect(find.text('Sunshine Tuna Chunk 180g'), findsWidgets);
      expect(find.text('1 items'), findsOneWidget);

      // 3. Simulate a Fraud / Weight Discrepancy scan (+95g mismatch)
      final fraudButtons = find.text('Fraud (+95g)');
      await tester.tap(fraudButtons.first);
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 300));

      // Item count remains 1 and discrepancy warning appears
      expect(find.text('1 items'), findsOneWidget);
      expect(find.textContaining('Weight discrepancy detected'), findsOneWidget);

      // Wait for any auto-dismiss timer to settle safely
      await tester.pump(const Duration(seconds: 4));
      await tester.pumpAndSettle();

      // 4. Proceed to Checkout
      await tester.tap(find.text('Proceed to Checkout'));
      await tester.pumpAndSettle();

      expect(find.byType(CheckoutScreen), findsOneWidget);
      expect(find.text('Checkout & Payment Summary'), findsOneWidget);

      // Select Mobile Wallet payment option and Confirm
      await tester.tap(find.text('Pay with Mobile Wallet (InstaPay / QR)'));
      await tester.pumpAndSettle();

      await tester.tap(find.text('Confirm'));
      await tester.pumpAndSettle();

      // 5. Verify Receipt & Exit Pass Screen
      expect(find.byType(ReceiptScreen), findsOneWidget);
      expect(find.text('Payment Successful!'), findsOneWidget);
      expect(find.text('Verified Exit Pass'), findsOneWidget);

      // 6. Tap Done to return to Welcome Screen
      await tester.tap(find.text('Done — Return to Welcome Screen'));
      await tester.pumpAndSettle();

      expect(find.byType(WelcomeCheckInScreen), findsOneWidget);
    },
  );

  testWidgets('Member NFC Sign-In Modal starts an authenticated member session', (tester) async {
    _setKioskViewport(tester);

    await tester.pumpWidget(const SareKioskApp());
    await tester.pumpAndSettle();

    await tester.tap(find.text('English'));
    await tester.pumpAndSettle();

    // Open NFC Sign-In Modal
    await tester.tap(find.text('Member Sign-in').last);
    await tester.pumpAndSettle();

    expect(find.text('Member NFC Sign-In'), findsOneWidget);
    expect(find.text('Ahmed Mohamed'), findsOneWidget);

    // Select Gold Member Ahmed Mohamed
    await tester.tap(find.text('Ahmed Mohamed'));
    await tester.pumpAndSettle();

    expect(find.byType(CartScreen), findsOneWidget);
    expect(find.text('Member: Ahmed Mohamed'), findsOneWidget);

    // End session from empty cart returns to WelcomeCheckInScreen
    await tester.tap(find.text('End Session'));
    await tester.pumpAndSettle();

    expect(find.byType(WelcomeCheckInScreen), findsOneWidget);
  });

  testWidgets(
    'Manual barcode scan, item removal, and theme/language toggles preserve active CartScreen route',
    (tester) async {
      _setKioskViewport(tester);

      await tester.pumpWidget(const SareKioskApp());
      await tester.pumpAndSettle();

      await tester.tap(find.text('English'));
      await tester.pumpAndSettle();

      await tester.tap(find.text('Start Shopping as Guest').last);
      await tester.pumpAndSettle();
      expect(find.byType(CartScreen), findsOneWidget);

      // Enter Juhayna Orange Juice barcode manually with empty weight field (auto catalog verify)
      final textFields = find.byType(TextField);
      await tester.enterText(textFields.first, '6223000654321');
      await tester.tap(find.text('Add Item'));
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 300));

      expect(find.text('1 items'), findsOneWidget);

      // Toggle Theme while inside CartScreen — must stay on CartScreen!
      await tester.tap(find.text('Dark'));
      await tester.pumpAndSettle();
      expect(find.byType(CartScreen), findsOneWidget);
      expect(find.text('Light'), findsOneWidget);

      // Toggle Language to Arabic while inside CartScreen — must stay on CartScreen!
      await tester.tap(find.text('العربية'));
      await tester.pumpAndSettle();
      expect(find.byType(CartScreen), findsOneWidget);
      expect(find.text('1 أصناف'), findsOneWidget);

      // Remove item from cart
      await tester.tap(find.text('حذف'));
      await tester.pump();
      await tester.pump(const Duration(seconds: 4));
      await tester.pumpAndSettle();

      expect(find.text('0 أصناف'), findsOneWidget);
      expect(find.text('السلة فارغة حاليًّا'), findsOneWidget);
    },
  );

  testWidgets('Call Staff button opens assistance modal and dismisses cleanly', (tester) async {
    _setKioskViewport(tester);

    await tester.pumpWidget(const SareKioskApp());
    await tester.pumpAndSettle();

    await tester.tap(find.text('استدعاء موظف'));
    await tester.pumpAndSettle();

    expect(find.text('تم إشعار موظف المساعدة'), findsOneWidget);

    await tester.tap(find.text('حسنًا'));
    await tester.pumpAndSettle();

    expect(find.text('تم إشعار موظف المساعدة'), findsNothing);
  });
}
