import 'package:equatable/equatable.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../utils/window_theme_helper.dart';

class ThemeState extends Equatable {
  const ThemeState(this.mode);
  final ThemeMode mode;

  bool get isDark => mode == ThemeMode.dark;

  @override
  List<Object?> get props => [mode];
}

class ThemeCubit extends Cubit<ThemeState> {
  /// Dark is the kiosk's showroom identity: neutral charcoal surfaces with a
  /// royal-blue accent keep the verification signals crisp in store lighting.
  ThemeCubit([ThemeMode initial = ThemeMode.dark]) : super(ThemeState(initial));

  void toggle() {
    final next = state.mode == ThemeMode.light ? ThemeMode.dark : ThemeMode.light;
    emit(ThemeState(next));
    // Fire-and-forget native title bar sync; never block emit.
    WindowThemeHelper.updateTitleBarTheme(isDark: next == ThemeMode.dark);
  }

  void setMode(ThemeMode mode) {
    if (mode == state.mode) return;
    emit(ThemeState(mode));
    WindowThemeHelper.updateTitleBarTheme(isDark: mode == ThemeMode.dark);
  }
}
