import 'dart:async';

import 'package:flutter/foundation.dart';
import 'package:flutter/widgets.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:intl/intl.dart' as intl;

import 'app_localizations_ar.dart';
import 'app_localizations_en.dart';

// ignore_for_file: type=lint

/// Callers can lookup localized strings with an instance of AppLocalizations
/// returned by `AppLocalizations.of(context)`.
///
/// Applications need to include `AppLocalizations.delegate()` in their app's
/// `localizationDelegates` list, and the locales they support in the app's
/// `supportedLocales` list. For example:
///
/// ```dart
/// import 'l10n/app_localizations.dart';
///
/// return MaterialApp(
///   localizationsDelegates: AppLocalizations.localizationsDelegates,
///   supportedLocales: AppLocalizations.supportedLocales,
///   home: MyApplicationHome(),
/// );
/// ```
///
/// ## Update pubspec.yaml
///
/// Please make sure to update your pubspec.yaml to include the following
/// packages:
///
/// ```yaml
/// dependencies:
///   # Internationalization support.
///   flutter_localizations:
///     sdk: flutter
///   intl: any # Use the pinned version from flutter_localizations
///
///   # Rest of dependencies
/// ```
///
/// ## iOS Applications
///
/// iOS applications define key application metadata, including supported
/// locales, in an Info.plist file that is built into the application bundle.
/// To configure the locales supported by your app, you’ll need to edit this
/// file.
///
/// First, open your project’s ios/Runner.xcworkspace Xcode workspace file.
/// Then, in the Project Navigator, open the Info.plist file under the Runner
/// project’s Runner folder.
///
/// Next, select the Information Property List item, select Add Item from the
/// Editor menu, then select Localizations from the pop-up menu.
///
/// Select and expand the newly-created Localizations item then, for each
/// locale your application supports, add a new item and select the locale
/// you wish to add from the pop-up menu in the Value field. This list should
/// be consistent with the languages listed in the AppLocalizations.supportedLocales
/// property.
abstract class AppLocalizations {
  AppLocalizations(String locale)
    : localeName = intl.Intl.canonicalizedLocale(locale.toString());

  final String localeName;

  static AppLocalizations? of(BuildContext context) {
    return Localizations.of<AppLocalizations>(context, AppLocalizations);
  }

  static const LocalizationsDelegate<AppLocalizations> delegate =
      _AppLocalizationsDelegate();

  /// A list of this localizations delegate along with the default localizations
  /// delegates.
  ///
  /// Returns a list of localizations delegates containing this delegate along with
  /// GlobalMaterialLocalizations.delegate, GlobalCupertinoLocalizations.delegate,
  /// and GlobalWidgetsLocalizations.delegate.
  ///
  /// Additional delegates can be added by appending to this list in
  /// MaterialApp. This list does not have to be used at all if a custom list
  /// of delegates is preferred or required.
  static const List<LocalizationsDelegate<dynamic>> localizationsDelegates =
      <LocalizationsDelegate<dynamic>>[
        delegate,
        GlobalMaterialLocalizations.delegate,
        GlobalCupertinoLocalizations.delegate,
        GlobalWidgetsLocalizations.delegate,
      ];

  /// A list of this localizations delegate's supported locales.
  static const List<Locale> supportedLocales = <Locale>[
    Locale('ar'),
    Locale('en'),
  ];

  /// No description provided for @appName.
  ///
  /// In en, this message translates to:
  /// **'SARE Smart Kiosk'**
  String get appName;

  /// No description provided for @welcomeTitle.
  ///
  /// In en, this message translates to:
  /// **'Welcome to Smart Shopping'**
  String get welcomeTitle;

  /// No description provided for @welcomeSubtitle.
  ///
  /// In en, this message translates to:
  /// **'Experience frictionless checkout with SARE AI & Weight-Verification Cart'**
  String get welcomeSubtitle;

