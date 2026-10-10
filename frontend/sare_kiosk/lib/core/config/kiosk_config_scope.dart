import 'package:flutter/widgets.dart';

import 'kiosk_config.dart';

/// Provides [KioskConfig] down the widget tree via an [InheritedWidget].
class KioskConfigScope extends InheritedWidget {
  const KioskConfigScope({
    super.key,
    required this.config,
    required super.child,
  });

  final KioskConfig config;

  static KioskConfig of(BuildContext context) {
    final scope =
        context.dependOnInheritedWidgetOfExactType<KioskConfigScope>();
    return scope?.config ?? const KioskConfig();
  }

  @override
  bool updateShouldNotify(KioskConfigScope oldWidget) {
    return config != oldWidget.config;
  }
}

/// Syntactic sugar extension for convenient BuildContext access.
extension KioskConfigContextX on BuildContext {
  KioskConfig get kioskConfig => KioskConfigScope.of(this);
}
