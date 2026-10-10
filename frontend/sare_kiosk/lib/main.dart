import 'package:flutter/material.dart';

import 'core/theme/app_theme.dart';
import 'core/utils/window_theme_helper.dart';
import 'features/check_in/presentation/screens/welcome_check_in_screen.dart';
import 'l10n/app_localizations.dart';

void main() {
  WidgetsFlutterBinding.ensureInitialized();
  runApp(const SareKioskApp());
}

class SareKioskApp extends StatefulWidget {
  const SareKioskApp({super.key});

  @override
  State<SareKioskApp> createState() => _SareKioskAppState();
}

class _SareKioskAppState extends State<SareKioskApp> {
  Locale _locale = const Locale('ar');
  ThemeMode _themeMode = ThemeMode.light;

  @override
  void initState() {
    super.initState();
    // Synchronize host window title bar and center window upon startup
    WidgetsBinding.instance.addPostFrameCallback((_) {
      WindowThemeHelper.updateTitleBarTheme(
        isDark: _themeMode == ThemeMode.dark,
      );
      WindowThemeHelper.centerWindow();
    });
  }

  void _toggleLocale() {
    setState(() {
      _locale = _locale.languageCode == 'ar'
          ? const Locale('en')
          : const Locale('ar');
    });
  }

  void _toggleThemeMode() {
    final nextMode =
        _themeMode == ThemeMode.light ? ThemeMode.dark : ThemeMode.light;
    setState(() {
      _themeMode = nextMode;
    });
    WindowThemeHelper.updateTitleBarTheme(isDark: nextMode == ThemeMode.dark);
  }

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'SARE Smart Kiosk',
      debugShowCheckedModeBanner: false,
      theme: AppTheme.lightTheme,
      darkTheme: AppTheme.darkTheme,
      themeMode: _themeMode,
      locale: _locale,
      supportedLocales: AppLocalizations.supportedLocales,
      localizationsDelegates: AppLocalizations.localizationsDelegates,
      home: WelcomeCheckInScreen(
        onToggleLocale: _toggleLocale,
        onToggleTheme: _toggleThemeMode,
        isDarkMode: _themeMode == ThemeMode.dark,
      ),
    );
  }
}