  /// No description provided for @startAsGuest.
  ///
  /// In en, this message translates to:
  /// **'Start Shopping as Guest'**
  String get startAsGuest;

  /// No description provided for @startAsGuestDescription.
  ///
  /// In en, this message translates to:
  /// **'No registration required. Add items, pay, and go.'**
  String get startAsGuestDescription;

  /// No description provided for @orDivider.
  ///
  /// In en, this message translates to:
  /// **'OR'**
  String get orDivider;

  /// No description provided for @loyaltySignIn.
  ///
  /// In en, this message translates to:
  /// **'Member Sign-in'**
  String get loyaltySignIn;

  /// No description provided for @tapCardOrQr.
  ///
  /// In en, this message translates to:
  /// **'Tap your Membership card (NFC) on the cart handle to access your account'**
  String get tapCardOrQr;

  /// No description provided for @cartTitle.
  ///
  /// In en, this message translates to:
  /// **'Shopping Cart'**
  String get cartTitle;

  /// No description provided for @emptyCartTitle.
  ///
  /// In en, this message translates to:
  /// **'Your cart is empty'**
  String get emptyCartTitle;

  /// No description provided for @emptyCartMessage.
  ///
  /// In en, this message translates to:
  /// **'Scan an item barcode or tap a catalog item below to begin shopping'**
  String get emptyCartMessage;

  /// No description provided for @emptyCartHint.
  ///
  /// In en, this message translates to:
  /// **'Place items in the basket after scanning — weight is verified within ±15g'**
  String get emptyCartHint;

  /// No description provided for @itemAddedSuccess.
  ///
  /// In en, this message translates to:
  /// **'Item added and verified successfully'**
  String get itemAddedSuccess;

  /// No description provided for @itemRemoved.
  ///
  /// In en, this message translates to:
  /// **'Item removed from cart and basket weight updated'**
  String get itemRemoved;

  /// No description provided for @weightVerifying.
  ///
  /// In en, this message translates to:
  /// **'Verifying item weight on load cells...'**
  String get weightVerifying;

  /// No description provided for @weightMatchSuccess.
  ///
  /// In en, this message translates to:
  /// **'Weight verified (±15g match)'**
  String get weightMatchSuccess;

  /// No description provided for @weightMismatchWarning.
  ///
  /// In en, this message translates to:
  /// **'Weight discrepancy detected! Please re-check the last placed item.'**
  String get weightMismatchWarning;

  /// No description provided for @weightMismatchDetail.
  ///
  /// In en, this message translates to:
  /// **'Expected {expected} but measured {measured} — item rejected'**
  String weightMismatchDetail(String expected, String measured);

  /// No description provided for @scanPrompt.
  ///
  /// In en, this message translates to:
  /// **'Scan barcode with 2D scanner or enter manually'**
  String get scanPrompt;

  /// No description provided for @barcodeHint.
  ///
  /// In en, this message translates to:
  /// **'Barcode number (e.g. 6223000123456)'**
  String get barcodeHint;

  /// No description provided for @addItem.
  ///
  /// In en, this message translates to:
  /// **'Add Item'**
  String get addItem;

  /// No description provided for @verifyWeightHint.
  ///
  /// In en, this message translates to:
  /// **'Enter measured weight (g) or leave empty to auto-verify by catalog weight'**
  String get verifyWeightHint;

  /// No description provided for @weightGramsHint.
  ///
  /// In en, this message translates to:
  /// **'Weight (g)'**
  String get weightGramsHint;

  /// No description provided for @totalAmount.
  ///
  /// In en, this message translates to:
  /// **'Total Amount'**
  String get totalAmount;

  /// No description provided for @subtotal.
  ///
  /// In en, this message translates to:
  /// **'Subtotal'**
  String get subtotal;

  /// No description provided for @tax.
  ///
  /// In en, this message translates to:
  /// **'VAT (14%)'**
  String get tax;

