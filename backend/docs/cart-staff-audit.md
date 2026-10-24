# Staff assistance and administrator basket recount

This workflow uses the existing `/hubs/cart` connection and **SessionUpdated** event.
There is no employee account/assignment workflow in this feature: an administrator
sees the request, sends an employee to the cart, starts the recount when they arrive,
and commits the reviewed result. Device credentials remain device credentials;
the administrator uses their own JWT on their own screen. Administrative commands
are HTTP only; the administrator's SignalR connection subscribes to updates.

## States and live data

`Verified / Pending -> NeedsStaff -> Auditing -> Verified`

- `NeedsStaff` freezes all ordinary financial changes, including scanner purchases.
  Physical evidence continues to be recorded without changing the invoice/Pending.
- `Auditing` also freezes ordinary accounting. Camera Enter/Exit evidence is recorded
  as NoChange; it is never queued for later financial processing.
- The session remains Open. Checkout and ordinary remove/cancel-removal commands are
  blocked throughout staff review and recount.
- A normal exit question is Pending, not NeedsStaff: shopping continues with FIFO
  exit settlement. RequestStaff or a contradictory/impossible allocation enters
  staff review. Starting a recount leaves the old questions recorded; successful
  Commit closes the entire queue, replaces the invoice and clears both unknown
  mass and the unallocated-exit balance. Rejected/failed Commit leaves them intact.
- `staffRequestedAt`, `staffRequestReason` and `audit` travel in every full session.
  The audit includes its ID, initiating administrator,
  last stable weight, scanned pieces with names/prices, and draft total.
- Admins invoke `JoinSessionsGroup`; initially/reconnecting they fetch `GET /api/sessions`.
  Derive the open help queue and UI notifications from these session states. There
  is no separate durable notification stream or backend-generated toast text.

## Commands

All commands return the same `CartMovementResult` with a full session and publish
SessionUpdated after committing. The common `CartAuditCommand` body is:

```json
{
  "eventId": "new-message-guid",
  "movementId": "new-decision-guid",
  "sequence": 12,
  "deviceBootId": "caller-boot-guid",
  "expectedVersion": "latest-session-version-string",
  "auditId": null,
  "auditItemId": null,
  "reason": null
}
```

`sequence` is the next **session** sequence, shared with device reports. Commands
must be serialized against the latest snapshot. DeviceBootId is correlation data,
not administrator authentication. On lost acknowledgement retry the exact body/IDs.
On a definite version/sequence rejection, synchronize before constructing a new command.
The server creates all draft/invoice piece IDs and the audit ID.

`CartAuditCommand` is the administrator's request body. The HTTP route selects the
action (`start`, `remove-item`, or `commit`); the client does not send CartAuditAction.
`auditId` identifies the active recount; `auditItemId` identifies a mistaken draft
scan for remove-item. `CartAuditResponse` is the response snapshot inside `session.audit`:

| Response field | Purpose |
| --- | --- |
| id | Server-created recount ID; include it in audit scans and administrator commands after Start. |
| startedByUserId / startedAtUtc | Who began the recount and when. |
| lastStableWeightG | Most recent stable basket reading during this recount. |
| items | Active draft scans with IDs, variant/barcode, names, stored price, weight and percentage. |
| totalMinor | Sum of the draft's unit prices; it becomes invoice total only after Commit. |

No active recount means `session.audit` is null. Successful Commit also returns null
for the active recount; the completed audit remains stored for review.

| Transport / operation | Caller | Extra fields / effect |
| --- | --- | --- |
| Hub RequestStaff(sessionId, command) | Joined device bound to this session | Optional reason (max 200); records help request and freezes accounting. |
| HTTP POST audit/start | Admin JWT | Requires NeedsStaff; creates an empty draft. Start **before** removing goods. |
| HTTP POST audit/remove-item | Admin JWT | auditId and auditItemId; removes a mistaken scan from the draft. Fresh final weight required afterward. |
| HTTP POST audit/commit | Admin JWT | auditId; validates the final stable weight and atomically replaces the invoice with the reviewed draft. |

Admin endpoints are under `/api/v1/admin/sessions/{sessionId}`:
`POST staff-request`, `POST audit/start`,
`POST audit/remove-item`, `POST audit/commit`.
All require the Admin policy. JWT identity is recorded by the server; no body field
can impersonate an administrator. Device-only callers cannot start/approve a recount.

## Employee procedure and device reports

1. Request help; administrator sends an employee to this cart.
2. Administrator begins the audit. The cart receives the new state and audit ID.
3. Employee empties the basket into the review area. Remove/exclude personal belongings
   and ensure the normal empty-basket tare is correct. Do not tare over existing goods.
4. Employee scans **each piece** and returns it to the basket. Scanning is available
   immediately after Start; there is no ConfirmEmpty phase or empty-weight button.
   The cart uses normal
   ReportCartMovement Scan observations, adding the active auditId to the request.
   These create draft pieces, not Unconfirmed purchases, and need no camera Enter.
