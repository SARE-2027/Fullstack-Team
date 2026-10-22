import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/config/kiosk_config_exports.dart';
import '../../../../core/routing/app_router.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../core/widgets/kiosk_widgets.dart';
import '../../../../l10n/app_localizations.dart';
import '../../../cart/data/repositories/local_smart_cart_engine.dart';
import '../../../session/presentation/cubit/session_cubit.dart';
import '../../../session/presentation/cubit/session_state.dart';

class WelcomeCheckInScreen extends StatefulWidget {
  const WelcomeCheckInScreen({
    super.key,
    required this.onToggleLocale,
    required this.onToggleTheme,
    required this.isDarkMode,
  });

  final VoidCallback onToggleLocale;
  final VoidCallback onToggleTheme;
  final bool isDarkMode;

  @override
  State<WelcomeCheckInScreen> createState() => _WelcomeCheckInScreenState();
}

class _WelcomeCheckInScreenState extends State<WelcomeCheckInScreen> {
  bool _starting = false;
  String? _localError;

  Future<void> _startGuest() async {
    if (_starting) return;
    setState(() {
      _starting = true;
      _localError = null;
    });
    final cartId = context.kioskConfig.cartId;
    final cubit = context.read<SessionCubit>();
    final ok = await cubit.startGuestSession(cartId);
    if (!mounted) return;
    setState(() => _starting = false);
    if (!ok) {
      final msg = cubit.state.error ?? AppLocalizations.of(context)!.sessionFailed;
      setState(() => _localError = msg);
      return;
    }
    final sid = cubit.state.session!.sessionId;
    if (!mounted) return;
    context.go('${AppRoutes.cart}?sessionId=$sid');
  }

  Future<void> _startMemberWithNfc(String nfcUid) async {
    if (_starting || nfcUid.trim().isEmpty) return;
    setState(() {
      _starting = true;
      _localError = null;
    });
    final cartId = context.kioskConfig.cartId;
    final cubit = context.read<SessionCubit>();
    final ok = await cubit.startMemberSession(cartId, nfcUid.trim());
    if (!mounted) return;
    setState(() => _starting = false);
    if (!ok) {
      final msg = cubit.state.error ?? AppLocalizations.of(context)!.sessionFailed;
      setState(() => _localError = msg);
      return;
    }
    final sid = cubit.state.session!.sessionId;
    if (!mounted) return;
    context.go('${AppRoutes.cart}?sessionId=$sid');
  }

