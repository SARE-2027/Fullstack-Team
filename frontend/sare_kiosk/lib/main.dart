import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

import 'core/bloc/app_bloc_observer.dart';
import 'core/bloc/locale_cubit.dart';
import 'core/bloc/theme_cubit.dart';
import 'core/config/kiosk_config_exports.dart';
import 'core/di/service_locator.dart';
import 'core/routing/app_router.dart';
import 'core/theme/app_theme.dart';
import 'core/utils/window_theme_helper.dart';
import 'features/cart/presentation/cubit/cart_cubit.dart';
import 'features/session/presentation/cubit/session_cubit.dart';
import 'l10n/app_localizations.dart';

void main(List<String> args) async {
  WidgetsFlutterBinding.ensureInitialized();
  Bloc.observer = AppBlocObserver();
  final config = await KioskConfigService.load(args: args);
  runApp(SareKioskApp(config: config));
}

typedef SareKioskRoot = SareKioskApp;

class SareKioskApp extends StatefulWidget {
  const SareKioskApp({
    super.key,
    this.config = const KioskConfig(),
    this.initialLocation = AppRoutes.welcome,
  });

  final KioskConfig config;
  final String initialLocation;

  @override
  State<SareKioskApp> createState() => _SareKioskAppState();
}

class _SareKioskAppState extends State<SareKioskApp> {
  late final ServiceLocator _locator;
  late final ThemeCubit _themeCubit;
  late final LocaleCubit _localeCubit;
  late final SessionCubit _sessionCubit;
  late final CartCubit _cartCubit;
  late final GoRouter _router;

  @override
  void initState() {
    super.initState();
    _locator = ServiceLocator(widget.config);
    _themeCubit = ThemeCubit();
    _localeCubit = LocaleCubit();
    _sessionCubit = SessionCubit(_locator.sessionRepository, _locator.sessionStore);
    _cartCubit = CartCubit(_locator.cartRepository, _sessionCubit);
    _router = buildAppRouter(
      initialLocation: widget.initialLocation,
      sessionCubit: _sessionCubit,
    );

    // Boot-time session restore: after a power flicker on the Raspberry Pi,
    // rehydrate the persisted session so the router redirect can carry the
    // shopper straight back into their open cart.
    _sessionCubit.loadPersisted();

    WidgetsBinding.instance.addPostFrameCallback((_) {
      WindowThemeHelper.updateTitleBarTheme(isDark: _themeCubit.state.isDark);
      WindowThemeHelper.centerWindow();
    });
  }

  @override
  void dispose() {
    _router.dispose();
    _cartCubit.close();
    _sessionCubit.close();
    _localeCubit.close();
    _themeCubit.close();
    _locator.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return MultiBlocProvider(
      providers: [
        BlocProvider<ThemeCubit>.value(value: _themeCubit),
        BlocProvider<LocaleCubit>.value(value: _localeCubit),
        BlocProvider<SessionCubit>.value(value: _sessionCubit),
        BlocProvider<CartCubit>.value(value: _cartCubit),
      ],
      child: KioskConfigScope(
        config: widget.config,
        child: BlocBuilder<ThemeCubit, ThemeState>(
          builder: (context, themeState) {
            return BlocBuilder<LocaleCubit, LocaleState>(
              builder: (context, localeState) {
                return MaterialApp.router(
                  title: 'SARE Smart Kiosk',
                  debugShowCheckedModeBanner: false,
                  theme: AppTheme.lightTheme,
                  darkTheme: AppTheme.darkTheme,
                  themeMode: themeState.mode,
                  locale: localeState.locale,
                  supportedLocales: AppLocalizations.supportedLocales,
                  localizationsDelegates: AppLocalizations.localizationsDelegates,
                  routerConfig: _router,
                );
              },
            );
          },
        ),
      ),
    );
  }
}
