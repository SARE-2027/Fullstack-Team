import 'package:flutter/material.dart';

import '../../../../core/theme/app_colors.dart';
import '../../../../core/widgets/custom_text.dart';
import '../../../../core/widgets/kiosk_button.dart';
import '../../../../l10n/app_localizations.dart';

/// The starting screen on the 10.1" smart shopping cart touchscreen.
/// Allows shoppers to begin instantly as a Guest or tap their loyalty card.
class WelcomeCheckInScreen extends StatelessWidget {
  const WelcomeCheckInScreen({
    super.key,
    required this.onToggleLocale,
  });

  final VoidCallback onToggleLocale;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;
    final colors = context.colors;
    final size = MediaQuery.sizeOf(context);

    return Scaffold(
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 32, vertical: 24),
          child: Column(
            children: [
              // ---------------------------------------------------------------
              // Top Header: Cart Info, Hardware Status & Language Switcher
              // ---------------------------------------------------------------
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Row(
                    children: [
                      Container(
                        padding: const EdgeInsets.symmetric(
                          horizontal: 14,
                          vertical: 8,
                        ),
                        decoration: BoxDecoration(
                          color: colors.textFieldFill,
                          borderRadius: BorderRadius.circular(10),
                          border: Border.all(color: colors.containerBorder),
                        ),
                        child: Row(
                          children: [
                            Icon(
                              Icons.shopping_cart_outlined,
                              size: 18,
                              color: colors.secondaryText,
                            ),
                            const SizedBox(width: 8),
                            CustomText(
                              l10n.cartNumberLabel('01'),
                              fontSize: 13,
                              fontWeight: FontWeight.bold,
                              color: colors.secondaryText,
                            ),
                          ],
                        ),
                      ),
                      const SizedBox(width: 12),
                      Container(
                        padding: const EdgeInsets.symmetric(
                          horizontal: 12,
                          vertical: 8,
                        ),
                        decoration: BoxDecoration(
                          color: colors.weightVerified.withValues(alpha: 0.12),
                          borderRadius: BorderRadius.circular(10),
                        ),
                        child: Row(
                          children: [
                            Container(
                              width: 8,
                              height: 8,
                              decoration: BoxDecoration(
                                color: colors.weightVerified,
                                shape: BoxShape.circle,
                              ),
                            ),
                            const SizedBox(width: 8),
                            CustomText(
                              'Scale Ready',
                              fontSize: 12,
                              fontWeight: FontWeight.w600,
                              color: colors.weightVerified,
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),

                  // Language Switcher Button
                  OutlinedButton.icon(
                    onPressed: onToggleLocale,
                    icon: const Icon(Icons.language, size: 18),
                    label: CustomText(
                      l10n.switchLanguage,
                      fontSize: 13,
                      fontWeight: FontWeight.w600,
                    ),
                    style: OutlinedButton.styleFrom(
                      padding: const EdgeInsets.symmetric(
                        horizontal: 16,
                        vertical: 10,
                      ),
                      minimumSize: const Size(0, 40),
                      side: BorderSide(color: colors.containerBorder),
                    ),
                  ),
                ],
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
              CustomText(
                l10n.appName,
                fontSize: 32,
                fontWeight: FontWeight.w900,
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: 8),
              CustomText(
                l10n.welcomeSubtitle,
                fontSize: 16,
                color: colors.secondaryText,
                textAlign: TextAlign.center,
              ),

              const SizedBox(height: 36),

              // ---------------------------------------------------------------
              // Two Action Paths: Guest Shopper (Primary) vs Loyalty (NFC/QR)
              // ---------------------------------------------------------------
              ConstrainedBox(
                constraints: BoxConstraints(
                  maxWidth: size.width * 0.75 > 650 ? 650 : size.width * 0.85,
                ),
                child: Row(
                  children: [
                    // Option 1: Guest Shopper Primary CTA
                    Expanded(
                      child: Container(
                        padding: const EdgeInsets.all(24),
                        decoration: BoxDecoration(
                          color: colors.textFieldFill,
                          borderRadius: BorderRadius.circular(20),
                          border: Border.all(
                            color: Theme.of(context).colorScheme.primary,
                            width: 1.5,
                          ),
                        ),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Row(
                              children: [
                                Container(
                                  padding: const EdgeInsets.all(8),
                                  decoration: BoxDecoration(
                                    color: Theme.of(context)
                                        .colorScheme
                                        .primary
                                        .withValues(alpha: 0.15),
                                    shape: BoxShape.circle,
                                  ),
                                  child: Icon(
                                    Icons.bolt_rounded,
                                    size: 20,
                                    color: Theme.of(context).colorScheme.primary,
                                  ),
                                ),
                                const SizedBox(width: 10),
                                Expanded(
                                  child: CustomText(
                                    l10n.startAsGuest,
                                    fontSize: 18,
                                    fontWeight: FontWeight.bold,
                                  ),
                                ),
                              ],
                            ),
                            const SizedBox(height: 10),
                            CustomText(
                              l10n.startAsGuestDescription,
                              fontSize: 13,
                              color: colors.secondaryText,
                            ),
                            const SizedBox(height: 20),
                            KioskButton(
                              text: l10n.startAsGuest,
                              height: 52,
                              icon: Icons.arrow_forward_rounded,
                              onPressed: () {
                                // Will navigate to Cart screen in next step
                              },
                            ),
                          ],
                        ),
                      ),
                    ),

                    const SizedBox(width: 20),

                    // Option 2: Loyalty Shopper Secondary NFC/QR Card
                    Expanded(
                      child: Container(
                        padding: const EdgeInsets.all(24),
                        decoration: BoxDecoration(
                          color: colors.textFieldFill,
                          borderRadius: BorderRadius.circular(20),
                          border: Border.all(color: colors.containerBorder),
                        ),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Row(
                              children: [
                                Container(
                                  padding: const EdgeInsets.all(8),
                                  decoration: BoxDecoration(
                                    color: colors.containerBorder
                                        .withValues(alpha: 0.2),
                                    shape: BoxShape.circle,
                                  ),
                                  child: Icon(
                                    Icons.nfc_rounded,
                                    size: 20,
                                    color: colors.secondaryText,
                                  ),
                                ),
                                const SizedBox(width: 10),
                                Expanded(
                                  child: CustomText(
                                    l10n.loyaltySignIn,
                                    fontSize: 18,
                                    fontWeight: FontWeight.bold,
                                  ),
                                ),
                              ],
                            ),
                            const SizedBox(height: 10),
                            CustomText(
                              l10n.tapCardOrQr,
                              fontSize: 13,
                              color: colors.secondaryText,
                            ),
                            const SizedBox(height: 20),
                            KioskButton(
                              text: l10n.loyaltySignIn,
                              height: 52,
                              variant: KioskButtonVariant.outlined,
                              icon: Icons.qr_code_scanner_rounded,
                              onPressed: () {
                                // Loyalty check-in trigger
                              },
                            ),
                          ],
                        ),
                      ),
                    ),
                  ],
                ),
              ),

              const Spacer(),
            ],
          ),
        ),
      ),
    );
  }
}
