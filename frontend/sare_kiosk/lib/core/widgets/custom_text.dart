import 'package:flutter/material.dart';

import '../theme/app_typography.dart';

/// Touch-kiosk text widget featuring Arabic baseline vertical alignment hardening
/// (`forceStrutHeight`), strikethrough styling for discount prices, and typography presets.
class CustomText extends StatelessWidget {
  const CustomText(
    this.text, {
    super.key,
    this.fontSize,
    this.fontWeight,
    this.color,
    this.textAlign,
    this.maxLines,
    this.forceStrutHeight = true,
    this.isStrikethrough = false,
    this.decorationColor,
    this.fontFamily,
    this.letterSpacing,
  });

  final String text;
  final double? fontSize;
  final FontWeight? fontWeight;
  final Color? color;
  final TextAlign? textAlign;
  final int? maxLines;

  /// Prevents Arabic vertical glyph shifting relative to adjacent latin numerals
  final bool forceStrutHeight;

  /// Used for displaying crossed-out original prices on discount items
  final bool isStrikethrough;
  final Color? decorationColor;
  final String? fontFamily;
  final double? letterSpacing;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final resolvedFontFamily = fontFamily ??
        (isArabic
            ? AppTypography.arabicFontFamily
            : AppTypography.englishFontFamily);

    return Text(
      text,
      textAlign: textAlign ?? TextAlign.start,
      maxLines: maxLines,
      overflow: maxLines != null ? TextOverflow.ellipsis : null,
      strutStyle: forceStrutHeight
          ? StrutStyle(
              forceStrutHeight: true,
              fontSize: fontSize,
              fontFamily: resolvedFontFamily,
            )
          : null,
      style: TextStyle(
        fontFamily: resolvedFontFamily,
        fontSize: fontSize,
        fontWeight: fontWeight ?? FontWeight.normal,
        color: color ?? theme.colorScheme.onSurface,
        letterSpacing: letterSpacing,
        decoration: isStrikethrough
            ? TextDecoration.lineThrough
            : TextDecoration.none,
        decorationColor: decorationColor ?? color ?? theme.colorScheme.onSurface,
      ),
    );
  }
}
