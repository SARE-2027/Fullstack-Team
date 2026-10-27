import 'package:intl/intl.dart';

/// Minor-unit helpers. Backend stores monetary values as integer minors (EGP × 100).
abstract final class Money {
  static double toMajor(int minor) => minor / 100.0;

  static String formatMinor(int minor, String localeCode, {String symbol = ''}) {
    final loc = localeCode == 'ar' ? 'ar_EG' : 'en_US';
    final major = toMajor(minor);
    return NumberFormat.currency(locale: loc, symbol: symbol, decimalDigits: 2).format(major);
  }

  /// Splits a minor amount into the raw localized amount string and the
  /// currency marker, so [KioskPriceText] can render the premium
  /// large-number + small-raised-symbol composition.
  static ({String amount, String symbol}) formatMinorSplit(
    int minor,
    String localeCode, {
    required String symbol,
  }) {
    final amount = formatMinor(minor, localeCode, symbol: '').trim();
    return (amount: amount, symbol: symbol);
  }

  /// 14% VAT included model: vat = total - subtotal, but shown explicitly at checkout.
  static ({int subtotalMinor, int vatMinor}) splitVat(int totalMinor, {double vatRate = 0.14}) {
    // total already includes VAT; derive subtotal = total / (1 + vatRate)
    final subtotal = (totalMinor / (1 + vatRate)).round();
    return (subtotalMinor: subtotal, vatMinor: totalMinor - subtotal);
  }
}