  /// No description provided for @currencySymbol.
  ///
  /// In en, this message translates to:
  /// **'EGP'**
  String get currencySymbol;

  /// No description provided for @weightUnitGrams.
  ///
  /// In en, this message translates to:
  /// **'g'**
  String get weightUnitGrams;

  /// No description provided for @weightUnitKg.
  ///
  /// In en, this message translates to:
  /// **'kg'**
  String get weightUnitKg;

  /// No description provided for @checkoutButton.
  ///
  /// In en, this message translates to:
  /// **'Proceed to Checkout'**
  String get checkoutButton;

  /// No description provided for @cancelCheckout.
  ///
  /// In en, this message translates to:
  /// **'Back to Cart'**
  String get cancelCheckout;

  /// No description provided for @cartNumberLabel.
  ///
  /// In en, this message translates to:
  /// **'Cart #{cartNumber}'**
  String cartNumberLabel(String cartNumber);

  /// No description provided for @switchLanguage.
  ///
  /// In en, this message translates to:
  /// **'العربية'**
  String get switchLanguage;

  /// No description provided for @themeLight.
  ///
  /// In en, this message translates to:
  /// **'Light'**
  String get themeLight;

  /// No description provided for @themeDark.
  ///
  /// In en, this message translates to:
  /// **'Dark'**
  String get themeDark;

  /// No description provided for @startingSession.
  ///
  /// In en, this message translates to:
  /// **'Starting session…'**
  String get startingSession;

  /// No description provided for @sessionFailed.
  ///
  /// In en, this message translates to:
  /// **'Failed to start session. Please try again.'**
  String get sessionFailed;

  /// No description provided for @cartOffline.
  ///
  /// In en, this message translates to:
  /// **'Cart is offline or disabled'**
  String get cartOffline;

  /// No description provided for @scaleOfflineHelp.
  ///
  /// In en, this message translates to:
  /// **'Scale is offline — items can still be added by barcode confirmation'**
  String get scaleOfflineHelp;

  /// No description provided for @networkError.
  ///
  /// In en, this message translates to:
  /// **'Network error. Check connection and retry.'**
  String get networkError;

  /// No description provided for @retry.
  ///
  /// In en, this message translates to:
  /// **'Retry'**
  String get retry;

  /// No description provided for @continueShopping.
  ///
  /// In en, this message translates to:
  /// **'Continue Shopping'**
  String get continueShopping;

  /// No description provided for @removeItemConfirm.
  ///
  /// In en, this message translates to:
  /// **'Remove this item from the cart?'**
  String get removeItemConfirm;

  /// No description provided for @confirm.
  ///
  /// In en, this message translates to:
  /// **'Confirm'**
  String get confirm;

  /// No description provided for @cancel.
  ///
  /// In en, this message translates to:
  /// **'Cancel'**
  String get cancel;

  /// No description provided for @itemsCount.
  ///
  /// In en, this message translates to:
  /// **'{count} items'**
  String itemsCount(int count);

  /// No description provided for @checkoutTitle.
  ///
  /// In en, this message translates to:
  /// **'Checkout & Payment Summary'**
  String get checkoutTitle;

  /// No description provided for @payWithCard.
  ///
  /// In en, this message translates to:
  /// **'Pay with Bank Card (Contactless POS)'**
  String get payWithCard;

  /// No description provided for @payWithWallet.
  ///
  /// In en, this message translates to:
  /// **'Pay with Mobile Wallet (InstaPay / QR)'**
  String get payWithWallet;

  /// No description provided for @payWithCashier.
  ///
  /// In en, this message translates to:
  /// **'Pay at Express Cashier Counter'**
  String get payWithCashier;

  /// No description provided for @orderTotal.
  ///
  /// In en, this message translates to:
  /// **'Order Total'**
  String get orderTotal;

  /// No description provided for @paymentSuccess.
  ///
  /// In en, this message translates to:
  /// **'Payment Successful!'**
  String get paymentSuccess;

