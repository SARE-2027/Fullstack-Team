# Startup integration

Integration branch: `codex/integration-startup`, based on catalog commit `ec48045`.

## Sources

- `origin/main` at `aaee2ec`: authentication, Identity/JWT, the original kiosk catalog/cart APIs, SignalR, and Flutter kiosk.
- `origin/feature/authentication` at `b7f3d76` is already an ancestor of main (PR #1); its work is included by the main merge.
- `origin/feat/session-cart-layer` at `8657dd7`: session lifecycle, telemetry, repositories, transactions, and hardened cart groups.

## Main integration decisions

- Keep versioned catalog routes and preserve main's `/api/products` routes in a separate compatibility controller.
- All product writes, including compatibility routes, require the admin policy. Legacy creation validates its product and first variant and saves both atomically. Public legacy reads follow the same product/variant availability rules as the versioned catalog.
- Identity uses `Admin`, `Staff`, and `Customer`. Catalog policies accept Identity role names and the existing lowercase development roles. JWT authentication is registered once, using the authentication feature's configuration.
- Keep all catalog registrations plus Identity and the main cart services. Keep the public partial `Program` for integration tests.
- Retain the earlier decision to remove cart zones from the domain. Incoming detection code does not reintroduce `Zone` fields.
- Automatic role/admin/demo seeding defaults to Development only; configure `Database:SeedOnStartup` explicitly elsewhere. Tests create their own data.

## Database history

The canonical initial migration is main's `20261008191122_InitialCreate`, preserved unchanged so existing main/authentication databases have a continuous upgrade path. The catalog's empty `ProtectCatalogEdits` migration remains. `IntegrateCatalogIdentity` carries the integrated model forward, including the previously agreed removal of zone columns. Its downgrade fills restored zones with `basket` before applying the old zone checks.

No database update or data reset is run by this integration. Before upgrading an existing database, back it up and inspect `__EFMigrationsHistory`. A database built from the old catalog-only `20261008190201_InitialCreate` (or the original `20261008164918_InitialCreate`) has a different baseline: it needs a reviewed data-preserving baseline/Identity upgrade, rather than blindly applying main's initial migration over existing tables. Do not delete migration history rows to bypass this difference.

`IntegrateSessionCartConcurrency` captures the session/cart branch's existing PostgreSQL row-version mapping (`xmin`). Npgsql uses PostgreSQL's system column; the generated SQL does not create a physical `xmin` column. No extra session index or new lifecycle feature is introduced by this merge.

## Session/cart conflict resolutions

- Both `/api/cart/...` and `/api/sessions` + `/api/carts/...` routes are retained. Their overlapping start/close operations use the session branch's lifecycle service. Starting on an unknown cart returns `404`, a disabled cart is rejected, and a cart with an existing open session returns `409` instead of silently abandoning that session. Checkout no longer forcibly reactivates a low-battery cart.
- Keep main's legacy request/response JSON shapes by renaming the colliding C# DTOs to `LegacyStartSessionRequest` and `LegacySessionItemDto`. The shared start DTO can resolve a legacy `UserId` or the session branch's `NfcUid`, with validation preventing both identifiers being submitted together.
- Detection still uses main's implemented weight tolerance of 15 grams. The earlier 5-gram proposal in PROJECT.md is not a change to detection behavior in this merge. Detection rejects inactive parent products consistently with the catalog availability rule. Invoice lines retain their stored prices when catalog prices change.
- One notification service implements both branches' notification contracts. Cart group joins/leaves preserve both group-name spellings; publishing uses `cart_{id}`. Existing events are retained, and `SessionStarted` now carries the shared session summary. Dashboard group subscription uses the existing staff/admin policy. A notification failure is logged after a successful database save.
- Keep the session branch's application exception hierarchy and both conflict-exception constructor forms. The shared exception handler supports those exceptions, FluentValidation, concurrency failures, and the catalog image-size error.
- Existing session/cart REST access rules and hardware-token behavior remain from their source branches; this merge does not add a new endpoint authorization scheme. The telemetry token check and cart hub token check are covered by integration tests.

## Verification environment

The backend integration suite has **141 passing Release tests** using isolated SQLite databases. Authentication integration tests use tokens issued by the actual login service and validate access to catalog routes; the existing tests retain independent signed test tokens. Session tests exercise both route families together, immutable invoice prices, battery behavior, transaction rollback on a stale close, and a real SignalR client receiving session and detection events. SQLite tests substitute timestamp concurrency tokens for PostgreSQL's `xmin`; they do not verify PostgreSQL's system-column behavior.

Frontend files are retained unchanged from main and are outside the backend verification scope.

No PostgreSQL service or running Docker engine is available locally. PostgreSQL migrations can be scaffolded and their SQL generated, but live PostgreSQL migration/concurrency verification needs the team's database environment.
