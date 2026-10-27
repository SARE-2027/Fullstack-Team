import 'package:flutter/material.dart';

import '../../../../core/theme/app_colors.dart';
import '../../../../core/widgets/custom_text.dart';
import '../../../../core/widgets/kiosk_button.dart';
import '../../../../core/widgets/kiosk_section_label.dart';
import '../../../../core/widgets/kiosk_surface_card.dart';
import '../../../../core/widgets/kiosk_text_field.dart';
import '../../../../l10n/app_localizations.dart';

/// Manual scan & weight entry bar. In production the HID scanner feeds the
/// cart screen directly; this bar remains for fallback entry and demos.
class ScanBar extends StatefulWidget {
  const ScanBar({
    super.key,
    required this.barcodeController,
    required this.weightController,
    required this.onAdd,
    required this.isAdding,
    this.onBarcodeChanged,
  });

  final TextEditingController barcodeController;
  final TextEditingController weightController;
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
      padding: const EdgeInsets.all(16),
      borderRadius: 18,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          KioskSectionLabel(
            l10n.scanPrompt,
            accent: colors.brand,
          ),
          const SizedBox(height: 12),
          Row(
            children: [
              Expanded(
                flex: 3,
                child: KioskTextField(
                  controller: widget.barcodeController,
                  focusNode: _barcodeFocus,
                  hintText: l10n.barcodeHint,
                  prefixIcon: Icon(
                    Icons.qr_code_2_rounded,
                    size: 20,
                    color: colors.textFaint,
                  ),
                  textInputAction: TextInputAction.done,
                  onSubmitted: (_) => widget.onAdd(),
                  onChanged: widget.onBarcodeChanged,
                  showClearButton: true,
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                flex: 2,
                child: KioskTextField(
                  controller: widget.weightController,
                  hintText: l10n.weightGramsHint,
                  prefixIcon: Icon(
                    Icons.scale_rounded,
                    size: 20,
                    color: colors.textFaint,
                  ),
                  keyboardType: TextInputType.number,
                  textInputAction: TextInputAction.done,
                  onSubmitted: (_) => widget.onAdd(),
                ),
              ),
              const SizedBox(width: 12),
              KioskButton(
                text: l10n.addItem,
                icon: Icons.add_rounded,
                isLoading: widget.isAdding,
                onPressed: widget.isAdding ? null : () => widget.onAdd(),
              ),
            ],
          ),
          const SizedBox(height: 10),
          Row(
            children: [
              Icon(
                Icons.info_outline_rounded,
                size: 13,
                color: colors.textFaint,
              ),
              const SizedBox(width: 6),
              Expanded(
                child: CustomText(
                  l10n.verifyWeightHint,
                  fontSize: 12,
                  color: colors.textFaint,
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}
