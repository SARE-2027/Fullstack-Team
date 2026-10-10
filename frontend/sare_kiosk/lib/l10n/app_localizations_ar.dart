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
  String get emptyCartMessage =>
      'السلة فارغة حاليًّا. امسح باركود المنتج لتبدأ التسوق!';

  @override
  String get itemAddedSuccess => 'تمت إضافة المنتج ومطابقة الوزن بنجاح';

  @override
  String get itemRemoved => 'تم حذف المنتج من السلة';

  @override
  String get weightVerifying => 'جارٍ التحقق من وزن السلعة...';

  @override
  String get weightMatchSuccess => 'تمت مطابقة الوزن بدقة (±15 جم)';

  @override
  String get weightMismatchWarning =>
      'تم رصد اختلاف في الوزن! يرجى مراجعة آخر منتج وُضع في السلة.';

  @override
  String get scanPrompt => 'امسح باركود السلعة أمام الماسح';

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
}
