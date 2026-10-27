import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../l10n/app_localizations.dart';
import '../bloc/locale_cubit.dart';
import '../bloc/theme_cubit.dart';
import '../config/kiosk_config_exports.dart';
import '../theme/app_colors.dart';
import '../theme/app_typography.dart';
import 'kiosk_header_button.dart';
import 'kiosk_staff_help_dialog.dart';
import 'kiosk_status_badge.dart';

/// Consistent chrome for every kiosk screen: the LTR-pinned header
/// (cart badge, scale status, optional shopper badge, staff assistance,
/// theme + language toggles) and [SafeArea] handling clipped to the 960×600
/// hardware minimum.
///
/// A single static emerald ambient glow anchors all screens at the top —
/// the only background flourish in the product.
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
    final effectiveIsDark = isDarkMode ??
        (Theme.of(context).brightness == Brightness.dark);

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
        // Vertical depth: the screen dims slightly toward the floor so
        // floating panels read as lit from above.
        decoration: BoxDecoration(
          gradient: LinearGradient(
            begin: Alignment.topCenter,
            end: Alignment.bottomCenter,
            colors: [
              Theme.of(context).scaffoldBackgroundColor,
              isDark ? const Color(0xFF0A0D12) : const Color(0xFFEDF0F4),
            ],
          ),
        ),
        child: DecoratedBox(
          decoration: BoxDecoration(
            gradient: RadialGradient(
              center: const Alignment(0.0, -1.35),
              radius: 1.15,
              colors: [
                colors.brandGlow.withValues(alpha: 0.5),
                colors.brandGlow.withValues(alpha: 0.0),
              ],
            ),
          ),
          child: SafeArea(
          child: Column(
            children: [
              if (showHeader)
                RepaintBoundary(
                  child: Padding(
                    padding: const EdgeInsets.fromLTRB(24, 16, 24, 8),
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
                                    leading: Icon(
                                      Icons.shopping_cart_outlined,
                                      size: 18,
                                      color: colors.textSecondary,
                                    ),
                                    label: l10n.cartNumberLabel(config.cartNumber),
                                  ),
                                  const SizedBox(width: 10),
                                  KioskStatusBadge(
                                    leading: Container(
                                      width: 8,
                                      height: 8,
                                      decoration: BoxDecoration(
                                        color: config.isScaleReady
                                            ? colors.verified
                                            : colors.danger,
                                        shape: BoxShape.circle,
                                      ),
                                    ),
                                    label: config.isScaleReady
                                        ? l10n.scaleReadyLabel
                                        : l10n.scaleOfflineLabel,
                                    backgroundColor: (config.isScaleReady
                                            ? colors.verified
                                            : colors.danger)
                                        .withValues(alpha: 0.12),
                                    textColor: config.isScaleReady
                                        ? colors.verified
                                        : colors.danger,
                                  ),
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
              AnimatedSize(
                duration: AppTypography.slow,
                curve: Curves.easeInOutCubic,
                alignment: Alignment.topCenter,
                child: AnimatedSwitcher(
                  duration: AppTypography.base,
                  switchInCurve: AppTypography.enterCurve,
                  switchOutCurve: AppTypography.exitCurve,
                  transitionBuilder: (child, animation) => FadeTransition(
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
                      ? const SizedBox(key: ValueKey('kiosk-banner-none'), width: double.infinity)
                      : Padding(
                          key: ValueKey('kiosk-banner-${floatingBanner!.key}'),
                          padding: const EdgeInsets.fromLTRB(24, 6, 24, 6),
                          child: RepaintBoundary(child: floatingBanner!),
                        ),
                ),
              ),
              Expanded(child: body),
            ],
          ),
        ),
        ),
      ),
    );
  }
}
