# Cart connection and inbound commands

## Transport and initial state

The existing endpoint is **SignalR** at `/hubs/cart`. Use a SignalR-compatible
client; sending arbitrary JSON through a raw WebSocket does not invoke hub
methods. SignalR can use WebSockets in production. The integration tests use
its Long Polling transport with TestServer, exercising the actual protocol,
authentication, hub calls, services, database writes, and notifications.

1. Register a `SessionUpdated` handler before connecting.
2. Connect and invoke `JoinCartGroup(cartId, hardwareToken)`.
3. Use its return value: the full open session with all active invoice items,
   or `null` if the cart has no open session. Closed history is not restored.
4. Replace the affected session when `SessionUpdated` arrives. The payload has
   the same fields as the HTTP session summary, including `items` and closure data.
5. After reconnecting, invoke `JoinCartGroup` again. SignalR group membership
   and the authenticated cart binding must be re-established on the new connection.

Keep snapshots and events by `sessionId` and `version`; preserve newer events
that arrive during the join call. Do not switch the UI back to an older session
because a delayed detection acknowledgement mentions it. `status` describes the
session lifecycle; a live socket or telemetry ping does not mean a session is open.
Disconnecting does not close a session, and closing a session does not disconnect
the device. It can stay connected for telemetry and the next shopper.

The hardware token is a **device credential**, not a customer browser credential.
The ordinary customer ownership/history flow is not implemented by these methods.
Admins use their JWT and `JoinSessionsGroup()` for all-session updates, and load
the initial collection through `GET /api/sessions`.

## Cart methods

All commands require a successful join. Cart identity comes from the connection,
not a client-supplied `cartId` on each message. Switching carts unsubscribes the
previous cart. Leaving removes command access. Commands recheck the device token
against the current hash, so a replaced credential cannot keep issuing commands.
Detection and close commands also check that the session belongs to that cart.

| Hub method | Arguments | Return value |
| --- | --- | --- |
| `JoinCartGroup` | `cartId`, `hardwareToken` | Full open `CartSummaryResponse` or `null`. |
| `StartSession` | `{ "nfcUid": null }` (or a registered NFC UID) | Full newly opened session. |
| `ProcessItemDetection` | `sessionId`, `{ "eventId": "<UUID>", "barcode": "ITEM-1", "measuredWeightG": 50, "source": 1 }` | Existing `CartItemProcessResult` acknowledgement. |
| `UpdateCartTelemetry` | `{ "batteryPct": 80, "swVersion": "cart-v1" }` | Cart status, battery, next report interval, acknowledgement time. |
| `CloseSession` | `sessionId` | Full closed session. |
| `LeaveCartGroup` | `cartId` | No result. |

`cartId` is required and limited to 32 characters. A cart must have a configured
token matching `hardwareToken`. Every method name in the table is the exact name
passed to the SignalR client's `invoke`; method arguments are positional.
`StartSession` still takes a request object, even when no NFC is provided: send
`{}` or `{ "nfcUid": null }`. An NFC UID, when provided, is limited to 64
characters and must resolve to an active user.

`UpdateCartTelemetry` accepts these optional, nullable fields:

| Field | Type / accepted values |
| --- | --- |
| `batteryPct` | Integer from 0 to 100. |
| `swVersion` | String, at most 32 characters. |
| `status` | Numeric cart enum: Active = 0, Disabled = 1. Omit to preserve the existing automatic battery handling. |

It returns `cartId`, string `status`, nullable `batteryPct`,
`nextReportIntervalSeconds`, and `acknowledgedAt`.

## Admin and dashboard methods

These methods do not require `JoinCartGroup`; they use the connection's JWT.
Configure the SignalR client's `accessTokenFactory` (or equivalent) with that JWT.

| Hub method | Arguments | Authorization | Result and subscription |
| --- | --- | --- | --- |
| `JoinSessionsGroup` | None. | Admin. | No result; subscribes to `SessionUpdated` for every cart. Load the initial collection through HTTP. |
| `LeaveSessionsGroup` | None. | Admin. | No result; unsubscribes from all-session updates. |
| `JoinDashboardGroup` | None. | Admin or Staff. | No result; subscribes to the existing dashboard group, currently used for `FraudAlert`. It does not return counts or publish catalog changes. |

## Invocation examples

The examples below assume `connection` and `adminConnection` are configured
SignalR clients. These are hub invocations, not HTTP URLs or Socket.IO `emit` calls.

