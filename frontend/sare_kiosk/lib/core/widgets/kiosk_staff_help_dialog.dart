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
    barrierColor: colors.scrim,
    builder: (dialogContext) {
      return Dialog(
        backgroundColor: Theme.of(dialogContext).colorScheme.surface,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(24),
          side: BorderSide(color: colors.borderStrong, width: 1.4),
        ),
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 440),
          child: Padding(
            padding: const EdgeInsets.all(28),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                Container(
                  width: 64,
                  height: 64,
                  decoration: BoxDecoration(
                    color: colors.brand.withValues(alpha: 0.12),
                    borderRadius: BorderRadius.circular(20),
                    border: Border.all(
                      color: colors.brand.withValues(alpha: 0.35),
                    ),
                  ),
                  child: Icon(
                    Icons.support_agent_rounded,
                    size: 34,
                    color: colors.brand,
                  ),
                ),
                const SizedBox(height: 18),
                CustomText(
                  l10n.staffModalTitle,
                  fontSize: 20,
                  fontWeight: FontWeight.w700,
                  textAlign: TextAlign.center,
                ),
                const SizedBox(height: 10),
                CustomText(
                  l10n.staffModalMessage(config.cartNumber, config.locationName),
                  fontSize: 14,
                  color: colors.textSecondary,
                  textAlign: TextAlign.center,
                ),
                const SizedBox(height: 24),
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
