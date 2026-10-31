import 'package:flutter/material.dart';

import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_typography.dart';
import '../../../../core/utils/kiosk_formatter.dart';
import '../../../../core/utils/kiosk_motion.dart';
import '../../../../core/utils/money.dart';
import '../../../../core/widgets/custom_text.dart';
import '../../../../core/widgets/kiosk_price_text.dart';
import '../../../../core/widgets/kiosk_shimmer.dart';
import '../../../../core/widgets/kiosk_surface_card.dart';
import '../../../../l10n/app_localizations.dart';
import '../../../session/data/models/session_models.dart';
import '../../data/repositories/local_smart_cart_engine.dart';

/// Tactile basket item card supporting:
/// - Coalesced quantity display & interactive `[-]` / `[+]` stepper on a single card.
/// - Shimmer highlight sweep & spring pulse when quantity increases.
/// - Apple WWDC rubber-banded horizontal swipe-to-remove with smooth slide-out
///   and vertical height collapse (`SizeTransition`) so sibling cards glide
///   upward without harsh jumps.
class CartItemTile extends StatefulWidget {
  const CartItemTile({
    super.key,
    required this.item,
    this.onRemove,
    this.onIncrement,
    this.onDecrement,
  });

  final SessionItem item;
  final VoidCallback? onRemove;
  final VoidCallback? onIncrement;
  final VoidCallback? onDecrement;

  @override
  State<CartItemTile> createState() => _CartItemTileState();
}