```javascript
// Register this before connecting. Update the session identified by sessionId;
// use status and version as described in session-realtime.md.
connection.on("SessionUpdated", session => {
    console.log(session.sessionId, session.status, session.items);
});
await connection.start();

// At connection/reconnection: full open session, or null.
const current = await connection.invoke(
    "JoinCartGroup", "C-TEST", "<device-token>");

// Start only if the cart has no open session.
const session = current ?? await connection.invoke(
    "StartSession", { nfcUid: null });

// Generate once per physical detection. Retain this object and session ID
// if the invocation fails and this same detection needs to be retried.
const detection = {
    eventId: crypto.randomUUID(),
    barcode: "ITEM-1",
    measuredWeightG: 50,
    source: 1
};
const result = await connection.invoke(
    "ProcessItemDetection", session.sessionId, detection);

const telemetry = await connection.invoke(
    "UpdateCartTelemetry", { batteryPct: 80, swVersion: "cart-v1" });

const closed = await connection.invoke("CloseSession", session.sessionId);
await connection.invoke("LeaveCartGroup", "C-TEST");

// On a separate JWT-authenticated admin connection:
await adminConnection.start();
await adminConnection.invoke("JoinSessionsGroup");
await adminConnection.invoke("LeaveSessionsGroup");
await adminConnection.invoke("JoinDashboardGroup");
```

Automatic reconnect does not automatically repeat application group joins.
In the client's reconnect callback, invoke the appropriate join method again
and refresh the snapshot/HTTP collection.

## Direct replies and pushed events

The return value of a hub method completes the originating client's
`invoke` call only. The application service also publishes `SessionUpdated`
after a successful session change; subscribed admins and the cart receive it,
including the caller if subscribed. Those are two delivery paths for the same
committed state. Use the invocation result to acknowledge the operation and
the pushed event to update subscribers' session state. A lost event is recovered
through the join snapshot or the admin's HTTP synchronization.

The full session payload is shared by HTTP reads, join/start/close results,
accepted detection results in `result.session`, and `SessionUpdated`. Example:

```json
{
  "sessionId": "7d105e61-bfbb-4552-9fa2-58e8421e0708",
  "cartId": "C-TEST",
  "userId": null,
  "userName": null,
  "status": "open",
  "totalMinor": 100,
  "itemsCount": 1,
  "startedAt": "2026-10-12T09:00:00Z",
  "items": [
    {
      "id": "1a91c2df-5c94-4f42-8c95-7e9623b9b88f",
      "variantId": "d74a2860-7b03-45d1-83e8-b43dc743e74f",
      "productId": "278fe137-9070-4e10-a20a-d31de426425a",
      "barcode": "ITEM-1",
      "nameAr": "منتج",
      "nameEn": "Product",
      "imageUrl": "/images/product.jpg",
      "unitPriceMinor": 100,
      "expectedWeightG": 50,
      "source": 1,
      "addedAt": "2026-10-12T09:00:01Z"
    }
  ],
  "updatedAt": "2026-10-12T09:00:01Z",
  "version": "639273924010000000",
  "closedAt": null,
  "closedByUserId": null,
  "closeReason": null
}
```

See [session-realtime.md](session-realtime.md) for field meanings, price snapshots,
current catalog fields, status handling, and HTTP response-contract changes.

## Detection outcomes and events

`source` uses the existing numeric enum: Vision = 0, Scanner = 1, Manual = 2.
Barcode is required and limited to 64 characters; measured weight must be
nonnegative; source must be a known enum value. Session-start and telemetry
validation use the existing service validators. The device start DTO only
contains NFC: it cannot choose another cart ID. Device close cannot attribute
the action to an arbitrary administrative user.

The detection acknowledgement keeps its operation-result fields (`success`,
`message`, weight fields), with an optional full `CartSummaryResponse` in
`session`. Its session DTO is identical to the HTTP reads, join result and
**`SessionUpdated`** payload. A fresh accepted detection reads that DTO once,
returns it in the acknowledgement and emits it
to the cart and subscribed admins after saving. An already processed retry does
not emit another update. Calling `JoinCartGroup` can resynchronize full state.
Start, accepted detection, and close all publish this same `SessionUpdated`
event. The former `SessionStarted`, `CartUpdated`, and `SessionClosed` events
are no longer sent. Existing clients must move their session state handlers to
`SessionUpdated`. `CheckoutSuccess` and weight/battery alerts remain unchanged.

