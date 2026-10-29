// ignore: unused_import
import 'package:intl/intl.dart' as intl;

import 'app_localizations.dart';

// ignore_for_file: type=lint

/// The translations for Arabic (`ar`).
class AppLocalizationsAr extends AppLocalizations {
  AppLocalizationsAr([String locale = 'ar']) : super(locale);

  @override
  String get appName => 'عربة SARE الذكية';

  @override
  String get welcomeTitle => 'مرحبًا بك في التسوق الذكي';

  @override
  String get welcomeSubtitle =>
      'امسح منتجاتك وضعها في السلة، وادفع في ثوانٍ بدون انتظار';

  @override
  String get startAsGuest => 'ابدأ التسوق كزائر / ضيف';

  @override
  String get startAsGuestDescription =>
      'تسوق فورًا بدون تسجيل مسبق وادفع بكل سهولة.';

  @override
  String get orDivider => 'أو';

  @override
  String get loyaltySignIn => 'تسجيل دخول الأعضاء';

  @override
  String get tapCardOrQr =>
      'مرر بطاقة العضوية للاستفادة من نقاطك وعروضك الخاصة';

  @override
  String get cartTitle => 'سلة المشتريات';

  @override
  String get emptyCartTitle => 'السلة فارغة حاليًّا';

  @override
  String get emptyCartMessage =>
      'امسح باركود أي منتج وضعه في السلة ليظهر هنا فورًا';

  @override
  String get emptyCartHint =>
      'يتم تحديث قائمة مشترياتك وإجمالي الفاتورة تلقائيًّا';

  @override
  String get itemAddedSuccess => 'تمت إضافة المنتج إلى سلتك';

  @override
  String get itemRemoved => 'تم حذف المنتج من السلة';

  @override
  String get itemRestored => 'تمت إعادة المنتج إلى سلتك';

  @override
  String get undo => 'تراجع';

  @override
  String get hide => 'إخفاء';

  @override
  String get show => 'عرض';

  @override
  String get weightVerifying => 'جارٍ إضافة المنتج...';

  @override
  String get weightMatchSuccess => 'تم تأكيد المنتج';

  @override
  String get weightMismatchWarning =>
      'يرجى مراجعة آخر منتج وُضع في السلة وإعادة مسحه.';

  @override
  String weightMismatchDetail(String expected, String measured) {
    return 'المنتج غير مطابق — لم تتم الإضافة';
  }

  @override
  String get scanPrompt => 'امسح باركود المنتج أو أدخل رقمه';

  @override
  String get barcodeHint => 'امسح الباركود أو اكتب رقم المنتج...';

  @override
  String get addItem => 'إضافة المنتج';

  @override
  String get verifyWeightHint => '';

  @override
  String get weightGramsHint => 'الوزن';

  @override
  String get totalAmount => 'الإجمالي الكلي';

  @override
  String get subtotal => 'المجموع الفرعي';

  @override
  String get tax => 'ضريبة القيمة المضافة (14%)';

  @override
  String get currencySymbol => 'ج.م';

  @override
  String get weightUnitGrams => 'جم';

  @override
  String get weightUnitKg => 'كجم';

  @override
  String get checkoutButton => 'إتمام الشراء والدفع';

  @override
  String get cancelCheckout => 'الرجوع للسلة';

  @override
  String cartNumberLabel(String cartNumber) {
    return 'عربة رقم $cartNumber';
  }

  @override
  String get switchLanguage => 'English';

  @override
  String get themeLight => 'الوضع النهاري';

  @override
  String get themeDark => 'الوضع الليلي';

  @override
  String get startingSession => 'جارٍ بدء الجلسة…';

  @override
  String get sessionFailed => 'فشل بدء الجلسة. حاول مرة أخرى.';

  @override
  String get cartOffline => 'العربة غير متاحة حاليًّا';

  @override
  String get scaleOfflineHelp => 'يمكنك متابعة إضافة المنتجات بالباركود';

  @override
  String get networkError => 'خطأ في الشبكة. تحقق من الاتصال وحاول مجددًا.';

  @override
  String get retry => 'إعادة المحاولة';

  @override
  String get continueShopping => 'متابعة التسوق';

  @override
  String get removeItemConfirm => 'هل تريد حذف هذا المنتج من السلة؟';

  @override
  String get confirm => 'تأكيد';