5. Administrator watches the draft and corrects accidental extra scans with `POST audit/remove-item`.
   To add a missing piece, scan it once. Distinct actual scans count separate pieces;
   network retries with the original eventId never duplicate a scan.
6. Cart sends the final stable weight-only report after all scanning/corrections.
   A weight-only report has `items: []`, the active `auditId` and a stable Weight object.
   Its capture timestamp must follow counted scans. Weight evidence must be at most
   30 seconds old at approval; request a fresh reading if necessary.
7. Employee confirms the physical contents to the administrator. Administrator reviews
   and commits. Matching weight is an additional check, not proof of product identity.

```javascript
await connection.invoke("ReportCartMovement", session.sessionId, {
  eventId: crypto.randomUUID(), movementId: crypto.randomUUID(),
  sequence: session.lastMovementSequence + 1, deviceBootId,
  startedAtUtc, endedAtUtc, isComplete: true,
  auditId: session.audit.id,
  items: [{ action: 0, source: 1, barcode: "622..." }]
});
```

Start immediately enables audit scans. Scans without the current audit ID
are not counted during review. Keep capture timestamps in UTC, synchronize the Pi
clock, and preserve timestamps/IDs when retrying. Do not retimestamp queued reports.
After commit, reports wholly captured before the last audit completion are retained
as non-financial evidence; they cannot become ordinary purchases. New shopping reports
use no audit ID and are captured after approval. A report starting during the audit
and ending after approval cannot be safely split: the server requests staff review
with `AUDIT_BOUNDARY_MOVEMENT` rather than silently ignoring new physical changes.
Reports using an expired/unknown audit ID and ending after approval likewise request
staff review with `STALE_AUDIT_CONTEXT`. Checkout remains blocked in both cases.
Refresh the device's mode from the latest session and retain capture metadata on retries.

## Commit, interruptions and prices

Commit verifies the latest session Version, active audit ID, fresh stable weight
after the last scan/correction, and the sum of percentage-based package weight ranges.
It then, in one database save:

- Retires old active invoice units without deleting their history.
- Adds counted draft units as Inside, once; recalculates the invoice total.
- Retains original purchase prices for matching prior units, oldest first. Extra
  counted pieces use current catalog prices. All barcodes must exist in the catalog;
  an inactive but physically present SKU can be counted by this administrator-reviewed workflow.
- Resolves prior pending reports, clears Pending and staff request, records the
  committing administrator/time, and restores Verified.

The draft and completed audit remain stored. Reconnection retrieves the open draft.
Disconnect, failed weight check, stale decision or failed save never unlock the cart,
zero Pending, or partially change the invoice. Staff review ends only after successful
approval through `POST audit/commit`.
There is no automatic unlock timeout or payment capture in this workflow.

## Weight tolerance

`weightG` stays on the Variant and represents **gross packaged** weight, including
the container. Product create/update accepts nullable `weightTolerancePercent`
(0..100, up to two decimal places). Every Variant of that Product uses this percentage
against its own weight. `null` uses `CartMovement:DefaultWeightTolerancePercent`
(default 3%). Zero is an explicit 0% override.

Admin Product responses return the saved `weightTolerancePercent` and the resolved
`effectiveWeightTolerancePercent`, including in listings and details. The frontend
can display the effective percentage and calculate its gram equivalent for each
Variant: `variant.weightG * product.effectiveWeightTolerancePercent / 100`.
There are no catalog min/max fields and no per-Variant tolerance override.

For example, 3% gives ±3g on a 100g Variant and ±15g on a 500g Variant. Internal
checks round fractional gram allowances up to whole grams. Invoice, movement and
audit items save the effective percentage alongside their nominal weight; later
Product/configuration edits do not rewrite already recorded pieces.

The cart sends `beforeG`, `afterG` and `isStable`. Tolerance is entirely server-owned:
the Product percentage or the global default. There is no device-requested allowance
and no extra fixed gram margin.

Calculate nominal weight ± percentage for each relevant unit, then sum the ranges;
negate/swap bounds for exits. Never apply the percentage to the net weight delta:
entering 500g and removing 100g at 3% gives 400g ±18g.
Residual checks account for variation of already recognized goods. Ordinary variation
inside this range does not create unknown Pending grams. Unknown residuals keep a
`pendingWeightUncertaintyG` margin; exit candidates/confirmation include this margin
and the recorded residual uncertainty. Pending grams remain an estimate, not piece counts.

The percentage is a configurable matching policy, not evidence of a measured
distribution. Existing pre-migration pieces keep 0% package allowance;
new pieces resolve the Product/default setting. Missing
historical nominal weights still require staff review. Weight can support a weak
barcode candidate only when the residual isolates it; equal weights do not establish
identity. The same percentage-based checks apply to small and large pieces.

Integration tests exercise software, persistence, authorization and replay. They do
not calibrate scales, validate model confidence, or guarantee detection of products
whose weights overlap within measurement uncertainty.