Rejected weight readings retain the existing `WeightMismatchWarning` for the
cart and `FraudAlert` for dashboard subscribers. They do not change the session
or its items, and repeated readings with the same event ID do not repeat alerts.
Telemetry updates the cart record; it does not invent a session item/update.

## Retry and concurrency rules

- Generate `eventId` once per physical detection and retain it while retrying.
  Generate a new ID for the next physical detection, even for the same barcode.
- Keep the original **sessionId**, barcode, weight and source when retrying.
  Reusing the same event ID with different content/session returns `EVENT_ID_REUSED`.
- The ID is stored as the existing detection event primary key. The item, event
  and session total are saved in one transaction. A duplicate that wins concurrently
  is read back after the losing transaction rolls back; it cannot add a second item.
- A replay acknowledges the persisted accepted/rejected outcome, even after the
  original session closes. It never applies an old detection to a new session.
  An accepted replay includes the original session's **current** full state;
  acknowledgements are not a historical byte-for-byte response cache.
- Rejected events do not store their original expected product weight. A replay
  therefore leaves `expectedWeightG` null instead of using a possibly changed
  catalog weight; the rejection and measured weight remain available.
- Existing HTTP item calls can optionally send `eventId`. Omitting it preserves
  their previous behavior; such requests cannot be deduplicated across retries.
- Start and close are not event-ID commands. After a lost acknowledgement, join
  again to discover the current session before deciding to retry them.

Errors from commands are SignalR invocation errors, not HTTP status responses.
Known application errors include an error code and safe message. A stale session
write produces `CONFLICT`; synchronize then retry the original detection with
the same ID. `CART_NOT_JOINED`, `INVALID_CART_TOKEN`, `SESSION_NOT_ACCESSIBLE`,
and `VALIDATION_FAILED` identify other actionable failures.

## Database invariant and migration

`ux_sessions_open_cart` is a unique index on `sessions.cart_id` filtered by
`status = 'open'`. It allows closed/abandoned history and enforces one open session
per cart even if simultaneous start requests both pass the application check.
The losing start becomes a conflict instead of an unhandled database error.

Migration: `20261011070733_EnsureSingleOpenSessionPerCart`. Apply through the
normal deployment migration step:

```powershell
dotnet ef database update --project backend/SARE.Infrastructure --startup-project backend/SARE.Api
```

If a database already contains duplicate open sessions for a cart, the migration
will fail until those records are reviewed. It does not close or delete history
automatically. The migration has been generated, not applied to a live database.

## Files and responsibilities

| File | Responsibility |
| --- | --- |
| `SARE.Api/Hubs/CartHub.cs` | Device authentication/binding, subscription, initial snapshot, inbound command routing, safe invocation errors. |
| `SARE.Application/DTOs/Cart/CartStartSessionRequest.cs` | Device NFC start input without a client-controlled cart identity. |
| `SARE.Application/DTOs/Cart/CartDtos.cs` | Optional stable detection ID in the existing request contract. |
| `SARE.Application/Validators/Cart/CartAddItemRequestValidator.cs` | Validate detection fields before any write. |
| `SARE.Application/Services/CartService.cs` | Existing detection business flow plus stored-outcome replay. |
| `SARE.Application/Common/Interfaces/ISessionRepository.cs` | Active snapshot and detection read/atomic-save contracts. |
| `SARE.Infrastructure/Persistence/Repositories/SessionRepository.cs` | Full active-session read, race handling, atomic detection saves. |
| `SARE.Infrastructure/Persistence/Configurations/Cart/SessionConfiguration.cs` | Database invariant; migration/designer/snapshot record the schema change. |
| `SARE.Api.Tests/CartHubCommandsTests.cs`, `CatalogApiFactory.cs` | Protocol integration tests and deterministic concurrent-write simulations. |

All mutations still run through the existing application services and notification
service. There is no second socket business implementation. Catalog realtime and
dashboard-count events remain deferred. Notifications remain best effort with
snapshot recovery; no durable outbox/event replay was added. Existing HTTP route
authorization is unchanged by this cart hub change.

## Internal hub helpers

These helpers are implementation details, not client commands:

- `SessionCartGroup(cartId)`: builds the internal cart-session group name.
- `RequireCartAsync()`: checks that this connection joined a cart and that its
  hardware credential is still valid.
- `RequireCartSessionAsync(cartId, sessionId)`: checks that the session belongs
  to the authenticated cart.
- `ExecuteAsync(command)`: translates known application/validation/concurrency
  errors into safe SignalR invocation errors.