  Future<void> _showNfcSignInModal() async {
    final selectedNfcUid = await showDialog<String>(
      context: context,
      builder: (dialogContext) => const _NfcMemberSignInDialog(),
    );
    if (!mounted || selectedNfcUid == null || selectedNfcUid.trim().isEmpty) {
      return;
    }
    await _startMemberWithNfc(selectedNfcUid);
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;
    final colors = context.colors;
    final config = context.kioskConfig;

    return BlocConsumer<SessionCubit, SessionState>(
      listenWhen: (prev, next) => prev.error != next.error && next.error != null,
      listener: (context, state) {
        if (state.error != null && !_starting) {
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(content: Text(state.error!), duration: const Duration(seconds: 3)),
          );
        }
      },
      builder: (context, sessionState) {
        final sessionError = sessionState.error;

        return Scaffold(
          body: SafeArea(
            child: LayoutBuilder(
              builder: (context, constraints) {
                return SingleChildScrollView(
                  physics: const ClampingScrollPhysics(),
                  child: ConstrainedBox(
                    constraints: BoxConstraints(minHeight: constraints.maxHeight),
                    child: IntrinsicHeight(
                      child: Padding(
                        padding: const EdgeInsets.symmetric(horizontal: 32, vertical: 24),
                        child: Column(
                          children: [
                            Directionality(
                              textDirection: TextDirection.ltr,
                              child: Row(
                                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                                children: [
                                  Flexible(
                                    child: FittedBox(
                                      fit: BoxFit.scaleDown,
                                      alignment: Alignment.centerLeft,
                                      child: Row(
                                        mainAxisSize: MainAxisSize.min,
                                        children: [
                                          KioskStatusBadge(
                                            leading: Icon(
                                              Icons.shopping_cart_outlined,
                                              size: 18,
                                              color: colors.secondaryText,
                                            ),
                                            label: l10n.cartNumberLabel(config.cartNumber),
                                            borderColor: colors.containerBorder,
                                          ),
                                          const SizedBox(width: 12),
                                          KioskStatusBadge(
                                            leading: Container(
                                              width: 8,
                                              height: 8,
                                              decoration: BoxDecoration(
                                                color: config.isScaleReady
                                                    ? colors.weightVerified
                                                    : colors.weightDiscrepancy,
                                                shape: BoxShape.circle,
                                              ),
                                            ),
                                            label: config.isScaleReady
                                                ? l10n.scaleReadyLabel
                                                : l10n.scaleOfflineLabel,
                                            backgroundColor: (config.isScaleReady
                                                    ? colors.weightVerified
                                                    : colors.weightDiscrepancy)
                                                .withValues(alpha: 0.12),
                                            textColor: config.isScaleReady
                                                ? colors.weightVerified
                                                : colors.weightDiscrepancy,
                                          ),
                                        ],
                                      ),
                                    ),
                                  ),
                                  const SizedBox(width: 16),
                                  Flexible(
                                    child: FittedBox(
                                      fit: BoxFit.scaleDown,
                                      alignment: Alignment.centerRight,
                                      child: Row(
                                        mainAxisSize: MainAxisSize.min,
                                        children: [
                                          KioskHeaderButton(
                                            onPressed: () => showKioskStaffHelpDialog(context),
                                            icon: Icons.support_agent_rounded,
                                            label: l10n.callStaff,
                                            borderColor: colors.containerBorder,
                                            backgroundColor: colors.textFieldFill,
                                          ),
                                          const SizedBox(width: 10),
                                          KioskHeaderButton(
                                            onPressed: widget.onToggleTheme,
                                            icon: widget.isDarkMode
                                                ? Icons.light_mode_outlined
                                                : Icons.dark_mode_outlined,
                                            label: widget.isDarkMode
                                                ? l10n.themeLight
                                                : l10n.themeDark,
                                            borderColor: colors.containerBorder,
                                            backgroundColor: colors.textFieldFill,
                                          ),
                                          const SizedBox(width: 10),
                                          KioskHeaderButton(
                                            onPressed: widget.onToggleLocale,
                                            icon: Icons.language,
                                            label: l10n.switchLanguage,
                                            borderColor: colors.containerBorder,
                                            backgroundColor: colors.textFieldFill,
                                          ),
                                        ],
                                      ),
                                    ),
                                  ),
                                ],
                              ),
                            ),
                            const Spacer(),
                            RepaintBoundary(
                              child: Container(
                                width: 76,
                                height: 76,
                                decoration: BoxDecoration(
                                  color: colors.textFieldFill,
                                  shape: BoxShape.circle,
                                  border: Border.all(color: colors.containerBorder, width: 1.5),
                                ),
                                child: Center(
                                  child: Icon(
                                    Icons.shopping_cart_checkout_rounded,
                                    size: 38,
                                    color: Theme.of(context).colorScheme.primary,
                                  ),
                                ),
                              ),
                            ),
                            const SizedBox(height: 18),
                            AnimatedSwitcher(
                              duration: const Duration(milliseconds: 220),
                              switchInCurve: Curves.easeOutCubic,
                              switchOutCurve: Curves.easeInCubic,
                              transitionBuilder: (child, animation) =>
                                  FadeTransition(opacity: animation, child: child),
                              child: CustomText(
                                l10n.appName,
                                key: ValueKey(l10n.appName),
                                fontSize: 32,
                                fontWeight: FontWeight.w900,
                                textAlign: TextAlign.center,
                              ),
                            ),
                            const SizedBox(height: 8),
                            AnimatedSwitcher(
                              duration: const Duration(milliseconds: 220),
                              switchInCurve: Curves.easeOutCubic,
                              switchOutCurve: Curves.easeInCubic,
                              transitionBuilder: (child, animation) =>
                                  FadeTransition(opacity: animation, child: child),
                              child: CustomText(
                                l10n.welcomeSubtitle,
                                key: ValueKey(l10n.welcomeSubtitle),
                                fontSize: 16,
                                color: colors.secondaryText,
                                textAlign: TextAlign.center,
                              ),
                            ),
                            const SizedBox(height: 36),
                            ConstrainedBox(
                              constraints: BoxConstraints(
                                maxWidth: (constraints.maxWidth * 0.74).clamp(720.0, 860.0),
                              ),
                              child: IntrinsicHeight(
                                child: Row(
                                  crossAxisAlignment: CrossAxisAlignment.stretch,
                                  children: [
                                    Expanded(
                                      child: KioskActionCard(
                                        title: l10n.startAsGuest,
                                        description: l10n.startAsGuestDescription,
                                        icon: Icons.bolt_rounded,
                                        isPrimary: true,
                                        actionButton: KioskButton(
                                          text: _starting
                                              ? l10n.startingSession
                                              : l10n.startAsGuest,
                                          height: 52,
                                          icon: Icons.arrow_forward_rounded,
                                          isLoading: _starting,
                                          onPressed: _starting ? null : _startGuest,
                                        ),
                                      ),
                                    ),
                                    const SizedBox(width: 24),
                                    Expanded(
                                      child: KioskActionCard(
                                        title: l10n.loyaltySignIn,
                                        description: l10n.tapCardOrQr,
                                        icon: Icons.contactless_rounded,
                                        isPrimary: false,
                                        actionButton: KioskButton(
                                          text: l10n.loyaltySignIn,
                                          height: 52,
                                          variant: KioskButtonVariant.outlined,
                                          icon: Icons.contactless_rounded,
                                          onPressed: _starting ? null : _showNfcSignInModal,
                                        ),
                                      ),
                                    ),
                                  ],
                                ),
                              ),
                            ),
                            if (_localError != null || sessionError != null) ...[
                              const SizedBox(height: 16),
                              Container(
                                padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
                                decoration: BoxDecoration(
                                  color: colors.weightDiscrepancy.withValues(alpha: 0.10),
                                  borderRadius: BorderRadius.circular(10),
                                  border: Border.all(
                                    color: colors.weightDiscrepancy.withValues(alpha: 0.35),
                                  ),
                                ),
                                child: Row(
                                  mainAxisSize: MainAxisSize.min,
                                  children: [
                                    Icon(
                                      Icons.error_outline_rounded,
                                      size: 18,
                                      color: colors.weightDiscrepancy,
                                    ),
                                    const SizedBox(width: 8),
                                    Flexible(
                                      child: CustomText(
                                        _localError ?? sessionError ?? l10n.sessionFailed,
                                        fontSize: 13,
                                        color: colors.weightDiscrepancy,
                                        fontWeight: FontWeight.w600,
                                      ),
                                    ),
                                  ],
                                ),
                              ),
                            ],
                            if (!config.isScaleReady) ...[
                              const SizedBox(height: 10),
                              CustomText(
                                l10n.scaleOfflineHelp,
                                fontSize: 12,
                                color: colors.secondaryText,
                                textAlign: TextAlign.center,
                              ),
                            ],
                            const Spacer(),
                          ],
                        ),
                      ),
                    ),
                  ),
                );
              },
            ),
          ),
        );
      },
    );
  }
}

