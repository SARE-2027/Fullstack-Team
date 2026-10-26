import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';
import 'package:qr_flutter/qr_flutter.dart';

import '../../../../core/routing/app_router.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../core/utils/money.dart';
import '../../../../core/widgets/kiosk_widgets.dart';
import '../../../../l10n/app_localizations.dart';
import '../../../session/presentation/cubit/session_cubit.dart';

class ReceiptScreen extends StatefulWidget {
  const ReceiptScreen({
    super.key,
    required this.receiptHash,
    required this.totalMinor,
    required this.sessionId,
    required this.onToggleLocale,
    required this.onToggleTheme,
    required this.isDarkMode,
    this.itemsCount = 0,
    this.paymentMethod = 'card',
  });

  final String receiptHash;
  final int totalMinor;
  final String sessionId;
  final VoidCallback onToggleLocale;
  final VoidCallback onToggleTheme;
  final bool isDarkMode;
  final int itemsCount;
  final String paymentMethod;

  @override
  State<ReceiptScreen> createState() => _ReceiptScreenState();
}

class _ReceiptScreenState extends State<ReceiptScreen> {
  static const int _autoResetSeconds = 12;
  int _secondsLeft = _autoResetSeconds;
  bool _isPaused = false;
  Timer? _timer;

  @override
  void initState() {
    super.initState();
    _timer = Timer.periodic(const Duration(seconds: 1), (t) {
      if (!mounted || _isPaused) return;
      if (_secondsLeft <= 1) {
        t.cancel();
        _goIdle();
      } else {
        setState(() => _secondsLeft--);
      }
    });
  }

  @override
  void dispose() {
    _timer?.cancel();
    super.dispose();
  }

