# Cart movements and Pending mass

## Packet and per-item decisions

The Pi sends one finished movement through `ReportCartMovement(sessionId, request)`
at `/hubs/cart`, or Admin HTTP `POST /api/cart/{sessionId}/movements`. The request
contains message IDs, session sequence, boot ID, UTC interval, observations,
and optional stable basket weight before/after. Enter/Exit require that weight.
Partial or unstable packets fail input validation before any writes.

Example: scan one piece, then place that same piece inside the basket. Replace the
barcode and weights with real catalog/device data; its packaged weight here is 100g.
Use current UTC capture times and the next sequence from the latest session.

```javascript
const result = await connection.invoke("ReportCartMovement", session.sessionId, {
  eventId: crypto.randomUUID(),
  movementId: crypto.randomUUID(),
  sequence: session.lastMovementSequence + 1,
  deviceBootId, // persisted for this device boot
  startedAtUtc,
  endedAtUtc,
  items: [
    { action: 0, source: 1, barcode: "6220000000001" },
    { action: 1, source: 0, barcode: "6220000000001", confidence: 0.97 }
  ],
  weight: { beforeG: 0, afterG: 100, isStable: true },
  isComplete: true,
  modelVersion: "vision-v1",
  auditId: null
});
// result: { eventId, nextSequence, movement, session }
```

For Scan-only reports, omit `weight`. During recount, send the active `session.audit.id`
as `auditId`; outside recount use null/omit it. `trackId` and `cameraId` are optional
observation metadata. The Pi never sends per-item weights, prices, tolerance values
or IDs for newly created invoice pieces. `sessionItemId` is an existing server ID
used when targeting an Exit or CancelScan.

The server generates movement-item and invoice-unit IDs. Barcode identifies the
variant, not a physical piece. Source is Vision=0 or Scanner=1. Actions are Scan=0,
Enter=1, Exit=2, CancelScan=3. Source/identity/confidence decisions belong to each item.

| Input | Decision |
| --- | --- |
| Scan | A new billed Unconfirmed unit, held outside expected basket mass. |
| High-confidence Enter | Accept the available variant independently (default threshold 0.90). |
| One low-confidence candidate | Compare its signed catalog/snapshot weight with the residual after known physical items. Record WEIGHT_SUPPORTED if it agrees; retain the original confidence. |
| Missing barcode, missing confidence, unknown barcode or unsupported weak candidate | Keep it unidentified. No SKU or quantity is invented from mass. |

Scanner observations contribute **zero** to the expected physical delta. Their later
Enter consumes the oldest waiting matching Scanner/Unconfirmed unit, including
scans from this packet in input order, without rebilling. Each waiting unit is used
once. CancelScan targets are excluded from placement within that packet. Once no
waiting unit remains, another Enter is another billed unit.

The configured vision confidence threshold is inclusive: a score of 0.90 meets the
default threshold of 0.90. Higher confidence does not identify any extra mass that
the camera failed to observe; that residual still follows the Pending rules.

Example: one Scan, two confident entries (100g and 200g), one weak 300g candidate:

- Scale delta 600g: residual is 300g, so the weak candidate can be supported.
- Scale delta 650g: residual is 350g, so that candidate remains unknown and Pending
  grows by 350g. The scan and two recognized entries remain billed.
- No barcode or confidence: no candidate rescue is attempted, even if a weight fits.

Several weak/unidentified items cannot be separated using their combined mass alone.
Known items in the same packet keep their decisions. Stored invoice price, barcode
and expected weight snapshots survive catalog edits or deactivation.

## Pending is mass, not a product list

`basket mass ≈ sum(Inside invoice snapshot weights) + pendingWeightG - unconfirmedExitWeightG`

`unconfirmedExitWeightG` on the session is the sum of exits already observed but
not yet allocated to invoice/Pending. Their physical mass leaves immediately;
billing waits for FIFO settlement. Do not interpret still-billed Inside rows as
proof that every piece is physically present while this exit balance is nonzero.

An unknown positive residual increases Pending by the actual unexplained grams.
The camera may miss pieces entirely; unexplained positive scale change is still
recorded without inventing item counts. Later known entries and ordinary scans
continue normally, and later unknown entries add to the same mass balance.

Example: the basket gains 300g and the camera identifies one 100g piece. The other
200g becomes Pending whether the camera reports two unknown observations, merges
them into one, or misses them entirely. The known piece remains billed; later
entries and placement of already-scanned pieces continue normally. `itemsCount`
counts invoice units only, not the unknown physical pieces inside Pending.

