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
  /// **'Sign In with Loyalty Card'**
  String get loyaltySignIn;

  /// No description provided for @tapCardOrQr.
  ///
  /// In en, this message translates to:
  /// **'Tap your NFC card on the cart handle or scan QR via mobile app'**
  String get tapCardOrQr;

  /// No description provided for @cartTitle.
  ///
  /// In en, this message translates to:
  /// **'Shopping Cart'**
  String get cartTitle;

  /// No description provided for @emptyCartMessage.
  ///
  /// In en, this message translates to:
  /// **'Your cart is empty. Scan an item barcode to begin shopping!'**
  String get emptyCartMessage;

  /// No description provided for @itemAddedSuccess.
  ///
  /// In en, this message translates to:
  /// **'Item added and verified successfully'**
  String get itemAddedSuccess;

  /// No description provided for @itemRemoved.
  ///
  /// In en, this message translates to:
  /// **'Item removed from cart'**
  String get itemRemoved;

  /// No description provided for @weightVerifying.
  ///
  /// In en, this message translates to:
  /// **'Verifying item weight...'**
  String get weightVerifying;

  /// No description provided for @weightMatchSuccess.
  ///
  /// In en, this message translates to:
  /// **'Weight verified (+/-15g match)'**
  String get weightMatchSuccess;

  /// No description provided for @weightMismatchWarning.
  ///
  /// In en, this message translates to:
  /// **'Weight discrepancy detected! Please re-check the last placed item.'**
  String get weightMismatchWarning;

  /// No description provided for @scanPrompt.
  ///
  /// In en, this message translates to:
  /// **'Scan barcode under camera scanner'**
  String get scanPrompt;

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
  /// **'English'**
  String get switchLanguage;
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