class _CartItemTileState extends State<CartItemTile>
    with TickerProviderStateMixin {
  double _dragOffset = 0.0;
  double _exitStartOffset = 0.0;
  double _exitSlideSign = -1.0;
  bool _dragging = false;
  bool _removing = false;

  late final AnimationController _removeCtrl;
  late final AnimationController _bumpCtrl;

  @override
  void initState() {
    super.initState();
    _removeCtrl = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 260),
    );
    _bumpCtrl = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 320),
    );
  }

  @override
  void didUpdateWidget(covariant CartItemTile oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (widget.item.quantity != oldWidget.item.quantity) {
      _bumpCtrl.forward(from: 0.0);
    }
  }

  @override
  void dispose() {
    _removeCtrl.dispose();
    _bumpCtrl.dispose();
    super.dispose();
  }

  void _onHorizontalDragUpdate(DragUpdateDetails details) {
    if (widget.onRemove == null || _removing) return;
    setState(() {
      _dragging = true;
      _dragOffset += details.delta.dx;
    });
  }

  void _onHorizontalDragEnd(DragEndDetails details) {
    if (widget.onRemove == null || _removing) return;
    final velocity = details.primaryVelocity ?? 0.0;
    final projected = _dragOffset +
        KioskMotion.projectMomentum(velocity, decelerationRate: 0.99);
    if (projected.abs() > 110 || _dragOffset.abs() > 90) {
      final sign = _dragOffset != 0
          ? (_dragOffset < 0 ? -1.0 : 1.0)
          : (velocity < 0 ? -1.0 : 1.0);
      _triggerAnimatedRemove(slideSign: sign);
      return;
    }
    setState(() {
      _dragging = false;
      _dragOffset = 0.0;
    });
  }

  void _triggerAnimatedRemove({double slideSign = -1.0}) {
    if (_removing || widget.onRemove == null) return;
    setState(() {
      _dragging = false;
      _removing = true;
      _exitStartOffset = KioskMotion.rubberband(_dragOffset, 180.0);
      _exitSlideSign = slideSign;
    });
    _removeCtrl.forward(from: 0.0).whenComplete(() {
      if (mounted) {
        widget.onRemove?.call();
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    final colors = context.colors;
    final l10n = AppLocalizations.of(context)!;
    final locale = Localizations.localeOf(context).languageCode;
    final isRtl = Directionality.of(context) == TextDirection.rtl;
    final name = widget.item.displayName(locale);
    final catalogMatch =
        LocalSmartCartEngine.findByBarcode(widget.item.barcode);
    final categoryLabel = catalogMatch?.displayCategory(locale);
    final qty = widget.item.quantity;

    // Apply Apple progressive rubber-banding to the live finger offset.
    final liveDampedOffset = KioskMotion.rubberband(_dragOffset, 180.0);
    final dragProgress = (_dragOffset.abs() / 90.0).clamp(0.0, 1.0);

    final sizeAnimation = Tween<double>(begin: 1.0, end: 0.0).animate(
      CurvedAnimation(
        parent: _removeCtrl,
        curve: const Interval(0.24, 1.0, curve: Curves.easeInOutCubic),
      ),
    );
    final exitProgress = CurvedAnimation(
      parent: _removeCtrl,
      curve: const Interval(0.0, 0.72, curve: Curves.easeOutCubic),
    );

    final unitPriceFormatted = Money.formatMinor(
      widget.item.unitPriceMinor,
      locale,
      symbol: ' ${l10n.currencySymbol}',
    );
    final qtyFormatted = KioskFormatter.formatNumber(qty, locale);

    return SizeTransition(
      sizeFactor: sizeAnimation,
      alignment: Alignment.topCenter,
      child: Padding(
        padding: const EdgeInsets.only(bottom: 10),
        child: TweenAnimationBuilder<double>(
          tween: Tween(begin: 0, end: 1),
          duration: AppTypography.slow,
          curve: AppTypography.enterCurve,
          builder: (context, t, child) => Opacity(
            opacity: t.clamp(0.0, 1.0),
            child: Transform.translate(
              offset: Offset(0, 10 * (1 - t)),
              child: child,
            ),
          ),
          child: AnimatedBuilder(
            animation: Listenable.merge([_removeCtrl, _bumpCtrl]),
            builder: (context, cardContent) {
              final double currentDx;
              final double currentOpacity;
              final double currentScale;

              if (_removing) {
                final slideTarget = _exitSlideSign * 360.0;
                currentDx = _exitStartOffset +
                    (slideTarget - _exitStartOffset) * exitProgress.value;
                currentOpacity = (1.0 - exitProgress.value).clamp(0.0, 1.0);
                currentScale = 1.0 - (0.05 * exitProgress.value);
              } else {
                currentDx = liveDampedOffset;
                currentOpacity = 1.0;
                final bumpT = _bumpCtrl.value;
                final bumpWave = bumpT > 0 && bumpT < 1
                    ? (bumpT < 0.45
                        ? (bumpT / 0.45)
                        : (1.0 - (bumpT - 0.45) / 0.55))
                    : 0.0;
                currentScale = 1.0 + (0.018 * bumpWave);
              }

              final showSwipeBackdrop =
                  (_dragging && _dragOffset.abs() > 4) || _removing;

              return Stack(
                alignment: Alignment.center,
                children: [
                  if (showSwipeBackdrop)
                    Positioned.fill(
                      child: Opacity(
                        opacity: _removing
                            ? currentOpacity
                            : (0.35 + 0.65 * dragProgress).clamp(0.0, 1.0),
                        child: Container(
                          padding: const EdgeInsets.symmetric(horizontal: 22),
                          decoration: BoxDecoration(
                            color: colors.danger.withValues(alpha: 0.14),
                            borderRadius: BorderRadius.circular(16),
                            border: Border.all(
                              color: colors.danger.withValues(alpha: 0.35),
                            ),
                          ),
                          alignment: _dragOffset >= 0
                              ? Alignment.centerLeft
                              : Alignment.centerRight,
                          child: Transform.scale(
                            scale: 0.85 + (0.25 * dragProgress),
                            child: Row(
                              mainAxisSize: MainAxisSize.min,
                              children: [
                                Icon(
                                  Icons.delete_sweep_rounded,
                                  color: colors.danger,
                                  size: 22,
                                ),
                                const SizedBox(width: 6),
                                CustomText(
                                  l10n.removeItemButton,
                                  fontSize: 13,
                                  fontWeight: FontWeight.w800,
                                  color: colors.danger,
                                ),
                              ],
                            ),
                          ),
                        ),
                      ),
                    ),
                  Opacity(
                    opacity: currentOpacity,
                    child: Transform.scale(
                      scale: currentScale,
                      child: AnimatedContainer(
                        duration: (_dragging || _removing)
                            ? Duration.zero
                            : AppTypography.base,
                        curve: AppTypography.springCurve,
                        transform: Matrix4.translationValues(currentDx, 0, 0),
                        child: cardContent,
                      ),
                    ),
                  ),
                ],
              );
            },
            child: GestureDetector(
              onHorizontalDragUpdate:
                  widget.onRemove != null ? _onHorizontalDragUpdate : null,
              onHorizontalDragEnd:
                  widget.onRemove != null ? _onHorizontalDragEnd : null,
              onHorizontalDragCancel: () {
                if (_dragging && !_removing) {
                  setState(() {
                    _dragging = false;
                    _dragOffset = 0.0;
                  });
                }
              },
              child: KioskShimmerSweep(
                triggerKey: widget.item.quantity,
                borderRadius: 16,
                child: KioskSurfaceCard(
                  padding:
                      const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
                  borderRadius: 16,
                  child: Row(
                    children: [
                      // Category Icon Squircle with optional quantity badge
                      Stack(
                        clipBehavior: Clip.none,
                        children: [
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
                          if (qty > 1)
                            Positioned(
                              top: -5,
                              right: isRtl ? null : -5,
                              left: isRtl ? -5 : null,
                              child: Container(
                                padding: const EdgeInsets.symmetric(
                                  horizontal: 6,
                                  vertical: 2,
                                ),
                                decoration: BoxDecoration(
                                  gradient: colors.brandGradient,
                                  borderRadius: BorderRadius.circular(999),
                                  boxShadow: [
                                    BoxShadow(
                                      color: colors.brandGlow,
                                      blurRadius: 8,
                                      offset: const Offset(0, 2),
                                    ),
                                  ],
                                ),
                                child: CustomText(
                                  '×$qtyFormatted',
                                  fontSize: 10.5,
                                  fontWeight: FontWeight.w800,
                                  color: colors.onBrand,
                                  tabularFigures: true,
                                ),
                              ),
                            ),
                        ],
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
                                    padding: const EdgeInsets.symmetric(
                                        horizontal: 7),
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
                                if (qty > 1) ...[
                                  Padding(
                                    padding: const EdgeInsets.symmetric(
                                        horizontal: 7),
                                    child: Container(
                                      width: 3.5,
                                      height: 3.5,
                                      decoration: BoxDecoration(
                                        color: colors.textFaint,
                                        shape: BoxShape.circle,
                                      ),
                                    ),
                                  ),
                                  Flexible(
                                    child: CustomText(
                                      '$qtyFormatted × $unitPriceFormatted',
                                      fontSize: 11.5,
                                      fontWeight: FontWeight.w700,
                                      color: colors.brand,
                                      tabularFigures: true,
                                      maxLines: 1,
                                    ),
                                  ),
                                ],
                              ],
                            ),
                          ],
                        ),
                      ),
                      const SizedBox(width: 10),
                      // Quantity Stepper Pill
                      if (widget.onIncrement != null ||
                          widget.onDecrement != null) ...[
                        _QuantityStepperPill(
                          quantity: qty,
                          locale: locale,
                          colors: colors,
                          onIncrement: widget.onIncrement,
                          onDecrement: () {
                            if (qty <= 1) {
                              _triggerAnimatedRemove(
                                slideSign: isRtl ? 1.0 : -1.0,
                              );
                            } else {
                              widget.onDecrement?.call();
                            }
                          },
                        ),
                        const SizedBox(width: 12),
                      ],
                      AnimatedSwitcher(
                        duration: AppTypography.fast,
                        transitionBuilder: (child, anim) => FadeTransition(
                          opacity: anim,
                          child: ScaleTransition(
                            scale: Tween<double>(begin: 0.92, end: 1.0)
                                .animate(anim),
                            child: child,
                          ),
                        ),
                        child: KioskPriceText(
                          key: ValueKey(widget.item.lineTotalMinor),
                          minor: widget.item.lineTotalMinor,
                          amountSize: 18,
                        ),
                      ),
                      if (widget.onRemove != null) ...[
                        const SizedBox(width: 12),
                        Material(
                          color: Colors.transparent,
                          child: InkWell(
                            onTap: () => _triggerAnimatedRemove(
                              slideSign: isRtl ? 1.0 : -1.0,
                            ),
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

class _QuantityStepperPill extends StatelessWidget {
  const _QuantityStepperPill({
    required this.quantity,
    required this.locale,
    required this.colors,
    this.onIncrement,
    this.onDecrement,
  });

  final int quantity;
  final String locale;
  final AppColorTheme colors;
  final VoidCallback? onIncrement;
  final VoidCallback? onDecrement;

  @override
  Widget build(BuildContext context) {
    final qtyStr = KioskFormatter.formatNumber(quantity, locale);

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 4, vertical: 3),
      decoration: BoxDecoration(
        color: colors.surfaceAlt,
        borderRadius: BorderRadius.circular(11),
        border: Border.all(color: colors.border),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          _StepBtn(
            icon: Icons.remove_rounded,
            color: quantity > 1 ? colors.textSecondary : colors.danger,
            onTap: onDecrement,
          ),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 8),
            child: AnimatedSwitcher(
              duration: AppTypography.fast,
              transitionBuilder: (child, anim) => ScaleTransition(
                scale: anim,
                child: FadeTransition(opacity: anim, child: child),
              ),
              child: CustomText(
                qtyStr,
                key: ValueKey(quantity),
                fontSize: 13.5,
                fontWeight: FontWeight.w800,
                tabularFigures: true,
              ),
            ),
          ),
          _StepBtn(
            icon: Icons.add_rounded,
            color: colors.brand,
            onTap: onIncrement,
          ),
        ],
      ),
    );
  }
}

class _StepBtn extends StatelessWidget {
  const _StepBtn({
    required this.icon,
    required this.color,
    this.onTap,
  });

  final IconData icon;
  final Color color;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    return Material(
      color: Colors.transparent,
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(8),
        child: Container(
          width: 28,
          height: 28,
          alignment: Alignment.center,
          decoration: BoxDecoration(
            color: color.withValues(alpha: 0.10),
            borderRadius: BorderRadius.circular(8),
          ),
          child: Icon(icon, size: 16, color: color),
        ),
      ),
    );
  }
}
