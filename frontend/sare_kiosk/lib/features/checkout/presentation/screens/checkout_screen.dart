import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/config/kiosk_config_exports.dart';
import '../../../../core/routing/app_router.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_typography.dart';
import '../../../../core/utils/money.dart';
import '../../../../core/widgets/kiosk_widgets.dart';
import '../../../../l10n/app_localizations.dart';
import '../../../cart/data/repositories/local_smart_cart_engine.dart';
import '../../../cart/presentation/cubit/cart_cubit.dart';
import '../../../cart/presentation/cubit/cart_state.dart';
import '../../../session/data/models/session_models.dart';
import '../../../session/presentation/cubit/session_cubit.dart';
import '../../../session/presentation/cubit/session_state.dart';

class CheckoutScreen extends StatefulWidget {
  const CheckoutScreen({
    super.key,
    required this.sessionId,
    required this.onToggleLocale,
    required this.onToggleTheme,
    required this.isDarkMode,
  });

  final String sessionId;
  final VoidCallback onToggleLocale;
  final VoidCallback onToggleTheme;
  final bool isDarkMode;

  @override
  State<CheckoutScreen> createState() => _CheckoutScreenState();
}

class _CheckoutScreenState extends State<CheckoutScreen> {
  String _selectedMethod = 'card'; // card | wallet | cashier

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (!mounted) return;
      if (widget.sessionId.isNotEmpty) {
        final cur = context.read<SessionCubit>().state.session?.sessionId;
        if (cur != widget.sessionId) {
          context.read<SessionCubit>().hydrate(widget.sessionId);
        }
      }
    });
  }

  Future<void> _confirm() async {
    final ok = await context
        .read<CartCubit>()
        .checkout(paymentMethod: _selectedMethod);
    if (!ok || !mounted) return;
    final checkout = context.read<SessionCubit>().state.checkout;
    if (checkout == null) return;
    context.go(
      AppRoutes.receipt,
      extra: {
        'receiptHash': checkout.receiptHash,
        'totalMinor': checkout.totalPaidMinor,
        'sessionId': checkout.sessionId,
        'itemsCount': checkout.itemsCount,
        'paymentMethod': checkout.paymentMethod,
      },
    );
  }

  String _instructionForMethod(AppLocalizations l10n, String cartNumber) {
    switch (_selectedMethod) {
      case 'wallet':
        return l10n.paymentMethodInstructionWallet;
      case 'cashier':
        return l10n.paymentMethodInstructionCashier(cartNumber);
      case 'card':
      default:
        return l10n.paymentMethodInstructionCard;
    }
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;
    final locale = Localizations.localeOf(context).languageCode;
    final config = context.kioskConfig;

    return BlocBuilder<SessionCubit, SessionState>(
      builder: (context, sessionState) {
        return BlocBuilder<CartCubit, CartState>(
          builder: (context, cartState) {
            final session = sessionState.session;
            final colors = context.colors;

            if (session == null) {
              return KioskScaffold(
                onToggleLocale: widget.onToggleLocale,
                onToggleTheme: widget.onToggleTheme,
                isDarkMode: widget.isDarkMode,
                body: const Center(child: CircularProgressIndicator()),
              );
            }

            final totalMinor = session.totalMinor;
            final split = Money.splitVat(totalMinor);
            final subLabel = Money.formatMinor(
              split.subtotalMinor,
              locale,
              symbol: ' ${l10n.currencySymbol}',
            );
            final vatLabel = Money.formatMinor(
              split.vatMinor,
              locale,
              symbol: ' ${l10n.currencySymbol}',
            );

            Widget? banner;
            if (cartState.bannerKind == WeightBannerKind.mismatch) {
              banner = KioskStatusBanner(
                key: ValueKey('banner-mismatch-${cartState.bannerMessage}'),
                message: cartState.bannerMessage ?? l10n.networkError,
                status: KioskBannerStatus.discrepancy,
              );
            }

            final shopperLabel = session.isMember
                ? l10n.memberShopperBadge(
                    session.userName ?? session.nfcUid ?? '')
                : l10n.guestShopperBadge;

            return KioskScaffold(
              onToggleLocale: widget.onToggleLocale,
              onToggleTheme: widget.onToggleTheme,
              isDarkMode: widget.isDarkMode,
              floatingBanner: banner,
              extraStatusBadge: KioskStatusBadge(
                leading: Icon(
                  session.isMember
                      ? Icons.verified_user_rounded
                      : Icons.person_outline_rounded,
                  size: 16,
                  color: session.isMember
                      ? colors.verified
                      : colors.textSecondary,
                ),
                label: shopperLabel,
              ),
              body: LayoutBuilder(
                builder: (context, constraints) {
                  final isWide = constraints.maxWidth >= 900;
                  return Padding(
                    padding: const EdgeInsets.fromLTRB(24, 10, 24, 18),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        Row(
                          mainAxisAlignment: MainAxisAlignment.spaceBetween,
                          children: [
                            Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                CustomText(
                                  l10n.checkoutTitle,
                                  fontSize: 22,
                                  fontWeight: FontWeight.w800,
                                  letterSpacing: -0.3,
                                ),
                                const SizedBox(height: 2),
                                Row(
                                  children: [
                                    Icon(
                                      Icons.verified_rounded,
                                      size: 14,
                                      color: colors.verified,
                                    ),
                                    const SizedBox(width: 6),
                                    CustomText(
                                      '${l10n.itemsCount(session.itemsCount)} • ${l10n.weightMatchSuccess}',
                                      fontSize: 13,
                                      color: colors.textSecondary,
                                      tabularFigures: true,
                                    ),
                                  ],
                                ),
                              ],
                            ),
                            KioskPriceText(
                              minor: totalMinor,
                              amountSize: 28,
                              amountColor: colors.brand,
                            ),
                          ],
                        ),
                        const SizedBox(height: 14),
                        Expanded(
                          child: isWide
                              ? Row(
                                  crossAxisAlignment:
                                      CrossAxisAlignment.stretch,
                                  children: [
                                    Expanded(
                                      flex: 5,
                                      child: _buildInvoiceCard(
                                        context,
                                        session: session,
                                        l10n: l10n,
                                        locale: locale,
                                        colors: colors,
                                        subLabel: subLabel,
                                        vatLabel: vatLabel,
                                        totalMinor: totalMinor,
                                      ),
                                    ),
                                    const SizedBox(width: 20),
                                    Expanded(
                                      flex: 6,
                                      child: _buildPaymentColumn(
                                        context,
                                        l10n: l10n,
                                        colors: colors,
                                        cartNumber: config.cartNumber,
                                        cartState: cartState,
                                      ),
                                    ),
                                  ],
                                )
                              : SingleChildScrollView(
                                  child: Column(
                                    children: [
                                      _buildInvoiceCard(
                                        context,
                                        session: session,
                                        l10n: l10n,
                                        locale: locale,
                                        colors: colors,
                                        subLabel: subLabel,
                                        vatLabel: vatLabel,
                                        totalMinor: totalMinor,
                                        shrinkWrap: true,
                                      ),
                                      const SizedBox(height: 16),
                                      _buildPaymentColumn(
                                        context,
                                        l10n: l10n,
                                        colors: colors,
                                        cartNumber: config.cartNumber,
                                        cartState: cartState,
                                      ),
                                    ],
                                  ),
                                ),
                        ),
                      ],
                    ),
                  );
                },
              ),
            );
          },
        );
      },
    );
  }

  Widget _buildInvoiceCard(
    BuildContext context, {
    required SessionSummary session,
    required AppLocalizations l10n,
    required String locale,
    required AppColorTheme colors,
    required String subLabel,
    required String vatLabel,
    required int totalMinor,
    bool shrinkWrap = false,
  }) {
    final itemsList = ListView.separated(
      shrinkWrap: shrinkWrap,
      physics: shrinkWrap ? const NeverScrollableScrollPhysics() : null,
      itemCount: session.items.length,
      separatorBuilder: (_, _) => Divider(height: 14, color: colors.border),
      itemBuilder: (context, index) {
        final item = session.items[index];
        final catalogProduct = LocalSmartCartEngine.findByBarcode(item.barcode);
        final categoryLabel = catalogProduct != null
            ? (locale == 'ar'
                ? catalogProduct.categoryAr
                : catalogProduct.categoryEn)
            : l10n.toleranceBadge;
        final itemIcon = catalogProduct?.icon ?? Icons.shopping_bag_rounded;
        final price = Money.formatMinor(
          item.unitPriceMinor,
          locale,
          symbol: ' ${l10n.currencySymbol}',
        );

        return Row(
          children: [
            Container(
              width: 36,
              height: 36,
              decoration: BoxDecoration(
                color: colors.brand.withValues(alpha: 0.10),
                borderRadius: BorderRadius.circular(10),
                border: Border.all(
                  color: colors.brand.withValues(alpha: 0.22),
                ),
              ),
              child: Icon(
                itemIcon,
                size: 18,
                color: colors.brand,
              ),
            ),
            const SizedBox(width: 10),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  CustomText(
                    item.displayName(locale),
                    fontSize: 13.5,
                    fontWeight: FontWeight.w700,
                    maxLines: 1,
                  ),
                  const SizedBox(height: 2),
                  CustomText(
                    categoryLabel,
                    fontSize: 11.5,
                    color: colors.textSecondary,
                    maxLines: 1,
                  ),
                ],
              ),
            ),
            const SizedBox(width: 8),
            CustomText(
              price,
              fontSize: 13.5,
              fontWeight: FontWeight.w700,
              tabularFigures: true,
            ),
          ],
        );
      },
    );

    return RepaintBoundary(
      child: KioskSurfaceCard(
        padding: const EdgeInsets.all(18),
        borderRadius: 20,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          mainAxisSize: shrinkWrap ? MainAxisSize.min : MainAxisSize.max,
          children: [
            KioskSectionLabel(
              l10n.orderItemsHeading,
              accent: colors.brand,
            ),
            const SizedBox(height: 10),
            if (shrinkWrap) itemsList else Expanded(child: itemsList),
            Padding(
              padding: const EdgeInsets.symmetric(vertical: 11),
              child: CustomPaint(
                size: const Size(100, 2),
                painter: _ReceiptPerforationPainter(color: colors.borderStrong),
              ),
            ),
            _kv(l10n.subtotal, subLabel, colors),
            const SizedBox(height: 6),
            _kv(l10n.tax, vatLabel, colors),
            const Padding(
              padding: EdgeInsets.symmetric(vertical: 10),
              child: KioskHairlineDivider(),
            ),
            Row(
              children: [
                Expanded(
                  child: CustomText(
                    l10n.orderTotal,
                    fontSize: 15,
                    fontWeight: FontWeight.w700,
                    maxLines: 1,
                  ),
                ),
                const SizedBox(width: 8),
                KioskPriceText(minor: totalMinor, amountSize: 22),
              ],
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildPaymentColumn(
    BuildContext context, {
    required AppLocalizations l10n,
    required AppColorTheme colors,
    required String cartNumber,
    required CartState cartState,
  }) {
    return SingleChildScrollView(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          _TenderMethodCard(
            icon: Icons.contactless_rounded,
            label: l10n.payWithCard,
            selected: _selectedMethod == 'card',
            colors: colors,
            onTap: () => setState(() => _selectedMethod = 'card'),
          ),
          const SizedBox(height: 10),
          _TenderMethodCard(
            icon: Icons.qr_code_2_rounded,
            label: l10n.payWithWallet,
            selected: _selectedMethod == 'wallet',
            colors: colors,
            onTap: () => setState(() => _selectedMethod = 'wallet'),
          ),
          const SizedBox(height: 10),
          _TenderMethodCard(
            icon: Icons.storefront_rounded,
            label: l10n.payWithCashier,
            selected: _selectedMethod == 'cashier',
            colors: colors,
            onTap: () => setState(() => _selectedMethod = 'cashier'),
          ),
          const SizedBox(height: 14),
          AnimatedSwitcher(
            duration: AppTypography.base,
            switchInCurve: AppTypography.enterCurve,
            switchOutCurve: AppTypography.exitCurve,
            transitionBuilder: (child, animation) => FadeTransition(
              opacity: animation,
              child: SlideTransition(
                position: Tween<Offset>(
                  begin: const Offset(0, 0.16),
                  end: Offset.zero,
                ).animate(animation),
                child: child,
              ),
            ),
            child: Container(
              key: ValueKey(_selectedMethod),
              padding: const EdgeInsets.all(14),
              decoration: BoxDecoration(
                gradient: colors.verifiedGradient,
                borderRadius: BorderRadius.circular(14),
                border: Border.all(
                  color: colors.verified.withValues(alpha: 0.34),
                ),
              ),
              child: Row(
                children: [
                  Container(
                    padding: const EdgeInsets.all(7),
                    decoration: BoxDecoration(
                      color: colors.verified.withValues(alpha: 0.16),
                      borderRadius: BorderRadius.circular(9),
                    ),
                    child: Icon(
                      Icons.verified_user_outlined,
                      size: 18,
                      color: colors.verified,
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: CustomText(
                      _instructionForMethod(l10n, cartNumber),
                      fontSize: 12.5,
                      color: colors.verified,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                ],
              ),
            ),
          ),
          const SizedBox(height: 18),
          Row(
            children: [
              Expanded(
                child: KioskButton(
                  text: l10n.cancelCheckout,
                  icon: Icons.arrow_back_rounded,
                  variant: KioskButtonVariant.secondary,
                  onPressed: () {
                    if (context.canPop()) {
                      context.pop();
                    } else {
                      context.go(
                        '${AppRoutes.cart}?sessionId=${widget.sessionId}',
                      );
                    }
                  },
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                flex: 2,
                child: KioskButton(
                  text: l10n.confirm,
                  icon: Icons.check_circle_outline_rounded,
                  isLoading: cartState.isAdding,
                  onPressed: cartState.isAdding ? null : _confirm,
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }

  Widget _kv(String k, String v, AppColorTheme colors) {
    return Row(
      children: [
        Expanded(
          child: CustomText(
            k,
            fontSize: 13,
            color: colors.textSecondary,
            maxLines: 1,
          ),
        ),
        const SizedBox(width: 8),
        CustomText(
          v,
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

/// Tactile Apple Pay–style tender selection card with pointer-down spring
/// compression and animated selection indicator.
class _TenderMethodCard extends StatefulWidget {
  const _TenderMethodCard({
    required this.icon,
    required this.label,
    required this.selected,
    required this.colors,
    required this.onTap,
  });

  final IconData icon;
  final String label;
  final bool selected;
  final AppColorTheme colors;
  final VoidCallback onTap;

  @override
  State<_TenderMethodCard> createState() => _TenderMethodCardState();
}

class _TenderMethodCardState extends State<_TenderMethodCard> {
  bool _pressed = false;

  @override
  Widget build(BuildContext context) {
    final primary = Theme.of(context).colorScheme.primary;
    final colors = widget.colors;

    return Listener(
      onPointerDown: (_) => setState(() => _pressed = true),
      onPointerUp: (_) => setState(() => _pressed = false),
      onPointerCancel: (_) => setState(() => _pressed = false),
      child: AnimatedScale(
        scale: _pressed ? 0.982 : 1.0,
        duration: _pressed ? AppTypography.fast : AppTypography.base,
        curve: _pressed ? Curves.easeOut : AppTypography.springCurve,
        child: Material(
          color: Colors.transparent,
          child: InkWell(
            onTap: widget.onTap,
            borderRadius: BorderRadius.circular(16),
            child: KioskSurfaceCard(
              padding:
                  const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
              borderRadius: 16,
              borderColor: widget.selected ? colors.brand : null,
              borderWidth: widget.selected ? 1.8 : 1.2,
              glow: widget.selected ? colors.brandGlow : null,
              child: Row(
                children: [
                  AnimatedContainer(
                    duration: AppTypography.base,
                    curve: AppTypography.enterCurve,
                    padding: const EdgeInsets.all(10),
                    decoration: BoxDecoration(
                      gradient: widget.selected ? colors.brandGradient : null,
                      color: widget.selected ? null : colors.surfaceAlt,
                      borderRadius: BorderRadius.circular(12),
                      border: widget.selected
                          ? Border.all(
                              color: Colors.white.withValues(alpha: 0.22),
                            )
                          : Border.all(color: colors.border),
                    ),
                    child: Icon(
                      widget.icon,
                      size: 20,
                      color: widget.selected
                          ? colors.onBrand
                          : colors.textSecondary,
                    ),
                  ),
                  const SizedBox(width: 14),
                  Expanded(
                    child: CustomText(
                      widget.label,
                      fontSize: 14.5,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                  AnimatedScale(
                    scale: widget.selected ? 1.0 : 0.86,
                    duration: AppTypography.base,
                    curve: AppTypography.springCurve,
                    child: Icon(
                      widget.selected
                          ? Icons.check_circle_rounded
                          : Icons.circle_outlined,
                      size: 22,
                      color: widget.selected ? primary : colors.textFaint,
                    ),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}

/// Dashed receipt-ticket perforation divider.
class _ReceiptPerforationPainter extends CustomPainter {
  _ReceiptPerforationPainter({required this.color});

  final Color color;

  @override
  void paint(Canvas canvas, Size size) {
    final paint = Paint()
      ..color = color.withValues(alpha: 0.55)
      ..strokeWidth = 1.2
      ..strokeCap = StrokeCap.round;

    const dashWidth = 5.0;
    const dashSpace = 5.0;
    double x = 0;
    final y = size.height / 2;
    while (x < size.width) {
      canvas.drawLine(
        Offset(x, y),
        Offset((x + dashWidth).clamp(0.0, size.width), y),
        paint,
      );
      x += dashWidth + dashSpace;
    }
  }

  @override
  bool shouldRepaint(covariant _ReceiptPerforationPainter oldDelegate) =>
      color != oldDelegate.color;
}