  Future<void> _goIdle() async {
    _timer?.cancel();
    if (!mounted) return;
    await context.read<SessionCubit>().reset();
    if (!mounted) return;
    context.go(AppRoutes.welcome);
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;
    final locale = Localizations.localeOf(context).languageCode;
    final colors = context.colors;
    final totalLabel = Money.formatMinor(
      widget.totalMinor,
      locale,
      symbol: ' ${l10n.currencySymbol}',
    );
    final qrData = widget.receiptHash.isNotEmpty
        ? widget.receiptHash
        : 'SARE:${widget.sessionId}';

    return KioskScaffold(
      onToggleLocale: widget.onToggleLocale,
      onToggleTheme: widget.onToggleTheme,
      isDarkMode: widget.isDarkMode,
      body: Center(
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 640),
          child: Padding(
            padding: const EdgeInsets.fromLTRB(24, 12, 24, 20),
            child: SingleChildScrollView(
              child: Column(
                children: [
                  const _SuccessBurst(),
                  const SizedBox(height: 14),
                  CustomText(
                    l10n.paymentSuccess,
                    fontSize: 24,
                    fontWeight: FontWeight.w800,
                    textAlign: TextAlign.center,
                  ),
                  const SizedBox(height: 4),
                  CustomText(
                    l10n.paymentSuccessSubtitle,
                    fontSize: 14,
                    color: colors.textSecondary,
                    textAlign: TextAlign.center,
                  ),
                  const SizedBox(height: 8),
                  CustomText(
                    totalLabel,
                    fontSize: 26,
                    fontWeight: FontWeight.w800,
                    color: colors.verified,
                    textAlign: TextAlign.center,
                  ),
                  const SizedBox(height: 18),
                  RepaintBoundary(
                    child: Container(
                      padding: const EdgeInsets.all(20),
                      decoration: BoxDecoration(
                        color: colors.surface,
                        borderRadius: BorderRadius.circular(18),
                        border: Border.all(color: colors.border),
                      ),
                      child: Column(
                        children: [
                          Container(
                            padding: const EdgeInsets.all(12),
                            decoration: BoxDecoration(
                              color: Colors.white,
                              borderRadius: BorderRadius.circular(14),
                            ),
                            child: QrImageView(
                              data: qrData,
                              version: QrVersions.auto,
                              size: 190,
                              backgroundColor: Colors.white,
                            ),
                          ),
                          const SizedBox(height: 14),
                          Row(
                            mainAxisSize: MainAxisSize.min,
                            children: [
                              Icon(
                                Icons.verified_rounded,
                                size: 16,
                                color: colors.verified,
                              ),
                              const SizedBox(width: 6),
                              CustomText(
                                l10n.exitPass,
                                fontSize: 14,
                                fontWeight: FontWeight.w700,
                                color: colors.verified,
                                textAlign: TextAlign.center,
                              ),
                            ],
                          ),
                          const SizedBox(height: 6),
                          CustomText(
                            l10n.receiptHint,
                            fontSize: 12,
                            color: colors.textSecondary,
                            textAlign: TextAlign.center,
                          ),
                          const SizedBox(height: 12),
                          Container(
                            padding: const EdgeInsets.symmetric(
                              horizontal: 12,
                              vertical: 8,
                            ),
                            decoration: BoxDecoration(
                              color: colors.surfaceAlt,
                              borderRadius: BorderRadius.circular(10),
                              border: Border.all(color: colors.border),
                            ),
                            child: Row(
                              mainAxisSize: MainAxisSize.min,
                              children: [
                                CustomText(
                                  '${l10n.receiptHashLabel}: ',
                                  fontSize: 11,
                                  color: colors.textSecondary,
                                  fontWeight: FontWeight.w600,
                                ),
                                Flexible(
                                  child: CustomText(
                                    widget.receiptHash.isEmpty
                                        ? widget.sessionId
                                        : widget.receiptHash,
                                    fontSize: 11,
                                    fontWeight: FontWeight.w700,
                                    maxLines: 1,
                                  ),
                                ),
                              ],
                            ),
                          ),
                        ],
                      ),
                    ),
                  ),
                  const SizedBox(height: 16),
                  CustomText(
                    l10n.autoResetHint(_secondsLeft),
                    fontSize: 12,
                    color: colors.textSecondary,
                    textAlign: TextAlign.center,
                  ),
                  const SizedBox(height: 8),
                  _CountdownBar(
                    secondsLeft: _secondsLeft,
                    totalSeconds: _autoResetSeconds,
                    paused: _isPaused,
                  ),
                  const SizedBox(height: 14),
                  Row(
                    children: [
                      Expanded(
                        child: KioskButton(
                          text: _isPaused ? l10n.resumeTimer : l10n.pauseTimer,
                          icon: _isPaused
                              ? Icons.play_arrow_rounded
                              : Icons.pause_circle_outline_rounded,
                          variant: KioskButtonVariant.secondary,
                          onPressed: () =>
                              setState(() => _isPaused = !_isPaused),
                        ),
                      ),
                      const SizedBox(width: 12),
                      Expanded(
                        flex: 2,
                        child: KioskButton(
                          text: l10n.doneAndExit,
                          icon: Icons.home_rounded,
                          onPressed: _goIdle,
                        ),
                      ),
                    ],
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

/// One-shot success moment: the check badge scales in with a soft overshoot
/// while an emerald ring expands and fades — the payment flow's finale.
class _SuccessBurst extends StatelessWidget {
  const _SuccessBurst();

  @override
  Widget build(BuildContext context) {
    final colors = context.colors;
    return TweenAnimationBuilder<double>(
      tween: Tween(begin: 0, end: 1),
      duration: const Duration(milliseconds: 700),
      curve: Curves.easeOutBack,
      builder: (context, t, child) {
        return Stack(
          alignment: Alignment.center,
          children: [
            // Expanding halo ring.
            Opacity(
              opacity: (1 - t).clamp(0.0, 1.0),
              child: Container(
                width: 70 + t * 60,
                height: 70 + t * 60,
                decoration: BoxDecoration(
                  shape: BoxShape.circle,
                  border: Border.all(
                    color: colors.verified.withValues(alpha: 0.5),
                    width: 2,
                  ),
                ),
              ),
            ),
            Transform.scale(
              scale: t.clamp(0.0, 1.0),
              child: child,
            ),
          ],
        );
      },
      child: RepaintBoundary(
        child: Container(
          width: 74,
          height: 74,
          decoration: BoxDecoration(
            color: colors.verified.withValues(alpha: 0.14),
            shape: BoxShape.circle,
            border: Border.all(color: colors.verified.withValues(alpha: 0.4)),
          ),
          child: Icon(
            Icons.check_circle_rounded,
            size: 44,
            color: colors.verified,
          ),
        ),
      ),
    );
  }
}

/// Smoothly draining countdown bar — one linear second of animation per tick.
class _CountdownBar extends StatelessWidget {
  const _CountdownBar({
    required this.secondsLeft,
    required this.totalSeconds,
    required this.paused,
  });

  final int secondsLeft;
  final int totalSeconds;
  final bool paused;

  @override
  Widget build(BuildContext context) {
    final colors = context.colors;
    final fraction = (secondsLeft / totalSeconds).clamp(0.0, 1.0);
    return TweenAnimationBuilder<double>(
      // Keyed on the tick only: pausing must hold the bar where it is, not
      // replay a fill.
      key: ValueKey('countdown-$secondsLeft'),
      tween: Tween(begin: 1, end: fraction),
      duration: const Duration(seconds: 1),
      curve: Curves.linear,
      builder: (context, value, _) {
        return Container(
          height: 6,
          decoration: BoxDecoration(
            color: colors.surfaceAlt,
            borderRadius: BorderRadius.circular(3),
          ),
          child: Align(
            alignment: AlignmentDirectional.centerStart,
            child: FractionallySizedBox(
              widthFactor: value.clamp(0.0, 1.0),
              child: Container(
                decoration: BoxDecoration(
                  color: paused ? colors.textFaint : colors.brand,
                  borderRadius: BorderRadius.circular(3),
                ),
              ),
            ),
          ),
        );
      },
    );
  }
}