A negative residual after accounting for known crossings is a blind exit, even
when the whole movement gained weight or stayed unchanged. For example, a known
200g entry with a net gain of 100g leaves a 100g exit to resolve against the previous
basket contents. It follows the same exit queue and choice rules. A new entry into
an empty basket with no weight increase still requires review; the server does not
invent a matching unseen exit to accept it.

There is no customer clarification scan command. Administrator recount has a separate
draft and active audit ID; see [staff recount](cart-staff-audit.md).
The shopper removes unknown goods through the ordinary exit flow. After resolution,
scanning or entering goods again uses the normal purchase/placement rules.
A normal Scan never consumes Pending or identifies an unknown item already inside.

A known item remains accepted when a different observation fails. An impossible
mass balance, invalid unit reference, historical missing weight, or simultaneous
unidentified directions requires staff review; it does not erase independently
accepted item decisions. Staff review blocks further automatic physical accounting.
Once NeedsStaff is saved, subsequent scans and camera reports cannot change billing.

## Exit rules

| State and exit | Processing |
| --- | --- |
| No Pending, identified exit | Remove a matching Inside invoice unit, using explicit ID/unique scoped track or the oldest matching purchase (removal intent first). |
| No Pending, blind exit | Search invoice combinations. One financially distinct solution is automatic only after a complete search. Several solutions are shown to the shopper. |
| Pending, recognized SKU absent from Inside invoice | Consume its observed exit mass from Pending if the balance covers it; no invoice deduction. |
| Pending, known exit matches an invoice unit and exceeds the available Pending including uncertainty | Remove the matching invoice unit automatically when it is the unique complete interpretation and reaches the queue head. The matching known exit does not consume Pending. |
| Pending, recognized SKU present and Pending can explain its exit | Ask whether matching invoice units or unbilled contents left. Other SKUs are not alternatives merely because their weights agree. |
| Pending, blind exit no greater than the available Pending including uncertainty | Search invoice-plus-Pending combinations. If a complete search finds no possible invoice contribution, consume Pending only; otherwise offer Pending-only and compatible invoice/mixed choices. |
| Pending, blind exit greater than the available Pending including uncertainty | Offer compatible invoice-only or invoice-plus-partial-Pending choices. If none can explain the exit after a complete search, request staff; Pending alone cannot cover it. |

The combination search includes partial unknown mass. For example, a 100g exit with
Pending=200g and an invoice unit weighing 60g can mean that unit plus 40g of Pending.
Absence of a full 100g invoice match does not justify automatically consuming Pending.

Known-exit suggestions and submitted answers obey the same identity constraints.
When the recognized observations explain the full exit weight, selections contain
only those SKUs, up to their observed counts. A known observation assigned to Pending
must fit the Pending portion of the measured exit. An actual blind remainder still
uses invoice/Pending combinations. For several known pieces, the total exceeding
Pending does not prove that every piece was billed: one smaller piece can still
come from Pending while another comes from the invoice. Such alternatives remain a question.

For a known invoice-only exit that matches its package-weight range, variation
inside that range does not subtract nominal residual grams from Pending. Matching
duplicate invoice SKUs use the original explicit ID/scoped track or saved purchase
order, preserving stored prices. All automatic settlements still respect FIFO.

For an exit larger than Pending, the shopper may select invoice units that explain
some of the exit. Example: milk=1000g, Pending=200g, exit=1100g. Selecting milk removes
its charge and consumes 100g of Pending, leaving 100g unresolved.

Choices are bounded to 5 financially distinct suggestions, 10 units per suggested
combination and 10,000 search steps. A truncated search never proves uniqueness or
absence of a match. The client may select up to 500 distinct current invoice IDs
manually, or request staff assistance. Same-variant units with different purchase
prices/weights count as different financial combinations.

## One customer decision

```javascript
const result = await connection.invoke("ConfirmExitChoice", session.sessionId, {
  eventId: crypto.randomUUID(), movementId: crypto.randomUUID(),
  sequence: session.lastMovementSequence + 1, deviceBootId,
  pendingMovementId: session.currentExitMovementId,
  expectedVersion: session.version,
  sessionItemIds: selectedInvoiceIds, // [] asserts that only Pending contents left
  needsStaff: false
});
```

The server validates:

`pendingPart = recordedExitMass - selectedInvoiceSnapshotMass`

