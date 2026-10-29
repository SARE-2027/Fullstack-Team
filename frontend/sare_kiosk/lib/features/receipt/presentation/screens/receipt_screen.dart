import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';
import 'package:qr_flutter/qr_flutter.dart';

import '../../../../core/routing/app_router.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../core/utils/kiosk_motion.dart';
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

  String _formatDisplayReference() {
    final raw = widget.receiptHash.isNotEmpty
        ? widget.receiptHash
        : widget.sessionId;
    if (raw.startsWith('RECEIPT_')) {
      final parts = raw.split('_');
      if (parts.length >= 2 && parts[1].isNotEmpty) {
        return '#${parts[1].toUpperCase()}';
      }
    }
    final cleaned = raw.replaceAll('-', '').toUpperCase();
    if (cleaned.length > 8) {
      return '#${cleaned.substring(0, 8)}';
    }
    return '#$cleaned';
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;
    final colors = context.colors;
    final qrData = widget.receiptHash.isNotEmpty
        ? widget.receiptHash
        : 'SARE:${widget.sessionId}';
    final displayRef = _formatDisplayReference();

    return KioskScaffold(
      onToggleLocale: widget.onToggleLocale,
      onToggleTheme: widget.onToggleTheme,
      isDarkMode: widget.isDarkMode,
      body: Center(
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 660),
          child: Padding(
            padding: const EdgeInsets.fromLTRB(24, 10, 24, 18),
            child: SingleChildScrollView(
              child: Column(
                children: [
                  const _SuccessBurst(),
                  const SizedBox(height: 10),
                  CustomText(
                    l10n.paymentSuccess,
                    fontSize: 25,
                    fontWeight: FontWeight.w800,
                    letterSpacing: -0.4,
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
                  KioskPriceText(
                    minor: widget.totalMinor,
                    amountSize: 28,
                    amountColor: colors.verified,
                  ),
                  const SizedBox(height: 14),
                  RepaintBoundary(
                    child: KioskSurfaceCard(
                      padding: const EdgeInsets.all(18),
                      borderRadius: 20,
                      borderColor: colors.verified.withValues(alpha: 0.35),
                      child: Column(
                        children: [
                          _QrViewfinder(
                            cornerColor: colors.brand,
                            accentColor: colors.verified,
                            child: QrImageView(
                              data: qrData,
                              version: QrVersions.auto,
                              size: 168,
                              backgroundColor: Colors.white,
                            ),
                          ),
                          const SizedBox(height: 14),
                          Container(
                            padding: const EdgeInsets.symmetric(
                              horizontal: 12,
                              vertical: 5,
                            ),
                            decoration: BoxDecoration(
                              color: colors.verified.withValues(alpha: 0.14),
                              borderRadius: BorderRadius.circular(999),
                              border: Border.all(
                                color: colors.verified.withValues(alpha: 0.34),
                              ),
                            ),
                            child: Row(
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
                                  fontSize: 13.5,
                                  fontWeight: FontWeight.w700,
                                  color: colors.verified,
                                  textAlign: TextAlign.center,
                                ),
                              ],
                            ),
                          ),
                          const SizedBox(height: 8),
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
                                Icon(
                                  Icons.receipt_long_rounded,
                                  size: 14,
                                  color: colors.brand,
                                ),
                                const SizedBox(width: 6),
                                CustomText(
                                  '${l10n.receiptHashLabel}: ',
                                  fontSize: 11.5,
                                  color: colors.textSecondary,
                                  fontWeight: FontWeight.w600,
                                ),
                                Flexible(
                                  child: CustomText(
                                    displayRef,
                                    fontSize: 11.5,
                                    fontWeight: FontWeight.w800,
                                    tabularFigures: true,
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
                    color: colors.textSecondary,
                    tabularFigures: true,
                    textAlign: TextAlign.center,
                  ),
                  const SizedBox(height: 8),
                  _CountdownBar(
                    secondsLeft: _secondsLeft,
                    totalSeconds: _autoResetSeconds,
                    paused: _isPaused,
                  ),
                  const SizedBox(height: 12),
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

/// Frames the exit-pass QR with precision optical viewfinder brackets and
/// a subtle laser alignment line.
class _QrViewfinder extends StatelessWidget {
  const _QrViewfinder({
    required this.cornerColor,
    required this.accentColor,
    required this.child,
  });

  final Color cornerColor;
  final Color accentColor;
  final Widget child;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(18),
        boxShadow: [
          BoxShadow(
            color: cornerColor.withValues(alpha: 0.18),
            blurRadius: 24,
            offset: const Offset(0, 6),
            spreadRadius: -4,
          ),
        ],
      ),
      child: CustomPaint(
        foregroundPainter: _ViewfinderCornersPainter(
          color: cornerColor,
          accentColor: accentColor,
        ),
        child: child,
      ),
    );
  }
}

class _ViewfinderCornersPainter extends CustomPainter {
  _ViewfinderCornersPainter({
    required this.color,
    required this.accentColor,
  });

  final Color color;
  final Color accentColor;

  static const double _arm = 20;
  static const double _stroke = 3.2;

  @override
  void paint(Canvas canvas, Size size) {
    final paint = Paint()
      ..color = color
      ..style = PaintingStyle.stroke
      ..strokeWidth = _stroke
      ..strokeCap = StrokeCap.round;

    const inset = _stroke / 2 + 1;
    final rect = Rect.fromLTWH(
      inset,
      inset,
      size.width - inset * 2,
      size.height - inset * 2,
    );

    void corner(Path path, Offset p, double dx, double dy) {
      path.moveTo(p.dx + dx * _arm, p.dy);
      path.lineTo(p.dx, p.dy);
      path.lineTo(p.dx, p.dy + dy * _arm);
    }

    final path = Path();
    corner(path, rect.topLeft, 1, 1);
    corner(path, rect.topRight, -1, 1);
    corner(path, rect.bottomRight, -1, -1);
    corner(path, rect.bottomLeft, 1, -1);
    canvas.drawPath(path, paint);

    // Optical center alignment ticks
    final tick = Paint()
      ..color = accentColor.withValues(alpha: 0.55)
      ..strokeWidth = 2.2
      ..strokeCap = StrokeCap.round;
    final midY = rect.center.dy;
    canvas.drawLine(
      Offset(rect.left + 3, midY),
      Offset(rect.left + 12, midY),
      tick,
    );
    canvas.drawLine(
      Offset(rect.right - 12, midY),
      Offset(rect.right - 3, midY),
      tick,
    );
  }

  @override
  bool shouldRepaint(covariant _ViewfinderCornersPainter oldDelegate) =>
      color != oldDelegate.color || accentColor != oldDelegate.accentColor;
}

/// One-shot celebratory moment driven by Apple's momentum spring
/// ([KioskMotion.springMomentum]): the check badge scales in while dual
/// emerald rings expand and fade.
class _SuccessBurst extends StatelessWidget {
  const _SuccessBurst();

  @override
  Widget build(BuildContext context) {
    final colors = context.colors;
    return TweenAnimationBuilder<double>(
      tween: Tween(begin: 0, end: 1),
      duration: const Duration(milliseconds: 680),
      curve: KioskMotion.springMomentum,
      builder: (context, t, child) {
        final clamped = t.clamp(0.0, 1.0);
        return Stack(
          alignment: Alignment.center,
          children: [
            Opacity(
              opacity: (1 - clamped).clamp(0.0, 1.0),
              child: Container(
                width: 68 + clamped * 64,
                height: 68 + clamped * 64,
                decoration: BoxDecoration(
                  shape: BoxShape.circle,
                  border: Border.all(
                    color: colors.verified.withValues(alpha: 0.48),
                    width: 2,
                  ),
                ),
              ),
            ),
            Transform.scale(
              scale: clamped,
              child: child,
            ),
          ],
        );
      },
      child: RepaintBoundary(
        child: Container(
          width: 72,
          height: 72,
          decoration: BoxDecoration(
            gradient: colors.verifiedGradient,
            shape: BoxShape.circle,
            border: Border.all(
              color: colors.verified.withValues(alpha: 0.45),
              width: 1.5,
            ),
          ),
          child: Icon(
            Icons.check_circle_rounded,
            size: 42,
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
                  gradient: paused ? null : colors.brandGradient,
                  color: paused ? colors.warning : null,
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
