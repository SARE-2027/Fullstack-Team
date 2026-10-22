import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/routing/app_router.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../core/utils/kiosk_formatter.dart';
import '../../../../core/widgets/kiosk_widgets.dart';
import '../../../../l10n/app_localizations.dart';
import '../../../session/data/models/session_models.dart';
import '../../../session/presentation/cubit/session_cubit.dart';
import '../../../session/presentation/cubit/session_state.dart';
import '../../data/repositories/local_smart_cart_engine.dart';
import '../cubit/cart_cubit.dart';
import '../cubit/cart_state.dart';
import '../widgets/cart_item_tile.dart';
import '../widgets/cart_summary_bar.dart';
import '../widgets/scan_bar.dart';
import '../widgets/sensor_simulator_strip.dart';

class CartScreen extends StatefulWidget {
  const CartScreen({
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
  State<CartScreen> createState() => _CartScreenState();
}

class _CartScreenState extends State<CartScreen> {
  final TextEditingController _barcodeCtrl = TextEditingController();
  final TextEditingController _weightCtrl = TextEditingController();
  final StringBuffer _hidBuffer = StringBuffer();
  DateTime? _lastHidKeystroke;

  @override
  void initState() {
    super.initState();
    HardwareKeyboard.instance.addHandler(_handleHardwareKey);
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (!mounted) return;
      final sid = widget.sessionId;
      if (sid.isNotEmpty) {
        context.read<SessionCubit>().hydrate(sid);
      }
    });
  }

  @override
  void dispose() {
    HardwareKeyboard.instance.removeHandler(_handleHardwareKey);
    _barcodeCtrl.dispose();
    _weightCtrl.dispose();
    super.dispose();
  }

  /// Captures USB HID barcode scanner keystrokes when no text input is focused.
  bool _handleHardwareKey(KeyEvent event) {
    if (event is! KeyDownEvent) return false;
    final primaryFocus = FocusManager.instance.primaryFocus;
    if (primaryFocus?.context?.widget is EditableText) {
      return false;
    }

    final now = DateTime.now();
    if (_lastHidKeystroke != null &&
        now.difference(_lastHidKeystroke!).inMilliseconds > 300) {
      _hidBuffer.clear();
    }
    _lastHidKeystroke = now;

    if (event.logicalKey == LogicalKeyboardKey.enter ||
        event.logicalKey == LogicalKeyboardKey.numpadEnter) {
      final scanned = _hidBuffer.toString().trim();
      _hidBuffer.clear();
      if (scanned.length >= 4 && mounted) {
        context.read<CartCubit>().addItemByScan(scanned);
        return true;
      }
      return false;
    }

    final char = event.character;
    if (char != null && char.isNotEmpty && RegExp(r'[0-9A-Za-z\-_]').hasMatch(char)) {
      _hidBuffer.write(char);
    }
    return false;
  }

  Future<void> _handleAdd() async {
    final rawWeight = _weightCtrl.text.trim();
    final weight = rawWeight.isEmpty ? null : int.tryParse(rawWeight);
    final ok = await context.read<CartCubit>().addItemByScan(
          _barcodeCtrl.text,
          measuredWeightG: weight,
        );
    if (ok && mounted) {
      _barcodeCtrl.clear();
      _weightCtrl.clear();
    }
  }

  Future<void> _handleSimulateScan(
    KioskCatalogProduct product, {
    required bool isFraud,
  }) async {
    final simulatedWeight = isFraud ? product.weightG + 95 : product.weightG;
    await context.read<CartCubit>().addItemByScan(
          product.barcode,
          measuredWeightG: simulatedWeight,
        );
  }