  /// No description provided for @paymentSuccessSubtitle.
  ///
  /// In en, this message translates to:
  /// **'Thank you for shopping with SARE Autonomous Cart'**
  String get paymentSuccessSubtitle;

  /// No description provided for @receiptHint.
  ///
  /// In en, this message translates to:
  /// **'Scan this QR code with your phone camera to save your digital receipt. It also serves as your Exit Gate Pass.'**
  String get receiptHint;

  /// No description provided for @receiptHashLabel.
  ///
  /// In en, this message translates to:
  /// **'Receipt Pass'**
  String get receiptHashLabel;

  /// No description provided for @doneAndExit.
  ///
  /// In en, this message translates to:
  /// **'Done — Return to Welcome Screen'**
  String get doneAndExit;

  /// No description provided for @exitPass.
  ///
  /// In en, this message translates to:
  /// **'Verified Exit Pass'**
  String get exitPass;

  /// No description provided for @autoResetHint.
  ///
  /// In en, this message translates to:
  /// **'Returning to welcome screen in {seconds}s'**
  String autoResetHint(int seconds);

  /// No description provided for @help.
  ///
  /// In en, this message translates to:
  /// **'Help'**
  String get help;

  /// No description provided for @callStaff.
  ///
  /// In en, this message translates to:
  /// **'Call Staff'**
  String get callStaff;

  /// No description provided for @dismiss.
  ///
  /// In en, this message translates to:
  /// **'Dismiss'**
  String get dismiss;

  /// No description provided for @ok.
  ///
  /// In en, this message translates to:
  /// **'OK'**
  String get ok;

  /// No description provided for @scaleReadyLabel.
  ///
  /// In en, this message translates to:
  /// **'Scale Ready'**
  String get scaleReadyLabel;

  /// No description provided for @scaleOfflineLabel.
  ///
  /// In en, this message translates to:
  /// **'Scale Offline'**
  String get scaleOfflineLabel;

  /// No description provided for @removeItemButton.
  ///
  /// In en, this message translates to:
  /// **'Remove'**
  String get removeItemButton;

  /// No description provided for @endSessionButton.
  ///
  /// In en, this message translates to:
  /// **'End Session'**
  String get endSessionButton;

  /// No description provided for @endSessionConfirmTitle.
  ///
  /// In en, this message translates to:
  /// **'End Shopping Session?'**
  String get endSessionConfirmTitle;

  /// No description provided for @endSessionConfirmMessage.
  ///
  /// In en, this message translates to:
  /// **'This will close your current cart session and return to the welcome screen.'**
  String get endSessionConfirmMessage;

  /// No description provided for @nfcModalTitle.
  ///
  /// In en, this message translates to:
  /// **'Member NFC Sign-In'**
  String get nfcModalTitle;

  /// No description provided for @nfcModalSubtitle.
  ///
  /// In en, this message translates to:
  /// **'Wave your store membership card near the handle NFC reader or pick a demo card below'**
  String get nfcModalSubtitle;

  /// No description provided for @nfcInputHint.
  ///
  /// In en, this message translates to:
  /// **'Enter NFC Card UID (e.g. 04:A2:19:7C)'**
  String get nfcInputHint;

  /// No description provided for @nfcConnectButton.
  ///
  /// In en, this message translates to:
  /// **'Sign In with NFC'**
  String get nfcConnectButton;

  /// No description provided for @guestShopperBadge.
  ///
  /// In en, this message translates to:
  /// **'Guest Shopper'**
  String get guestShopperBadge;

  /// No description provided for @memberShopperBadge.
  ///
  /// In en, this message translates to:
  /// **'Member: {name}'**
  String memberShopperBadge(String name);

  /// No description provided for @liveScaleTitle.
  ///
  /// In en, this message translates to:
  /// **'Basket Load-Cell Telemetry'**
  String get liveScaleTitle;