class _NfcMemberSignInDialog extends StatefulWidget {
  const _NfcMemberSignInDialog();

  @override
  State<_NfcMemberSignInDialog> createState() => _NfcMemberSignInDialogState();
}

class _NfcMemberSignInDialogState extends State<_NfcMemberSignInDialog> {
  final TextEditingController _nfcController = TextEditingController();

  @override
  void dispose() {
    _nfcController.dispose();
    super.dispose();
  }

  void _submitManual() {
    final text = _nfcController.text.trim();
    if (text.isEmpty) return;
    Navigator.of(context).pop(text);
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;
    final colors = context.colors;
    final locale = Localizations.localeOf(context).languageCode;

    return Dialog(
      backgroundColor: Theme.of(context).scaffoldBackgroundColor,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(22),
        side: BorderSide(color: colors.containerBorder, width: 1.5),
      ),
      child: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: 540),
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: SingleChildScrollView(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Row(
                  children: [
                    Container(
                      padding: const EdgeInsets.all(10),
                      decoration: BoxDecoration(
                        color: colors.weightVerified.withValues(alpha: 0.14),
                        shape: BoxShape.circle,
                      ),
                      child: Icon(
                        Icons.contactless_rounded,
                        size: 26,
                        color: colors.weightVerified,
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          CustomText(
                            l10n.nfcModalTitle,
                            fontSize: 19,
                            fontWeight: FontWeight.w800,
                          ),
                          const SizedBox(height: 2),
                          CustomText(
                            l10n.nfcModalSubtitle,
                            fontSize: 12,
                            color: colors.secondaryText,
                          ),
                        ],
                      ),
                    ),
                    IconButton(
                      onPressed: () => Navigator.of(context).pop(),
                      icon: Icon(Icons.close_rounded, color: colors.secondaryText),
                    ),
                  ],
                ),
                const SizedBox(height: 18),
                ...LocalSmartCartEngine.members.map(
                  (member) => Padding(
                    padding: const EdgeInsets.only(bottom: 10),
                    child: Material(
                      color: Colors.transparent,
                      child: InkWell(
                        onTap: () => Navigator.of(context).pop(member.nfcUid),
                        borderRadius: BorderRadius.circular(14),
                        child: Container(
                          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
                          decoration: BoxDecoration(
                            color: colors.textFieldFill,
                            borderRadius: BorderRadius.circular(14),
                            border: Border.all(color: colors.containerBorder),
                          ),
                          child: Row(
                            children: [
                              Container(
                                padding: const EdgeInsets.all(8),
                                decoration: BoxDecoration(
                                  color: Theme.of(context)
                                      .colorScheme
                                      .primary
                                      .withValues(alpha: 0.10),
                                  shape: BoxShape.circle,
                                ),
                                child: Icon(
                                  Icons.credit_card_rounded,
                                  size: 18,
                                  color: Theme.of(context).colorScheme.primary,
                                ),
                              ),
                              const SizedBox(width: 12),
                              Expanded(
                                child: Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    CustomText(
                                      member.displayName(locale),
                                      fontSize: 15,
                                      fontWeight: FontWeight.w700,
                                    ),
                                    const SizedBox(height: 2),
                                    CustomText(
                                      '${member.displayTier(locale)} • UID: ${member.nfcUid}',
                                      fontSize: 12,
                                      color: colors.secondaryText,
                                    ),
                                  ],
                                ),
                              ),
                              Icon(
                                Icons.arrow_forward_ios_rounded,
                                size: 15,
                                color: colors.secondaryText,
                              ),
                            ],
                          ),
                        ),
                      ),
                    ),
                  ),
                ),
                const SizedBox(height: 8),
                Row(
                  children: [
                    Expanded(
                      child: KioskTextField(
                        controller: _nfcController,
                        hintText: l10n.nfcInputHint,
                        prefixIcon: Icon(
                          Icons.nfc_rounded,
                          size: 20,
                          color: colors.secondaryText,
                        ),
                        textInputAction: TextInputAction.done,
                        onSubmitted: (_) => _submitManual(),
                        showClearButton: true,
                      ),
                    ),
                    const SizedBox(width: 10),
                    KioskButton(
                      text: l10n.nfcConnectButton,
                      icon: Icons.login_rounded,
                      onPressed: _submitManual,
                    ),
                  ],
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
