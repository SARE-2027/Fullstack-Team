import 'package:flutter/material.dart';

import '../../../../core/config/kiosk_config_exports.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../core/widgets/kiosk_widgets.dart';
import '../../../../l10n/app_localizations.dart';

/// The starting screen on the 10.1" smart shopping cart touchscreen.
/// Allows shoppers to begin instantly as a Guest or tap their membership card.
class WelcomeCheckInScreen extends StatelessWidget {
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
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;
    final colors = context.colors;
    final config = context.kioskConfig;

    return Scaffold(
      body: SafeArea(
        child: LayoutBuilder(
          builder: (context, constraints) {
            return SingleChildScrollView(
              physics: const ClampingScrollPhysics(),
              child: ConstrainedBox(
                constraints: BoxConstraints(
                  minHeight: constraints.maxHeight,
                ),
                child: IntrinsicHeight(
                  child: Padding(
                    padding: const EdgeInsets.symmetric(
                      horizontal: 32,
                      vertical: 24,
                    ),
                    child: Column(
                      children: [
              // ---------------------------------------------------------------
              // Top Header: Pinned Spatially (LTR) so Controls Never Jump Across Screen
              // ---------------------------------------------------------------
              Directionality(
                textDirection: TextDirection.ltr,
                child: Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    // Cart Info + Scale Status (Fixed on the Left)
                    Row(
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
                              ? 'Scale Ready'
                              : 'Scale Offline',
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

                    // Actions: Theme Switcher + Language Switcher (Fixed on the Right)
                    Row(
                      children: [
                        // Theme Switcher Button with smooth morphing & tactile scale
                        KioskHeaderButton(
                          onPressed: onToggleTheme,
                          icon: isDarkMode
                              ? Icons.light_mode_outlined
                              : Icons.dark_mode_outlined,
                          label: isDarkMode ? l10n.themeLight : l10n.themeDark,
                          borderColor: colors.containerBorder,
                          backgroundColor: colors.textFieldFill,
                        ),
                        const SizedBox(width: 10),

                        // Language Switcher Button with smooth morphing & tactile scale
                        KioskHeaderButton(
                          onPressed: onToggleLocale,
                          icon: Icons.language,
                          label: l10n.switchLanguage,
                          borderColor: colors.containerBorder,
                          backgroundColor: colors.textFieldFill,
                        ),
                      ],
                    ),
                  ],
                ),
              ),

              const Spacer(),

              // ---------------------------------------------------------------
              // Center Hero Area: Brand Identity & Welcome Message
              // ---------------------------------------------------------------
              Container(
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
              const SizedBox(height: 18),
              AnimatedSwitcher(
                duration: const Duration(milliseconds: 220),
                switchInCurve: Curves.easeOutCubic,
                switchOutCurve: Curves.easeInCubic,
                transitionBuilder: (child, animation) => FadeTransition(
                  opacity: animation,
                  child: child,
                ),
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
                transitionBuilder: (child, animation) => FadeTransition(
                  opacity: animation,
                  child: child,
                ),
                child: CustomText(
                  l10n.welcomeSubtitle,
                  key: ValueKey(l10n.welcomeSubtitle),
                  fontSize: 16,
                  color: colors.secondaryText,
                  textAlign: TextAlign.center,
                ),
              ),

              const SizedBox(height: 36),

              // ---------------------------------------------------------------
              // Two Action Paths: Guest Shopper (Primary) vs Membership (NFC)
              // ---------------------------------------------------------------
              ConstrainedBox(
                constraints: BoxConstraints(
                  maxWidth:
                      (constraints.maxWidth * 0.72).clamp(720.0, 840.0),
                ),
                child: IntrinsicHeight(
                  child: Row(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      // Option 1: Guest Shopper Primary CTA
                      Expanded(
                        child: KioskActionCard(
                          title: l10n.startAsGuest,
                          description: l10n.startAsGuestDescription,
                          icon: Icons.bolt_rounded,
                          isPrimary: true,
                          actionButton: KioskButton(
                            text: l10n.startAsGuest,
                            height: 52,
                            icon: Icons.arrow_forward_rounded,
                            onPressed: () {
                              // Will navigate to Cart screen in next step
                            },
                          ),
                        ),
                      ),

                      const SizedBox(width: 24),

                      // Option 2: Membership Shopper Secondary NFC Card
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
                            onPressed: () {
                              // Loyalty check-in trigger
                            },
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
              ),

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
  }
}
