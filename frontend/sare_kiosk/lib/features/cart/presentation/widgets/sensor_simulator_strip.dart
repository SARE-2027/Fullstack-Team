import 'package:flutter/material.dart';

import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_typography.dart';
import '../../../../core/utils/money.dart';
import '../../../../core/widgets/custom_text.dart';
import '../../../../core/widgets/kiosk_surface_card.dart';
import '../../../../l10n/app_localizations.dart';
import '../../data/repositories/local_smart_cart_engine.dart';

/// Quick Products & Demo Bar for the 10.1" SARE kiosk.
///
/// Enables 1-tap product addition and committee demonstrations of:
/// 1. Instant verified product scan (`+ Scan (Match)`).
/// 2. Weight discrepancy alert (`Fraud (+95g)`).
///
/// Collapsible with spring physics so shoppers enjoy a calm, uncluttered
/// interface while keeping instant access to all 7 catalog items.
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

    return KioskSurfaceCard(
      padding: const EdgeInsets.fromLTRB(14, 9, 14, 9),
      borderRadius: 18,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        mainAxisSize: MainAxisSize.min,
        children: [
          Row(
            children: [
              Container(
                padding: const EdgeInsets.all(6),
                decoration: BoxDecoration(
                  color: colors.brand.withValues(alpha: 0.12),
                  borderRadius: BorderRadius.circular(9),
                  border: Border.all(
                    color: colors.brand.withValues(alpha: 0.25),
                  ),
                ),
                child: Icon(
                  Icons.grid_view_rounded,
                  size: 14,
                  color: colors.brand,
                ),
              ),
              const SizedBox(width: 10),
              Expanded(
                child: Row(
                  children: [
                    Flexible(
                      child: CustomText(
                        l10n.simulatorTitle,
                        fontSize: 12.5,
                        fontWeight: FontWeight.w700,
                        maxLines: 1,
                      ),
                    ),
                    if (_expanded) ...[
                      const SizedBox(width: 10),
                      Flexible(
                        child: CustomText(
                          '•  ${l10n.simulatorSubtitle}',
                          fontSize: 11,
                          color: colors.textFaint,
                          maxLines: 1,
                        ),
                      ),
                    ],
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
            curve: AppTypography.enterCurve,
            alignment: Alignment.topCenter,
            child: _expanded
                ? Padding(
                    padding: const EdgeInsets.only(top: 8),
                    child: SizedBox(
                      height: 84,
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

                          return Container(
                            width: 244,
                            padding: const EdgeInsets.symmetric(
                              horizontal: 10,
                              vertical: 8,
                            ),
                            decoration: BoxDecoration(
                              color: colors.surfaceAlt,
                              borderRadius: BorderRadius.circular(13),
                              border: Border.all(color: colors.border),
                            ),
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              mainAxisAlignment: MainAxisAlignment.spaceBetween,
                              children: [
                                Row(
                                  children: [
                                    Container(
                                      padding: const EdgeInsets.all(5),
                                      decoration: BoxDecoration(
                                        color: colors.brand
                                            .withValues(alpha: 0.12),
                                        borderRadius: BorderRadius.circular(7),
                                      ),
                                      child: Icon(
                                        product.icon,
                                        size: 14,
                                        color: colors.brand,
                                      ),
                                    ),
                                    const SizedBox(width: 7),
                                    Expanded(
                                      child: CustomText(
                                        product.displayName(locale),
                                        fontSize: 12,
                                        fontWeight: FontWeight.w700,
                                        maxLines: 1,
                                      ),
                                    ),
                                    const SizedBox(width: 6),
                                    CustomText(
                                      priceStr,
                                      fontSize: 11.5,
                                      fontWeight: FontWeight.w800,
                                      color: colors.brand,
                                      tabularFigures: true,
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
                : const SizedBox.shrink(),
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
          padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
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
                fontSize: 11.5,
                fontWeight: FontWeight.w700,
                color: colors.textSecondary,
              ),
              const SizedBox(width: 4),
              AnimatedRotation(
                turns: expanded ? 0 : -0.25,
                duration: AppTypography.base,
                curve: AppTypography.springCurve,
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

class _SimActionChip extends StatefulWidget {
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
  State<_SimActionChip> createState() => _SimActionChipState();
}

class _SimActionChipState extends State<_SimActionChip> {
  bool _pressed = false;

  @override
  Widget build(BuildContext context) {
    return Listener(
      onPointerDown: (_) {
        if (widget.enabled) setState(() => _pressed = true);
      },
      onPointerUp: (_) {
        if (_pressed) setState(() => _pressed = false);
      },
      onPointerCancel: (_) {
        if (_pressed) setState(() => _pressed = false);
      },
      child: AnimatedScale(
        scale: _pressed ? 0.95 : 1.0,
        duration: AppTypography.fast,
        curve: AppTypography.springCurve,
        child: Material(
          color: Colors.transparent,
          child: InkWell(
            onTap: widget.enabled ? widget.onTap : null,
            borderRadius: BorderRadius.circular(8),
            child: Container(
              padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 8),
              decoration: BoxDecoration(
                color: widget.color
                    .withValues(alpha: widget.enabled ? 0.14 : 0.06),
                borderRadius: BorderRadius.circular(8),
                border: Border.all(
                  color: widget.color
                      .withValues(alpha: widget.enabled ? 0.45 : 0.18),
                ),
              ),
              alignment: Alignment.center,
              child: CustomText(
                widget.label,
                fontSize: 11,
                fontWeight: FontWeight.w700,
                color: widget.color,
                maxLines: 1,
              ),
            ),
          ),
        ),
      ),
    );
  }
}
