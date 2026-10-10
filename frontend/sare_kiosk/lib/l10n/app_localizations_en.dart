// ignore: unused_import
import 'package:intl/intl.dart' as intl;

import 'app_localizations.dart';

// ignore_for_file: type=lint

/// The translations for English (`en`).
class AppLocalizationsEn extends AppLocalizations {
  AppLocalizationsEn([String locale = 'en']) : super(locale);

  @override
  String get appName => 'SARE Smart Kiosk';

  @override
  String get welcomeTitle => 'Welcome to Smart Shopping';

  @override
  String get welcomeSubtitle =>
      'Experience frictionless checkout with SARE AI & Weight-Verification Cart';

  @override
  String get startAsGuest => 'Start Shopping as Guest';

  @override
  String get startAsGuestDescription =>
      'No registration required. Add items, pay, and go.';

  @override
  String get orDivider => 'OR';

  @override
  String get loyaltySignIn => 'Member Sign-in';

  @override
  String get tapCardOrQr =>
      'Tap your Membership card (NFC) on the cart handle to access your account';

  @override
  String get cartTitle => 'Shopping Cart';

  @override
  String get emptyCartMessage =>
      'Your cart is empty. Scan an item barcode to begin shopping!';

  @override
  String get itemAddedSuccess => 'Item added and verified successfully';

  @override
  String get itemRemoved => 'Item removed from cart';

  @override
  String get weightVerifying => 'Verifying item weight...';

  @override
  String get weightMatchSuccess => 'Weight verified (+/-15g match)';

  @override
  String get weightMismatchWarning =>
      'Weight discrepancy detected! Please re-check the last placed item.';

  @override
  String get scanPrompt => 'Scan barcode under camera scanner';

  @override
  String get totalAmount => 'Total Amount';

  @override
  String get subtotal => 'Subtotal';

  @override
  String get tax => 'VAT (14%)';

  @override
  String get currencySymbol => 'EGP';

  @override
  String get weightUnitGrams => 'g';

  @override
  String get weightUnitKg => 'kg';

  @override
  String get checkoutButton => 'Proceed to Checkout';

  @override
  String get cancelCheckout => 'Back to Cart';

  @override
  String cartNumberLabel(String cartNumber) {
    return 'Cart #$cartNumber';
  }

  @override
  String get switchLanguage => 'العربية';

  @override
  String get themeLight => 'Light';

  @override
  String get themeDark => 'Dark';
}
