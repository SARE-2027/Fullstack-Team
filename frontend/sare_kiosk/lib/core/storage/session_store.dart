import 'package:shared_preferences/shared_preferences.dart';

/// Persists the current shopping session and ephemeral cart state so a kiosk
/// can survive app restart, OS kill, or power flicker on Raspberry Pi.
class SessionStore {
  static const _kSessionId = 'sare.session_id';
  static const _kCartId = 'sare.cart_id';
  static const _kUpdatedAtTicks = 'sare.session_updated_ticks';
  static const _kWeightStatus = 'sare.weight_status';

  Future<void> saveSession({
    required String sessionId,
    required String cartId,
    String? updatedAtTicks,
  }) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(_kSessionId, sessionId);
    await prefs.setString(_kCartId, cartId);
    if (updatedAtTicks != null) {
      await prefs.setString(_kUpdatedAtTicks, updatedAtTicks);
    }
  }

  Future<({String sessionId, String cartId, String? updatedAtTicks})?> loadSession() async {
    final prefs = await SharedPreferences.getInstance();
    final sid = prefs.getString(_kSessionId);
    final cid = prefs.getString(_kCartId);
    if (sid == null || cid == null) return null;
    return (sessionId: sid, cartId: cid, updatedAtTicks: prefs.getString(_kUpdatedAtTicks));
  }

  Future<void> updateTicks(String ticks) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(_kUpdatedAtTicks, ticks);
  }

  Future<void> clearSession() async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.remove(_kSessionId);
    await prefs.remove(_kCartId);
    await prefs.remove(_kUpdatedAtTicks);
  }

  Future<void> saveWeightStatus(String status) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(_kWeightStatus, status);
  }

  Future<String?> loadWeightStatus() async {
    final prefs = await SharedPreferences.getInstance();
    return prefs.getString(_kWeightStatus);
  }
}
