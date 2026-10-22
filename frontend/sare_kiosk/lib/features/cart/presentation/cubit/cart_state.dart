import 'package:equatable/equatable.dart';

enum WeightBannerKind { none, verifying, success, mismatch, removed }

class CartState extends Equatable {
  const CartState({
    this.isAdding = false,
    this.bannerKind = WeightBannerKind.none,
    this.bannerMessage,
    this.lastExpectedG,
    this.lastMeasuredG,
    this.lastItemNameAr,
    this.lastItemNameEn,
  });

  final bool isAdding;
  final WeightBannerKind bannerKind;
  final String? bannerMessage;
  final int? lastExpectedG;
  final int? lastMeasuredG;
  final String? lastItemNameAr;
  final String? lastItemNameEn;

  CartState copyWith({
    bool? isAdding,
    WeightBannerKind? bannerKind,
    String? bannerMessage,
    int? lastExpectedG,
    int? lastMeasuredG,
    String? lastItemNameAr,
    String? lastItemNameEn,
    bool clearBanner = false,
  }) {
    return CartState(
      isAdding: isAdding ?? this.isAdding,
      bannerKind: clearBanner ? WeightBannerKind.none : (bannerKind ?? this.bannerKind),
      bannerMessage: clearBanner ? null : (bannerMessage ?? this.bannerMessage),
      lastExpectedG: clearBanner ? null : (lastExpectedG ?? this.lastExpectedG),
      lastMeasuredG: clearBanner ? null : (lastMeasuredG ?? this.lastMeasuredG),
      lastItemNameAr: clearBanner ? null : (lastItemNameAr ?? this.lastItemNameAr),
      lastItemNameEn: clearBanner ? null : (lastItemNameEn ?? this.lastItemNameEn),
    );
  }

  @override
  List<Object?> get props => [
        isAdding,
        bannerKind,
        bannerMessage,
        lastExpectedG,
        lastMeasuredG,
        lastItemNameAr,
        lastItemNameEn,
      ];
}