  Future<void> _handleEndSession(SessionSummary session) async {
    final l10n = AppLocalizations.of(context)!;
    final colors = context.colors;

    if (session.items.isNotEmpty) {
      final confirmed = await showDialog<bool>(
        context: context,
        builder: (dialogContext) => Dialog(
          backgroundColor: Theme.of(dialogContext).scaffoldBackgroundColor,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(20),
            side: BorderSide(color: colors.containerBorder, width: 1.5),
          ),
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 440),
            child: Padding(
              padding: const EdgeInsets.all(24),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Icon(
                    Icons.exit_to_app_rounded,
                    size: 40,
                    color: colors.weightDiscrepancy,
                  ),
                  const SizedBox(height: 14),
                  CustomText(
                    l10n.endSessionConfirmTitle,
                    fontSize: 19,
                    fontWeight: FontWeight.w800,
                    textAlign: TextAlign.center,
                  ),
                  const SizedBox(height: 8),
                  CustomText(
                    l10n.endSessionConfirmMessage,
                    fontSize: 14,
                    color: colors.secondaryText,
                    textAlign: TextAlign.center,
                  ),
                  const SizedBox(height: 22),
                  Row(
                    children: [
                      Expanded(
                        child: KioskButton(
                          text: l10n.cancel,
                          variant: KioskButtonVariant.outlined,
                          onPressed: () => Navigator.of(dialogContext).pop(false),
                        ),
                      ),
                      const SizedBox(width: 12),
                      Expanded(
                        child: KioskButton(
                          text: l10n.confirm,
                          variant: KioskButtonVariant.danger,
                          onPressed: () => Navigator.of(dialogContext).pop(true),
                        ),
                      ),
                    ],
                  ),
                ],
              ),
            ),
          ),
        ),
      );
      if (confirmed != true || !mounted) return;
    }

    await context.read<SessionCubit>().closeSession(reason: 'abandoned');
    if (!mounted) return;
    context.go(AppRoutes.welcome);
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;
    final locale = Localizations.localeOf(context).languageCode;

    return BlocBuilder<SessionCubit, SessionState>(
      builder: (context, sessionState) {
        return BlocBuilder<CartCubit, CartState>(
          builder: (context, cartState) {
            final session = sessionState.session;
            final colors = context.colors;

            final banner = _buildBanner(context, l10n, locale, sessionState, cartState);

            if (sessionState.isLoading && session == null) {
              return KioskScaffold(
                onToggleLocale: widget.onToggleLocale,
                onToggleTheme: widget.onToggleTheme,
                isDarkMode: widget.isDarkMode,
                floatingBanner: banner,
                body: const Center(child: CircularProgressIndicator()),
              );
            }

            if (session == null) {
              return KioskScaffold(
                onToggleLocale: widget.onToggleLocale,
                onToggleTheme: widget.onToggleTheme,
                isDarkMode: widget.isDarkMode,
                floatingBanner: banner,
                body: Center(
                  child: Padding(
                    padding: const EdgeInsets.all(32),
                    child: Column(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Icon(
                          Icons.receipt_long_rounded,
                          size: 48,
                          color: colors.secondaryText,
                        ),
                        const SizedBox(height: 12),
                        CustomText(
                          l10n.sessionFailed,
                          fontSize: 16,
                          fontWeight: FontWeight.w700,
                          textAlign: TextAlign.center,
                        ),
                        const SizedBox(height: 8),
                        CustomText(
                          sessionState.error ?? l10n.networkError,
                          fontSize: 13,
                          color: colors.secondaryText,
                          textAlign: TextAlign.center,
                        ),
                        const SizedBox(height: 18),
                        KioskButton(
                          text: l10n.continueShopping,
                          icon: Icons.home_rounded,
                          onPressed: () => context.go(AppRoutes.welcome),
                        ),
                      ],
                    ),
                  ),
                ),
              );
            }

            if (!session.isOpen) {
              WidgetsBinding.instance.addPostFrameCallback((_) {
                if (mounted) context.go(AppRoutes.welcome);
              });
            }

            final shopperLabel = session.isMember
                ? l10n.memberShopperBadge(session.userName ?? session.nfcUid ?? '')
                : l10n.guestShopperBadge;

            return KioskScaffold(
              onToggleLocale: widget.onToggleLocale,
              onToggleTheme: widget.onToggleTheme,
              isDarkMode: widget.isDarkMode,
              floatingBanner: banner,
              extraStatusBadge: KioskStatusBadge(
                leading: Icon(
                  session.isMember ? Icons.verified_user_rounded : Icons.person_outline_rounded,
                  size: 16,
                  color: session.isMember ? colors.weightVerified : colors.secondaryText,
                ),
                label: shopperLabel,
                borderColor: colors.containerBorder,
              ),
              extraActionButton: KioskHeaderButton(
                onPressed: () => _handleEndSession(session),
                icon: Icons.logout_rounded,
                label: l10n.endSessionButton,
                borderColor: colors.containerBorder,
                backgroundColor: colors.textFieldFill,
              ),
              body: LayoutBuilder(
                builder: (context, constraints) {
                  final isWide = constraints.maxWidth >= 960;
                  return Padding(
                    padding: const EdgeInsets.fromLTRB(24, 6, 24, 16),
                    child: Column(
                      children: [
                        RepaintBoundary(
                          child: ScanBar(
                            barcodeController: _barcodeCtrl,
                            weightController: _weightCtrl,
                            isAdding: cartState.isAdding,
                            onAdd: _handleAdd,
                          ),
                        ),
                        const SizedBox(height: 12),
                        Expanded(
                          child: isWide
                              ? Row(
                                  crossAxisAlignment: CrossAxisAlignment.stretch,
                                  children: [
                                    Expanded(
                                      flex: 3,
                                      child: _buildItemsList(session, l10n, colors),
                                    ),
                                    const SizedBox(width: 18),
                                    SizedBox(
                                      width: 350,
                                      child: SingleChildScrollView(
                                        child: _buildSidePanel(session, cartState),
                                      ),
                                    ),
                                  ],
                                )
                              : Column(
                                  children: [
                                    Expanded(child: _buildItemsList(session, l10n, colors)),
                                    const SizedBox(height: 12),
                                    _buildSidePanel(session, cartState),
                                  ],
                                ),
                        ),
                        const SizedBox(height: 12),
                        RepaintBoundary(
                          child: SensorSimulatorStrip(
                            isBusy: cartState.isAdding,
                            onSimulateScan: _handleSimulateScan,
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

  Widget? _buildBanner(
    BuildContext context,
    AppLocalizations l10n,
    String locale,
    SessionState sessionState,
    CartState cartState,
  ) {
    switch (cartState.bannerKind) {
      case WeightBannerKind.verifying:
        return KioskStatusBanner(
          message: l10n.weightVerifying,
          status: KioskBannerStatus.info,
          icon: Icons.scale_rounded,
        );
      case WeightBannerKind.success:
        final itemName =
            locale == 'ar' ? cartState.lastItemNameAr : cartState.lastItemNameEn;
        final msg = itemName != null && itemName.isNotEmpty
            ? '${l10n.itemAddedSuccess} — $itemName'
            : l10n.itemAddedSuccess;
        return KioskStatusBanner(
          message: msg,
          status: KioskBannerStatus.verified,
          actionLabel: l10n.dismiss,
          onAction: () => context.read<CartCubit>().dismissBanner(),
        );
      case WeightBannerKind.removed:
        return KioskStatusBanner(
          message: l10n.itemRemoved,
          status: KioskBannerStatus.info,
          icon: Icons.remove_shopping_cart_rounded,
          actionLabel: l10n.dismiss,
          onAction: () => context.read<CartCubit>().dismissBanner(),
        );
      case WeightBannerKind.mismatch:
        String msg = cartState.bannerMessage ?? l10n.weightMismatchWarning;
        if (cartState.lastExpectedG != null && cartState.lastMeasuredG != null) {
          final expStr = KioskFormatter.formatWeight(
            cartState.lastExpectedG!.toDouble(),
            locale,
          );
          final measStr = KioskFormatter.formatWeight(
            cartState.lastMeasuredG!.toDouble(),
            locale,
          );
          msg = '${l10n.weightMismatchWarning} (${l10n.weightMismatchDetail(expStr, measStr)})';
        }
        return KioskStatusBanner(
          message: msg,
          status: KioskBannerStatus.discrepancy,
          actionLabel: l10n.dismiss,
          onAction: () => context.read<CartCubit>().dismissBanner(),
        );
      case WeightBannerKind.none:
        if (sessionState.error != null) {
          return KioskStatusBanner(
            message: sessionState.error!,
            status: KioskBannerStatus.discrepancy,
            actionLabel: l10n.retry,
            onAction: () => context.read<SessionCubit>().refresh(),
          );
        }
        return null;
    }
  }

  Widget _buildItemsList(
    SessionSummary session,
    AppLocalizations l10n,
    AppColorTheme colors,
  ) {
    final items = session.items;
    if (items.isEmpty) {
      return Container(
        decoration: BoxDecoration(
          color: colors.textFieldFill,
          borderRadius: BorderRadius.circular(16),
          border: Border.all(color: colors.containerBorder),
        ),
        child: Center(
          child: Padding(
            padding: const EdgeInsets.all(24),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                Icon(
                  Icons.shopping_bag_outlined,
                  size: 42,
                  color: colors.secondaryText,
                ),
                const SizedBox(height: 12),
                CustomText(
                  l10n.emptyCartTitle,
                  fontSize: 16,
                  fontWeight: FontWeight.bold,
                ),
                const SizedBox(height: 6),
                CustomText(
                  l10n.emptyCartMessage,
                  fontSize: 13,
                  color: colors.secondaryText,
                  textAlign: TextAlign.center,
                ),
                const SizedBox(height: 4),
                CustomText(
                  l10n.emptyCartHint,
                  fontSize: 12,
                  color: colors.secondaryText,
                  textAlign: TextAlign.center,
                ),
              ],
            ),
          ),
        ),
      );
    }
    return RepaintBoundary(
      child: ListView.separated(
        itemCount: items.length,
        separatorBuilder: (_, _) => const SizedBox(height: 10),
        itemBuilder: (context, index) {
          final item = items[index];
          return RepaintBoundary(
            child: CartItemTile(
              key: ValueKey(item.id),
              item: item,
              onRemove: () => context.read<CartCubit>().removeItem(item.id),
            ),
          );
        },
      ),
    );
  }

  Widget _buildSidePanel(SessionSummary session, CartState cartState) {
    return RepaintBoundary(
      child: CartSummaryBar(
        itemsCount: session.itemsCount,
        totalMinor: session.totalMinor,
        totalWeightG: session.totalExpectedWeightG,
        isBusy: cartState.isAdding,
        onCheckout: () {
          context.push('${AppRoutes.checkout}?sessionId=${session.sessionId}');
        },
      ),
    );
  }
}
