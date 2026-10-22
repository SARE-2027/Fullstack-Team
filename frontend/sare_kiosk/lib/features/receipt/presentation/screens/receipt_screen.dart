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
                  RepaintBoundary(
                    child: Container(
                      padding: const EdgeInsets.all(14),
                      decoration: BoxDecoration(
                        color: colors.weightVerified.withValues(alpha: 0.12),
                        shape: BoxShape.circle,
                      ),
                      child: Icon(
                        Icons.check_circle_rounded,
                        size: 40,
                        color: colors.weightVerified,
                      ),
                    ),
                  ),
                  const SizedBox(height: 12),
                  CustomText(
                    l10n.paymentSuccess,
                    fontSize: 22,
                    fontWeight: FontWeight.w900,
                    textAlign: TextAlign.center,
                  ),
                  const SizedBox(height: 4),
                  CustomText(
                    l10n.paymentSuccessSubtitle,
                    fontSize: 14,
                    color: colors.secondaryText,
                    textAlign: TextAlign.center,
                  ),
                  const SizedBox(height: 8),
                  CustomText(
                    totalLabel,
                    fontSize: 22,
                    fontWeight: FontWeight.w900,
                    color: colors.weightVerified,
                    textAlign: TextAlign.center,
                  ),
                  const SizedBox(height: 16),
                  RepaintBoundary(
                    child: Container(
                      padding: const EdgeInsets.all(18),
                      decoration: BoxDecoration(
                        color: colors.textFieldFill,
                        borderRadius: BorderRadius.circular(16),
                        border: Border.all(color: colors.containerBorder),
                      ),
                      child: Column(
                        children: [
                          Container(
                            padding: const EdgeInsets.all(12),
                            decoration: BoxDecoration(
                              color: Colors.white,
                              borderRadius: BorderRadius.circular(12),
                            ),
                            child: QrImageView(
                              data: qrData,
                              version: QrVersions.auto,
                              size: 185,
                              backgroundColor: Colors.white,
                            ),
                          ),
                          const SizedBox(height: 12),
                          CustomText(
                            l10n.exitPass,
                            fontSize: 13,
                            fontWeight: FontWeight.bold,
                            color: colors.weightVerified,
                            textAlign: TextAlign.center,
                          ),
                          const SizedBox(height: 6),
                          CustomText(
                            l10n.receiptHint,
                            fontSize: 12,
                            color: colors.secondaryText,
                            textAlign: TextAlign.center,
                          ),
                          const SizedBox(height: 10),
                          Container(
                            padding: const EdgeInsets.symmetric(
                              horizontal: 12,
                              vertical: 8,
                            ),
                            decoration: BoxDecoration(
                              color: Theme.of(context).colorScheme.surface,
                              borderRadius: BorderRadius.circular(10),
                              border: Border.all(
                                color: colors.containerBorder.withValues(alpha: 0.7),
                              ),
                            ),
                            child: Row(
                              mainAxisSize: MainAxisSize.min,
                              children: [
                                CustomText(
                                  '${l10n.receiptHashLabel}: ',
                                  fontSize: 11,
                                  color: colors.secondaryText,
                                  fontWeight: FontWeight.w600,
                                ),
                                Flexible(
                                  child: CustomText(
                                    widget.receiptHash.isEmpty
                                        ? widget.sessionId
                                        : widget.receiptHash,
                                    fontSize: 11,
                                    fontWeight: FontWeight.bold,
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
                  const SizedBox(height: 14),
                  CustomText(
                    l10n.autoResetHint(_secondsLeft),
                    fontSize: 12,
                    color: colors.secondaryText,
                    textAlign: TextAlign.center,
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
                          variant: KioskButtonVariant.outlined,
                          onPressed: () => setState(() => _isPaused = !_isPaused),
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
