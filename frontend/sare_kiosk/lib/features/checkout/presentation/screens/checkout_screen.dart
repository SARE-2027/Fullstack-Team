import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/config/kiosk_config_exports.dart';
import '../../../../core/routing/app_router.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_typography.dart';
import '../../../../core/utils/kiosk_formatter.dart';
import '../../../../core/utils/money.dart';
import '../../../../core/widgets/kiosk_widgets.dart';
import '../../../../l10n/app_localizations.dart';
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
    final ok =
        await context.read<CartCubit>().checkout(paymentMethod: _selectedMethod);
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
            final totalLabel = Money.formatMinor(
              totalMinor,
              locale,
              symbol: ' ${l10n.currencySymbol}',
            );
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
            final totalWeightLabel = KioskFormatter.formatWeight(
              session.totalExpectedWeightG.toDouble(),
              locale,
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
                  color:
                      session.isMember ? colors.verified : colors.textSecondary,
                ),
                label: shopperLabel,
              ),
              body: LayoutBuilder(
                builder: (context, constraints) {
                  final isWide = constraints.maxWidth >= 900;
                  return Padding(
                    padding: const EdgeInsets.fromLTRB(24, 8, 24, 20),
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
                                ),
                                const SizedBox(height: 2),
                                CustomText(
                                  '${l10n.itemsCount(session.itemsCount)} • $totalWeightLabel',
                                  fontSize: 13,
                                  color: colors.textSecondary,
                                ),
                              ],
                            ),
                            CustomText(
                              totalLabel,
                              fontSize: 26,
                              fontWeight: FontWeight.w800,
                              color: colors.brand,
                            ),
                          ],
                        ),
                        const SizedBox(height: 14),
                        Expanded(
                          child: isWide
                              ? Row(
                                  crossAxisAlignment: CrossAxisAlignment.stretch,
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
                                        totalLabel: totalLabel,
                                        totalWeightLabel: totalWeightLabel,
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
                                        totalLabel: totalLabel,
                                        totalWeightLabel: totalWeightLabel,
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
    required String totalLabel,
    required String totalWeightLabel,
    bool shrinkWrap = false,
  }) {
    final itemsList = ListView.separated(
      shrinkWrap: shrinkWrap,
      physics: shrinkWrap ? const NeverScrollableScrollPhysics() : null,
      itemCount: session.items.length,
      separatorBuilder: (_, _) =>
          Divider(height: 14, color: colors.border),
      itemBuilder: (context, index) {
        final item = session.items[index];
        final price = Money.formatMinor(
          item.unitPriceMinor,
          locale,
          symbol: ' ${l10n.currencySymbol}',
        );
        final weight = KioskFormatter.formatWeight(
          item.expectedWeightG.toDouble(),
          locale,
        );
        return Row(
          children: [
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  CustomText(
                    item.displayName(locale),
                    fontSize: 13,
                    fontWeight: FontWeight.w700,
                    maxLines: 1,
                  ),
                  CustomText(
                    '${item.barcode} • $weight',
                    fontSize: 11,
                    color: colors.textFaint,
                  ),
                ],
              ),
            ),
            const SizedBox(width: 8),
            CustomText(
              price,
              fontSize: 13,
              fontWeight: FontWeight.w700,
            ),
          ],
        );
      },
    );

    return RepaintBoundary(
      child: Container(
        padding: const EdgeInsets.all(18),
        decoration: BoxDecoration(
          color: colors.surface,
          borderRadius: BorderRadius.circular(18),
          border: Border.all(color: colors.border),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          mainAxisSize: shrinkWrap ? MainAxisSize.min : MainAxisSize.max,
          children: [
            Row(
              children: [
                Icon(
                  Icons.receipt_long_rounded,
                  size: 16,
                  color: colors.brand,
                ),
                const SizedBox(width: 8),
                Expanded(
                  child: CustomText(
                    l10n.orderItemsHeading,
                    fontSize: 14,
                    fontWeight: FontWeight.w700,
                    maxLines: 1,
                  ),
                ),
              ],
            ),
            const SizedBox(height: 10),
            if (shrinkWrap) itemsList else Expanded(child: itemsList),
            Divider(height: 22, color: colors.border),
            _kv(l10n.totalBasketWeight, totalWeightLabel, colors),
            const SizedBox(height: 6),
            _kv(l10n.subtotal, subLabel, colors),
            const SizedBox(height: 6),
            _kv(l10n.tax, vatLabel, colors),
            Divider(height: 20, color: colors.border),
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
                CustomText(
                  totalLabel,
                  fontSize: 19,
                  fontWeight: FontWeight.w800,
                  maxLines: 1,
                ),
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
          _methodTile(
            icon: Icons.contactless_rounded,
            label: l10n.payWithCard,
            value: 'card',
            colors: colors,
          ),
          const SizedBox(height: 10),
          _methodTile(
            icon: Icons.qr_code_2_rounded,
            label: l10n.payWithWallet,
            value: 'wallet',
            colors: colors,
          ),
          const SizedBox(height: 10),
          _methodTile(
            icon: Icons.storefront_rounded,
            label: l10n.payWithCashier,
            value: 'cashier',
            colors: colors,
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
                  begin: const Offset(0, 0.15),
                  end: Offset.zero,
                ).animate(animation),
                child: child,
              ),
            ),
            child: Container(
              key: ValueKey(_selectedMethod),
              padding: const EdgeInsets.all(14),
              decoration: BoxDecoration(
                color: colors.verified.withValues(alpha: 0.08),
                borderRadius: BorderRadius.circular(12),
                border: Border.all(
                  color: colors.verified.withValues(alpha: 0.30),
                ),
              ),
              child: Row(
                children: [
                  Icon(
                    Icons.verified_user_outlined,
                    size: 20,
                    color: colors.verified,
                  ),
                  const SizedBox(width: 10),
                  Expanded(
                    child: CustomText(
                      _instructionForMethod(l10n, cartNumber),
                      fontSize: 12,
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
                          '${AppRoutes.cart}?sessionId=${widget.sessionId}');
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
          maxLines: 1,
        ),
      ],
    );
  }

  Widget _methodTile({
    required IconData icon,
    required String label,
    required String value,
    required AppColorTheme colors,
  }) {
    final selected = _selectedMethod == value;
    final primary = Theme.of(context).colorScheme.primary;
    return Material(
      color: Colors.transparent,
      child: InkWell(
        onTap: () => setState(() => _selectedMethod = value),
        borderRadius: BorderRadius.circular(14),
        child: AnimatedContainer(
          duration: AppTypography.base,
          curve: AppTypography.enterCurve,
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 15),
          decoration: BoxDecoration(
            color: selected
                ? primary.withValues(alpha: 0.08)
                : colors.surface,
            borderRadius: BorderRadius.circular(14),
            border: Border.all(
              color: selected ? primary : colors.border,
              width: selected ? 1.8 : 1.2,
            ),
            boxShadow: selected
                ? [
                    BoxShadow(
                      color: context.colors.brandGlow,
                      blurRadius: 16,
                      offset: const Offset(0, 5),
                      spreadRadius: -4,
                    ),
                  ]
                : null,
          ),
          child: Row(
            children: [
              AnimatedContainer(
                duration: AppTypography.base,
                curve: AppTypography.enterCurve,
                padding: const EdgeInsets.all(9),
                decoration: BoxDecoration(
                  gradient: selected ? context.colors.brandGradient : null,
                  color: selected ? null : colors.surfaceAlt,
                  borderRadius: BorderRadius.circular(11),
                  border: selected ? null : Border.all(color: colors.border),
                ),
                child: Icon(
                  icon,
                  size: 19,
                  color: selected
                      ? context.colors.onBrand
                      : colors.textSecondary,
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: CustomText(
                  label,
                  fontSize: 14,
                  fontWeight: FontWeight.w700,
                ),
              ),
              AnimatedScale(
                scale: selected ? 1 : 0.85,
                duration: AppTypography.fast,
                curve: AppTypography.enterCurve,
                child: Icon(
                  selected
                      ? Icons.check_circle_rounded
                      : Icons.circle_outlined,
                  size: 22,
                  color: selected ? primary : colors.textFaint,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
