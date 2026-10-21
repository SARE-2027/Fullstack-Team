# Session HTTP reads and live updates

See [cart-movements.md](cart-movements.md) for the current movement-based contract,
confirmed basket presence, pending item evidence and scanner/physical reconciliation.
HTTP individual-session routes require a device token or permitted Admin/Staff JWT.

## Load the initial state

`GET /api/sessions` requires an **Admin** JWT. It returns an array of all sessions
across all carts, including closed and abandoned sessions, newest first. This is
an unpaged endpoint for loading the current session collection in one request.
Session metadata, user names, and active invoice items are read together in one
database query; there is no query per session or per item.

`GET /api/sessions/{sessionId}`, `GET /api/cart/{sessionId}`,
`GET /api/carts/{cartId}/session`, and `GET /api/cart/active/{cartId}` all return
the same `CartSummaryResponse` for an individual session. Both HTTP start routes
and the hub start/close/join methods also use that session contract. Movement
acknowledgements keep their operation-result envelope, with the same full DTO
in `session` when present. Device credentials permit access to that cart only;
Admin JWTs permit management, Staff JWTs permit individual reads. The all-sessions
endpoint is admin-only. Ordinary customer ownership/history is separate work.

Each session contains:

- `sessionId`, `cartId`, `userId`, `userName`, and `status`.
- `totalMinor`, `itemsCount`, `startedAt`, and `items`.
- `updatedAt`, `version`, `closedAt`, `closedByUserId`, and `closeReason`.
- `lastMovementSequence`, `basketWeightG`, `expectedBasketWeightG`, and `pendingMovements`.

Each item contains its `id`, `variantId`, `productId`, `barcode`, Arabic and
English product names (`nameAr`, `nameEn`), nullable `imageUrl`, `unitPriceMinor`,
`expectedWeightG`, `source`, and `addedAt`. `source` uses the numeric CartItemSource
enum: Vision = 0, Scanner = 1, Manual = 2. Removed items are excluded. Each row
represents one added unit, matching the existing invoice model. The unit price
comes from the session item, preserving the price recorded when it was added.
Barcode, price and expected weight are now captured when adding a unit. Historical
rows may have null expected weight and fall back to today's barcode for display.
Names and images still come from the catalog. Full options/other variants are not
nested into invoice items. `basketPresence`, `removalRequestedAt`, movement counters
and full pending movement details are included; these fields require the movement migration.

The former `CartSessionResponseDto` and `LegacySessionItemDto` response contracts
are removed. Clients of `/api/cart` must use string session statuses (`open`,
`closed`, `abandoned`), `nameAr`/`nameEn` instead of `productName`, and the unified
fields above. The former per-row `quantity` was always 1 and is no longer sent.
The existing start request with `cartId` and optional `userId` is retained.
Session reads that are not found use the application's standard not-found error.

After a successful movement save, the service reads the full summary once,
publishes that DTO through `SessionUpdated`, and returns it in the operation
acknowledgement. Exact receipt retries return the same contract for the
original session's current state without publishing another update. Closing
recalculates the total from active items' stored prices in the database, without
loading product display data for the calculation.

`version` is the persisted `LastActivityAt` UTC timestamp expressed as ticks in a
string. This uses the existing timestamp and needs no database migration. Keep
versions per session and compare them with `BigInt` in JavaScript to preserve
precision. Ignore socket events whose version is older than or equal to the
stored version; repeated delivery should be safe. An HTTP snapshot can replace
an equal version, since it also refreshes current catalog labels.

## Subscribe to changes

Connect to `/hubs/cart` and listen for **`SessionUpdated`**. The payload is one
complete session DTO with all its active items, matching the HTTP response.

| Client | Subscribe | Receives |
| --- | --- | --- |
| Admin | `JoinSessionsGroup()` with an Admin JWT | Changes to every session. |
| Cart client | `JoinCartGroup(cartId, hardwareToken)` | Changes to that cart's sessions only. |

`LeaveSessionsGroup()` and `LeaveCartGroup(cartId)` unsubscribe. Anonymous users,
customers, and staff cannot join the all-sessions group. Cart subscriptions
require the existing hardware credential to match the cart's configured token;
an unprovisioned cart cannot expose its session data without a token.

`JoinCartGroup` now returns the complete **open** session with its items, or
`null` when there is no open session. It subscribes before reading the snapshot.
Repeat this call after every reconnect; historical sessions are not returned by
this method. A connection belongs to one cart at a time. See
[cart-signalr.md](cart-signalr.md) for inbound device commands and retry rules.

Admins receive `SessionUpdated` through `JoinSessionsGroup`, including when they
also join a cart for its separate alerts. The session admin and cart update groups
have separate membership so these subscriptions do not duplicate a session
update. Group names for this routing are internal; clients use the hub methods.

## When an update is published

| Successful operation | Session update |
| --- | --- |
| `POST /api/sessions` | Newly opened session with empty items. |
| `POST /api/cart/session/start` | Same update through the existing start flow. |
| `POST /api/cart/{sessionId}/movements` | Entire session after saving movement decisions. |
| `POST /api/sessions/{sessionId}/close` | Entire closed or abandoned session. |
| `POST /api/cart/{sessionId}/checkout` | Same closed-session update through the existing close flow. |
| Hub `StartSession` | Newly opened session. |
| Hub `ReportCartMovement` | Entire session after every saved report, including pending evidence. Each report changes the invoice atomically. |
| Hub `RemoveSessionItem`, `CancelItemRemoval` | Entire session after a change to removal intent. |
| Hub `ReconcileCartBasket` | Entire session after Admin recovery. |
| Hub `CloseSession` | Entire closed session. |

The application service saves first, reads the committed session, then calls
the notification service. Closing publishes only after the whole session/cart
transaction commits. Validation failures and rolled-back writes publish no
`SessionUpdated` event. Uncertain movements are visible in pendingMovements; uncertainty alone
does not accuse the customer of fraud.

Physical item entry/exit, scan cancellation and removal intent use the movement
hub commands. No session-delete endpoint is provided. Direct SQL writes do not
generate application notifications.

## Initial load and reconnection

1. Register the `SessionUpdated` handler and connect.
2. Admins join `JoinSessionsGroup`; devices call `JoinCartGroup`.
3. Admins load the current sessions over HTTP. Devices use the snapshot returned
   by `JoinCartGroup`. Merge by `sessionId` and `version`, preserving any newer
   update received while the request was running. A `null` device snapshot means
   there is no active session; clear any previously displayed closed session.
4. On each event, replace only that session; add it if its ID is new. Apply the
   screen's active/closed filters using its updated status.
5. After reconnection, rejoin and repeat the appropriate synchronization above.

SignalR notifications are best effort. A failed send is logged after the save;
HTTP reads or the device join snapshot recover the state after missed updates. There is
no durable event replay or outbox in this change.

`SessionUpdated` is the only session state event for both admins and carts.
The former `SessionStarted`, `SessionClosed`, and `CartUpdated` events are no
longer sent; clients subscribing to them must use `SessionUpdated` and read the
session's `status` instead. Its payload always includes all active items.
`CheckoutSuccess`, fraud/weight warnings, and low-battery alerts remain separate
events with their existing payloads.
Catalog and dashboard-count SignalR updates are deferred.
