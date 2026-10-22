import 'package:flutter/material.dart';

import '../../l10n/app_localizations.dart';
import '../config/kiosk_config_exports.dart';
import '../theme/app_colors.dart';
import 'custom_text.dart';
import 'kiosk_button.dart';

/// Displays a tactile confirmation dialog when a shopper requests store staff
/// assistance on the 10.1" cart touchscreen.
Future<void> showKioskStaffHelpDialog(BuildContext context) {
  final l10n = AppLocalizations.of(context)!;
  final config = context.kioskConfig;
  final colors = context.colors;

  return showDialog<void>(
    context: context,
    builder: (dialogContext) {
      return Dialog(
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
                Container(
                  width: 60,
                  height: 60,
                  decoration: BoxDecoration(
                    color: colors.brandPrimary.withValues(alpha: 0.14),
                    shape: BoxShape.circle,
                  ),
                  child: Icon(
                    Icons.support_agent_rounded,
                    size: 32,
                    color: colors.brandPrimary,
                  ),
                ),
                const SizedBox(height: 16),
                CustomText(
                  l10n.staffModalTitle,
                  fontSize: 20,
                  fontWeight: FontWeight.w800,
                  textAlign: TextAlign.center,
                ),
                const SizedBox(height: 10),
                CustomText(
                  l10n.staffModalMessage(config.cartNumber, config.locationName),
                  fontSize: 14,
                  color: colors.secondaryText,
                  textAlign: TextAlign.center,
                ),
                const SizedBox(height: 22),
                Row(
                  children: [
                    Expanded(
                      child: KioskButton(
                        text: l10n.ok,
                        icon: Icons.check_rounded,
                        onPressed: () => Navigator.of(dialogContext).pop(),
                      ),
                    ),
                  ],
                ),
              ],
            ),
          ),
        ),
      );
    },
  );
}
