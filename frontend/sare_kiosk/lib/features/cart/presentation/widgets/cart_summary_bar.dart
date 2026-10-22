import 'package:flutter/material.dart';

import '../../../../core/theme/app_colors.dart';
import '../../../../core/utils/kiosk_formatter.dart';
import '../../../../core/utils/money.dart';
import '../../../../core/widgets/custom_text.dart';
import '../../../../core/widgets/kiosk_button.dart';
import '../../../../l10n/app_localizations.dart';

class CartSummaryBar extends StatelessWidget {
  const CartSummaryBar({
    super.key,
    required this.itemsCount,
    required this.totalMinor,
    required this.totalWeightG,
    required this.onCheckout,
    this.isBusy = false,
  });

  final int itemsCount;
  final int totalMinor;
  final int totalWeightG;
  final VoidCallback onCheckout;
  final bool isBusy;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;
    final locale = Localizations.localeOf(context).languageCode;
    final colors = context.colors;
    final split = Money.splitVat(totalMinor);
    final sub = Money.formatMinor(split.subtotalMinor, locale, symbol: ' ${l10n.currencySymbol}');
    final vat = Money.formatMinor(split.vatMinor, locale, symbol: ' ${l10n.currencySymbol}');
    final tot = Money.formatMinor(totalMinor, locale, symbol: ' ${l10n.currencySymbol}');
    final weightFormatted = KioskFormatter.formatWeight(totalWeightG.toDouble(), locale);

    return Container(
      padding: const EdgeInsets.all(18),
      decoration: BoxDecoration(
        color: colors.textFieldFill,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: colors.containerBorder, width: 1.2),
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          // Live Load-Cell Scale Telemetry Box
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
            decoration: BoxDecoration(
              color: Theme.of(context).colorScheme.surface,
              borderRadius: BorderRadius.circular(12),
              border: Border.all(color: colors.containerBorder.withValues(alpha: 0.6)),
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Icon(
                      Icons.scale_rounded,
                      size: 16,
                      color: colors.weightVerified,
                    ),
                    const SizedBox(width: 6),
                    Expanded(
                      child: CustomText(
                        l10n.liveScaleTitle,
                        fontSize: 12,
                        fontWeight: FontWeight.w700,
                        maxLines: 1,
                      ),
                    ),
                    const SizedBox(width: 6),
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                      decoration: BoxDecoration(
                        color: colors.weightVerified.withValues(alpha: 0.14),
                        borderRadius: BorderRadius.circular(6),
                      ),
                      child: CustomText(
                        l10n.toleranceBadge,
                        fontSize: 11,
                        fontWeight: FontWeight.w700,
                        color: colors.weightVerified,
                        maxLines: 1,
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 6),
                Row(
                  children: [
                    Expanded(
                      child: CustomText(
                        l10n.totalBasketWeight,
                        fontSize: 12,
                        color: colors.secondaryText,
                        maxLines: 1,
                      ),
                    ),
                    const SizedBox(width: 8),
                    CustomText(
                      weightFormatted,
                      fontSize: 14,
                      fontWeight: FontWeight.w800,
                      color: colors.weightVerified,
                      maxLines: 1,
                    ),
                  ],
                ),
              ],
            ),
          ),
          const SizedBox(height: 14),
          Row(
            children: [
              Expanded(
                child: CustomText(
                  l10n.itemsCount(itemsCount),
                  fontSize: 13,
                  color: colors.secondaryText,
                  fontWeight: FontWeight.w600,
                  maxLines: 1,
                ),
              ),
              const SizedBox(width: 8),
              CustomText(tot, fontSize: 18, fontWeight: FontWeight.w900, maxLines: 1),
            ],
          ),
          const SizedBox(height: 10),
          _row(l10n.subtotal, sub, colors),
          const SizedBox(height: 6),
          _row(l10n.tax, vat, colors),
          const Divider(height: 18),
          Row(
            children: [
              Expanded(
                child: CustomText(
                  l10n.totalAmount,
                  fontSize: 15,
                  fontWeight: FontWeight.bold,
                  maxLines: 1,
                ),
              ),
              const SizedBox(width: 8),
              CustomText(tot, fontSize: 17, fontWeight: FontWeight.w900, maxLines: 1),
            ],
          ),
          const SizedBox(height: 16),
          KioskButton(
            text: l10n.checkoutButton,
            icon: Icons.payments_rounded,
            isLoading: isBusy,
            onPressed: itemsCount == 0 || isBusy ? null : onCheckout,
          ),
        ],
      ),
    );
  }

  Widget _row(String label, String value, AppColorTheme colors) {
    return Row(
      children: [
        Expanded(
          child: CustomText(
            label,
            fontSize: 13,
            color: colors.secondaryText,
            maxLines: 1,
          ),
        ),
        const SizedBox(width: 8),
        CustomText(
          value,
          fontSize: 13,
          fontWeight: FontWeight.w600,
          color: colors.secondaryText,
          maxLines: 1,
        ),
      ],
    );
  }
}
