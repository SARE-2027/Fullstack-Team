import 'package:flutter/material.dart';

import '../../l10n/app_localizations.dart';
import '../theme/app_colors.dart';
import '../theme/app_typography.dart';
import '../utils/money.dart';
import 'custom_text.dart';

/// Receipt-grade kinetic price readout: tabular odometer numerals with a
/// raised currency marker that rolls smoothly with spring physics whenever
/// the basket total changes.
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
        AnimatedSwitcher(
          duration: AppTypography.base,
          switchInCurve: AppTypography.enterCurve,
          switchOutCurve: AppTypography.exitCurve,
          transitionBuilder: (child, animation) {
            return FadeTransition(
              opacity: animation,
              child: SlideTransition(
                position: Tween<Offset>(
                  begin: const Offset(0, 0.22),
                  end: Offset.zero,
                ).animate(animation),
                child: child,
              ),
            );
          },
          child: CustomText(
            parts.amount,
            key: ValueKey<String>('price-${parts.amount}'),
            fontSize: amountSize,
            fontWeight: amountWeight,
            color: amountColor,
            letterSpacing: -0.4,
            tabularFigures: true,
          ),
        ),
        const SizedBox(width: 4),
        Padding(
          padding: EdgeInsets.only(bottom: amountSize * 0.14),
          child: CustomText(
            parts.symbol,
            fontSize: (amountSize * 0.52).clamp(10, 15),
            fontWeight: FontWeight.w700,
            color: symbolColor ?? colors.textSecondary,
          ),
        ),
      ],
    );
  }
}
