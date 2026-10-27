import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

import '../../core/bloc/locale_cubit.dart';
import '../../core/bloc/theme_cubit.dart';
import '../../core/theme/app_typography.dart';
import '../../features/cart/presentation/screens/cart_screen.dart';
import '../../features/check_in/presentation/screens/welcome_check_in_screen.dart';
import '../../features/checkout/presentation/screens/checkout_screen.dart';
import '../../features/receipt/presentation/screens/receipt_screen.dart';
import '../../features/session/presentation/cubit/session_cubit.dart';

abstract final class AppRoutes {
  static const welcome = '/';
  static const cart = '/cart';
  static const checkout = '/checkout';
  static const receipt = '/receipt';
}

/// Builds the single application [GoRouter] instance.
///
/// Must be created once in `initState` and disposed in `dispose` — never
/// recreated inside `build()` so active routes (`/cart`, `/checkout`,
/// `/receipt`) are preserved across theme and language switches.
///
/// [sessionCubit] powers boot-time session restore: after a power flicker the
/// kiosk rehydrates its persisted session and the redirect carries the
/// shopper straight back into their open cart instead of the welcome screen.
GoRouter buildAppRouter({
  String initialLocation = AppRoutes.welcome,
  SessionCubit? sessionCubit,
}) {
  final refresh = sessionCubit == null
      ? null
      : GoRouterRefreshStream(sessionCubit.stream);

  return GoRouter(
    initialLocation: initialLocation,
    refreshListenable: refresh,
    redirect: (context, state) {
      if (sessionCubit == null) return null;
      final session = sessionCubit.state.session;
      final atWelcome = state.matchedLocation == AppRoutes.welcome;
      if (session != null && session.isOpen && atWelcome) {
        return '${AppRoutes.cart}?sessionId=${session.sessionId}';
      }
      return null;
    },
    routes: [
      GoRoute(
        path: AppRoutes.welcome,
        pageBuilder: (context, state) {
          final isDark = context.watch<ThemeCubit>().state.isDark;
          return _fadeThrough(
            state,
            WelcomeCheckInScreen(
              onToggleLocale: () => context.read<LocaleCubit>().toggle(),
              onToggleTheme: () => context.read<ThemeCubit>().toggle(),
              isDarkMode: isDark,
            ),
          );
        },
      ),
      GoRoute(
        path: AppRoutes.cart,
        pageBuilder: (context, state) {
          final isDark = context.watch<ThemeCubit>().state.isDark;
          final sessionId = state.uri.queryParameters['sessionId'] ??
              state.extra as String? ??
              '';
          return _fadeThrough(
            state,
            CartScreen(
              sessionId: sessionId,
              onToggleLocale: () => context.read<LocaleCubit>().toggle(),
              onToggleTheme: () => context.read<ThemeCubit>().toggle(),
              isDarkMode: isDark,
            ),
          );
        },
      ),
      GoRoute(
        path: AppRoutes.checkout,
        pageBuilder: (context, state) {
          final isDark = context.watch<ThemeCubit>().state.isDark;
          final sessionId = state.uri.queryParameters['sessionId'] ??
              state.extra as String? ??
              '';
          return _fadeThrough(
            state,
            CheckoutScreen(
              sessionId: sessionId,
              onToggleLocale: () => context.read<LocaleCubit>().toggle(),
              onToggleTheme: () => context.read<ThemeCubit>().toggle(),
              isDarkMode: isDark,
            ),
          );
        },
      ),
      GoRoute(
        path: AppRoutes.receipt,
        pageBuilder: (context, state) {
          final isDark = context.watch<ThemeCubit>().state.isDark;
          final extra = state.extra;
          if (extra is Map<String, dynamic>) {
            return _fadeThrough(
              state,
              ReceiptScreen(
                receiptHash: (extra['receiptHash'] ?? '').toString(),
                totalMinor: (extra['totalMinor'] ?? 0) as int,
                sessionId: (extra['sessionId'] ?? '').toString(),
                itemsCount: (extra['itemsCount'] ?? 0) as int,
                paymentMethod: (extra['paymentMethod'] ?? 'card').toString(),
                onToggleLocale: () => context.read<LocaleCubit>().toggle(),
                onToggleTheme: () => context.read<ThemeCubit>().toggle(),
                isDarkMode: isDark,
              ),
            );
          }
          final qp = state.uri.queryParameters;
          return _fadeThrough(
            state,
            ReceiptScreen(
              receiptHash: (qp['receiptHash'] ?? '').toString(),
              totalMinor:
                  int.tryParse((qp['totalMinor'] ?? '0').toString()) ?? 0,
              sessionId: (qp['sessionId'] ?? '').toString(),
              itemsCount: int.tryParse((qp['itemsCount'] ?? '0').toString()) ?? 0,
              paymentMethod: (qp['paymentMethod'] ?? 'card').toString(),
              onToggleLocale: () => context.read<LocaleCubit>().toggle(),
              onToggleTheme: () => context.read<ThemeCubit>().toggle(),
              isDarkMode: isDark,
            ),
          );
        },
      ),
    ],
  );
}

/// Quiet fade-through page transition — shared identity for every hop.
CustomTransitionPage<T> _fadeThrough<T>(GoRouterState state, Widget child) {
  return CustomTransitionPage<T>(
    key: state.pageKey,
    child: child,
    transitionDuration: AppTypography.slow,
    reverseTransitionDuration: AppTypography.base,
    transitionsBuilder: (context, animation, secondaryAnimation, child) {
      final curved = CurvedAnimation(
        parent: animation,
        curve: AppTypography.enterCurve,
        reverseCurve: AppTypography.exitCurve,
      );
      return FadeTransition(
        opacity: curved,
        child: SlideTransition(
          position: Tween<Offset>(
            begin: const Offset(0, 0.015),
            end: Offset.zero,
          ).animate(curved),
          child: child,
        ),
      );
    },
  );
}

/// Bridges a Bloc stream into a [Listenable] so route redirects re-run
/// whenever the session state changes (canonical go_router pattern).
class GoRouterRefreshStream extends ChangeNotifier {
  GoRouterRefreshStream(Stream<dynamic> stream) {
    notifyListeners();
    _subscription =
        stream.asBroadcastStream().listen((dynamic _) => notifyListeners());
  }

  late final StreamSubscription<dynamic> _subscription;

  @override
  void dispose() {
    _subscription.cancel();
    super.dispose();
  }
}