  @override
  String get cancel => 'إلغاء';

  @override
  String itemsCount(int count) {
    return '$count أصناف';
  }

  @override
  String get checkoutTitle => 'ملخص الحساب والدفع';

  @override
  String get payWithCard => 'الدفع بالبطاقة البنكية (POS لا تلامسي)';

  @override
  String get payWithWallet => 'الدفع بمحفظة الهاتف (إنستا باي / QR)';

  @override
  String get payWithCashier => 'الدفع النقدي لدى الكاشير السريع';

  @override
  String get orderTotal => 'الإجمالي المطلوب سداده';

  @override
  String get paymentSuccess => 'تم السداد بنجاح!';

  @override
  String get paymentSuccessSubtitle => 'شكرًا لتسوقك مع عربة SARE الذكية';

  @override
  String get receiptHint =>
      'امسح الرمز بهاتفك لحفظ الفاتورة الرقمية والمرور من البوابة.';

  @override
  String get receiptHashLabel => 'رقم الفاتورة';

  @override
  String get doneAndExit => 'تم — العودة لشاشة الترحيب';

  @override
  String get exitPass => 'تصريح خروج معتمد';

  @override
  String autoResetHint(int seconds) {
    return 'العودة لشاشة الترحيب خلال $seconds ثانية';
  }

  @override
  String get help => 'مساعدة';

  @override
  String get callStaff => 'استدعاء موظف';

  @override
  String get dismiss => 'إغلاق';

  @override
  String get ok => 'حسنًا';

  @override
  String get scaleReadyLabel => 'جاهزة للتسوق';

  @override
  String get scaleOfflineLabel => 'وضع الباركود';

  @override
  String get removeItemButton => 'حذف';

  @override
  String get endSessionButton => 'إنهاء الجلسة';

  @override
  String get endSessionConfirmTitle => 'إنهاء جلسة التسوق؟';

  @override
  String get endSessionConfirmMessage =>
      'سيتم إغلاق جلسة التسوق الحالية وإعادة العربة لشاشة الترحيب.';

  @override
  String get nfcModalTitle => 'تسجيل دخول الأعضاء (NFC)';

  @override
  String get nfcModalSubtitle => 'مرر بطاقة عضويتك أو اختر حسابك للبدء فورًا';

  @override
  String get nfcInputHint => 'أدخل رقم بطاقة العضوية...';

  @override
  String get nfcConnectButton => 'دخول بالبطاقة';

  @override
  String get guestShopperBadge => 'متسوق زائر';

  @override
  String memberShopperBadge(String name) {
    return 'العضو: $name';
  }

  @override
  String get liveScaleTitle => 'جميع المنتجات مؤكدة';

  @override
  String get autoTareBadge => 'جاهز';

  @override
  String get toleranceBadge => 'مؤكد';

  @override
  String get simulatorTitle => 'المنتجات السريعة (وضع التجربة)';

  @override
  String get simulatorSubtitle => 'اضغط لإضافة المنتج مباشرة إلى السلة';

  @override
  String get simulateValidScan => '+ مسح (مطابق)';

  @override
  String get simulateFraudScan => 'اختلاف وزن (+95جم)';

  @override
  String get staffModalTitle => 'تم إشعار موظف المساعدة';

  @override
  String staffModalMessage(String cartNumber, String location) {
    return 'تم إرسال تنبيه فوري لموظف المتجر وهو في طريقه إلى عربة رقم $cartNumber ($location).';
  }

  @override
  String get paymentMethodInstructionCard =>
      'مرر بطاقتك البنكية على قارئ الدفع اللاتلامسي بجانب الشاشة.';

  @override
  String get paymentMethodInstructionWallet =>
      'امسح رمز الدفع عبر تطبيق إنستا باي أو محفظتك البنكية لإتمام السداد.';

  @override
  String paymentMethodInstructionCashier(String cartNumber) {
    return 'توجه إلى كاشير المسار السريع وأبرز رقم العربة ($cartNumber) للسداد نقدًا.';
  }

  @override
  String get orderItemsHeading => 'مشترياتك في السلة';

  @override
  String get totalBasketWeight => 'إجمالي الوزن';

  @override
  String get pauseTimer => 'تثبيت الشاشة';

  @override
  String get resumeTimer => 'استئناف العد';
}
