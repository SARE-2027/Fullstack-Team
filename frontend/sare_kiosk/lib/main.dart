import 'package:flutter/material.dart';

import 'core/theme/app_theme.dart';

void main() {
  runApp(const SareKioskApp());
}

class SareKioskApp extends StatelessWidget {
  const SareKioskApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'SARE Smart Kiosk',
      debugShowCheckedModeBanner: false,
      theme: AppTheme.lightTheme,
      darkTheme: AppTheme.darkTheme,
      themeMode: ThemeMode.dark,
      home: const KioskPlaceholderScreen(),
    );
  }
}

class KioskPlaceholderScreen extends StatelessWidget {
  const KioskPlaceholderScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('SARE Kiosk'),
      ),
      body: const Center(
        child: Text('SARE Smart Shopping Cart Kiosk'),
      ),
    );
  }
}
