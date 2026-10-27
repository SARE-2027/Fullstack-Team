import 'package:flutter/material.dart';

import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_typography.dart';
import '../../../../core/utils/kiosk_formatter.dart';
import '../../../../core/utils/money.dart';
import '../../../../core/widgets/custom_text.dart';
import '../../../../l10n/app_localizations.dart';
import '../../data/repositories/local_smart_cart_engine.dart';

/// Interactive hardware & catalog simulator for the 10.1" kiosk.
///
/// Enables instant testing of:
/// 1. Valid barcode / vision scan + matching load-cell weight delta (±15g).
/// 2. Fraud / weight discrepancy alert (barcode swapped or mismatched weight).
///
/// Collapsible so real shoppers see a clean cart screen while demo operators
/// keep one tap access to the full sensor bench.
class SensorSimulatorStrip extends StatefulWidget {
  const SensorSimulatorStrip({
    super.key,
    required this.onSimulateScan,
    this.isBusy = false,
  });

  final void Function(KioskCatalogProduct product, {required bool isFraud})
      onSimulateScan;
  final bool isBusy;

  @override
  State<SensorSimulatorStrip> createState() => _SensorSimulatorStripState();
}

class _SensorSimulatorStripState extends State<SensorSimulatorStrip> {
  bool _expanded = true;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;
    final colors = context.colors;
    final locale = Localizations.localeOf(context).languageCode;

    return Container(
      padding: const EdgeInsets.fromLTRB(16, 12, 16, 14),
      decoration: BoxDecoration(
        color: colors.surface,
        borderRadius: BorderRadius.circular(18),
        border: Border.all(color: colors.border),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        mainAxisSize: MainAxisSize.min,
        children: [
          Row(
            children: [
              Container(
                padding: const EdgeInsets.all(7),
                decoration: BoxDecoration(
                  color: colors.brand.withValues(alpha: 0.10),
                  borderRadius: BorderRadius.circular(9),
                ),
                child: Icon(
                  Icons.sensors_rounded,
                  size: 16,
                  color: colors.brand,
                ),
              ),
              const SizedBox(width: 10),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    CustomText(
                      l10n.simulatorTitle,
                      fontSize: 13,
                      fontWeight: FontWeight.w700,
                      maxLines: 1,
                    ),
                    if (_expanded)
                      CustomText(
                        l10n.simulatorSubtitle,
                        fontSize: 11,
                        color: colors.textFaint,
                        maxLines: 1,
                      ),
                  ],
                ),
              ),
              const SizedBox(width: 8),
              _CollapseChip(
                expanded: _expanded,
                label: _expanded ? l10n.hide : l10n.show,
                onTap: () => setState(() => _expanded = !_expanded),
              ),
            ],
          ),
          AnimatedSize(
            duration: AppTypography.slow,
            curve: Curves.easeInOutCubic,
            alignment: Alignment.topCenter,
            child: _expanded
                ? Padding(
                    padding: const EdgeInsets.only(top: 10),
                    child: SizedBox(
                      height: 118,
                      child: ListView.separated(
                          scrollDirection: Axis.horizontal,
                          itemCount: LocalSmartCartEngine.catalog.length,
                          separatorBuilder: (_, _) => const SizedBox(width: 10),
                          itemBuilder: (context, index) {
                            final product =
                                LocalSmartCartEngine.catalog[index];
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
                              width: 250,
                              padding: const EdgeInsets.all(10),
                              decoration: BoxDecoration(
                                color: colors.surfaceAlt,
                                borderRadius: BorderRadius.circular(14),
                                border: Border.all(color: colors.border),
                              ),
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                                children: [
                                  Row(
                                    children: [
                                      Container(
                                        padding: const EdgeInsets.all(6),
                                        decoration: BoxDecoration(
                                          color:
                                              colors.brand.withValues(alpha: 0.10),
                                          borderRadius: BorderRadius.circular(8),
                                        ),
                                        child: Icon(
                                          product.icon,
                                          size: 16,
                                          color: colors.brand,
                                        ),
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
                                    mainAxisAlignment:
                                        MainAxisAlignment.spaceBetween,
                                    children: [
                                      CustomText(
                                        priceStr,
                                        fontSize: 12,
                                        fontWeight: FontWeight.w700,
                                      ),
                                      CustomText(
                                        weightStr,
                                        fontSize: 11,
                                        color: colors.textSecondary,
                                      ),
                                    ],
                                  ),
                                  Row(
                                    children: [
                                      Expanded(
                                        child: _SimActionChip(
                                          label: l10n.simulateValidScan,
                                          color: colors.verified,
                                          enabled: !widget.isBusy,
                                          onTap: () => widget.onSimulateScan(
                                            product,
                                            isFraud: false,
                                          ),
                                        ),
                                      ),
                                      const SizedBox(width: 6),
                                      Expanded(
                                        child: _SimActionChip(
                                          label: l10n.simulateFraudScan,
                                          color: colors.danger,
                                          enabled: !widget.isBusy,
                                          onTap: () => widget.onSimulateScan(
                                            product,
                                            isFraud: true,
                                          ),
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
                    )
                : const SizedBox(width: double.infinity),
          ),
        ],
      ),
    );
  }
}

class _CollapseChip extends StatelessWidget {
  const _CollapseChip({
    required this.expanded,
    required this.label,
    required this.onTap,
  });

  final bool expanded;
  final String label;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final colors = context.colors;
    return Material(
      color: Colors.transparent,
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(9),
        child: Container(
          padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 9),
          decoration: BoxDecoration(
            color: colors.surfaceAlt,
            borderRadius: BorderRadius.circular(9),
            border: Border.all(color: colors.border),
          ),
          child: Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              CustomText(
                label,
                fontSize: 12,
                fontWeight: FontWeight.w700,
                color: colors.textSecondary,
              ),
              const SizedBox(width: 4),
              AnimatedRotation(
                turns: expanded ? 0 : -0.25,
                duration: AppTypography.base,
                curve: AppTypography.enterCurve,
                child: Icon(
                  Icons.keyboard_arrow_up_rounded,
                  size: 16,
                  color: colors.textSecondary,
                ),
              ),
            ],
          ),
        ),
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
        borderRadius: BorderRadius.circular(8),
        child: Container(
          padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 12),
          decoration: BoxDecoration(
            color: color.withValues(alpha: enabled ? 0.14 : 0.06),
            borderRadius: BorderRadius.circular(8),
            border: Border.all(
              color: color.withValues(alpha: enabled ? 0.45 : 0.18),
            ),
          ),
          alignment: Alignment.center,
          child: CustomText(
            label,
            fontSize: 11,
            fontWeight: FontWeight.w700,
            color: color,
            maxLines: 1,
          ),
        ),
      ),
    );
  }
}
