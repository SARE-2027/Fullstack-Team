import 'package:flutter/material.dart';

import '../theme/app_colors.dart';
import 'custom_text.dart';

/// Editorial micro-label: accent tick + quiet small-caps-feeling caption.
/// Letter-spacing is applied for Latin only — Arabic script must keep its
/// natural joining.
class KioskSectionLabel extends StatelessWidget {
  const KioskSectionLabel(
    this.text, {
    super.key,
    this.color,
    this.accent,
  });

  final String text;
  final Color? color;

  /// Accent tick color (defaults to brand).
  final Color? accent;

  @override
  Widget build(BuildContext context) {
    final colors = context.colors;
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';

    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Container(
          width: 3.5,
          height: 15,
          decoration: BoxDecoration(
            color: accent ?? colors.brand,
            borderRadius: BorderRadius.circular(2),
          ),
        ),
        const SizedBox(width: 9),
        Flexible(
          child: CustomText(
            text,
            fontSize: 12.5,
            fontWeight: FontWeight.w700,
            color: color ?? colors.textSecondary,
            letterSpacing: isArabic ? null : 0.7,
            maxLines: 1,
          ),
        ),
      ],
    );
  }
}

/// Hairline divider that fades at both edges — quieter than a hard rule.
class KioskHairlineDivider extends StatelessWidget {
  const KioskHairlineDivider({super.key, this.indent = 0});

  /// Horizontal inset where the line starts being visible.
  final double indent;

  @override
  Widget build(BuildContext context) {
    final colors = context.colors;
    return Container(
      margin: EdgeInsets.symmetric(horizontal: indent),
      height: 1,
      decoration: BoxDecoration(
        gradient: LinearGradient(
          colors: [
            colors.border.withValues(alpha: 0.0),
            colors.border,
            colors.border.withValues(alpha: 0.0),
          ],
          stops: const [0.0, 0.5, 1.0],
        ),
      ),
    );
  }
}
