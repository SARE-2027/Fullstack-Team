import 'package:flutter/material.dart';

import '../../../../core/theme/app_colors.dart';
import '../../../../core/utils/kiosk_formatter.dart';
import '../../../../core/utils/money.dart';
import '../../../../core/widgets/custom_text.dart';
import '../../../../l10n/app_localizations.dart';
import '../../../session/data/models/session_models.dart';
import '../../data/repositories/local_smart_cart_engine.dart';

class CartItemTile extends StatelessWidget {
  const CartItemTile({
    super.key,
    required this.item,
    this.onRemove,
  });

  final SessionItem item;
  final VoidCallback? onRemove;

  @override
  Widget build(BuildContext context) {
    final colors = context.colors;
    final l10n = AppLocalizations.of(context)!;
    final locale = Localizations.localeOf(context).languageCode;
    final name = item.displayName(locale);
    final priceLabel = Money.formatMinor(
      item.unitPriceMinor,
      locale,
      symbol: ' ${l10n.currencySymbol}',
    );
    final weightLabel = KioskFormatter.formatWeight(item.expectedWeightG.toDouble(), locale);
    final catalogMatch = LocalSmartCartEngine.findByBarcode(item.barcode);

    return Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: colors.textFieldFill,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: colors.containerBorder),
      ),
      child: Row(
        children: [
          Container(
            width: 52,
            height: 52,
            decoration: BoxDecoration(
              color: Theme.of(context).colorScheme.surface,
              borderRadius: BorderRadius.circular(12),
              border: Border.all(color: colors.containerBorder.withValues(alpha: 0.6)),
            ),
            child: _buildIcon(context, catalogMatch?.icon),
          ),
          const SizedBox(width: 14),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                CustomText(
                  name,
                  fontSize: 15,
                  fontWeight: FontWeight.w700,
                  maxLines: 1,
                ),
                const SizedBox(height: 5),
                Wrap(
                  spacing: 8,
                  runSpacing: 4,
                  crossAxisAlignment: WrapCrossAlignment.center,
                  children: [
                    CustomText(
                      item.barcode,
                      fontSize: 12,
                      color: colors.secondaryText,
                    ),
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                      decoration: BoxDecoration(
                        color: colors.weightVerified.withValues(alpha: 0.12),
                        borderRadius: BorderRadius.circular(6),
                      ),
                      child: Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Icon(
                            Icons.verified_rounded,
                            size: 13,
                            color: colors.weightVerified,
                          ),
                          const SizedBox(width: 4),
                          CustomText(
                            weightLabel,
                            fontSize: 11,
                            fontWeight: FontWeight.w700,
                            color: colors.weightVerified,
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
              ],
            ),
          ),
          const SizedBox(width: 12),
          Column(
            crossAxisAlignment: CrossAxisAlignment.end,
            children: [
              CustomText(
                priceLabel,
                fontSize: 15,
                fontWeight: FontWeight.w800,
              ),
              if (onRemove != null) ...[
                const SizedBox(height: 6),
                Material(
                  color: Colors.transparent,
                  child: InkWell(
                    onTap: onRemove,
                    borderRadius: BorderRadius.circular(8),
                    child: Container(
                      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
                      decoration: BoxDecoration(
                        color: colors.weightDiscrepancy.withValues(alpha: 0.12),
                        borderRadius: BorderRadius.circular(8),
                        border: Border.all(
                          color: colors.weightDiscrepancy.withValues(alpha: 0.35),
                        ),
                      ),
                      child: Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Icon(
                            Icons.delete_outline_rounded,
                            size: 15,
                            color: colors.weightDiscrepancy,
                          ),
                          const SizedBox(width: 5),
                          CustomText(
                            l10n.removeItemButton,
                            fontSize: 12,
                            fontWeight: FontWeight.w700,
                            color: colors.weightDiscrepancy,
                          ),
                        ],
                      ),
                    ),
                  ),
                ),
              ],
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildIcon(BuildContext context, IconData? iconData) {
    return Center(
      child: Icon(
        iconData ?? Icons.inventory_2_outlined,
        size: 24,
        color: Theme.of(context).colorScheme.primary.withValues(alpha: 0.8),
      ),
    );
  }
}
