import 'package:flutter/material.dart';

import '../../l10n/app_localizations.dart';
import '../theme/app_colors.dart';
import '../utils/money.dart';
import 'custom_text.dart';

/// Receipt-grade price: large tabular-feeling amount with a smaller, lighter
/// currency marker sitting raised beside it — the classic premium-invoice
/// treatment.
class KioskPriceText extends StatelessWidget {
  const KioskPriceText({
    super.key,
    required this.minor,
    this.amountSize = 20,
    this.amountWeight = FontWeight.w800,
    this.amountColor,
    this.symbolColor,
  });

  final int minor;
  final double amountSize;
  final FontWeight amountWeight;
  final Color? amountColor;
  final Color? symbolColor;

  @override
  Widget build(BuildContext context) {
    final colors = context.colors;
    final l10n = AppLocalizations.of(context)!;
    final locale = Localizations.localeOf(context).languageCode;
    final parts = Money.formatMinorSplit(
      minor,
      locale,
      symbol: l10n.currencySymbol,
    );

    return Row(
      mainAxisSize: MainAxisSize.min,
      crossAxisAlignment: CrossAxisAlignment.end,
      children: [
        CustomText(
          parts.amount,
          fontSize: amountSize,
          fontWeight: amountWeight,
          color: amountColor,
          letterSpacing: -0.3,
        ),
        const SizedBox(width: 4),
        Padding(
          padding: EdgeInsets.only(bottom: amountSize * 0.14),
          child: CustomText(
            parts.symbol,
            fontSize: (amountSize * 0.52).clamp(10, 15),
            fontWeight: FontWeight.w600,
            color: symbolColor ?? colors.textSecondary,
          ),
        ),
      ],
    );
  }
}
