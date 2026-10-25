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
      'تسوق بسلاسة وسرعة مع عربة SARE المدعومة بالذكاء الاصطناعي والميزان الحساس';

  @override
  String get startAsGuest => 'ابدأ التسوق كزائر / ضيف';

  @override
  String get startAsGuestDescription =>
      'بدون أي تسجيل مسبق. تسوق وادفع وغادر في أي وقت.';

  @override
  String get orDivider => 'أو';

  @override
  String get loyaltySignIn => 'تسجيل دخول الأعضاء';

  @override
  String get tapCardOrQr =>
      'مرر بطاقة العضوية (NFC) على مقبض العربة للتعرف على حسابك فورًا';

  @override
  String get cartTitle => 'سلة المشتريات';

  @override
  String get emptyCartTitle => 'السلة فارغة حاليًّا';

  @override
  String get emptyCartMessage =>
      'امسح باركود المنتج أو اختر صنفًا من المحاكي أدناه لتبدأ التسوق';

  @override
  String get emptyCartHint =>
      'ضع المنتج داخل السلة بعد المسح — يتم التحقق من الوزن بدقة ±15 جم';

  @override
  String get itemAddedSuccess => 'تمت إضافة المنتج ومطابقة الوزن بنجاح';

  @override
  String get itemRemoved => 'تم حذف المنتج وتحديث وزن السلة';

  @override
  String get itemRestored => 'تمت إعادة المنتج إلى سلتك';

  @override
  String get undo => 'تراجع';

  @override
  String get hide => 'إخفاء';

  @override
  String get show => 'عرض';

  @override
  String get weightVerifying => 'جارٍ التحقق من وزن السلعة...';

  @override
  String get weightMatchSuccess => 'تمت مطابقة الوزن بدقة (±15 جم)';

  @override
  String get weightMismatchWarning =>
      'تم رصد اختلاف في الوزن! يرجى مراجعة آخر منتج وُضع في السلة.';

  @override
  String weightMismatchDetail(String expected, String measured) {
    return 'المتوقع $expected بينما المقاس $measured — لم تتم الإضافة';
  }

  @override
  String get scanPrompt => 'امسح باركود السلعة أمام الماسح أو أدخله يدويًّا';

  @override
  String get barcodeHint => 'رقم الباركود (مثال: 6223000123456)';

  @override
  String get addItem => 'إضافة المنتج';

  @override
  String get verifyWeightHint =>
      'أدخل الوزن المقاس (جم) أو اتركه فارغًا للمطابقة التلقائية من الكتالوج';

  @override
  String get weightGramsHint => 'الوزن (جم)';

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
  String get scaleOfflineHelp =>
      'الميزان غير متصل — يمكن إضافة المنتجات بتأكيد الباركود';

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
      'امسح هذا الرمز بكاميرا هاتفك لعرض الفاتورة الرقمية وحفظها، كما يمثل تصريح الخروج الذكي من البوابة.';

  @override
  String get receiptHashLabel => 'تصريح الفاتورة';

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
  String get scaleReadyLabel => 'الميزان متصل';

  @override
  String get scaleOfflineLabel => 'الميزان غير متصل';

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
  String get nfcModalSubtitle =>
      'مرر بطاقة عضويتك على مقبض العربة أو اختر بطاقة عضوية تجريبية أدناه';

  @override
  String get nfcInputHint => 'أدخل معرف بطاقة NFC (مثال: 04:A2:19:7C)';

  @override
  String get nfcConnectButton => 'دخول بالبطاقة';

  @override
  String get guestShopperBadge => 'متسوق زائر';

  @override
  String memberShopperBadge(String name) {
    return 'العضو: $name';
  }

  @override
  String get liveScaleTitle => 'قراءة ميزان السلة الحساس';

  @override
  String get autoTareBadge => 'تصفير تلقائي: 0 جم';

  @override
  String get toleranceBadge => 'دقة ±15 جم';

  @override
  String get simulatorTitle => 'محاكي الحساسات وكتالوج السوبرماركت';

  @override
  String get simulatorSubtitle =>
      'اضغط على أي منتج لمحاكاة مسح الباركود والتحقق الفوري من الوزن';

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
      'مرر بطاقة فيزا أو ماستركارد أو ميزة على قارئ الدفع اللاتلامسي بجانب الشاشة.';

  @override
  String get paymentMethodInstructionWallet =>
      'امسح رمز الدفع الديناميكي عبر تطبيق إنستا باي أو محفظتك البنكية لإتمام السداد.';

  @override
  String paymentMethodInstructionCashier(String cartNumber) {
    return 'توجه إلى كاشير المسار السريع وأبرز رقم العربة ($cartNumber) للسداد نقدًا.';
  }

  @override
  String get orderItemsHeading => 'الأصناف المؤكدة في السلة';

  @override
  String get totalBasketWeight => 'إجمالي الوزن المطابق';

  @override
  String get pauseTimer => 'تثبيت الشاشة';

  @override
  String get resumeTimer => 'استئناف العد';
}
