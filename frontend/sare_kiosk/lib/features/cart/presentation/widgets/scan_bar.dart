import 'package:flutter/material.dart';

import '../../../../core/theme/app_colors.dart';
import '../../../../core/widgets/custom_text.dart';
import '../../../../core/widgets/kiosk_button.dart';
import '../../../../core/widgets/kiosk_surface_card.dart';
import '../../../../core/widgets/kiosk_text_field.dart';
import '../../../../l10n/app_localizations.dart';

/// Sleek, single-row barcode & product entry bar at the top of the cart screen.
class ScanBar extends StatefulWidget {
  const ScanBar({
    super.key,
    required this.barcodeController,
    this.weightController,
    required this.onAdd,
    required this.isAdding,
    this.onBarcodeChanged,
  });

  final TextEditingController barcodeController;
  final TextEditingController? weightController;
  final Future<void> Function() onAdd;
  final bool isAdding;
  final ValueChanged<String>? onBarcodeChanged;

  @override
  State<ScanBar> createState() => _ScanBarState();
}

class _ScanBarState extends State<ScanBar> {
  final _barcodeFocus = FocusNode();

  @override
  void dispose() {
    _barcodeFocus.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;
    final colors = context.colors;

    return KioskSurfaceCard(
      padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
      borderRadius: 16,
      child: Row(
        children: [
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
            decoration: BoxDecoration(
              color: colors.brand.withValues(alpha: 0.10),
              borderRadius: BorderRadius.circular(12),
              border: Border.all(
                color: colors.brand.withValues(alpha: 0.22),
              ),
            ),
            child: Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                Icon(
                  Icons.qr_code_scanner_rounded,
                  size: 18,
                  color: colors.brand,
                ),
                const SizedBox(width: 8),
                CustomText(
                  l10n.scanPrompt,
                  fontSize: 12.5,
                  fontWeight: FontWeight.w700,
                  color: colors.brand,
                ),
              ],
            ),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: KioskTextField(
              controller: widget.barcodeController,
              focusNode: _barcodeFocus,
              height: 46,
              hintText: l10n.barcodeHint,
              prefixIcon: Icon(
                Icons.search_rounded,
                size: 19,
                color: colors.textSecondary,
              ),
              textInputAction: TextInputAction.done,
              onSubmitted: (_) => widget.onAdd(),
              onChanged: widget.onBarcodeChanged,
              showClearButton: true,
            ),
          ),
          const SizedBox(width: 10),
          KioskButton(
            text: l10n.addItem,
            height: 46,
            icon: Icons.add_rounded,
            isLoading: widget.isAdding,
            onPressed: widget.isAdding ? null : () => widget.onAdd(),
          ),
        ],
      ),
    );
  }
}
