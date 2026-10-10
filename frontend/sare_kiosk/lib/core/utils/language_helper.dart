import '../theme/app_typography.dart';

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
}
