import 'package:equatable/equatable.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

class LocaleState extends Equatable {
  const LocaleState(this.locale);
  final Locale locale;

  bool get isArabic => locale.languageCode == 'ar';

  @override
  List<Object?> get props => [locale.languageCode];
}

class LocaleCubit extends Cubit<LocaleState> {
  LocaleCubit([Locale initial = const Locale('ar')]) : super(LocaleState(initial));

  void toggle() {
    final next = state.locale.languageCode == 'ar' ? const Locale('en') : const Locale('ar');
    emit(LocaleState(next));
  }

  void setLocale(Locale locale) {
    if (locale.languageCode == state.locale.languageCode) return;
    emit(LocaleState(locale));
  }
}
