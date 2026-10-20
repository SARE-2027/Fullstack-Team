import 'dart:math';

import 'package:equatable/equatable.dart';
import 'package:flutter/material.dart';
import '../../../session/data/models/session_models.dart';

/// Represents a supermarket catalog product available in the SARE ecosystem.
class KioskCatalogProduct extends Equatable {
  const KioskCatalogProduct({
    required this.productId,
    required this.variantId,
    required this.barcode,
    required this.nameAr,
    required this.nameEn,
    required this.categoryAr,
    required this.categoryEn,
    required this.priceMinor,
    required this.weightG,
    required this.icon,
    this.isProduce = false,
  });

  final String productId;
  final String variantId;
  final String barcode;
  final String nameAr;
  final String nameEn;
  final String categoryAr;
  final String categoryEn;
  final int priceMinor;
  final int weightG;
  final IconData icon;
  final bool isProduce;

  String displayName(String localeCode) => localeCode == 'ar' ? nameAr : nameEn;
  String displayCategory(String localeCode) => localeCode == 'ar' ? categoryAr : categoryEn;

  @override
  List<Object?> get props => [productId, variantId, barcode, nameAr, nameEn, priceMinor, weightG, isProduce];
}

/// Represents a store loyalty member profile linked to an NFC card UID.
class KioskLoyaltyMember extends Equatable {
  const KioskLoyaltyMember({
    required this.userId,
    required this.nfcUid,
    required this.nameAr,
    required this.nameEn,
    required this.tierAr,
    required this.tierEn,
    required this.points,
  });

  final String userId;
  final String nfcUid;
  final String nameAr;
  final String nameEn;
  final String tierAr;
  final String tierEn;
  final int points;

  String displayName(String localeCode) => localeCode == 'ar' ? nameAr : nameEn;
  String displayTier(String localeCode) => localeCode == 'ar' ? tierAr : tierEn;

  @override
  List<Object?> get props => [userId, nfcUid, nameAr, nameEn, tierAr, tierEn, points];
}

/// Offline-first edge decision engine & local session store for SARE Smart Kiosk.
///
/// Mirrors the exact business rules of `.NET 10` `CartService` and `SessionService`:
/// - ±15g weight verification tolerance (`weightToleranceGrams = 15`)
/// - Minor-unit integer pricing (`priceMinor`, `totalMinor`)
/// - Identical seeded barcodes and SKUs as `DbInitializer.cs`
class LocalSmartCartEngine {
  LocalSmartCartEngine();

  static const int weightToleranceGrams = 15;

  /// Seeded supermarket catalog matching `DbInitializer.cs` + extended demo SKUs.
  static const List<KioskCatalogProduct> catalog = [
    KioskCatalogProduct(
      productId: 'prod-tuna-01',
      variantId: 'var-tuna-01',
      barcode: '6223000123456',
      nameAr: 'تونا صن شاين قطع 180جم',
      nameEn: 'Sunshine Tuna Chunk 180g',
      categoryAr: 'معلبات',
      categoryEn: 'Canned Goods',
      priceMinor: 6500,
      weightG: 180,
      icon: Icons.set_meal_rounded,
    ),
    KioskCatalogProduct(
      productId: 'prod-juice-02',
      variantId: 'var-juice-02',
      barcode: '6223000654321',
      nameAr: 'عصير جهينة بيور برتقال 1لتر',
      nameEn: 'Juhayna Pure Orange Juice 1L',
      categoryAr: 'مشروبات',
      categoryEn: 'Beverages',
      priceMinor: 4500,
      weightG: 1050,
      icon: Icons.local_drink_rounded,
    ),
    KioskCatalogProduct(
      productId: 'prod-biscuit-03',
      variantId: 'var-biscuit-03',
      barcode: '6223000789012',
      nameAr: 'بسكويت بسكريم كاكاو 80جم',
      nameEn: 'Biskrem Cocoa Filled Biscuits 80g',
      categoryAr: 'سناكس',
      categoryEn: 'Snacks',
      priceMinor: 1500,
      weightG: 80,
      icon: Icons.cookie_rounded,
    ),
    KioskCatalogProduct(
      productId: 'prod-milk-04',
      variantId: 'var-milk-04',
      barcode: '6223000111222',
      nameAr: 'حليب المراعي كامل الدسم 1لتر',
      nameEn: 'Almarai Full Cream Milk 1L',
      categoryAr: 'ألبان',
      categoryEn: 'Dairy',
      priceMinor: 4200,
      weightG: 1030,
      icon: Icons.kitchen_rounded,
    ),
    KioskCatalogProduct(
      productId: 'prod-coffee-05',
      variantId: 'var-coffee-05',
      barcode: '6223000333444',
      nameAr: 'بن أبو عوف تركي محوج 200جم',
      nameEn: 'Abu Auf Turkish Coffee 200g',
      categoryAr: 'قهوة ومشروبات',
      categoryEn: 'Coffee & Tea',
      priceMinor: 12500,
      weightG: 200,
      icon: Icons.coffee_rounded,
    ),
    KioskCatalogProduct(
      productId: 'prod-chips-06',
      variantId: 'var-chips-06',
      barcode: '6223000555666',
      nameAr: 'شيبسي تايجر جبنة متبلة 65جم',
      nameEn: 'Tiger Spiced Cheese Chips 65g',
      categoryAr: 'سناكس',
      categoryEn: 'Snacks',
      priceMinor: 1500,
      weightG: 65,
      icon: Icons.fastfood_rounded,
    ),
    KioskCatalogProduct(
      productId: 'prod-banana-07',
      variantId: 'var-banana-07',
      barcode: '6223000999000',
      nameAr: 'موز بلدي طازج (500 جم)',
      nameEn: 'Fresh Organic Bananas (500g)',
      categoryAr: 'خضار وفاكهة',
      categoryEn: 'Fresh Produce',
      priceMinor: 3500,
      weightG: 500,
      icon: Icons.eco_rounded,
      isProduce: true,
    ),
  ];

