import 'package:flutter/material.dart';

import '../theme/app_colors.dart';
import '../theme/app_typography.dart';

/// Touchscreen-optimized text field for the 10.1" kiosk.
/// Designed for manual barcode entry, membership lookup, and SKU search.
/// Features high-contrast focus animation (`AnimatedContainer`), generous
/// touch targets (56px default height), and Arabic baseline hardening.
class KioskTextField extends StatefulWidget {
  const KioskTextField({
    super.key,
    this.controller,
    this.focusNode,
    this.hintText,
    this.prefixIcon,
    this.suffixIcon,
    this.onChanged,
    this.onSubmitted,
    this.keyboardType,
    this.textInputAction,
    this.autofocus = false,
    this.enabled = true,
    this.obscureText = false,
    this.height = 56.0,
    this.borderRadius = 14.0,
    this.showClearButton = false,
  });

  final TextEditingController? controller;
  final FocusNode? focusNode;
  final String? hintText;
  final Widget? prefixIcon;
  final Widget? suffixIcon;
  final ValueChanged<String>? onChanged;
  final ValueChanged<String>? onSubmitted;
  final TextInputType? keyboardType;
  final TextInputAction? textInputAction;
  final bool autofocus;
  final bool enabled;
  final bool obscureText;
  final double height;
  final double borderRadius;
  final bool showClearButton;

  @override
  State<KioskTextField> createState() => _KioskTextFieldState();
}

class _KioskTextFieldState extends State<KioskTextField> {
  late final FocusNode _effectiveFocusNode;
  late final TextEditingController _effectiveController;
  bool _hasFocus = false;
  bool _hasText = false;

  @override
  void initState() {
    super.initState();
    _effectiveFocusNode = widget.focusNode ?? FocusNode();
    _effectiveController = widget.controller ?? TextEditingController();

    _effectiveFocusNode.addListener(_handleFocusChanged);
    _effectiveController.addListener(_handleTextChanged);
    _hasText = _effectiveController.text.isNotEmpty;
  }

  @override
  void dispose() {
    _effectiveFocusNode.removeListener(_handleFocusChanged);
    _effectiveController.removeListener(_handleTextChanged);
    if (widget.focusNode == null) _effectiveFocusNode.dispose();
    if (widget.controller == null) _effectiveController.dispose();
    super.dispose();
  }

  void _handleFocusChanged() {
    if (mounted) {
      setState(() => _hasFocus = _effectiveFocusNode.hasFocus);
    }
  }

  void _handleTextChanged() {
    final hasText = _effectiveController.text.isNotEmpty;
    if (hasText != _hasText && mounted) {
      setState(() => _hasText = hasText);
    }
  }

  @override
  Widget build(BuildContext context) {
    final colors = context.colors;
    final theme = Theme.of(context);
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final fontFamily = isArabic
        ? AppTypography.arabicFontFamily
        : AppTypography.englishFontFamily;

    final resolvedBorderColor = _hasFocus
        ? theme.colorScheme.primary
        : colors.containerBorder;

    return AnimatedContainer(
      duration: const Duration(milliseconds: 200),
      curve: Curves.easeOutCubic,
      height: widget.height,
      decoration: BoxDecoration(
        color: colors.textFieldFill,
        borderRadius: BorderRadius.circular(widget.borderRadius),
        border: Border.all(
          color: resolvedBorderColor,
          width: _hasFocus ? 1.8 : 1.2,
        ),
      ),
      child: Center(
        child: TextField(
          controller: _effectiveController,
          focusNode: _effectiveFocusNode,
          autofocus: widget.autofocus,
          enabled: widget.enabled,
          obscureText: widget.obscureText,
          keyboardType: widget.keyboardType,
          textInputAction: widget.textInputAction,
          onChanged: widget.onChanged,
          onSubmitted: widget.onSubmitted,
          style: TextStyle(
            fontFamily: fontFamily,
            fontSize: 16,
            fontWeight: FontWeight.w600,
            color: theme.colorScheme.onSurface,
          ),
          decoration: InputDecoration(
            isDense: true,
            hintText: widget.hintText,
            hintStyle: TextStyle(
              fontFamily: fontFamily,
              fontSize: 15,
              fontWeight: FontWeight.normal,
              color: colors.secondaryText,
            ),
            contentPadding: const EdgeInsets.symmetric(
              horizontal: 16,
              vertical: 14,
            ),
            border: InputBorder.none,
            enabledBorder: InputBorder.none,
            focusedBorder: InputBorder.none,
            prefixIcon: widget.prefixIcon != null
                ? Padding(
                    padding: const EdgeInsetsDirectional.only(start: 12, end: 8),
                    child: widget.prefixIcon,
                  )
                : null,
            prefixIconConstraints: const BoxConstraints(
              minWidth: 40,
              minHeight: 40,
            ),
            suffixIcon: widget.showClearButton && _hasText
                ? IconButton(
                    icon: const Icon(Icons.close_rounded, size: 20),
                    color: colors.secondaryText,
                    onPressed: () {
                      _effectiveController.clear();
                      widget.onChanged?.call('');
                    },
                  )
                : widget.suffixIcon,
            suffixIconConstraints: const BoxConstraints(
              minWidth: 40,
              minHeight: 40,
            ),
          ),
        ),
      ),
    );
  }
}
