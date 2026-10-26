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

class _WelcomeCheckInScreenState extends State<WelcomeCheckInScreen>
    with SingleTickerProviderStateMixin {
  static const Duration _entrance = Duration(milliseconds: 1100);

  bool _starting = false;
  String? _localError;
  late final AnimationController _intro;

  @override
  void initState() {
    super.initState();
    _intro = AnimationController(vsync: this, duration: _entrance)..forward();
  }

  @override
  void dispose() {
    _intro.dispose();
    super.dispose();
  }

  /// One-shot staggered entrance: fade + rise, each element entering on its
  /// own interval of the shared controller.
  Widget _entranceSlot(Interval interval, {required Widget child}) {
    return FadeTransition(
      opacity: CurvedAnimation(parent: _intro, curve: interval),
      child: SlideTransition(
        position: Tween<Offset>(
          begin: const Offset(0, 0.35),
          end: Offset.zero,
        ).animate(CurvedAnimation(parent: _intro, curve: interval)),
        child: child,
      ),
    );
  }

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
      barrierColor: context.colors.scrim,
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

    return BlocBuilder<SessionCubit, SessionState>(
      builder: (context, sessionState) {
        final sessionError = sessionState.error;

        return KioskScaffold(
          onToggleLocale: widget.onToggleLocale,
          onToggleTheme: widget.onToggleTheme,
          isDarkMode: widget.isDarkMode,
          body: LayoutBuilder(
            builder: (context, constraints) {
              return SingleChildScrollView(
                physics: const ClampingScrollPhysics(),
                child: ConstrainedBox(
                  constraints: BoxConstraints(minHeight: constraints.maxHeight),
                  child: IntrinsicHeight(
                    child: Padding(
                      padding: const EdgeInsets.symmetric(
                        horizontal: 32,
                        vertical: 16,
                      ),
                      child: Column(
                        children: [
                          const Spacer(flex: 2),
                          _entranceSlot(
                            const Interval(0.0, 0.45, curve: Curves.easeOutCubic),
                            child: RepaintBoundary(
                              child: _BrandMark(colors: colors),
                            ),
                          ),
                          const SizedBox(height: 24),
                          _entranceSlot(
                            const Interval(0.12, 0.55, curve: Curves.easeOutCubic),
                            child: AnimatedSwitcher(
                              duration: const Duration(milliseconds: 220),
                              transitionBuilder: (child, animation) =>
                                  FadeTransition(opacity: animation, child: child),
                              child: CustomText(
                                l10n.appName,
                                key: ValueKey(l10n.appName),
                                fontSize: 34,
                                fontWeight: FontWeight.w800,
                                letterSpacing: -0.4,
                                textAlign: TextAlign.center,
                              ),
                            ),
                          ),
                          const SizedBox(height: 8),
                          _entranceSlot(
                            const Interval(0.2, 0.62, curve: Curves.easeOutCubic),
                            child: AnimatedSwitcher(
                              duration: const Duration(milliseconds: 220),
                              transitionBuilder: (child, animation) =>
                                  FadeTransition(opacity: animation, child: child),
                              child: CustomText(
                                l10n.welcomeSubtitle,
                                key: ValueKey(l10n.welcomeSubtitle),
                                fontSize: 16,
                                color: colors.textSecondary,
                                textAlign: TextAlign.center,
                              ),
                            ),
                          ),
                          const SizedBox(height: 36),
                          _entranceSlot(
                            const Interval(0.3, 0.75, curve: Curves.easeOutCubic),
                            child: ConstrainedBox(
                              constraints: BoxConstraints(
                                maxWidth:
                                    (constraints.maxWidth * 0.74).clamp(720.0, 880.0),
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
                                          onPressed:
                                              _starting ? null : _startGuest,
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
                                          variant: KioskButtonVariant.secondary,
                                          icon: Icons.contactless_rounded,
                                          onPressed:
                                              _starting ? null : _showNfcSignInModal,
                                        ),
                                      ),
                                    ),
                                  ],
                                ),
                              ),
                            ),
                          ),
                          if (_localError != null || sessionError != null) ...[
                            const SizedBox(height: 16),
                            _entranceSlot(
                              const Interval(0.0, 1.0),
                              child: Container(
                                padding: const EdgeInsets.symmetric(
                                  horizontal: 14,
                                  vertical: 10,
                                ),
                                decoration: BoxDecoration(
                                  color: colors.danger.withValues(alpha: 0.10),
                                  borderRadius: BorderRadius.circular(10),
                                  border: Border.all(
                                    color: colors.danger.withValues(alpha: 0.35),
                                  ),
                                ),
                                child: Row(
                                  mainAxisSize: MainAxisSize.min,
                                  children: [
                                    Icon(
                                      Icons.error_outline_rounded,
                                      size: 18,
                                      color: colors.danger,
                                    ),
                                    const SizedBox(width: 8),
                                    Flexible(
                                      child: CustomText(
                                        _localError ??
                                            sessionError ??
                                            l10n.sessionFailed,
                                        fontSize: 13,
                                        color: colors.danger,
                                        fontWeight: FontWeight.w600,
                                      ),
                                    ),
                                  ],
                                ),
                              ),
                            ),
                          ],
                          if (!config.isScaleReady) ...[
                            const SizedBox(height: 10),
                            CustomText(
                              l10n.scaleOfflineHelp,
                              fontSize: 12,
                              color: colors.textSecondary,
                              textAlign: TextAlign.center,
                            ),
                          ],
                          const Spacer(flex: 3),
                        ],
                      ),
                    ),
                  ),
                ),
              );
            },
          ),
        );
      },
    );
  }
}

