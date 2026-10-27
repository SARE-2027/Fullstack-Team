import 'package:equatable/equatable.dart';
import '../../../../core/utils/money.dart';

enum SessionStatusX { open, closed, abandoned }

SessionStatusX parseSessionStatus(String raw) {
  switch (raw.toLowerCase()) {
    case 'open':
      return SessionStatusX.open;
    case 'closed':
      return SessionStatusX.closed;
    case 'abandoned':
      return SessionStatusX.abandoned;
    default:
      return SessionStatusX.open;
  }
}

/// Mirrors `CartSummaryResponse` from `SARE.Application/DTOs/Cart/CartSummaryResponse.cs`
class SessionSummary extends Equatable {
  SessionSummary({
    required this.sessionId,
    required this.cartId,
    this.userId,
    this.userName,
    this.nfcUid,
    required this.status,
    required this.totalMinor,
    required this.itemsCount,
    required this.startedAt,
    required List<SessionItem> items,
    required this.updatedAt,
    this.closedAt,
    this.closedByUserId,
    this.closeReason,
    this.isOfflineMode = false,
  }) : items = List<SessionItem>.unmodifiable(items);

  final String sessionId;
  final String cartId;
  final String? userId;
  final String? userName;
  final String? nfcUid;
  final SessionStatusX status;
  final int totalMinor;
  final int itemsCount;
  final DateTime startedAt;
  final List<SessionItem> items;
  final DateTime updatedAt;
  final DateTime? closedAt;
  final String? closedByUserId;
  final String? closeReason;
  final bool isOfflineMode;

  String get version => updatedAt.millisecondsSinceEpoch.toString();

  bool get isOpen => status == SessionStatusX.open;

  bool get isMember => (userName != null && userName!.isNotEmpty) || (nfcUid != null && nfcUid!.isNotEmpty);

  int get totalExpectedWeightG => items.fold<int>(0, (sum, item) => sum + item.expectedWeightG);

  SessionSummary copyWith({
    String? sessionId,
    String? cartId,
    String? userId,
    String? userName,
    String? nfcUid,
    SessionStatusX? status,
    int? totalMinor,
    int? itemsCount,
    DateTime? startedAt,
    List<SessionItem>? items,
    DateTime? updatedAt,
    DateTime? closedAt,
    String? closedByUserId,
    String? closeReason,
    bool? isOfflineMode,
  }) {
    return SessionSummary(
      sessionId: sessionId ?? this.sessionId,
      cartId: cartId ?? this.cartId,
      userId: userId ?? this.userId,
      userName: userName ?? this.userName,
      nfcUid: nfcUid ?? this.nfcUid,
      status: status ?? this.status,
      totalMinor: totalMinor ?? this.totalMinor,
      itemsCount: itemsCount ?? this.itemsCount,
      startedAt: startedAt ?? this.startedAt,
      items: items ?? this.items,
      updatedAt: updatedAt ?? this.updatedAt,
      closedAt: closedAt ?? this.closedAt,
      closedByUserId: closedByUserId ?? this.closedByUserId,
      closeReason: closeReason ?? this.closeReason,
      isOfflineMode: isOfflineMode ?? this.isOfflineMode,
    );
  }

  factory SessionSummary.fromJson(Map<String, dynamic> json, {bool isOfflineMode = false}) {
    DateTime parseDate(dynamic v) {
      if (v == null) return DateTime.now().toUtc();
      return DateTime.tryParse(v.toString()) ?? DateTime.now().toUtc();
    }

    final itemsRaw = (json['items'] ?? json['Items']) as List? ?? const [];
    final parsedItems = itemsRaw.whereType<Map<String, dynamic>>().map(SessionItem.fromJson).toList();
    return SessionSummary(
      sessionId: (json['sessionId'] ?? json['SessionId'] ?? json['id'] ?? '').toString(),
      cartId: (json['cartId'] ?? json['CartId'] ?? '').toString(),
      userId: (json['userId'] ?? json['UserId'])?.toString(),
      userName: (json['userName'] ?? json['UserName'])?.toString(),
      nfcUid: (json['nfcUid'] ?? json['NfcUid'])?.toString(),
      status: parseSessionStatus((json['status'] ?? json['Status'] ?? 'Open').toString()),
      totalMinor: (json['totalMinor'] ?? json['TotalMinor'] ?? 0) as int? ?? int.tryParse('${json['totalMinor']}') ?? 0,
      itemsCount: (json['itemsCount'] ?? json['ItemsCount'] ?? parsedItems.length) as int? ?? parsedItems.length,
      startedAt: parseDate(json['startedAt'] ?? json['StartedAt']),
      updatedAt: parseDate(json['updatedAt'] ?? json['UpdatedAt'] ?? json['startedAt'] ?? json['StartedAt']),
      closedAt: (json['closedAt'] ?? json['ClosedAt']) == null ? null : parseDate(json['closedAt'] ?? json['ClosedAt']),
      closedByUserId: (json['closedByUserId'] ?? json['ClosedByUserId'])?.toString(),
      closeReason: (json['closeReason'] ?? json['CloseReason'])?.toString(),
      items: parsedItems,
      isOfflineMode: isOfflineMode,
    );
  }

  String totalLabel(String localeCode, {String symbol = ''}) =>
      Money.formatMinor(totalMinor, localeCode, symbol: symbol);

