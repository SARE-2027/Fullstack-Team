import 'package:flutter/foundation.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Global observer — logs state transitions in debug, silent in release.
/// No sensitive payloads are printed; only cubit type + runtimeType of states.
class AppBlocObserver extends BlocObserver {
  @override
  void onChange(BlocBase<dynamic> bloc, Change<dynamic> change) {
    super.onChange(bloc, change);
    if (kDebugMode) {
      debugPrint('[Bloc] ${bloc.runtimeType}: ${change.currentState.runtimeType} → ${change.nextState.runtimeType}');
    }
  }

  @override
  void onError(BlocBase<dynamic> bloc, Object error, StackTrace stackTrace) {
    if (kDebugMode) {
      debugPrint('[Bloc:Error] ${bloc.runtimeType}: $error');
    }
    super.onError(bloc, error, stackTrace);
  }
}
