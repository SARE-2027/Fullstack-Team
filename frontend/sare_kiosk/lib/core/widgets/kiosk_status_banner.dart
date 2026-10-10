import 'package:flutter/material.dart';

import '../theme/app_colors.dart';
import 'custom_text.dart';

enum KioskBannerStatus {
  verified,
  warning,
  discrepancy,
  info,
}

/// Floating status banner for cart balance, scale verification, and hardware events
class KioskStatusBanner extends StatelessWidget {
  const KioskStatusBanner({
    super.key,
    required this.message,
    this.status = KioskBannerStatus.verified,
    this.icon,
    this.actionLabel,
    this.onAction,
  });

  final String message;
  final KioskBannerStatus status;
  final IconData? icon;
  final String? actionLabel;
  final VoidCallback? onAction;

  @override
  Widget build(BuildContext context) {
    final colors = context.colors;

    Color primaryColor;
    Color backgroundColor;
    IconData defaultIcon;

    switch (status) {
      case KioskBannerStatus.verified:
        primaryColor = colors.weightVerified;
        backgroundColor = colors.weightVerified.withValues(alpha: 0.12);
        defaultIcon = Icons.check_circle_rounded;
        break;
      case KioskBannerStatus.warning:
        primaryColor = colors.weightWarning;
        backgroundColor = colors.weightWarning.withValues(alpha: 0.12);
        defaultIcon = Icons.warning_amber_rounded;
        break;
      case KioskBannerStatus.discrepancy:
        primaryColor = colors.weightDiscrepancy;
        backgroundColor = colors.weightDiscrepancy.withValues(alpha: 0.15);
        defaultIcon = Icons.error_outline_rounded;
        break;
      case KioskBannerStatus.info:
        primaryColor = colors.brandPrimary;
        backgroundColor = colors.brandPrimary.withValues(alpha: 0.12);
        defaultIcon = Icons.info_outline_rounded;
        break;
    }

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 18, vertical: 14),
      decoration: BoxDecoration(
        color: backgroundColor,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(
          color: primaryColor.withValues(alpha: 0.4),
          width: 1.5,
        ),
      ),
      child: Row(
        children: [
          Icon(
            icon ?? defaultIcon,
            color: primaryColor,
            size: 26,
          ),
          const SizedBox(width: 14),
          Expanded(
            child: CustomText(
              message,
              fontSize: 15,
              fontWeight: FontWeight.w600,
              color: primaryColor,
            ),
          ),
          if (actionLabel != null && onAction != null) ...[
            const SizedBox(width: 12),
            TextButton(
              onPressed: onAction,
              style: TextButton.styleFrom(
                foregroundColor: primaryColor,
                padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 8),
              ),
              child: CustomText(
                actionLabel!,
                fontSize: 14,
                fontWeight: FontWeight.bold,
                color: primaryColor,
              ),
            ),
          ],
        ],
      ),
    );
  }
}
