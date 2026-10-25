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
  String get emptyCartTitle => 'Your cart is empty';

  @override
  String get emptyCartMessage =>
      'Scan an item barcode or tap a catalog item below to begin shopping';

  @override
  String get emptyCartHint =>
      'Place items in the basket after scanning — weight is verified within ±15g';

  @override
  String get itemAddedSuccess => 'Item added and verified successfully';

  @override
  String get itemRemoved => 'Item removed from cart and basket weight updated';

  @override
  String get itemRestored => 'Item restored to your cart';

  @override
  String get undo => 'Undo';

  @override
  String get hide => 'Hide';

  @override
  String get show => 'Show';

  @override
  String get weightVerifying => 'Verifying item weight on load cells...';

  @override
  String get weightMatchSuccess => 'Weight verified (±15g match)';

  @override
  String get weightMismatchWarning =>
      'Weight discrepancy detected! Please re-check the last placed item.';

  @override
  String weightMismatchDetail(String expected, String measured) {
    return 'Expected $expected but measured $measured — item rejected';
  }

  @override
  String get scanPrompt => 'Scan barcode with 2D scanner or enter manually';

  @override
  String get barcodeHint => 'Barcode number (e.g. 6223000123456)';

  @override
  String get addItem => 'Add Item';

  @override
  String get verifyWeightHint =>
      'Enter measured weight (g) or leave empty to auto-verify by catalog weight';

  @override
  String get weightGramsHint => 'Weight (g)';

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
  String get scaleOfflineHelp =>
      'Scale is offline — items can still be added by barcode confirmation';

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
      'Thank you for shopping with SARE Autonomous Cart';

  @override
  String get receiptHint =>
      'Scan this QR code with your phone camera to save your digital receipt. It also serves as your Exit Gate Pass.';

  @override
  String get receiptHashLabel => 'Receipt Pass';

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
  String get scaleReadyLabel => 'Scale Ready';

  @override
  String get scaleOfflineLabel => 'Scale Offline';

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
      'Wave your store membership card near the handle NFC reader or pick a demo card below';

  @override
  String get nfcInputHint => 'Enter NFC Card UID (e.g. 04:A2:19:7C)';

  @override
  String get nfcConnectButton => 'Sign In with NFC';

  @override
  String get guestShopperBadge => 'Guest Shopper';

  @override
  String memberShopperBadge(String name) {
    return 'Member: $name';
  }

  @override
  String get liveScaleTitle => 'Basket Load-Cell Telemetry';

  @override
  String get autoTareBadge => 'Auto-Tare: 0g';

  @override
  String get toleranceBadge => '±15g Gate';

  @override
  String get simulatorTitle => 'Smart Sensor & Catalog Simulator';

  @override
  String get simulatorSubtitle =>
      'Tap any supermarket product to simulate barcode scan + load-cell weight verification';

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
  String get orderItemsHeading => 'Verified Basket Items';

  @override
  String get totalBasketWeight => 'Total Verified Weight';

  @override
  String get pauseTimer => 'Hold Screen';

  @override
  String get resumeTimer => 'Resume Auto-Reset';
}