The result must lie in `0..Pending available at that exit ± effective tolerance`. Selected units
are removed once, and pendingPart is deducted from Pending. This is a recorded human
assertion, not independent physical proof. An inconsistent answer or needsStaff=true
records NeedsStaff without changing the invoice. A stale Version is rejected.
For a fully identified, matching invoice-only exit, pendingPart is zero; its package
variation within tolerance does not count as unknown contents removed.

Report metadata IDs are reused unchanged on network retries. Confirmation is a new
command with new event/movement IDs, tied to the saved exit report; it does not rerun
or rewrite its original physical observations. Resolved reports retain their evidence
and point to the decision through resolvedByMovementId.

### FIFO while shopping continues

Known entries, unknown entries, scans, scan cancellations and further exits continue
while an exit question waits. The device sends the same movement packet shape.
Once a question exists, later exits join its queue, including identified exits;
their financial deduction waits for earlier questions. A waiting question alone
does not request staff or put the cart in recount mode.

- `pendingMovements` retains queued reports, ordered by Sequence, as well as ordinary
  unknown-ingress reports. Queue entries have a non-null unconfirmedExitWeightG.
- `currentExitMovementId` identifies the oldest answerable exit (null when none, or
  while staff review/audit is active). `exitChoices` belongs **only to that report**.
- The client presents that question and submits its ID through ConfirmExitChoice.
  A later ID is rejected with EXIT_ORDER_REQUIRED without consuming sequence or money.
- Refresh/use the latest session Version when answering. A stale Version still fails.
- Candidates must have entered by that movement's Sequence. A scan placed later,
  a new duplicate SKU, or unknown mass added later cannot answer an older exit.
- Each exit retains its own available Pending balance and uncertainty. Settling an
  earlier exit reduces the balances of later queued exits; later unknown ingress
  never increases the available balance of an earlier question.
- After an answer, walk the queue in order. Apply unique complete interpretations
  automatically (including a now-unique invoice/Pending combination); stop at the
  next ambiguity. An initial mixed-Pending question still requires customer input
  unless Pending-only removal or the uniquely identified invoice-only rule resolves it.
- Recognized portions of a queued mixed exit remain constraints when Pending is
  zero. Matching total weight cannot replace a recognized SKU with an unrelated one.
- Later identified exits also restrict earlier choices when their available Pending
  cannot explain them, including when it is zero. For example, an earlier A-or-D blind exit followed by a recognized D exit
  leaves A for the first exit and can resolve both automatically. Reserve enough
  earlier-placed units for the later recognized evidence, including explicit unit
  targets; uncertain observations alone do not establish those constraints.
- Queued identified exits use saved purchase order (or the original explicit ID /
  scoped unique track). A removal button pressed after capture does not change
  which historical purchase price is selected.
- An impossible next exit requests staff; prior valid allocations remain recorded.
  NeedsStaff/Auditing still freeze ordinary financial changes. Checkout remains
  blocked while any question or unknown mass remains.

Example: invoice A=100g and C=300g, Pending=200g, scale=600g. Blind exit 200g
creates question 1 (Pending-only or A+100g Pending); blind exit 300g queues behind
it, scale=100g. Answer question 1 with Pending-only: Pending becomes zero, question
2 now has the unique C=300g interpretation and resolves automatically. The final
invoice contains only A. If unknown mass entered between those exits, its balance
would remain available to question 2; mass entering afterward belongs to neither.

Every settlement uses distinct current invoice-piece IDs. Identical SKUs may be
removed more than once only when they represent different purchased pieces. The
head decision, any automatic cascade, total, receipts and updated question balances
save atomically and publish one full SessionUpdated. Resolved reports link to that
decision through resolvedByMovementId; an exact retry never repeats the cascade.

HTTP equivalent: Admin `POST /api/cart/{sessionId}/exit-confirmation`, same body/result.
The hub verifies the joined device credential and that the session belongs to its cart.

Each `session.exitChoices` entry is one possible explanation of the saved exit:

| Field | Meaning |
| --- | --- |
| sessionItemIds | Current Inside invoice pieces selected by this option. Resolve names from session.items. |
| invoiceWeightG | Sum of those pieces' saved nominal weights. |
| pendingWeightG | Estimated unknown mass removed **by this option**, not the session's entire Pending balance. |

For Pending=200g and exit=1100g, an option can be milk=1000g plus Pending=100g.
Another option can be an invoice-only 1100g combination if one exists. Display the
options as choices, submit only selected IDs, and let the server recalculate the
weights. `needsStaff: true` represents the customer's request for assistance.

## Response and payment guards