/// SARE brand mark: emerald gradient squircle inside a quiet ring —
/// the single ornamental element on the welcome screen.
class _BrandMark extends StatelessWidget {
  const _BrandMark({required this.colors});

  final AppColorTheme colors;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(10),
      decoration: BoxDecoration(
        shape: BoxShape.circle,
        border: Border.all(color: colors.brand.withValues(alpha: 0.28)),
      ),
      child: Container(
        width: 78,
        height: 78,
        decoration: BoxDecoration(
          gradient: colors.brandGradient,
          borderRadius: BorderRadius.circular(26),
          boxShadow: [
            BoxShadow(
              color: colors.brandGlow,
              blurRadius: 30,
              offset: const Offset(0, 10),
              spreadRadius: -4,
            ),
          ],
        ),
        child: const Center(
          child: Icon(
            Icons.shopping_cart_checkout_rounded,
            size: 40,
            color: Colors.white,
          ),
        ),
      ),
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
      backgroundColor: Theme.of(context).colorScheme.surface,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(24),
        side: BorderSide(color: colors.borderStrong, width: 1.4),
      ),
      child: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: 540),
        child: Padding(
          padding: const EdgeInsets.all(26),
          child: SingleChildScrollView(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Row(
                  children: [
                    Container(
                      padding: const EdgeInsets.all(11),
                      decoration: BoxDecoration(
                        color: colors.brand.withValues(alpha: 0.12),
                        borderRadius: BorderRadius.circular(14),
                        border: Border.all(
                          color: colors.brand.withValues(alpha: 0.35),
                        ),
                      ),
                      child: Icon(
                        Icons.contactless_rounded,
                        size: 26,
                        color: colors.brand,
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
                            fontWeight: FontWeight.w700,
                          ),
                          const SizedBox(height: 2),
                          CustomText(
                            l10n.nfcModalSubtitle,
                            fontSize: 12,
                            color: colors.textSecondary,
                          ),
                        ],
                      ),
                    ),
                    IconButton(
                      onPressed: () => Navigator.of(context).pop(),
                      icon: Icon(Icons.close_rounded, color: colors.textSecondary),
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
                          padding: const EdgeInsets.symmetric(
                            horizontal: 16,
                            vertical: 12,
                          ),
                          decoration: BoxDecoration(
                            color: colors.surfaceAlt,
                            borderRadius: BorderRadius.circular(14),
                            border: Border.all(color: colors.border),
                          ),
                          child: Row(
                            children: [
                              Container(
                                padding: const EdgeInsets.all(9),
                                decoration: BoxDecoration(
                                  color: colors.brand.withValues(alpha: 0.10),
                                  borderRadius: BorderRadius.circular(11),
                                ),
                                child: Icon(
                                  Icons.credit_card_rounded,
                                  size: 19,
                                  color: colors.brand,
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
                                      color: colors.textSecondary,
                                    ),
                                  ],
                                ),
                              ),
                              Icon(
                                Icons.arrow_forward_ios_rounded,
                                size: 15,
                                color: colors.textFaint,
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
                          color: colors.textSecondary,
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
