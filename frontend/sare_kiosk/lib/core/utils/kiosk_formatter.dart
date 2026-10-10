import 'package:intl/intl.dart';

/// Central formatting utility for the smart cart kiosk, handling localized numbers,
/// currencies (EGP / ج.م), and physical weight measurements (grams / kilograms).
abstract final class KioskFormatter {
  static String _getFullLocale(String localeCode) {
    if (localeCode == 'ar') return 'ar_EG';
    if (localeCode == 'en') return 'en_US';
    return localeCode;
  }

  /// Formats a standard numeric value
  static String formatNumber(dynamic number, String localeCode) {
    if (number == null) return '';
    final num parsed = _parseNumber(number);
    final String fullLocale = _getFullLocale(localeCode);
    return NumberFormat('#', fullLocale).format(parsed);
  }

  /// Formats with thousand grouping separators (e.g., 1,500)
  static String formatNumberWithSeparator(dynamic number, String localeCode) {
    if (number == null) return '';
    final num parsed = _parseNumber(number);
    final String fullLocale = _getFullLocale(localeCode);
    return NumberFormat.decimalPattern(fullLocale).format(parsed);
  }

  /// Formats currency with customizable symbol and decimal places
  static String formatCurrency(
    dynamic number,
    String localeCode, {
    String symbol = '',
    int decimalDigits = 2,
  }) {
    if (number == null) return '';
    final num parsed = _parseNumber(number);
    final String fullLocale = _getFullLocale(localeCode);
    return NumberFormat.currency(
      locale: fullLocale,
      symbol: symbol,
      decimalDigits: decimalDigits,
    ).format(parsed);
  }

  /// Formats a weight measurement dynamically in grams or kilograms
  static String formatWeight(
    double weightInGrams,
    String localeCode, {
    bool forceKilograms = false,
  }) {
    final isArabic = localeCode.startsWith('ar');
    if (forceKilograms || weightInGrams.abs() >= 1000) {
      final inKg = weightInGrams / 1000.0;
      final formatted = formatCustom(inKg, localeCode, '#,##0.00');
      final unit = isArabic ? 'كجم' : 'kg';
      return '$formatted $unit';
    } else {
      final formatted =
          formatNumberWithSeparator(weightInGrams.round(), localeCode);
      final unit = isArabic ? 'جم' : 'g';
      return '$formatted $unit';
    }
  }

  /// Formats custom patterns
  static String formatCustom(
    dynamic number,
    String localeCode,
    String pattern,
  ) {
    if (number == null) return '';
    final num parsed = _parseNumber(number);
    final String fullLocale = _getFullLocale(localeCode);
    return NumberFormat(pattern, fullLocale).format(parsed);
  }

  static num _parseNumber(dynamic value) {
    if (value is num) return value;
    if (value is String) {
      return num.tryParse(value.replaceAll(',', '')) ?? 0;
    }
    return 0;
  }
}