All HTTP reads, hub replies and `SessionUpdated` contain the same full session:
invoice items, basketWeightG, expectedBasketWeightG, pendingWeightG, basketState,
exitChoices, currentExitMovementId, unconfirmedExitWeightG, pendingMovements,
canCheckout and the existing session metadata/Version.
States: Verified=0, Pending=1, NeedsStaff=2, Auditing=3. Pending includes unexplained mass or an
unanswered exit question; it is separate from the session being open/closed.

Each movement reply includes item outcomes, pending mass before/after, any
unconfirmedExitWeightG, decision links and confirmedByCustomer. Item IDs are server IDs.

Checkout/close require an open Verified session, zero Pending, known Inside snapshot
weights, no unresolved reports and no removal intent. The server enforces these guards;
clearing a warning in the UI does not clear the backend state. Unconfirmed billed scans
held outside alone do not block checkout. RemoveSessionItem records intent only;
CancelItemRemoval clears it; physical Exit or cancellation of a held scan changes billing.

### Invoice amounts

`unitPriceMinor`, `totalMinor` and audit totals use integer minor currency units
(for a currency with 100 minor units, 1000 represents 10.00). Each SessionItem is
one physical piece; repeated pieces have separate server IDs.

- Scan bills once as Unconfirmed; its later placement adds no second charge.
- Recognized new Enter bills once. Unknown Pending mass adds no charge.
- Removing a billed piece subtracts its stored purchase price, even if the catalog price changed.
- An unresolved exit choice keeps its charges until settlement. New purchases
  continue to add their own stored prices. Later queued exits deduct in FIFO order.
- NeedsStaff freezes normal accounting; audit scans change only the draft total.
- Audit Commit replaces the invoice once, retaining matching purchase prices and using
  current catalog prices for additional counted pieces. A failed commit leaves it unchanged.

Checkout currently closes/recalculates the session and emits CheckoutSuccess with
receipt information. Payment-provider integration and actual capture/refund are
separate work; CheckoutSuccess alone is not confirmation from a payment provider.

Administrator recovery uses the [staff recount](cart-staff-audit.md): request help,
start the audit, scan the physical contents into its draft, send a final stable
weight and approve with `POST audit/commit`. Only that approval replaces the invoice
and clears unresolved mass/questions. The old weight-only reconciliation endpoint
has been removed.

## Storage, tolerance and delivery

Gross weight stays on each Variant. Product `weightTolerancePercent` overrides the
global `CartMovement:DefaultWeightTolerancePercent` (3% by default); null falls back
and zero explicitly disables the package allowance. The effective percentage is
snapshotted per unit and applied to its weight before summing the movement/basket
ranges, including signed exits. The device sends the before/after readings and
stability flag only; there is no client tolerance field or extra fixed gram allowance.
Unknown grams retain pendingWeightUncertaintyG; see the
[weight/recount contract](cart-staff-audit.md). A known movement does not silently
clear an older small Pending balance. If an exit exhausts the estimated Pending
and no exit question remains, the final measured weight must still fit the known
Inside contents. Any positive excess beyond their weight range remains Pending:
the server re-estimates its mass from the current reading instead of hiding it
by marking the basket Verified. No item count or invoice charge is added.
Real sensors and model confidence need calibration;
matching weight is not proof of SKU identity, and model score is not a probability guarantee.

Report, item changes, total and receipt save atomically. Session concurrency and unique
sequence/receipt constraints protect duplicate writes. Publish the full SessionUpdated
after commit. Exact retries return current session state, including after closure,
without saving or publishing again. Same eventId with changed contents is rejected.
Notifications are best effort; subscribe before connecting and rejoin/read snapshots
after reconnect. Persist and serialize the device delivery queue.

Apply migration `AddCartPendingFlow` after the committed migrations. Historical
unresolved reports move to NeedsStaff because their unknown mass cannot be safely
backfilled. Earlier experimental migrations are retained only in the saved stash.
Then apply `AddCartWeightToleranceAndStaffAudit` for Product percentages, help requests and recounts.
Finally apply `AddCartExitQueue` for placement sequences and per-question Pending balances;
it also removes the unused `reconciled_basket_weight_g` column. `basket_weight_g`
remains the latest observed basket weight.

```powershell
dotnet ef database update --project backend/SARE.Infrastructure --startup-project backend/SARE.Api
```

Migration generation/SQL inspection does not apply it to PostgreSQL. Integration tests
use SQLite and do not verify physical camera or sensor accuracy. Shelf correlation,
payments, customer ownership/history, and catalog/dashboard realtime remain separate work.
