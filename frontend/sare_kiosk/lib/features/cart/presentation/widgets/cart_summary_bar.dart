import 'package:flutter/material.dart';

import '../../../../core/theme/app_colors.dart';
import '../../../../core/utils/money.dart';
import '../../../../core/widgets/custom_text.dart';
import '../../../../core/widgets/kiosk_button.dart';
import '../../../../core/widgets/kiosk_price_text.dart';
import '../../../../core/widgets/kiosk_section_label.dart';
import '../../../../core/widgets/kiosk_surface_card.dart';
import '../../../../l10n/app_localizations.dart';

/// Clean, luxury Order Summary card for the active shopping screen.
///
/// Focuses 100% on what the shopper cares about: item count, subtotal, VAT,
/// crystal-clear total price, a calm verification status pill, and the
/// primary Checkout CTA.
class CartSummaryBar extends StatelessWidget {
  const CartSummaryBar({
    super.key,
    required this.itemsCount,
    required this.totalMinor,
    required this.totalWeightG,
    required this.onCheckout,
    this.isBusy = false,
    this.hasDiscrepancy = false,
    this.isVerifying = false,
  });

  final int itemsCount;
  final int totalMinor;
  final int totalWeightG;
  final VoidCallback onCheckout;
  final bool isBusy;
  final bool hasDiscrepancy;
  final bool isVerifying;

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

    return KioskSurfaceCard(
      padding: const EdgeInsets.all(20),
      borderRadius: 20,
      borderColor: hasDiscrepancy
          ? colors.danger.withValues(alpha: 0.45)
          : colors.border,
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          // Header: Cart title + Item count pill
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Flexible(
                child: CustomText(
                  l10n.cartTitle,
                  fontSize: 16,
                  fontWeight: FontWeight.w800,
                  maxLines: 1,
                ),
              ),
              const SizedBox(width: 8),
              Container(
                padding: const EdgeInsets.symmetric(
                  horizontal: 10,
                  vertical: 4,
                ),
                decoration: BoxDecoration(
                  color: colors.brand.withValues(alpha: 0.12),
                  borderRadius: BorderRadius.circular(999),
                  border: Border.all(
                    color: colors.brand.withValues(alpha: 0.25),
                  ),
                ),
                child: CustomText(
                  l10n.itemsCount(itemsCount),
                  fontSize: 12.5,
                  fontWeight: FontWeight.w700,
                  color: colors.brand,
                  tabularFigures: true,
                  maxLines: 1,
                ),
              ),
            ],
          ),
          const SizedBox(height: 16),
          // Hero Total Display Box
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
            decoration: BoxDecoration(
              color: colors.surfaceAlt,
              borderRadius: BorderRadius.circular(16),
              border: Border.all(color: colors.border),
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                CustomText(
                  l10n.totalAmount,
                  fontSize: 12.5,
                  color: colors.textSecondary,
                  fontWeight: FontWeight.w600,
                ),
                const SizedBox(height: 6),
                Align(
                  alignment: AlignmentDirectional.centerEnd,
                  child: KioskPriceText(
                    minor: totalMinor,
                    amountSize: 28,
                    amountColor: colors.brand,
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(height: 16),
          _row(l10n.subtotal, sub, colors),
          const SizedBox(height: 8),
          _row(l10n.tax, vat, colors),
          if (itemsCount > 0 || hasDiscrepancy) ...[
            const Padding(
              padding: EdgeInsets.symmetric(vertical: 12),
              child: KioskHairlineDivider(),
            ),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 9),
              decoration: BoxDecoration(
                color: (hasDiscrepancy ? colors.danger : colors.verified)
                    .withValues(alpha: 0.10),
                borderRadius: BorderRadius.circular(12),
                border: Border.all(
                  color: (hasDiscrepancy ? colors.danger : colors.verified)
                      .withValues(alpha: 0.26),
                ),
              ),
              child: Row(
                children: [
                  Icon(
                    hasDiscrepancy
                        ? Icons.info_outline_rounded
                        : Icons.verified_rounded,
                    size: 16,
                    color: hasDiscrepancy ? colors.danger : colors.verified,
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: CustomText(
                      hasDiscrepancy
                          ? l10n.weightMismatchWarning
                          : l10n.liveScaleTitle,
                      fontSize: 12,
                      fontWeight: FontWeight.w700,
                      color: hasDiscrepancy ? colors.danger : colors.verified,
                      maxLines: 2,
                    ),
                  ),
                ],
              ),
            ),
          ],
          const SizedBox(height: 16),
          KioskButton(
            text: l10n.checkoutButton,
            height: 54,
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
          tabularFigures: true,
          maxLines: 1,
        ),
      ],
    );
  }
}
