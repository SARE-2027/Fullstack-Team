# SARE Smart Kiosk — عربة SARE الذكية

Touchscreen kiosk interface for the **SARE autonomous smart shopping cart**, targeting the 10.1" Raspberry Pi 5 display (Linux) and Windows demo machines.

The kiosk guides a shopper through the complete frictionless journey:
**Idle → Guest / NFC member check-in → Scan & weight-verify (±15g) → Checkout → QR exit pass → auto-reset.**

---

## ✨ Product highlights

| Capability | Details |
|---|---|
| **Weight-verification-first UI** | Royal blue is the brand (trust, checkout, primary actions) over quiet charcoal surfaces; color carries meaning around the ±15g load-cell gate: emerald = verified / go, amber = attention, red = mismatch / stop. Color always pairs with an icon and a label — never color alone. |
| **Bi-directional & bilingual** | Full Arabic (RTL, Tajawal) and English (LTR, Poppins) with live toggle — layouts are direction-aware down to the countdown bar. |
| **Offline-first edge engine** | When the .NET backend is unreachable, a local decision engine mirrors `CartService` rules (±15g tolerance, minor-unit pricing, seeded catalog) so demos never stall. |
| **Power-flicker resilience** | The active session is persisted to disk; on boot the router restores the shopper straight into their open cart. |
| **USB HID scanner support** | Hardware barcode scanners (keyboard-wedge mode) are captured screen-wide with no focused input required. |
| **Undo removal** | Removing an item offers a one-tap Undo window on the status banner; items scanned after the removal are preserved. |
| **Demo sensor bench** | A collapsible catalog simulator triggers both valid scans and fraud (+95g) weight mismatches for live demos. |

## 🎨 Design system — "Fresh-Verify"

- **Dark showroom theme (default)**: neutral charcoal surfaces (`#0E1116` family) with a quiet royal-blue ambient glow anchoring every screen; emerald is reserved for verification signals only.
- **Light daytime theme**: porcelain surfaces with the same verification semantics.
- **One voice of motion**: shared durations (`140/220/320ms`) and curves (`easeOutCubic` in, `easeInCubic` out); ambient animations are disabled automatically under `flutter test` and reduced-motion settings.
- **Performance discipline**: `const`-first widgets, `RepaintBoundary` isolation around every animated/expensive region, rebuilds scoped to leaf consumers via `BlocBuilder`/`BlocSelector`.

Tokens live in [`lib/core/theme/`](lib/core/theme/) — `app_colors.dart` (semantic palette), `app_typography.dart` (type & motion contract), `app_theme.dart` (Material 3 component themes).

## 🏗️ Architecture

```
lib/
├── core/            # Design system, API client, config, storage, routing, DI
├── features/
│   ├── check_in/    # Welcome & guest/member check-in
│   ├── session/     # Session models, repository, SessionCubit
│   ├── cart/        # Catalog engine, repositories, CartCubit, cart UI
│   ├── checkout/    # Payment method selection & invoice
│   └── receipt/     # QR exit pass & auto-reset
└── l10n/            # AR/EN ARB sources + generated localizations
```

- **State**: Cubit (`flutter_bloc`) — deterministic finite-state flow, audited by `AppBlocObserver`.
- **Navigation**: `go_router` with fade-through page transitions and a session-aware redirect.
- **Data**: repository layer over a typed `ApiClient` (12s timeout, .NET `GlobalExceptionHandler` error shapes) with `Either<Failure, T>` results.

## 🚀 Running

```bash
cd Fullstack-Team/frontend/sare_kiosk
flutter pub get
flutter run -d windows          # or -d linux on the Pi
```

The kiosk boots against `http://localhost:5000` by default. Override per device via CLI flags, environment variables, or a JSON config file:

```bash
flutter run -d linux -- --cart-id=CART-05 --cart-number=05 --api-url=http://sare.local:5000
# or SARE_CART_ID=CART-05 SARE_API_URL=http://sare.local:5000
# or /etc/sare/config.json on the Pi
```

If the backend is down, the kiosk transparently switches to the local engine — the full shopping journey still works.

## ✅ Quality gates

```bash
flutter analyze   # 0 errors · 0 warnings · 0 infos
flutter test      # 23/23 passing
```

The suite covers the full guest journey E2E (check-in → valid scan → fraud alert → checkout → receipt → auto-reset), member NFC sign-in, theme/language toggles preserving the active route, boot-time session restore, and unit-level `bloc_test` coverage for `SessionCubit` and `CartCubit` (weight match/mismatch, remove & undo, checkout, offline fallback).
