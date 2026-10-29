import 'package:flutter/material.dart';

import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_typography.dart';
import '../../../../core/utils/kiosk_motion.dart';
import '../../../../core/widgets/custom_text.dart';
import '../../../../core/widgets/kiosk_price_text.dart';
import '../../../../core/widgets/kiosk_surface_card.dart';
import '../../../../l10n/app_localizations.dart';
import '../../../session/data/models/session_models.dart';
import '../../data/repositories/local_smart_cart_engine.dart';

/// Tactile basket item card supporting both 1-tap removal and Apple WWDC
/// rubber-banded horizontal swipe-to-remove direct manipulation.
class CartItemTile extends StatefulWidget {
  const CartItemTile({
    super.key,
    required this.item,
    this.onRemove,
  });

  final SessionItem item;
  final VoidCallback? onRemove;

  @override
  State<CartItemTile> createState() => _CartItemTileState();
}

class _CartItemTileState extends State<CartItemTile> {
  double _dragOffset = 0.0;
  bool _dragging = false;

  void _onHorizontalDragUpdate(DragUpdateDetails details) {
    if (widget.onRemove == null) return;
    setState(() {
      _dragging = true;
      _dragOffset += details.delta.dx;
    });
  }

  void _onHorizontalDragEnd(DragEndDetails details) {
    if (widget.onRemove == null) return;
    final velocity = details.primaryVelocity ?? 0.0;
    final projected =
        _dragOffset + KioskMotion.projectMomentum(velocity, decelerationRate: 0.99);
    if (projected.abs() > 110 || _dragOffset.abs() > 90) {
      setState(() {
        _dragging = false;
        _dragOffset = 0.0;
      });
      widget.onRemove?.call();
      return;
    }
    setState(() {
      _dragging = false;
      _dragOffset = 0.0;
    });
  }

  @override
  Widget build(BuildContext context) {
    final colors = context.colors;
    final l10n = AppLocalizations.of(context)!;
    final locale = Localizations.localeOf(context).languageCode;
    final name = widget.item.displayName(locale);
    final catalogMatch =
        LocalSmartCartEngine.findByBarcode(widget.item.barcode);
    final categoryLabel = catalogMatch?.displayCategory(locale);

    // Apply Apple progressive rubber-banding to the live finger offset.
    final dampedOffset = KioskMotion.rubberband(_dragOffset, 180.0);

    return TweenAnimationBuilder<double>(
      tween: Tween(begin: 0, end: 1),
      duration: AppTypography.slow,
      curve: AppTypography.enterCurve,
      builder: (context, t, child) => Opacity(
        opacity: t.clamp(0.0, 1.0),
        child: Transform.translate(
          offset: Offset(0, 12 * (1 - t)),
          child: child,
        ),
      ),
      child: GestureDetector(
        onHorizontalDragUpdate:
            widget.onRemove != null ? _onHorizontalDragUpdate : null,
        onHorizontalDragEnd:
            widget.onRemove != null ? _onHorizontalDragEnd : null,
        onHorizontalDragCancel: () {
          if (_dragging) {
            setState(() {
              _dragging = false;
              _dragOffset = 0.0;
            });
          }
        },
        child: AnimatedContainer(
          duration: _dragging ? Duration.zero : AppTypography.base,
          curve: AppTypography.springCurve,
          transform: Matrix4.translationValues(dampedOffset, 0, 0),
          child: KioskSurfaceCard(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
            borderRadius: 16,
            child: Row(
              children: [
                // Category Icon Squircle
                Container(
                  width: 48,
                  height: 48,
                  decoration: BoxDecoration(
                    gradient: LinearGradient(
                      begin: Alignment.topLeft,
                      end: Alignment.bottomRight,
                      colors: [
                        colors.brand.withValues(alpha: 0.16),
                        colors.brand.withValues(alpha: 0.06),
                      ],
                    ),
                    borderRadius: BorderRadius.circular(14),
                    border: Border.all(
                      color: colors.brand.withValues(alpha: 0.25),
                    ),
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
                        fontSize: 15.5,
                        fontWeight: FontWeight.w700,
                        maxLines: 1,
                      ),
                      const SizedBox(height: 5),
                      Row(
                        children: [
                          if (categoryLabel != null &&
                              categoryLabel.isNotEmpty) ...[
                            CustomText(
                              categoryLabel,
                              fontSize: 12,
                              color: colors.textSecondary,
                              fontWeight: FontWeight.w600,
                            ),
                            Padding(
                              padding:
                                  const EdgeInsets.symmetric(horizontal: 7),
                              child: Container(
                                width: 3.5,
                                height: 3.5,
                                decoration: BoxDecoration(
                                  color: colors.textFaint,
                                  shape: BoxShape.circle,
                                ),
                              ),
                            ),
                          ],
                          Icon(
                            Icons.check_circle_rounded,
                            size: 13.5,
                            color: colors.verified,
                          ),
                          const SizedBox(width: 4),
                          CustomText(
                            l10n.toleranceBadge,
                            fontSize: 11.5,
                            fontWeight: FontWeight.w600,
                            color: colors.verified,
                          ),
                        ],
                      ),
                    ],
                  ),
                ),
                const SizedBox(width: 12),
                KioskPriceText(
                  minor: widget.item.unitPriceMinor,
                  amountSize: 18,
                ),
                if (widget.onRemove != null) ...[
                  const SizedBox(width: 14),
                  Material(
                    color: Colors.transparent,
                    child: InkWell(
                      onTap: widget.onRemove,
                      borderRadius: BorderRadius.circular(10),
                      child: Container(
                        padding: const EdgeInsets.symmetric(
                          horizontal: 11,
                          vertical: 8,
                        ),
                        decoration: BoxDecoration(
                          color: colors.danger.withValues(alpha: 0.10),
                          borderRadius: BorderRadius.circular(10),
                          border: Border.all(
                            color: colors.danger.withValues(alpha: 0.28),
                          ),
                        ),
                        child: Row(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            Icon(
                              Icons.delete_outline_rounded,
                              size: 16,
                              color: colors.danger,
                            ),
                            const SizedBox(width: 4),
                            CustomText(
                              l10n.removeItemButton,
                              fontSize: 12,
                              fontWeight: FontWeight.w700,
                              color: colors.danger,
                            ),
                          ],
                        ),
                      ),
                    ),
                  ),
                ],
              ],
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildIcon(BuildContext context, IconData? iconData) {
    return Center(
      child: Icon(
        iconData ?? Icons.shopping_bag_outlined,
        size: 23,
        color: context.colors.brand,
      ),
    );
  }
}
