import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../l10n/app_localizations.dart';
import '../bloc/locale_cubit.dart';
import '../bloc/theme_cubit.dart';
import '../config/kiosk_config_exports.dart';
import '../theme/app_colors.dart';
import '../theme/app_typography.dart';
import '../utils/kiosk_motion.dart';
import 'kiosk_header_button.dart';
import 'kiosk_pulse_dot.dart';
import 'kiosk_staff_help_dialog.dart';
import 'kiosk_status_badge.dart';

/// Unified Precision Glass Cockpit chrome for every SARE kiosk screen.
///
/// Houses:
/// - The LTR-pinned hardware HUD bar (cart ID badge, live load-cell scale
///   status with breathing dot, active shopper badge, staff assistance, theme
///   and language switches).
/// - A subtle fading horizon hairline separating floating chrome from content.
/// - Spring-animated [floatingBanner] slot with strictly bounded constraints.
class KioskScaffold extends StatelessWidget {
  const KioskScaffold({
    super.key,
    required this.body,
    this.onToggleLocale,
    this.onToggleTheme,
    this.isDarkMode,
    this.showHeader = true,
    this.showStaffHelp = true,
    this.floatingBanner,
    this.extraStatusBadge,
    this.extraActionButton,
  });

  final Widget body;
  final VoidCallback? onToggleLocale;
  final VoidCallback? onToggleTheme;
  final bool? isDarkMode;
  final bool showHeader;
  final bool showStaffHelp;
  final Widget? floatingBanner;
  final Widget? extraStatusBadge;
  final Widget? extraActionButton;

  @override
  Widget build(BuildContext context) {
    final colors = context.colors;
    final l10n = AppLocalizations.of(context)!;
    final config = context.kioskConfig;
    final effectiveIsDark =
        isDarkMode ?? (Theme.of(context).brightness == Brightness.dark);

    final effectiveToggleTheme = onToggleTheme ??
        () {
          try {
            context.read<ThemeCubit>().toggle();
          } catch (_) {}
        };

    final effectiveToggleLocale = onToggleLocale ??
        () {
          try {
            context.read<LocaleCubit>().toggle();
          } catch (_) {}
        };

    final isDark = Theme.of(context).brightness == Brightness.dark;

    return Scaffold(
      body: DecoratedBox(
        decoration: BoxDecoration(
          gradient: LinearGradient(
            begin: Alignment.topCenter,
            end: Alignment.bottomCenter,
            colors: [
              Theme.of(context).scaffoldBackgroundColor,
              isDark ? const Color(0xFF06080D) : const Color(0xFFE8EDF5),
            ],
          ),
        ),
        child: DecoratedBox(
          decoration: BoxDecoration(
            gradient: RadialGradient(
              center: const Alignment(0.0, -1.30),
              radius: 1.20,
              colors: [
                colors.brandGlow.withValues(alpha: isDark ? 0.55 : 0.35),
                colors.brandGlow.withValues(alpha: 0.0),
              ],
            ),
          ),
          child: SafeArea(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                if (showHeader)
                  RepaintBoundary(
                    child: Container(
                      padding: const EdgeInsets.fromLTRB(24, 12, 24, 10),
                      decoration: BoxDecoration(
                        color: colors.surface.withValues(
                          alpha: isDark ? 0.65 : 0.78,
                        ),
                        border: Border(
                          bottom: BorderSide(
                            color: colors.border.withValues(alpha: 0.75),
                          ),
                        ),
                      ),
                      child: Directionality(
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
                                      leading: Row(
                                        mainAxisSize: MainAxisSize.min,
                                        children: [
                                          KioskPulseDot(
                                            color: config.isScaleReady
                                                ? colors.verified
                                                : colors.warning,
                                            size: 7.5,
                                            animate: config.isScaleReady &&
                                                KioskMotion.ambientEnabled(
                                                    context),
                                          ),
                                          const SizedBox(width: 6),
                                          Icon(
                                            Icons.shopping_cart_outlined,
                                            size: 16,
                                            color: colors.brand,
                                          ),
                                        ],
                                      ),
                                      label: l10n.cartNumberLabel(
                                        config.cartNumber,
                                      ),
                                    ),
                                    if (!config.isScaleReady) ...[
                                      const SizedBox(width: 10),
                                      KioskStatusBadge(
                                        leading: Icon(
                                          Icons.qr_code_scanner_rounded,
                                          size: 15,
                                          color: colors.warning,
                                        ),
                                        label: l10n.scaleOfflineLabel,
                                        backgroundColor: colors.warning
                                            .withValues(alpha: 0.12),
                                        borderColor: colors.warning
                                            .withValues(alpha: 0.30),
                                        textColor: colors.warning,
                                      ),
                                    ],
                                    if (extraStatusBadge != null) ...[
                                      const SizedBox(width: 10),
                                      extraStatusBadge!,
                                    ],
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
                                    if (extraActionButton != null) ...[
                                      extraActionButton!,
                                      const SizedBox(width: 10),
                                    ],
                                    if (showStaffHelp) ...[
                                      KioskHeaderButton(
                                        onPressed: () =>
                                            showKioskStaffHelpDialog(context),
                                        icon: Icons.support_agent_rounded,
                                        label: l10n.callStaff,
                                      ),
                                      const SizedBox(width: 10),
                                    ],
                                    KioskHeaderButton(
                                      onPressed: effectiveToggleTheme,
                                      icon: effectiveIsDark
                                          ? Icons.light_mode_outlined
                                          : Icons.dark_mode_outlined,
                                      label: effectiveIsDark
                                          ? l10n.themeLight
                                          : l10n.themeDark,
                                    ),
                                    const SizedBox(width: 10),
                                    KioskHeaderButton(
                                      onPressed: effectiveToggleLocale,
                                      icon: Icons.language,
                                      label: l10n.switchLanguage,
                                    ),
                                  ],
                                ),
                              ),
                            ),
                          ],
                        ),
                      ),
                    ),
                  ),
                Expanded(
                  child: Stack(
                    children: [
                      Positioned.fill(child: body),
                      Align(
                        alignment: Alignment.topCenter,
                        child: AnimatedSwitcher(
                          duration: AppTypography.base,
                          switchInCurve: AppTypography.enterCurve,
                          switchOutCurve: AppTypography.exitCurve,
                          transitionBuilder: (child, animation) =>
                              FadeTransition(
                            opacity: animation,
                            child: SlideTransition(
                              position: Tween<Offset>(
                                begin: const Offset(0, -0.35),
                                end: Offset.zero,
                              ).animate(animation),
                              child: child,
                            ),
                          ),
                          child: floatingBanner == null
                              ? const SizedBox.shrink(
                                  key: ValueKey('kiosk-banner-none'),
                                )
                              : Padding(
                                  key: ValueKey(
                                    'kiosk-banner-${floatingBanner!.key}',
                                  ),
                                  padding:
                                      const EdgeInsets.fromLTRB(24, 10, 24, 0),
                                  child: RepaintBoundary(
                                    child: floatingBanner!,
                                  ),
                                ),
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
