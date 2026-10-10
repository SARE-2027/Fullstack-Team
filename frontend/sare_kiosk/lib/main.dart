import 'package:flutter/material.dart';

import 'core/theme/app_theme.dart';
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

  void _toggleLocale() {
    setState(() {
      _locale = _locale.languageCode == 'ar'
          ? const Locale('en')
          : const Locale('ar');
    });
  }

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'SARE Smart Kiosk',
      debugShowCheckedModeBanner: false,
      theme: AppTheme.lightTheme,
      darkTheme: AppTheme.darkTheme,
      themeMode: ThemeMode.dark,
      locale: _locale,
      supportedLocales: AppLocalizations.supportedLocales,
      localizationsDelegates: AppLocalizations.localizationsDelegates,
      home: WelcomeCheckInScreen(
        onToggleLocale: _toggleLocale,
      ),
    );
  }
}
