# Cart SignalR contract

SignalR endpoint: `/hubs/cart`. Use a SignalR client; raw WebSocket JSON does not
invoke hub methods. Commands use application services, the same database and full
session DTO as HTTP. See [cart-movements.md](cart-movements.md) for the current
finished-movement input, examples, business rules and manual recovery/retry semantics.

## Connection and session state

1. Register `connection.on("SessionUpdated", handler)` before connecting.
2. Connect. An unprovisioned device invokes `RegisterCart({})` and persists its
   returned identity/credential, then invokes `JoinCartGroup(cartId, hardwareToken)`.
3. Join returns the full open session or null. It subscribes before reading.
4. Replace only the affected session on each SessionUpdated, using sessionId/version.
5. After reconnect, rejoin and synchronize again. Group membership is not restored automatically.

The hardware credential belongs to the device, not the customer. Commands recheck
it and session ownership on every invocation. Start/close/disconnect are separate:
closing a session keeps the device connected for telemetry and the next shopper.
A live connection does not prove the displayed session is open; read its status.
Customer ownership/history is separate work. See [cart registration](cart-registration.md).

## Device commands

All except RegisterCart require a successful cart join. Argument order below is
the exact positional order used by the client's `invoke` call.

| Method | Arguments | Return |
| --- | --- | --- |
| RegisterCart | `{}` or `{ swVersion: "pi-v1" }`; optional version, max 32 trimmed characters. | cartId, raw hardwareToken, string status, nullable swVersion. Persist, then join. |
| JoinCartGroup | cartId (max 32 characters), hardwareToken. | Full open CartSummaryResponse or null. Switching carts unsubscribes the previous one. |
| StartSession | `{}` or `{ nfcUid: "registered-uid" }`. Optional NFC max 64 characters, must identify an active user. | Full newly opened session. Cart identity comes from the connection. |
| ReportCartMovement | sessionId, CartMovementRequest. [Exact fields](cart-movements.md). | eventId, nextSequence, movement decisions and full session. Only completed reports with stable physical weights are accepted. |
| ConfirmExitChoice | sessionId, `{ eventId, movementId, sequence, deviceBootId, pendingMovementId, expectedVersion, sessionItemIds, needsStaff? }`. Set pendingMovementId to session.currentExitMovementId. | Full movement/session result; settles the oldest exit and any uniquely resolved successors. [Rules and examples](cart-movements.md). |
| RequestStaff | sessionId, CartAuditCommand. | Records a help request, freezes accounting and publishes the full session. [Staff contract](cart-staff-audit.md). |
| RemoveSessionItem | sessionId, itemId from invoice items. | Full session; removal intent only. |
| CancelItemRemoval | sessionId, itemId. | Full session; clears removal intent. |
| UpdateCartTelemetry | `{ batteryPct?, swVersion?, status? }`. Battery 0..100, version max 32, status Active=0/Disabled=1. | cartId, status, batteryPct, nextReportIntervalSeconds, acknowledgedAt. |
| CloseSession | sessionId. | Full closed session. Pending mass, unresolved exits, staff review, missing Inside weights and removal requests block closure. |
| LeaveCartGroup | cartId. | No result; removes subscriptions and command access for the bound cart. |

RegisterCart is currently open to anonymous callers and does not join the cart.
An enrollment toggle is deferred. Unprovisioned/invalid credentials cannot read a
cart's session. Replaced credentials cannot continue issuing commands.

## Admin/dashboard subscriptions

These use the connection's JWT and do not require a device join. Configure
`accessTokenFactory` or the client's equivalent.

| Method | Arguments | Policy and result |
| --- | --- | --- |
| JoinSessionsGroup | None. | Admin; subscribes to SessionUpdated for all carts. Initial collection: GET /api/sessions. |
| LeaveSessionsGroup | None. | Admin; unsubscribes from all-session updates. |
Administrative mutations use **HTTP endpoints only**. The hub has no administrator
recount methods, even when connected with an Admin JWT. Admin role
checks here protect subscription to all sessions; devices need their hardware
credential and bound session, not a customer JWT or an Admin role.

For administrative operations under `/api/v1/admin/sessions/{sessionId}`, use
`POST audit/start`, `audit/remove-item`, `audit/commit`,
and `staff-request`. Exact bodies, scanner routing
and approval procedure: [cart-staff-audit.md](cart-staff-audit.md).
They publish the same SessionUpdated after saving. Device audit reports add
`auditId` to the usual CartMovementRequest.

## Reply versus event

```javascript
connection.on("SessionUpdated", session => updateSession(session));
await connection.start();
const current = await connection.invoke("JoinCartGroup", cartId, hardwareToken);
const session = current ?? await connection.invoke("StartSession", {});
const result = await connection.invoke("ReportCartMovement", session.sessionId, finishedReport);
// Await acknowledges this command. SessionUpdated updates all subscribed screens.
```

A return completes only the originating invoke. After committing a change, the
service publishes SessionUpdated to the cart and subscribed Admins, including the
caller when subscribed. Clients may observe event/reply in either order. Merge
by sessionId/version and preserve newer state. Do not display an older session
because a delayed acknowledgement mentions it.

All HTTP reads, join/start/close replies, result.session and SessionUpdated use
CartSummaryResponse. Fields and lifecycle handling: [session-realtime.md](session-realtime.md).
SessionUpdated replaces the old SessionStarted/CartUpdated/SessionClosed events.
CheckoutSuccess and LowBatteryWarning remain separate. Catalog/dashboard realtime
and dedicated fraud events are deferred; their unused prototype handlers are removed. Uncertain movement
identity/weight is represented as NeedsConfirmation, not automatically as fraud.

## Failures and retries

Known application errors appear as safe SignalR invocation errors with a code,
not HTTP status responses. Examples: CART_NOT_JOINED, INVALID_CART_TOKEN,
SESSION_NOT_ACCESSIBLE, VALIDATION_FAILED, EVENT_ID_REUSED,
MOVEMENT_ALREADY_RECORDED and SEQUENCE_GAP.

Persist/retry the exact original request after lost acknowledgements. Shopping continues
while exit questions queue in Sequence order; the UI shows only currentExitMovementId
with its exitChoices. Answering a later exit fails with EXIT_ORDER_REQUIRED. One
answer may settle several subsequent reports, so replace the whole returned session
and present the new head rather than advancing a local question index. Impossible
cases use staff recovery. No clarification-scan command exists; the device cannot rewrite old reports. Replay returns
current session state, including after closure, without broadcasting again.
Start/close have no event IDs; after a lost reply, join/read to discover their result.

The old ProcessItemDetection method is removed. HTTP item processing uses
POST /api/cart/{sessionId}/movements; the former /items route is removed.
Notifications are best effort with snapshot recovery, not a durable replay log.

## Internal helpers

- SessionCartGroup builds the internal group name.
- RequireCartAsync verifies the joined device against its current credential.
- RequireCartSessionAsync enforces the bound cart/session boundary.
- ExecuteAsync maps known errors to safe invocation errors.

One open session per cart is enforced by the filtered ux_sessions_open_cart index,
including concurrent start requests. Apply migrations through
AddCartPendingFlow, AddCartWeightToleranceAndStaffAudit and AddCartExitQueue before using the new
protocol; see the movement guide. These are required schema changes, not applied
automatically by documentation or migration generation.