  @override
  List<Object?> get props => [
        sessionId,
        cartId,
        userId,
        userName,
        nfcUid,
        status,
        totalMinor,
        itemsCount,
        startedAt,
        items,
        updatedAt,
        closedAt,
        closedByUserId,
        closeReason,
        isOfflineMode,
      ];
}

class SessionItem extends Equatable {
  const SessionItem({
    required this.id,
    required this.variantId,
    required this.productId,
    required this.barcode,
    required this.nameAr,
    required this.nameEn,
    this.imageUrl,
    required this.unitPriceMinor,
    required this.expectedWeightG,
    required this.source,
    required this.addedAt,
  });

  final String id;
  final String variantId;
  final String productId;
  final String barcode;
  final String nameAr;
  final String nameEn;
  final String? imageUrl;
  final int unitPriceMinor;
  final int expectedWeightG;
  final String source;
  final DateTime addedAt;

  String displayName(String localeCode) => localeCode == 'ar' ? nameAr : nameEn;

  factory SessionItem.fromJson(Map<String, dynamic> json) {
    DateTime parseDate(dynamic v) =>
        v == null ? DateTime.now().toUtc() : DateTime.tryParse(v.toString()) ?? DateTime.now().toUtc();
    return SessionItem(
      id: (json['id'] ?? json['Id'] ?? '').toString(),
      variantId: (json['variantId'] ?? json['VariantId'] ?? '').toString(),
      productId: (json['productId'] ?? json['ProductId'] ?? '').toString(),
      barcode: (json['barcode'] ?? json['Barcode'] ?? '').toString(),
      nameAr: (json['nameAr'] ?? json['NameAr'] ?? '').toString(),
      nameEn: (json['nameEn'] ?? json['NameEn'] ?? '').toString(),
      imageUrl: (json['imageUrl'] ?? json['ImageUrl'])?.toString(),
      unitPriceMinor: (json['unitPriceMinor'] ?? json['UnitPriceMinor'] ?? 0) as int? ??
          int.tryParse('${json['unitPriceMinor']}') ??
          0,
      expectedWeightG: (json['expectedWeightG'] ?? json['ExpectedWeightG'] ?? 0) as int? ??
          int.tryParse('${json['expectedWeightG']}') ??
          0,
      source: (json['source'] ?? json['Source'] ?? 'Scanner').toString(),
      addedAt: parseDate(json['addedAt'] ?? json['AddedAt']),
    );
  }

  @override
  List<Object?> get props => [
        id,
        variantId,
        productId,
        barcode,
        nameAr,
        nameEn,
        imageUrl,
        unitPriceMinor,
        expectedWeightG,
        source,
        addedAt,
      ];
}

class AddItemResult extends Equatable {
  const AddItemResult({
    required this.success,
    required this.message,
    this.isWeightMismatch = false,
    this.expectedWeightG,
    this.measuredWeightG,
    this.session,
    this.itemNameAr,
    this.itemNameEn,
  });

  final bool success;
  final String message;
  final bool isWeightMismatch;
  final int? expectedWeightG;
  final int? measuredWeightG;
  final SessionSummary? session;
  final String? itemNameAr;
  final String? itemNameEn;

  factory AddItemResult.fromJson(Map<String, dynamic> json) {
    SessionSummary? s;
    final rawSession = json['session'] ?? json['Session'];
    if (rawSession is Map<String, dynamic>) {
      s = SessionSummary.fromJson(rawSession);
    }
    return AddItemResult(
      success: (json['success'] ?? json['Success'] ?? false) as bool? ?? false,
      message: (json['message'] ?? json['Message'] ?? '').toString(),
      isWeightMismatch: (json['isWeightMismatch'] ?? json['IsWeightMismatch'] ?? false) as bool? ?? false,
      expectedWeightG: (json['expectedWeightG'] ?? json['ExpectedWeightG']) as int?,
      measuredWeightG: (json['measuredWeightG'] ?? json['MeasuredWeightG']) as int?,
      session: s,
    );
  }

  @override
  List<Object?> get props => [
        success,
        message,
        isWeightMismatch,
        expectedWeightG,
        measuredWeightG,
        session,
        itemNameAr,
        itemNameEn,
      ];
}

class CheckoutResult extends Equatable {
  const CheckoutResult({
    required this.sessionId,
    required this.cartId,
    required this.totalPaidMinor,
    required this.receiptHash,
    required this.closedAt,
    this.itemsCount = 0,
    this.paymentMethod = 'card',
  });

  final String sessionId;
  final String cartId;
  final int totalPaidMinor;
  final String receiptHash;
  final DateTime closedAt;
  final int itemsCount;
  final String paymentMethod;

  factory CheckoutResult.fromJson(Map<String, dynamic> json, {int itemsCount = 0, String paymentMethod = 'card'}) {
    return CheckoutResult(
      sessionId: (json['sessionId'] ?? json['SessionId'] ?? '').toString(),
      cartId: (json['cartId'] ?? json['CartId'] ?? '').toString(),
      totalPaidMinor: (json['totalPaidMinor'] ?? json['TotalPaidMinor'] ?? 0) as int? ?? 0,
      receiptHash: (json['receiptHash'] ?? json['ReceiptHash'] ?? '').toString(),
      closedAt: DateTime.tryParse((json['closedAt'] ?? json['ClosedAt'] ?? '').toString()) ?? DateTime.now().toUtc(),
      itemsCount: itemsCount,
      paymentMethod: paymentMethod,
    );
  }

  @override
  List<Object?> get props => [sessionId, cartId, totalPaidMinor, receiptHash, closedAt, itemsCount, paymentMethod];
}
