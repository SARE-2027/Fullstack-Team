import '../../features/cart/presentation/cubit/cart_state.dart';
import '../../l10n/app_localizations.dart';
import '../theme/app_typography.dart';
import 'kiosk_formatter.dart';

abstract final class LanguageHelper {
  static const supportedLocales = ['ar', 'en'];

  static String getNativeName(String code) {
    switch (code.toLowerCase()) {
      case 'ar':
        return 'العربية';
      case 'en':
        return 'English';
      default:
        return code.toUpperCase();
    }
  }

  static String getFontFamily(String code) {
    if (code == 'ar') {
      return AppTypography.arabicFontFamily;
    }
    return AppTypography.englishFontFamily;
  }

  /// Localizes a weight mismatch or cart error banner into the active [locale]
  /// so shoppers never see raw English backend/engine strings when browsing in Arabic.
  static String localizeBannerMismatch({
    required CartState cartState,
    required AppLocalizations l10n,
    required String locale,
  }) {
    final isAr = locale.toLowerCase().startsWith('ar');
    if (cartState.lastExpectedG != null && cartState.lastMeasuredG != null) {
      if (isAr) {
        final itemName = cartState.lastItemNameAr;
        final expected =
            KioskFormatter.formatNumber(cartState.lastExpectedG, 'ar');
        final measured =
            KioskFormatter.formatNumber(cartState.lastMeasuredG, 'ar');
        if (itemName != null && itemName.isNotEmpty) {
          return 'تنبيه اختلاف الوزن في «$itemName» — المتوقع $expected جم والمقاس $measured جم. لم تتم الإضافة.';
        }
        return 'تنبيه اختلاف الوزن! المتوقع $expected جم والمقاس $measured جم — لم تتم الإضافة.';
      } else {
        final itemName = cartState.lastItemNameEn;
        if (itemName != null && itemName.isNotEmpty) {
          return 'Weight mismatch on "$itemName" — expected ${cartState.lastExpectedG}g, measured ${cartState.lastMeasuredG}g.';
        }
        return 'Weight mismatch! Expected ${cartState.lastExpectedG}g, measured ${cartState.lastMeasuredG}g.';
      }
    }

    return localizeError(
      cartState.bannerMessage,
      l10n,
      locale,
      fallback: l10n.weightMismatchWarning,
    );
  }

  /// Translates any raw backend (.NET RFC 7807) or local engine error message
  /// into natural Arabic or English according to [locale].
  static String localizeError(
    String? raw,
    AppLocalizations l10n,
    String locale, {
    String? fallback,
  }) {
    final defaultMsg = fallback ?? l10n.networkError;
    if (raw == null || raw.trim().isEmpty) return defaultMsg;

    final isAr = locale.toLowerCase().startsWith('ar');
    final trimmed = raw.trim();
    final lower = trimmed.toLowerCase();

    // 1. Unknown barcode / product not found
    if (lower.contains('no active product') ||
        lower.contains('matching barcode') ||
        (lower.contains('barcode') && lower.contains('not found')) ||
        (lower.contains('variant') && lower.contains('not found'))) {
      final match = RegExp(r'["\x27]([^"\x27]+)["\x27]').firstMatch(trimmed);
      final code = match?.group(1);
      if (isAr) {
        if (code != null && code.isNotEmpty) {
          return 'لم يتم العثور على منتج مسجل بالباركود «$code». تأكد من الرقم وحاول مجددًا.';
        }
        return 'لم يتم العثور على منتج مسجل بهذا الباركود. تأكد من الرقم وحاول مجددًا.';
      } else {
        if (code != null && code.isNotEmpty) {
          return 'No product found matching barcode "$code". Please verify the code and try again.';
        }
        return 'Product barcode not found in catalog. Please verify the code.';
      }
    }

    // 2. Weight discrepancy / mismatch
    if (lower.contains('weight mismatch') ||
        lower.contains('weight discrepancy') ||
        (lower.contains('expected') && lower.contains('measured'))) {
      return l10n.weightMismatchWarning;
    }

    // 3. Session not found / expired
    if (lower.contains('active session not found') ||
        lower.contains('session not found') ||
        lower.contains('no active session')) {
      return isAr
          ? 'الجلسة الحالية غير نشطة. يرجى بدء جلسة تسوق جديدة.'
          : 'Active shopping session not found. Please start a new session.';
    }

    // 4. Item removal error
    if (lower.contains('could not remove item')) {
      return isAr
          ? 'تعذر حذف المنتج من السلة. يرجى المحاولة مرة أخرى.'
          : 'Could not remove item from your basket. Please try again.';
    }

    // 5. Cart offline / invalid cart / battery critical
    if (lower.contains('invalid cart') ||
        lower.contains('cart not found') ||
        lower.contains('disabled') ||
        lower.contains('battery') ||
        lower.contains('maintenance')) {
      return l10n.cartOffline;
    }

    // 6. Session start / close failures
    if (lower.contains('failed to start') || lower.contains('session failed')) {
      return l10n.sessionFailed;
    }
    if (lower.contains('failed to close')) {
      return isAr
          ? 'تعذر إغلاق الجلسة حاليًا. حاول مرة أخرى.'
          : 'Could not close the session. Please try again.';
    }

    // 7. Network / timeout / socket / HTTP client exceptions
    if (lower.contains('timeout') ||
        lower.contains('timed out') ||
        lower.contains('unreachable') ||
        lower.contains('socket') ||
        lower.contains('connection') ||
        lower.contains('network') ||
        lower.contains('clientexception') ||
        lower.contains('empty response') ||
        lower.contains('empty checkout')) {
      return l10n.networkError;
    }

    // 8. Unauthorized / token mismatch
    if (lower.contains('unauthorized') ||
        lower.contains('forbidden') ||
        lower.contains('token')) {
      return isAr
          ? 'غير مصرح بالعملية. يرجى الاستعانة بموظف المتجر.'
          : 'Unauthorized cart operation. Please ask store staff for assistance.';
    }

    // 9. If Arabic is active and the message contains no Arabic script at all,
    // never leak raw English exceptions to the shopper.
    if (isAr && !RegExp(r'[\u0600-\u06FF]').hasMatch(trimmed)) {
      return defaultMsg;
    }

    return trimmed;
  }
}

