import 'package:flutter_test/flutter_test.dart';

import 'package:sare_kiosk/main.dart';

void main() {
  testWidgets('SARE Kiosk app smoke test', (WidgetTester tester) async {
    await tester.pumpWidget(const SareKioskApp());

    expect(find.text('SARE Kiosk'), findsOneWidget);
  });
}
