import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

import '../../core/bloc/locale_cubit.dart';
import '../../core/bloc/theme_cubit.dart';
import '../../features/cart/presentation/screens/cart_screen.dart';
import '../../features/check_in/presentation/screens/welcome_check_in_screen.dart';
import '../../features/checkout/presentation/screens/checkout_screen.dart';
import '../../features/receipt/presentation/screens/receipt_screen.dart';

abstract final class AppRoutes {
  static const welcome = '/';
  static const cart = '/cart';
  static const checkout = '/checkout';
  static const receipt = '/receipt';
}

/// Builds the single application [GoRouter] instance.
///
/// Must be created once in `initState` and disposed in `dispose` — never
/// recreated inside `build()` so active routes (`/cart`, `/checkout`, `/receipt`)
/// are preserved across theme and language switches.
GoRouter buildAppRouter({String initialLocation = AppRoutes.welcome}) {
  return GoRouter(
    initialLocation: initialLocation,
    routes: [
      GoRoute(
        path: AppRoutes.welcome,
        builder: (context, state) {
          final isDark = context.watch<ThemeCubit>().state.isDark;
          return WelcomeCheckInScreen(
            onToggleLocale: () => context.read<LocaleCubit>().toggle(),
            onToggleTheme: () => context.read<ThemeCubit>().toggle(),
            isDarkMode: isDark,
          );
        },
      ),
      GoRoute(
        path: AppRoutes.cart,
        builder: (context, state) {
          final isDark = context.watch<ThemeCubit>().state.isDark;
          final sessionId =
              state.uri.queryParameters['sessionId'] ?? state.extra as String? ?? '';
          return CartScreen(
            sessionId: sessionId,
            onToggleLocale: () => context.read<LocaleCubit>().toggle(),
            onToggleTheme: () => context.read<ThemeCubit>().toggle(),
            isDarkMode: isDark,
          );
        },
      ),
      GoRoute(
        path: AppRoutes.checkout,
        builder: (context, state) {
          final isDark = context.watch<ThemeCubit>().state.isDark;
          final sessionId =
              state.uri.queryParameters['sessionId'] ?? state.extra as String? ?? '';
          return CheckoutScreen(
            sessionId: sessionId,
            onToggleLocale: () => context.read<LocaleCubit>().toggle(),
            onToggleTheme: () => context.read<ThemeCubit>().toggle(),
            isDarkMode: isDark,
          );
        },
      ),
      GoRoute(
        path: AppRoutes.receipt,
        builder: (context, state) {
          final isDark = context.watch<ThemeCubit>().state.isDark;
          final extra = state.extra;
          if (extra is Map<String, dynamic>) {
            return ReceiptScreen(
              receiptHash: (extra['receiptHash'] ?? '').toString(),
              totalMinor: (extra['totalMinor'] ?? 0) as int,
              sessionId: (extra['sessionId'] ?? '').toString(),
              itemsCount: (extra['itemsCount'] ?? 0) as int,
              paymentMethod: (extra['paymentMethod'] ?? 'card').toString(),
              onToggleLocale: () => context.read<LocaleCubit>().toggle(),
              onToggleTheme: () => context.read<ThemeCubit>().toggle(),
              isDarkMode: isDark,
            );
          }
          final qp = state.uri.queryParameters;
          return ReceiptScreen(
            receiptHash: (qp['receiptHash'] ?? '').toString(),
            totalMinor: int.tryParse((qp['totalMinor'] ?? '0').toString()) ?? 0,
            sessionId: (qp['sessionId'] ?? '').toString(),
            itemsCount: int.tryParse((qp['itemsCount'] ?? '0').toString()) ?? 0,
            paymentMethod: (qp['paymentMethod'] ?? 'card').toString(),
            onToggleLocale: () => context.read<LocaleCubit>().toggle(),
            onToggleTheme: () => context.read<ThemeCubit>().toggle(),
            isDarkMode: isDark,
          );
        },
      ),
    ],
  );
}