  /// Pre-provisioned NFC loyalty cards for member sign-in.
  static const List<KioskLoyaltyMember> members = [
    KioskLoyaltyMember(
      userId: 'usr-member-01',
      nfcUid: '04:A2:19:7C',
      nameAr: 'أحمد محمد',
      nameEn: 'Ahmed Mohamed',
      tierAr: 'عضوية ذهبية',
      tierEn: 'Gold Member',
      points: 1450,
    ),
    KioskLoyaltyMember(
      userId: 'usr-member-02',
      nfcUid: '04:B8:44:2E',
      nameAr: 'سارة علي',
      nameEn: 'Sara Ali',
      tierAr: 'عضوية بلاتينية',
      tierEn: 'Platinum Member',
      points: 3200,
    ),
    KioskLoyaltyMember(
      userId: 'usr-member-03',
      nfcUid: 'NFC-TEST',
      nameAr: 'عمر خالد',
      nameEn: 'Omar Khaled',
      tierAr: 'عضوية مميزة',
      tierEn: 'VIP Member',
      points: 2100,
    ),
  ];

  final Map<String, SessionSummary> _sessions = {};

  static KioskCatalogProduct? findByBarcode(String barcode) {
    final normalized = barcode.trim();
    for (final item in catalog) {
      if (item.barcode == normalized) return item;
    }
    return null;
  }

  static KioskLoyaltyMember resolveMember(String nfcUid) {
    final normalized = nfcUid.trim().toUpperCase();
    for (final m in members) {
      if (m.nfcUid.toUpperCase() == normalized) return m;
    }
    return KioskLoyaltyMember(
      userId: 'usr-custom-${normalized.hashCode.abs()}',
      nfcUid: normalized,
      nameAr: 'عضو SARE ($normalized)',
      nameEn: 'SARE Member ($normalized)',
      tierAr: 'عضوية قياسية',
      tierEn: 'Standard Member',
      points: 500,
    );
  }

  SessionSummary startSession({
    required String cartId,
    String? nfcUid,
  }) {
    final now = DateTime.now().toUtc();
    final sessionId = _generateGuid();
    KioskLoyaltyMember? member;
    if (nfcUid != null && nfcUid.trim().isNotEmpty) {
      member = resolveMember(nfcUid);
    }

    final session = SessionSummary(
      sessionId: sessionId,
      cartId: cartId,
      userId: member?.userId,
      userName: member?.nameEn,
      nfcUid: member?.nfcUid,
      status: SessionStatusX.open,
      totalMinor: 0,
      itemsCount: 0,
      startedAt: now,
      updatedAt: now,
      items: const [],
      isOfflineMode: true,
    );
    _sessions[sessionId] = session;
    return session;
  }

  SessionSummary? getSession(String sessionId) => _sessions[sessionId];

  void cacheSession(SessionSummary session) {
    _sessions[session.sessionId] = session;
  }

