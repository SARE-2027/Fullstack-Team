import 'package:flutter/material.dart';

import '../../../../core/theme/app_colors.dart';
import '../../../../core/utils/kiosk_formatter.dart';
import '../../../../core/utils/money.dart';
import '../../../../core/widgets/custom_text.dart';
import '../../../../l10n/app_localizations.dart';
import '../../data/repositories/local_smart_cart_engine.dart';

/// Interactive hardware & catalog simulator strip for the 10.1" kiosk.
///
/// Enables instant testing of:
/// 1. Valid barcode / vision scan + matching load-cell weight delta (±15g).
/// 2. Fraud / weight discrepancy alert (barcode swapped or mismatched weight).
class SensorSimulatorStrip extends StatelessWidget {
  const SensorSimulatorStrip({
    super.key,
    required this.onSimulateScan,
    this.isBusy = false,
  });

  final void Function(KioskCatalogProduct product, {required bool isFraud}) onSimulateScan;
  final bool isBusy;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;
    final colors = context.colors;
    final locale = Localizations.localeOf(context).languageCode;

    return Container(
      padding: const EdgeInsets.fromLTRB(16, 12, 16, 12),
      decoration: BoxDecoration(
        color: colors.textFieldFill,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: colors.containerBorder),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        mainAxisSize: MainAxisSize.min,
        children: [
          Row(
            children: [
              Icon(
                Icons.sensors_rounded,
                size: 18,
                color: Theme.of(context).colorScheme.primary,
              ),
              const SizedBox(width: 8),
              Expanded(
                child: CustomText(
                  l10n.simulatorTitle,
                  fontSize: 13,
                  fontWeight: FontWeight.w800,
                  maxLines: 1,
                ),
              ),
              CustomText(
                l10n.simulatorSubtitle,
                fontSize: 11,
                color: colors.secondaryText,
                maxLines: 1,
              ),
            ],
          ),
          const SizedBox(height: 10),
          SizedBox(
            height: 98,
            child: ListView.separated(
              scrollDirection: Axis.horizontal,
              itemCount: LocalSmartCartEngine.catalog.length,
              separatorBuilder: (_, _) => const SizedBox(width: 10),
              itemBuilder: (context, index) {
                final product = LocalSmartCartEngine.catalog[index];
                final priceStr = Money.formatMinor(
                  product.priceMinor,
                  locale,
                  symbol: ' ${l10n.currencySymbol}',
                );
                final weightStr = KioskFormatter.formatWeight(
                  product.weightG.toDouble(),
                  locale,
                );

                return Container(
                  width: 245,
                  padding: const EdgeInsets.all(10),
                  decoration: BoxDecoration(
                    color: Theme.of(context).colorScheme.surface,
                    borderRadius: BorderRadius.circular(12),
                    border: Border.all(color: colors.containerBorder.withValues(alpha: 0.7)),
                  ),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Row(
                        children: [
                          Icon(
                            product.icon,
                            size: 18,
                            color: Theme.of(context).colorScheme.primary,
                          ),
                          const SizedBox(width: 8),
                          Expanded(
                            child: CustomText(
                              product.displayName(locale),
                              fontSize: 12,
                              fontWeight: FontWeight.w700,
                              maxLines: 1,
                            ),
                          ),
                        ],
                      ),
                      Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: [
                          CustomText(
                            priceStr,
                            fontSize: 12,
                            fontWeight: FontWeight.w800,
                          ),
                          CustomText(
                            weightStr,
                            fontSize: 11,
                            color: colors.secondaryText,
                          ),
                        ],
                      ),
                      Row(
                        children: [
                          Expanded(
                            child: _SimActionChip(
                              label: l10n.simulateValidScan,
                              color: colors.weightVerified,
                              enabled: !isBusy,
                              onTap: () => onSimulateScan(product, isFraud: false),
                            ),
                          ),
                          const SizedBox(width: 6),
                          Expanded(
                            child: _SimActionChip(
                              label: l10n.simulateFraudScan,
                              color: colors.weightDiscrepancy,
                              enabled: !isBusy,
                              onTap: () => onSimulateScan(product, isFraud: true),
                            ),
                          ),
                        ],
                      ),
                    ],
                  ),
                );
              },
            ),
          ),
        ],
      ),
    );
  }
}

class _SimActionChip extends StatelessWidget {
  const _SimActionChip({
    required this.label,
    required this.color,
    required this.enabled,
    required this.onTap,
  });

  final String label;
  final Color color;
  final bool enabled;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return Material(
      color: Colors.transparent,
      child: InkWell(
        onTap: enabled ? onTap : null,
        borderRadius: BorderRadius.circular(7),
        child: Container(
          padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 4),
          decoration: BoxDecoration(
            color: color.withValues(alpha: 0.14),
            borderRadius: BorderRadius.circular(7),
            border: Border.all(color: color.withValues(alpha: 0.45)),
          ),
          alignment: Alignment.center,
          child: CustomText(
            label,
            fontSize: 10,
            fontWeight: FontWeight.w700,
            color: color,
            maxLines: 1,
          ),
        ),
      ),
    );
  }
}
