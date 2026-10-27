import '../../features/cart/data/repositories/cart_repository.dart';
import '../../features/cart/data/repositories/local_smart_cart_engine.dart';
import '../../features/session/data/repositories/session_repository.dart';
import '../api/api_client.dart';
import '../config/kiosk_config.dart';
import '../storage/session_store.dart';

/// Minimal service locator — no code-gen, no global mutable singleton.
/// Holds only stateless factories and the local edge decision engine.
/// Stateful cubits are owned by the widget tree via BlocProvider.
class ServiceLocator {
  ServiceLocator(this._config);

  final KioskConfig _config;

  late final LocalSmartCartEngine localEngine = LocalSmartCartEngine();
  late final ApiClient apiClient = ApiClient(baseUrl: _config.apiBaseUrl);
  late final SessionStore sessionStore = SessionStore();
  late final SessionRepository sessionRepository = SessionRepository(apiClient, localEngine);
  late final CartRepository cartRepository = CartRepository(apiClient, localEngine);

  void dispose() {
    apiClient.close();
  }
}
