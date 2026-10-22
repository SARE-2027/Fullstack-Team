import 'dart:async';

import 'package:flutter_bloc/flutter_bloc.dart';
import '../../../session/presentation/cubit/session_cubit.dart';
import '../../data/repositories/cart_repository.dart';
import 'cart_state.dart';

class CartCubit extends Cubit<CartState> {
  CartCubit(this._cartRepo, this._sessionCubit) : super(const CartState());

  final CartRepository _cartRepo;
  final SessionCubit _sessionCubit;

  Timer? _bannerHideTimer;
  bool _busy = false;

  String get _sessionId => _sessionCubit.state.session?.sessionId ?? '';

  Future<bool> addItemByScan(
    String rawBarcode, {
    int? measuredWeightG,
    String source = 'Scanner',
  }) async {
    if (_busy) return false;
    final barcode = rawBarcode.trim();
    if (barcode.isEmpty) return false;
    if (_sessionId.isEmpty) return false;

    _busy = true;
    _bannerHideTimer?.cancel();
    emit(state.copyWith(isAdding: true, bannerKind: WeightBannerKind.verifying));

    final result = await _cartRepo.addItem(
      sessionId: _sessionId,
      barcode: barcode,
      measuredWeightG: measuredWeightG,
      source: source,
    );

    if (isClosed) {
      _busy = false;
      return false;
    }

    final ok = result.fold(
      (failure) {
        emit(CartState(
          isAdding: false,
          bannerKind: WeightBannerKind.mismatch,
          bannerMessage: failure.message,
        ));
        return false;
      },
      (addResult) {
        if (addResult.isWeightMismatch) {
          emit(CartState(
            isAdding: false,
            bannerKind: WeightBannerKind.mismatch,
            bannerMessage: addResult.message,
            lastExpectedG: addResult.expectedWeightG,
            lastMeasuredG: addResult.measuredWeightG ?? measuredWeightG,
            lastItemNameAr: addResult.itemNameAr,
            lastItemNameEn: addResult.itemNameEn,
          ));
          return false;
        }
        if (!addResult.success) {
          emit(CartState(
            isAdding: false,
            bannerKind: WeightBannerKind.mismatch,
            bannerMessage: addResult.message,
          ));
          return false;
        }
        if (addResult.session != null) {
          _sessionCubit.setSession(addResult.session!);
        } else {
          _sessionCubit.refresh();
        }
        emit(CartState(
          isAdding: false,
          bannerKind: WeightBannerKind.success,
          bannerMessage: addResult.message.isEmpty ? null : addResult.message,
          lastExpectedG: addResult.expectedWeightG,
          lastMeasuredG: addResult.measuredWeightG ?? measuredWeightG,
          lastItemNameAr: addResult.itemNameAr,
          lastItemNameEn: addResult.itemNameEn,
        ));
        _scheduleAutoDismiss(WeightBannerKind.success);
        return true;
      },
    );
    _busy = false;
    return ok;
  }

  Future<bool> removeItem(String itemId) async {
    if (_busy) return false;
    if (_sessionId.isEmpty || itemId.isEmpty) return false;
    _busy = true;
    _bannerHideTimer?.cancel();

    final result = await _cartRepo.removeItem(sessionId: _sessionId, itemId: itemId);
    if (isClosed) {
      _busy = false;
      return false;
    }

    final ok = result.fold(
      (f) {
        emit(CartState(
          isAdding: false,
          bannerKind: WeightBannerKind.mismatch,
          bannerMessage: f.message,
        ));
        return false;
      },
      (updatedSession) {
        _sessionCubit.setSession(updatedSession);
        emit(const CartState(
          isAdding: false,
          bannerKind: WeightBannerKind.removed,
        ));
        _scheduleAutoDismiss(WeightBannerKind.removed);
        return true;
      },
    );
    _busy = false;
    return ok;
  }

  Future<bool> checkout({String paymentMethod = 'card'}) async {
    if (_busy) return false;
    final sid = _sessionId;
    if (sid.isEmpty) return false;
    final currentItemsCount = _sessionCubit.state.session?.itemsCount ?? 0;
    _busy = true;
    _bannerHideTimer?.cancel();
    emit(state.copyWith(isAdding: true, clearBanner: true));
    final result = await _cartRepo.checkout(
      sid,
      paymentMethod: paymentMethod,
      itemsCount: currentItemsCount,
    );
    if (isClosed) {
      _busy = false;
      return false;
    }
    final ok = result.fold(
      (f) {
        emit(CartState(
          isAdding: false,
          bannerKind: WeightBannerKind.mismatch,
          bannerMessage: f.message,
        ));
        return false;
      },
      (checkoutResult) {
        _sessionCubit.setCheckout(checkoutResult);
        emit(state.copyWith(isAdding: false, clearBanner: true));
        return true;
      },
    );
    _busy = false;
    return ok;
  }

  void _scheduleAutoDismiss(WeightBannerKind expectedKind) {
    _bannerHideTimer?.cancel();
    _bannerHideTimer = Timer(const Duration(milliseconds: 3500), () {
      if (!isClosed && state.bannerKind == expectedKind) {
        emit(state.copyWith(clearBanner: true));
      }
    });
  }

  void dismissBanner() {
    if (isClosed) return;
    _bannerHideTimer?.cancel();
    emit(state.copyWith(clearBanner: true));
  }

  @override
  Future<void> close() {
    _bannerHideTimer?.cancel();
    _busy = false;
    return super.close();
  }
}
