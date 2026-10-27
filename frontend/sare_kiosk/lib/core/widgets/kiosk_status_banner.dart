import 'package:flutter/material.dart';

import '../theme/app_colors.dart';
import 'custom_text.dart';

enum KioskBannerStatus { verified, warning, discrepancy, info }

/// Status banner for cart balance, scale verification, and hardware events.
///
/// The leading icon sits in a tinted rounded chip so the state reads from
/// across the aisle; color always pairs with an icon (never color alone).
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

    final Color primaryColor;
    final IconData defaultIcon;
    switch (status) {
      case KioskBannerStatus.verified:
        primaryColor = colors.verified;
        defaultIcon = Icons.check_circle_rounded;
      case KioskBannerStatus.warning:
        primaryColor = colors.warning;
        defaultIcon = Icons.warning_amber_rounded;
      case KioskBannerStatus.discrepancy:
        primaryColor = colors.danger;
        defaultIcon = Icons.error_outline_rounded;
      case KioskBannerStatus.info:
        primaryColor = colors.info;
        defaultIcon = Icons.info_outline_rounded;
    }

    return RepaintBoundary(
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
        decoration: BoxDecoration(
          color: primaryColor.withValues(alpha: 0.10),
          borderRadius: BorderRadius.circular(14),
          border: Border.all(
            color: primaryColor.withValues(alpha: 0.38),
            width: 1.4,
          ),
        ),
        child: Row(
          children: [
            Container(
              width: 40,
              height: 40,
              decoration: BoxDecoration(
                color: primaryColor.withValues(alpha: 0.14),
                borderRadius: BorderRadius.circular(12),
              ),
              child: Icon(
                icon ?? defaultIcon,
                color: primaryColor,
                size: 22,
              ),
            ),
            const SizedBox(width: 14),
            Expanded(
              child: CustomText(
                message,
                fontSize: 15,
                fontWeight: FontWeight.w600,
                color: primaryColor,
                maxLines: 2,
              ),
            ),
            if (actionLabel != null && onAction != null) ...[
              const SizedBox(width: 12),
              TextButton(
                onPressed: onAction,
                style: TextButton.styleFrom(
                  foregroundColor: primaryColor,
                  backgroundColor: primaryColor.withValues(alpha: 0.12),
                  padding: const EdgeInsets.symmetric(
                    horizontal: 16,
                    vertical: 10,
                  ),
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(10),
                  ),
                ),
                child: CustomText(
                  actionLabel!,
                  fontSize: 13,
                  fontWeight: FontWeight.w700,
                  color: primaryColor,
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }
}
