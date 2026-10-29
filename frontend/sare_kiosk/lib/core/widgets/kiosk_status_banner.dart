import 'package:flutter/material.dart';

import '../theme/app_colors.dart';
import 'custom_text.dart';

enum KioskBannerStatus { verified, warning, discrepancy, info }

/// Precision sensor-fusion status banner for load-cell verification, undo
/// windows, and discrepancy alerts.
///
/// Combines a glowing status squircle, high-contrast message typography, and
/// a tactile action pill (never relying on color alone).
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
        defaultIcon = Icons.gpp_bad_rounded;
      case KioskBannerStatus.info:
        primaryColor = colors.info;
        defaultIcon = Icons.sensors_rounded;
    }

    return RepaintBoundary(
      child: Container(
        constraints: const BoxConstraints(maxWidth: 640),
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
        decoration: BoxDecoration(
          color: colors.surfaceRaised,
          borderRadius: BorderRadius.circular(999),
          border: Border.all(
            color: primaryColor.withValues(alpha: 0.50),
            width: 1.4,
          ),
          boxShadow: [
            BoxShadow(
              color: Colors.black.withValues(alpha: 0.22),
              blurRadius: 24,
              offset: const Offset(0, 8),
              spreadRadius: -4,
            ),
            BoxShadow(
              color: primaryColor.withValues(alpha: 0.16),
              blurRadius: 16,
              offset: const Offset(0, 2),
              spreadRadius: -2,
            ),
          ],
        ),
        child: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Container(
              width: 34,
              height: 34,
              decoration: BoxDecoration(
                color: primaryColor.withValues(alpha: 0.16),
                shape: BoxShape.circle,
              ),
              child: Icon(
                icon ?? defaultIcon,
                color: primaryColor,
                size: 19,
              ),
            ),
            const SizedBox(width: 12),
            Flexible(
              child: CustomText(
                message,
                fontSize: 13.5,
                fontWeight: FontWeight.w700,
                maxLines: 2,
              ),
            ),
            if (actionLabel != null && onAction != null) ...[
              const SizedBox(width: 12),
              TextButton(
                onPressed: onAction,
                style: TextButton.styleFrom(
                  foregroundColor: primaryColor,
                  backgroundColor: primaryColor.withValues(alpha: 0.14),
                  minimumSize: const Size(56, 34),
                  padding: const EdgeInsets.symmetric(
                    horizontal: 14,
                    vertical: 6,
                  ),
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(999),
                    side: BorderSide(
                      color: primaryColor.withValues(alpha: 0.30),
                    ),
                  ),
                ),
                child: CustomText(
                  actionLabel!,
                  fontSize: 12.5,
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
