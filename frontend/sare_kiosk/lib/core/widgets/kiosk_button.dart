import 'package:flutter/material.dart';

import '../theme/app_colors.dart';
import 'custom_text.dart';

enum KioskButtonVariant {
  primary,
  secondary,
  outlined,
  danger,
}

/// Large, tactile button optimized for 10.1" supermarket touchscreen kiosks
class KioskButton extends StatelessWidget {
  const KioskButton({
    super.key,
    required this.text,
    required this.onPressed,
    this.icon,
    this.variant = KioskButtonVariant.primary,
    this.isLoading = false,
    this.height = 56.0,
    this.width,
    this.borderRadius = 14.0,
  });

  final String text;
  final VoidCallback? onPressed;
  final IconData? icon;
  final KioskButtonVariant variant;
  final bool isLoading;
  final double height;
  final double? width;
  final double borderRadius;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final colors = context.colors;

    Color backgroundColor;
    Color foregroundColor;
    BorderSide? borderSide;

    switch (variant) {
      case KioskButtonVariant.primary:
        backgroundColor = theme.colorScheme.primary;
        foregroundColor = theme.colorScheme.onPrimary;
        break;
      case KioskButtonVariant.secondary:
        backgroundColor = colors.textFieldFill;
        foregroundColor = colors.secondaryText;
        borderSide = BorderSide(color: colors.containerBorder);
        break;
      case KioskButtonVariant.outlined:
        backgroundColor = Colors.transparent;
        foregroundColor = theme.colorScheme.primary;
        borderSide = BorderSide(color: colors.containerBorder, width: 1.5);
        break;
      case KioskButtonVariant.danger:
        backgroundColor = colors.weightDiscrepancy;
        foregroundColor = Colors.white;
        break;
    }

    Widget content = Row(
      mainAxisSize: MainAxisSize.min,
      mainAxisAlignment: MainAxisAlignment.center,
      children: [
        if (isLoading) ...[
          SizedBox(
            width: 22,
            height: 22,
            child: CircularProgressIndicator(
              strokeWidth: 2.5,
              valueColor: AlwaysStoppedAnimation<Color>(foregroundColor),
            ),
          ),
          const SizedBox(width: 12),
        ] else if (icon != null) ...[
          Icon(icon, size: 22, color: foregroundColor),
          const SizedBox(width: 10),
        ],
        Flexible(
          child: FittedBox(
            fit: BoxFit.scaleDown,
            child: CustomText(
              text,
              fontSize: 16,
              fontWeight: FontWeight.bold,
              color: foregroundColor,
              maxLines: 1,
            ),
          ),
        ),
      ],
    );

    return SizedBox(
      height: height,
      width: width,
      child: ElevatedButton(
        onPressed: isLoading ? null : onPressed,
        style: ElevatedButton.styleFrom(
          elevation: 0,
          backgroundColor: backgroundColor,
          foregroundColor: foregroundColor,
          disabledBackgroundColor: backgroundColor.withValues(alpha: 0.5),
          padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 12),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(borderRadius),
            side: borderSide ?? BorderSide.none,
          ),
        ),
        child: content,
      ),
    );
  }
}
