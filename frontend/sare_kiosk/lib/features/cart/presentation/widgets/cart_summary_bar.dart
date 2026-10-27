import 'package:flutter/material.dart';

import '../../../../core/theme/app_colors.dart';
import '../../../../core/utils/kiosk_formatter.dart';
import '../../../../core/utils/kiosk_motion.dart';
import '../../../../core/utils/money.dart';
import '../../../../core/widgets/custom_text.dart';
import '../../../../core/widgets/kiosk_button.dart';
import '../../../../core/widgets/kiosk_price_text.dart';
import '../../../../core/widgets/kiosk_pulse_dot.dart';
import '../../../../core/widgets/kiosk_section_label.dart';
import '../../../../core/widgets/kiosk_surface_card.dart';
import '../../../../l10n/app_localizations.dart';

/// Side-panel summary: live load-cell telemetry, VAT breakdown, and the
/// primary checkout CTA. The telemetry module is the kiosk's verification
/// signature — pulsing dot, tolerance gate badge, verified weight.
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
    final sub = Money.formatMinor(
      split.subtotalMinor,
      locale,
      symbol: ' ${l10n.currencySymbol}',
    );
    final vat = Money.formatMinor(
      split.vatMinor,
      locale,
      symbol: ' ${l10n.currencySymbol}',
    );
    final weightFormatted =
        KioskFormatter.formatWeight(totalWeightG.toDouble(), locale);

    return KioskSurfaceCard(
      padding: const EdgeInsets.all(18),
      borderRadius: 18,
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          // Live Load-Cell Scale Telemetry — the verification signature.
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
            decoration: BoxDecoration(
              color: colors.verified.withValues(alpha: 0.07),
              borderRadius: BorderRadius.circular(14),
              border: Border.all(
                color: colors.verified.withValues(alpha: 0.28),
              ),
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    KioskPulseDot(
                      color: colors.verified,
                      size: 8,
                      animate: KioskMotion.ambientEnabled(context),
                    ),
                    const SizedBox(width: 4),
                    Expanded(
                      child: CustomText(
                        l10n.liveScaleTitle,
                        fontSize: 12,
                        fontWeight: FontWeight.w700,
                        color: colors.verified,
                        maxLines: 1,
                      ),
                    ),
                    const SizedBox(width: 6),
                    Container(
                      padding: const EdgeInsets.symmetric(
                        horizontal: 8,
                        vertical: 3,
                      ),
                      decoration: BoxDecoration(
                        color: colors.verified.withValues(alpha: 0.14),
                        borderRadius: BorderRadius.circular(7),
                      ),
                      child: CustomText(
                        l10n.toleranceBadge,
                        fontSize: 11,
                        fontWeight: FontWeight.w700,
                        color: colors.verified,
                        maxLines: 1,
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 8),
                Row(
                  crossAxisAlignment: CrossAxisAlignment.end,
                  children: [
                    Expanded(
                      child: CustomText(
                        l10n.totalBasketWeight,
                        fontSize: 12,
                        color: colors.textSecondary,
                        maxLines: 1,
                      ),
                    ),
                    const SizedBox(width: 8),
                    CustomText(
                      weightFormatted,
                      fontSize: 16,
                      fontWeight: FontWeight.w700,
                      color: colors.verified,
                      maxLines: 1,
                    ),
                  ],
                ),
              ],
            ),
          ),
          const SizedBox(height: 16),
          Row(
            crossAxisAlignment: CrossAxisAlignment.end,
            children: [
              Expanded(
                child: CustomText(
                  l10n.itemsCount(itemsCount),
                  fontSize: 13,
                  color: colors.textSecondary,
                  fontWeight: FontWeight.w600,
                  maxLines: 1,
                ),
              ),
              const SizedBox(width: 8),
              KioskPriceText(minor: totalMinor, amountSize: 26),
            ],
          ),
          const SizedBox(height: 14),
          _row(l10n.subtotal, sub, colors),
          const SizedBox(height: 6),
          _row(l10n.tax, vat, colors),
          const Padding(
            padding: EdgeInsets.symmetric(vertical: 12),
            child: KioskHairlineDivider(),
          ),
          Row(
            children: [
              Expanded(
                child: CustomText(
                  l10n.totalAmount,
                  fontSize: 15,
                  fontWeight: FontWeight.w700,
                  maxLines: 1,
                ),
              ),
              const SizedBox(width: 8),
              KioskPriceText(
                minor: totalMinor,
                amountSize: 20,
                amountColor: colors.brand,
              ),
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
            color: colors.textSecondary,
            maxLines: 1,
          ),
        ),
        const SizedBox(width: 8),
        CustomText(
          value,
          fontSize: 13,
          fontWeight: FontWeight.w600,
          color: colors.textSecondary,
          maxLines: 1,
        ),
      ],
    );
  }
}