  AddItemResult processItemDetection({
    required String sessionId,
    required String barcode,
    required int measuredWeightG,
    String source = 'Scanner',
  }) {
    final current = _sessions[sessionId];
    if (current == null || !current.isOpen) {
      return const AddItemResult(
        success: false,
        message: 'Active session not found.',
      );
    }

    final product = findByBarcode(barcode);
    if (product == null) {
      return AddItemResult(
        success: false,
        message: 'No active product matching barcode "$barcode".',
      );
    }

    final weightDelta = (measuredWeightG - product.weightG).abs();
    final isWeightValid = weightDelta <= weightToleranceGrams;

    if (!isWeightValid) {
      return AddItemResult(
        success: false,
        message:
            'Weight mismatch! Expected ${product.weightG}g but measured ${measuredWeightG}g on ${product.nameEn}.',
        isWeightMismatch: true,
        expectedWeightG: product.weightG,
        measuredWeightG: measuredWeightG,
        itemNameAr: product.nameAr,
        itemNameEn: product.nameEn,
      );
    }

    final now = DateTime.now().toUtc();
    final newItem = SessionItem(
      id: _generateGuid(),
      variantId: product.variantId,
      productId: product.productId,
      barcode: product.barcode,
      nameAr: product.nameAr,
      nameEn: product.nameEn,
      unitPriceMinor: product.priceMinor,
      expectedWeightG: measuredWeightG,
      source: source,
      addedAt: now,
    );

    final updatedItems = [...current.items, newItem];
    final updatedTotal = updatedItems.fold<int>(0, (sum, i) => sum + i.unitPriceMinor);
    final updatedSession = current.copyWith(
      items: updatedItems,
      itemsCount: updatedItems.length,
      totalMinor: updatedTotal,
      updatedAt: now,
    );
    _sessions[sessionId] = updatedSession;

    return AddItemResult(
      success: true,
      message: 'Item added and weight verified (${measuredWeightG}g).',
      expectedWeightG: product.weightG,
      measuredWeightG: measuredWeightG,
      session: updatedSession,
      itemNameAr: product.nameAr,
      itemNameEn: product.nameEn,
    );
  }

  SessionSummary? removeItem({
    required String sessionId,
    required String itemId,
  }) {
    final current = _sessions[sessionId];
    if (current == null) return null;
    final updatedItems = current.items.where((i) => i.id != itemId).toList();
    final updatedTotal = updatedItems.fold<int>(0, (sum, i) => sum + i.unitPriceMinor);
    final updated = current.copyWith(
      items: updatedItems,
      itemsCount: updatedItems.length,
      totalMinor: updatedTotal,
      updatedAt: DateTime.now().toUtc(),
    );
    _sessions[sessionId] = updated;
    return updated;
  }

  SessionSummary? closeSession(String sessionId, {String reason = 'closed'}) {
    final current = _sessions[sessionId];
    if (current == null) return null;
    final now = DateTime.now().toUtc();
    final closed = current.copyWith(
      status: reason == 'abandoned' ? SessionStatusX.abandoned : SessionStatusX.closed,
      closedAt: now,
      updatedAt: now,
      closeReason: reason,
    );
    _sessions[sessionId] = closed;
    return closed;
  }

  CheckoutResult? checkout(String sessionId, {String paymentMethod = 'card'}) {
    final closed = closeSession(sessionId, reason: 'closed');
    if (closed == null) return null;
    final closedAt = closed.closedAt ?? DateTime.now().toUtc();
    final shortId = closed.sessionId.length >= 8 ? closed.sessionId.substring(0, 8).toUpperCase() : closed.sessionId;
    final receiptHash = 'RECEIPT_${shortId}_${closedAt.millisecondsSinceEpoch}';
    return CheckoutResult(
      sessionId: closed.sessionId,
      cartId: closed.cartId,
      totalPaidMinor: closed.totalMinor,
      receiptHash: receiptHash,
      closedAt: closedAt,
      itemsCount: closed.itemsCount,
      paymentMethod: paymentMethod,
    );
  }

  static String _generateGuid() {
    final r = Random.secure();
    String h(int n) => n.toRadixString(16).padLeft(2, '0');
    final bytes = List<int>.generate(16, (_) => r.nextInt(256));
    bytes[6] = (bytes[6] & 0x0F) | 0x40;
    bytes[8] = (bytes[8] & 0x3F) | 0x80;
    return '${bytes.sublist(0, 4).map(h).join()}'
        '-${bytes.sublist(4, 6).map(h).join()}'
        '-${bytes.sublist(6, 8).map(h).join()}'
        '-${bytes.sublist(8, 10).map(h).join()}'
        '-${bytes.sublist(10, 16).map(h).join()}';
  }
}