  /// No description provided for @autoTareBadge.
  ///
  /// In en, this message translates to:
  /// **'Auto-Tare: 0g'**
  String get autoTareBadge;

  /// No description provided for @toleranceBadge.
  ///
  /// In en, this message translates to:
  /// **'±15g Gate'**
  String get toleranceBadge;

  /// No description provided for @simulatorTitle.
  ///
  /// In en, this message translates to:
  /// **'Smart Sensor & Catalog Simulator'**
  String get simulatorTitle;

  /// No description provided for @simulatorSubtitle.
  ///
  /// In en, this message translates to:
  /// **'Tap any supermarket product to simulate barcode scan + load-cell weight verification'**
  String get simulatorSubtitle;

  /// No description provided for @simulateValidScan.
  ///
  /// In en, this message translates to:
  /// **'+ Scan (Match)'**
  String get simulateValidScan;

  /// No description provided for @simulateFraudScan.
  ///
  /// In en, this message translates to:
  /// **'Fraud (+95g)'**
  String get simulateFraudScan;

  /// No description provided for @staffModalTitle.
  ///
  /// In en, this message translates to:
  /// **'Staff Assistance Notified'**
  String get staffModalTitle;

  /// No description provided for @staffModalMessage.
  ///
  /// In en, this message translates to:
  /// **'A store attendant has been alerted and is heading to Cart #{cartNumber} ({location}).'**
  String staffModalMessage(String cartNumber, String location);

  /// No description provided for @paymentMethodInstructionCard.
  ///
  /// In en, this message translates to:
  /// **'Tap your contactless Visa, Mastercard, or Meeza card on the terminal beside the screen.'**
  String get paymentMethodInstructionCard;

  /// No description provided for @paymentMethodInstructionWallet.
  ///
  /// In en, this message translates to:
  /// **'Scan the dynamic Paymob / InstaPay QR using your banking wallet app to authorize payment.'**
  String get paymentMethodInstructionWallet;

  /// No description provided for @paymentMethodInstructionCashier.
  ///
  /// In en, this message translates to:
  /// **'Present Cart #{cartNumber} at the express checkout desk to settle in cash.'**
  String paymentMethodInstructionCashier(String cartNumber);

  /// No description provided for @orderItemsHeading.
  ///
  /// In en, this message translates to:
  /// **'Verified Basket Items'**
  String get orderItemsHeading;

  /// No description provided for @totalBasketWeight.
  ///
  /// In en, this message translates to:
  /// **'Total Verified Weight'**
  String get totalBasketWeight;

  /// No description provided for @pauseTimer.
  ///
  /// In en, this message translates to:
  /// **'Hold Screen'**
  String get pauseTimer;

  /// No description provided for @resumeTimer.
  ///
  /// In en, this message translates to:
  /// **'Resume Auto-Reset'**
  String get resumeTimer;
}

class _AppLocalizationsDelegate
    extends LocalizationsDelegate<AppLocalizations> {
  const _AppLocalizationsDelegate();

  @override
  Future<AppLocalizations> load(Locale locale) {
    return SynchronousFuture<AppLocalizations>(lookupAppLocalizations(locale));
  }

  @override
  bool isSupported(Locale locale) =>
      <String>['ar', 'en'].contains(locale.languageCode);

  @override
  bool shouldReload(_AppLocalizationsDelegate old) => false;
}

AppLocalizations lookupAppLocalizations(Locale locale) {
  // Lookup logic when only language code is specified.
  switch (locale.languageCode) {
    case 'ar':
      return AppLocalizationsAr();
    case 'en':
      return AppLocalizationsEn();
  }

  throw FlutterError(
    'AppLocalizations.delegate failed to load unsupported locale "$locale". This is likely '
    'an issue with the localizations generation tool. Please file an issue '
    'on GitHub with a reproducible sample app and the gen-l10n configuration '
    'that was used.',
  );
}
