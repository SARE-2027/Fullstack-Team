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
      'Scan your items, place them in the basket, and pay in seconds';

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
      'Tap your membership card to earn points and unlock member offers';

  @override
  String get cartTitle => 'Shopping Cart';

  @override
  String get emptyCartTitle => 'Your cart is empty';

  @override
  String get emptyCartMessage =>
      'Scan any product barcode and place it in your basket to begin';

  @override
  String get emptyCartHint =>
      'Your items and running total update automatically as you shop';

  @override
  String get itemAddedSuccess => 'Added to your basket';

  @override
  String get itemRemoved => 'Item removed from your basket';

  @override
  String get itemRestored => 'Item restored to your cart';

  @override
  String get undo => 'Undo';

  @override
  String get hide => 'Hide';

  @override
  String get show => 'Show';

  @override
  String get weightVerifying => 'Adding item...';

  @override
  String get weightMatchSuccess => 'Item verified';

  @override
  String get weightMismatchWarning =>
      'Weight discrepancy detected! Please re-check the last placed item.';

  @override
  String weightMismatchDetail(String expected, String measured) {
    return 'Expected $expected but measured $measured — item rejected';
  }

  @override
  String get scanPrompt => 'Scan product barcode or enter code';

  @override
  String get barcodeHint => 'Scan barcode or enter product code...';

  @override
  String get addItem => 'Add Item';

  @override
  String get verifyWeightHint => '';

  @override
  String get weightGramsHint => 'Weight';

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

  @override
  String get startingSession => 'Starting session…';

  @override
  String get sessionFailed => 'Failed to start session. Please try again.';

  @override
  String get cartOffline => 'Cart is offline or disabled';

  @override
  String get scaleOfflineHelp => 'You can continue adding items by barcode';

  @override
  String get networkError => 'Network error. Check connection and retry.';

  @override
  String get retry => 'Retry';

  @override
  String get continueShopping => 'Continue Shopping';

  @override
  String get removeItemConfirm => 'Remove this item from the cart?';

  @override
  String get confirm => 'Confirm';

  @override
  String get cancel => 'Cancel';

  @override
  String itemsCount(int count) {
    return '$count items';
  }

  @override
  String get checkoutTitle => 'Checkout & Payment Summary';

  @override
  String get payWithCard => 'Pay with Bank Card (Contactless POS)';

  @override
  String get payWithWallet => 'Pay with Mobile Wallet (InstaPay / QR)';

  @override
  String get payWithCashier => 'Pay at Express Cashier Counter';

  @override
  String get orderTotal => 'Order Total';

  @override
  String get paymentSuccess => 'Payment Successful!';

  @override
  String get paymentSuccessSubtitle =>
      'Thank you for shopping with SARE Smart Cart';

  @override
  String get receiptHint =>
      'Scan this QR code with your phone to save your digital receipt and exit through the gate.';

  @override
  String get receiptHashLabel => 'Receipt No.';

  @override
  String get doneAndExit => 'Done — Return to Welcome Screen';

  @override
  String get exitPass => 'Verified Exit Pass';

  @override
  String autoResetHint(int seconds) {
    return 'Returning to welcome screen in ${seconds}s';
  }

  @override
  String get help => 'Help';

  @override
  String get callStaff => 'Call Staff';

  @override
  String get dismiss => 'Dismiss';

  @override
  String get ok => 'OK';

  @override
  String get scaleReadyLabel => 'Ready';

  @override
  String get scaleOfflineLabel => 'Barcode Mode';

  @override
  String get removeItemButton => 'Remove';

  @override
  String get endSessionButton => 'End Session';

  @override
  String get endSessionConfirmTitle => 'End Shopping Session?';

  @override
  String get endSessionConfirmMessage =>
      'This will close your current cart session and return to the welcome screen.';

  @override
  String get nfcModalTitle => 'Member NFC Sign-In';

  @override
  String get nfcModalSubtitle =>
      'Tap your membership card or select your account below';

  @override
  String get nfcInputHint => 'Enter membership card code...';

  @override
  String get nfcConnectButton => 'Sign In with NFC';

  @override
  String get guestShopperBadge => 'Guest Shopper';

  @override
  String memberShopperBadge(String name) {
    return 'Member: $name';
  }

  @override
  String get liveScaleTitle => 'All items verified';

  @override
  String get autoTareBadge => 'Ready';

  @override
  String get toleranceBadge => 'Verified';

  @override
  String get simulatorTitle => 'Quick Products (Demo Catalog)';

  @override
  String get simulatorSubtitle => 'Tap any product to add it to your basket';

  @override
  String get simulateValidScan => '+ Scan (Match)';

  @override
  String get simulateFraudScan => 'Fraud (+95g)';

  @override
  String get staffModalTitle => 'Staff Assistance Notified';

  @override
  String staffModalMessage(String cartNumber, String location) {
    return 'A store attendant has been alerted and is heading to Cart #$cartNumber ($location).';
  }

  @override
  String get paymentMethodInstructionCard =>
      'Tap your contactless Visa, Mastercard, or Meeza card on the terminal beside the screen.';

  @override
  String get paymentMethodInstructionWallet =>
      'Scan the dynamic Paymob / InstaPay QR using your banking wallet app to authorize payment.';

  @override
  String paymentMethodInstructionCashier(String cartNumber) {
    return 'Present Cart #$cartNumber at the express checkout desk to settle in cash.';
  }

  @override
  String get orderItemsHeading => 'Your Basket Items';

  @override
  String get totalBasketWeight => 'Total Weight';

  @override
  String get pauseTimer => 'Hold Screen';

  @override
  String get resumeTimer => 'Resume Auto-Reset';
}
