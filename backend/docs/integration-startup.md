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

## Verification environment

The backend integration suite uses isolated SQLite databases. Authentication integration tests use tokens issued by the actual login service and validate access to catalog routes; the existing tests retain independent signed test tokens.

Flutter files are retained from main. The locally installed Dart SDK is 3.9.2; the kiosk declares `^3.13.4`, so `flutter pub get` fails before its tests can run. No SDK requirement is lowered during the merge.

No PostgreSQL service or running Docker engine is available locally. PostgreSQL migrations can be scaffolded and their SQL generated, but live PostgreSQL migration/concurrency verification needs the team's database environment.
