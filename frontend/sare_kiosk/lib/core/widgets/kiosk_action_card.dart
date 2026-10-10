import 'package:flutter/material.dart';

import '../theme/app_colors.dart';
import 'custom_text.dart';

/// Reusable action & selection card for the 10.1" kiosk workflow.
/// Used for Welcome options (Guest vs Membership), checkout payment methods,
/// and hardware verification dialogs.
///
/// Features smooth theme interpolation (`AnimatedContainer`) and
/// crossfading text (`AnimatedSwitcher`) on localization changes.
class KioskActionCard extends StatelessWidget {
  const KioskActionCard({
    super.key,
    required this.title,
    required this.description,
    required this.icon,
    required this.actionButton,
    this.isPrimary = false,
    this.iconColor,
    this.iconBackgroundColor,
    this.padding = const EdgeInsets.all(24),
    this.borderRadius = 20.0,
  });

  final String title;
  final String description;
  final IconData icon;
  final Widget actionButton;
  final bool isPrimary;
  final Color? iconColor;
  final Color? iconBackgroundColor;
  final EdgeInsetsGeometry padding;
  final double borderRadius;

  @override
  Widget build(BuildContext context) {
    final colors = context.colors;
    final theme = Theme.of(context);

    final resolvedBorderColor = isPrimary
        ? theme.colorScheme.primary
        : colors.containerBorder;

    final resolvedBorderWidth = isPrimary ? 1.5 : 1.0;

    final resolvedIconColor = iconColor ??
        (isPrimary ? theme.colorScheme.primary : colors.secondaryText);

    final resolvedIconBg = iconBackgroundColor ??
        (isPrimary
            ? theme.colorScheme.primary.withValues(alpha: 0.15)
            : colors.containerBorder.withValues(alpha: 0.2));

    return Container(
      padding: padding,
      decoration: BoxDecoration(
        color: colors.textFieldFill,
        borderRadius: BorderRadius.circular(borderRadius),
        border: Border.all(
          color: resolvedBorderColor,
          width: resolvedBorderWidth,
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
                  color: resolvedIconBg,
                  shape: BoxShape.circle,
                ),
                child: Icon(
                  icon,
                  size: 20,
                  color: resolvedIconColor,
                ),
              ),
              const SizedBox(width: 10),
              Expanded(
                child: AnimatedSwitcher(
                  duration: const Duration(milliseconds: 220),
                  layoutBuilder: (current, previous) => Stack(
                    alignment: AlignmentDirectional.centerStart,
                    children: [...previous, ?current],
                  ),
                  transitionBuilder: (child, animation) => FadeTransition(
                    opacity: animation,
                    child: child,
                  ),
                  child: CustomText(
                    title,
                    key: ValueKey(title),
                    fontSize: 18,
                    fontWeight: FontWeight.bold,
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: 10),
          AnimatedSwitcher(
            duration: const Duration(milliseconds: 220),
            layoutBuilder: (current, previous) => Stack(
              alignment: AlignmentDirectional.topStart,
              children: [...previous, ?current],
            ),
            transitionBuilder: (child, animation) => FadeTransition(
              opacity: animation,
              child: child,
            ),
            child: CustomText(
              description,
              key: ValueKey(description),
              fontSize: 13,
              color: colors.secondaryText,
            ),
          ),
          const Spacer(),
          const SizedBox(height: 18),
          Row(
            children: [
              Expanded(child: actionButton),
            ],
          ),
        ],
      ),
    );
  }
}
