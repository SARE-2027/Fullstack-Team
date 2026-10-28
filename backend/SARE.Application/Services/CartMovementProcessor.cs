using Microsoft.Extensions.Options;
using SARE.Application.Common;
using SARE.Application.DTOs.Cart;
using SARE.Domain.Cart;
using SARE.Domain.Enums;

namespace SARE.Application.Services;

// قرار الهوية لكل item؛ المعلق رصيد كتلة للسلة، وليس قائمة منتجات نخمن عددها.
public sealed class CartMovementProcessor(IOptions<CartMovementOptions> options)
{
    private readonly CartWeightPolicy weightPolicy = new(options);
    private sealed record Change(CartMovementItem Item, SessionItem? Unit, CartItemEffect Effect);

    public IReadOnlyList<SessionItem> Process(Session session, CartMovement movement,
        List<SessionItem> invoice, Dictionary<string, CartVariantData> catalog, IReadOnlyList<CartMovement> exitQueue)
    {
        var exitQuestionOpen = exitQueue.Any(CartExitQueue.IsOpen);
        foreach (var item in movement.Items) Identify(item, catalog);
        movement.PendingWeightBeforeG = session.PendingWeightG;
        var changes = new List<Change>();
        var additions = new List<SessionItem>();
        var inside = invoice.Where(item => item.RemovedAt == null && item.BasketPresence == BasketPresence.Inside).ToList();

        // السكان شراء مستقل، حتى لو كان في نفس الحركة دخول مجهول أو كانت هناك مراجعة مفتوحة.
        foreach (var scan in movement.Items.Where(item => item.Action == CartItemAction.Scan))
        {
            if (scan.ReasonCode is not null || !catalog[scan.DetectedBarcode!].IsAvailable)
            {
                scan.Outcome = CartItemOutcome.Rejected;
                scan.ReasonCode ??= "PRODUCT_UNAVAILABLE";
                continue;
            }
            var unit = NewUnit(session.Id, scan, catalog[scan.DetectedBarcode!], BasketPresence.Unconfirmed);
            additions.Add(unit);
            changes.Add(new(scan, unit, CartItemEffect.Added));
        }
        var cancelled = movement.Items.Where(item => item.Action == CartItemAction.CancelScan)
            .Select(item => item.TargetSessionItemId).ToHashSet();
        var waiting = invoice.Where(item => item.RemovedAt == null).OrderBy(item => item.AddedAt).ThenBy(item => item.Id)
            .Concat(additions).Where(item => item.Source == CartItemSource.Scanner
                && item.BasketPresence == BasketPresence.Unconfirmed && !cancelled.Contains(item.Id)).ToList();
        var roundTrips = RoundTrips(movement.Items);
        changes.AddRange(roundTrips.Select(item => new Change(item, null, CartItemEffect.NoChange)));
        var unknownEntries = new List<CartMovementItem>();
        var exits = new List<CartMovementItem>();
        string? itemProblem = null;

        foreach (var item in movement.Items.Where(item => item.Action == CartItemAction.Enter).Except(roundTrips))
        {
            if (item.ReasonCode is not null || !StageEntry(session.Id, item, catalog, waiting, changes, additions))
                unknownEntries.Add(item);
        }
        foreach (var item in movement.Items.Where(item => item.Action == CartItemAction.Exit).Except(roundTrips))
        {
            if (item.ReasonCode is not null || exitQuestionOpen)
            {
                exits.Add(item);
                continue;
            }
            var candidates = ExitCandidates(item, inside, changes);
            var selected = ChooseExit(item, movement, candidates);
            if (item.TargetSessionItemId.HasValue && selected is null)
                itemProblem ??= item.ReasonCode = "ITEM_NOT_IN_SESSION";
            else if (session.PendingWeightG > 0)
            {
                // مع وجود مجهول، رؤية نفس الباركود لا تثبت أن الوحدة المحسوبة هي التي خرجت.
                item.ReasonCode = candidates.Count > 0 ? "INVOICE_OR_PENDING" : null;
                exits.Add(item);
            }
            else if (selected is not null)
                changes.Add(new(item, selected, CartItemEffect.Removed));
            else
                itemProblem ??= item.ReasonCode = "ITEM_NOT_IN_SESSION";
        }
        foreach (var item in movement.Items.Where(item => item.Action == CartItemAction.CancelScan))
        {
            var unit = invoice.SingleOrDefault(unit => unit.Id == item.TargetSessionItemId);
            if (unit is null || unit.Barcode != item.DetectedBarcode)
                itemProblem ??= item.ReasonCode = "ITEM_NOT_IN_SESSION";
            else if (unit.RemovedAt.HasValue || changes.Any(change => change.Unit == unit && change.Effect == CartItemEffect.Removed))
                changes.Add(new(item, unit, CartItemEffect.NoChange));
            else if (unit.BasketPresence != BasketPresence.Unconfirmed
                || changes.Any(change => change.Unit == unit && change.Effect == CartItemEffect.PlacementConfirmed))
                itemProblem ??= item.ReasonCode = "PHYSICAL_EXIT_REQUIRED";
            else
                changes.Add(new(item, unit, CartItemEffect.Removed));
        }

        // كل سماحية مشتقة من نسبة المنتج المحفوظة لكل قطعة؛ الجهاز يرسل قراءات الوزن فقط.
        var actual = movement.HasBasketChange ? (long)movement.WeightAfterG!.Value - movement.WeightBeforeG!.Value : 0;
        var beforeRange = CartWeightPolicy.Sum(inside.Select(UnitRange))
            + new CartWeightRange(Math.Max(0, session.PendingWeightG - session.PendingWeightUncertaintyG),
                (long)session.PendingWeightG + session.PendingWeightUncertaintyG);
        var outstanding = exitQueue.Where(CartExitQueue.IsOpen).ToList();
        var outstandingWeight = outstanding.Sum(report => (long)report.UnconfirmedExitWeightG!.Value);
        var outstandingError = outstanding.Sum(report => (long)report.ExitWeightUncertaintyG);
        beforeRange -= new CartWeightRange(outstandingWeight - outstandingError, outstandingWeight + outstandingError);
        var missingWeight = inside.Any(unit => !unit.ExpectedWeightG.HasValue)
            || changes.Any(change => change.Item.Action == CartItemAction.Enter && change.Unit is not null
                && !change.Unit.ExpectedWeightG.HasValue);
        var baselineValid = !movement.HasBasketChange || !missingWeight
            && beforeRange.Contains(movement.WeightBeforeG!.Value);

        // نستخدم المتبقي لدعم مرشح ضعيف واحد فقط. الباركود والثقة الأصليان لا يتغيران.
        var uncertain = unknownEntries.Concat(exits.Where(item => item.ReasonCode == "LOW_CONFIDENCE")).ToList();
        if (baselineValid && uncertain.Count == 1 && unknownEntries.Count + exits.Count == 1
            && uncertain[0] is
            {
                ReasonCode: "LOW_CONFIDENCE", Confidence: not null, ExpectedWeightG: > 0
            } weak)
        {
            var existing = weak.Action == CartItemAction.Enter ? waiting.FirstOrDefault(unit => unit.Barcode == weak.DetectedBarcode)
                : ChooseExit(weak, movement, ExitCandidates(weak, inside, changes));
            var candidateRange = existing is not null ? UnitRange(existing)
                : CartWeightRange.For(weak.ExpectedWeightG, weak.WeightTolerancePercent);
            var expectedRange = CartWeightPolicy.Sum(changes.Select(PhysicalRange))
                + (weak.Action == CartItemAction.Enter ? candidateRange : candidateRange.Negative());
            if (expectedRange.Contains(actual) && actual != 0)
            {
                if (weak.Action == CartItemAction.Enter && StageEntry(session.Id, weak, catalog, waiting, changes, additions))
                {
                    unknownEntries.Remove(weak);
                    weak.ReasonCode = "WEIGHT_SUPPORTED";
                }
                else if (weak.Action == CartItemAction.Exit)
                {
                    weak.ReasonCode = "WEIGHT_SUPPORTED";
                    if (!exitQuestionOpen && session.PendingWeightG == 0 && existing is not null)
                    {
                        exits.Remove(weak);
                        changes.Add(new(weak, existing, CartItemEffect.Removed));
                    }
                }
            }
        }

        var remainder = actual - changes.Sum(PhysicalWeight);
        var physicalRange = CartWeightPolicy.Sum(changes.Select(PhysicalRange));
        var remainderRange = new CartWeightRange(actual, actual) - physicalRange;
        var physicalUncertainty = (int)Math.Max(Math.Abs(remainderRange.Min - remainder), Math.Abs(remainderRange.Max - remainder));
        Apply(changes, additions, invoice, movement);
        if (itemProblem is not null) return Review(session, movement, itemProblem, additions);
        if (!movement.HasBasketChange)
        {
            movement.Status = CartMovementStatus.Accepted;
            return additions;
        }
        if (!baselineValid)
            return Review(session, movement, missingWeight
                ? "WEIGHT_SNAPSHOT_MISSING" : "WEIGHT_CONTINUITY_MISMATCH", additions);
        // قد لا ترصد الكاميرا بعض القطع أصلًا؛ الفرق الموجب غير المفسر يظل Pending بلا عدد مفترض.
        if (unknownEntries.Count == 0 && exits.Count == 0 && remainderRange.Min > 0)
            unknownEntries.Add(new CartMovementItem
            {
                ReasonCode = "UNIDENTIFIED_WEIGHT"
            });
        // المتبقي السالب خروج لم ترصده الكاميرا، حتى لو غطاه دخول معروف وصافي الحركة موجب أو صفر.
        // نحتاج محتويات سابقة يمكن أن تخرج؛ لا نخترع خروجًا من سلة كانت فارغة لتبرير دخول لم يزِد الوزن.
        if (unknownEntries.Count == 0 && exits.Count == 0 && remainderRange.Max < 0
            && (inside.Count > 0 || session.PendingWeightG > 0))
            exits.Add(new CartMovementItem
            {
                ReasonCode = "UNIDENTIFIED_EXIT"
            });
        // قراءة بلا تغير أو تفسير مقابل لا تثبت عبور قطعة لها وزن، حتى لو كانت القطعة صغيرة.
        if (actual == 0 && changes.Sum(PhysicalWeight) != 0 && unknownEntries.Count == 0 && exits.Count == 0)
            return Review(session, movement, "MOVEMENT_WEIGHT_MISMATCH", additions);
        if (unknownEntries.Count > 0 && exits.Count > 0)
            return Review(session, movement, "MIXED_UNIDENTIFIED_CHANGES", additions);

        if (exits.Count > 0)
            QueueExit(session, movement, -remainder, physicalUncertainty);
        else if (unknownEntries.Count > 0)
        {
            var pending = (long)session.PendingWeightG + remainder;
            if (remainder <= 0 || pending > 1_000_000)
                return Review(session, movement, "MOVEMENT_WEIGHT_MISMATCH", additions);
            session.PendingWeightG = (int)pending;
            session.PendingWeightUncertaintyG = Math.Min(1_000_000,
                session.PendingWeightUncertaintyG + physicalUncertainty);
            session.BasketState = BasketState.Pending;
            movement.ReasonCode = unknownEntries[0].ReasonCode;
            movement.Status = CartMovementStatus.NeedsConfirmation;
            foreach (var item in unknownEntries) item.Effect = CartItemEffect.PendingAdded;
        }
        else if (!physicalRange.Contains(actual))
            return Review(session, movement, "MOVEMENT_WEIGHT_MISMATCH", additions);
        else
        {
            // تحريك منتج معروف لا يصفر رصيدًا قديمًا صغيرًا داخل السماحية.
            session.BasketState = session.PendingWeightG == 0 && !exitQuestionOpen ? BasketState.Verified : BasketState.Pending;
            movement.Status = CartMovementStatus.Accepted;
        }
        movement.PendingWeightAfterG = session.PendingWeightG;
        return additions;
    }

    private static void QueueExit(Session session, CartMovement movement, long weight, int uncertainty)
    {
        if (weight <= 0 || weight > 1_000_000)
        {
            Review(session, movement, "EXIT_WEIGHT_MISMATCH", []);
            return;
        }
        // الوزن خرج بالفعل؛ هوية القطع والخصم يتحسمان من رأس الطابور فقط.
        movement.UnconfirmedExitWeightG = (int)weight;
        movement.ExitWeightUncertaintyG = uncertainty;
        movement.ExitPendingWeightG = session.PendingWeightG;
        movement.ExitPendingUncertaintyG = session.PendingWeightUncertaintyG;
        movement.Status = CartMovementStatus.NeedsConfirmation;
        movement.ReasonCode = "SELECT_EXIT_ITEMS";
        session.BasketState = BasketState.Pending;
    }
    private static bool StageEntry(Guid sessionId, CartMovementItem item, Dictionary<string, CartVariantData> catalog,
        List<SessionItem> waiting, List<Change> changes, List<SessionItem> additions)
    {
        var held = waiting.FirstOrDefault(unit => unit.Barcode == item.DetectedBarcode);
        if (held is not null)
        {
            waiting.Remove(held);
            changes.Add(new(item, held, CartItemEffect.PlacementConfirmed));
            return true;
        }
        var variant = catalog[item.DetectedBarcode!];
        if (!variant.IsAvailable)
        {
            item.ReasonCode = "PRODUCT_UNAVAILABLE";
            return false;
        }
        var unit = NewUnit(sessionId, item, variant, BasketPresence.Inside);
        additions.Add(unit);
        changes.Add(new(item, unit, CartItemEffect.Added));
        return true;
    }

    private static List<SessionItem> ExitCandidates(CartMovementItem item, List<SessionItem> inside, List<Change> changes) =>
        inside.Concat(changes.Where(change => change.Item.Action == CartItemAction.Enter).Select(change => change.Unit!))
            .DistinctBy(unit => unit.Id).Where(unit => unit.Barcode == item.DetectedBarcode
                && !changes.Any(change => change.Unit == unit && change.Effect == CartItemEffect.Removed)).ToList();

    internal static SessionItem? ChooseExit(CartMovementItem item, CartMovement movement, List<SessionItem> candidates,
        bool preferRemovalIntent = true)
    {
        if (item.TargetSessionItemId.HasValue) return candidates.SingleOrDefault(unit => unit.Id == item.TargetSessionItemId);
        var tracked = candidates.Where(unit => !string.IsNullOrWhiteSpace(item.TrackId)
            && unit.BasketTrackId == item.TrackId && unit.BasketCameraId == item.CameraId
            && unit.BasketDeviceBootId == movement.DeviceBootId).ToList();
        return tracked.Count == 1 ? tracked[0] : candidates.OrderByDescending(unit => preferRemovalIntent && unit.RemovalRequestedAt.HasValue)
            .ThenBy(unit => unit.AddedAt).ThenBy(unit => unit.Id).FirstOrDefault();
    }

    private void Identify(CartMovementItem item, Dictionary<string, CartVariantData> catalog)
    {
        if (string.IsNullOrWhiteSpace(item.DetectedBarcode)) item.ReasonCode = "ITEM_NOT_IDENTIFIED";
        else if (!catalog.TryGetValue(item.DetectedBarcode, out var variant)) item.ReasonCode = "UNKNOWN_BARCODE";
        else
        {
            item.VariantId = variant.Id;
            item.ExpectedWeightG = variant.WeightG;
            item.WeightTolerancePercent = weightPolicy.ResolvePercent(variant.WeightTolerancePercent);
            item.UnitPriceMinor = variant.PriceMinor;
            if (item.Source == CartItemSource.Vision && (!item.Confidence.HasValue || item.Confidence < options.Value.MinimumVisionConfidence))
                item.ReasonCode = "LOW_CONFIDENCE";
        }
    }

    public decimal ResolveTolerancePercent(decimal? productPercent) => weightPolicy.ResolvePercent(productPercent);

    public static CartWeightRange UnitRange(SessionItem unit) => CartWeightRange.For(unit.ExpectedWeightG, unit.WeightTolerancePercent);

    private static CartWeightRange PhysicalRange(Change change)
    {
        if (change.Item.Action == CartItemAction.Enter && change.Effect is CartItemEffect.Added or CartItemEffect.PlacementConfirmed)
            return UnitRange(change.Unit!);
        if (change.Item.Action == CartItemAction.Exit && change.Effect == CartItemEffect.Removed)
            return UnitRange(change.Unit!).Negative();
        return new(0, 0);
    }

    private static long PhysicalWeight(Change change)
    {
        if (change.Item.Action == CartItemAction.Enter
            && change.Effect is CartItemEffect.Added or CartItemEffect.PlacementConfirmed)
            return change.Unit!.ExpectedWeightG ?? 0;
        if (change.Item.Action == CartItemAction.Exit && change.Effect == CartItemEffect.Removed)
            return -(change.Unit!.ExpectedWeightG ?? 0);
        return 0; // السكان والإلغاء خارج السلة لا يدخلان في فرق الوزن.
    }

    private static void Apply(List<Change> changes, List<SessionItem> additions, List<SessionItem> invoice, CartMovement movement)
    {
        foreach (var change in changes)
        {
            change.Item.Outcome = CartItemOutcome.Accepted;
            change.Item.Effect = change.Effect;
            var unit = change.Unit;
            if (unit is null) continue;
            change.Item.SessionItemId = unit.Id;
            change.Item.VariantId = unit.VariantId;
            change.Item.UnitPriceMinor = unit.UnitPriceMinor;
            change.Item.ExpectedWeightG = unit.ExpectedWeightG;
            change.Item.WeightTolerancePercent = unit.WeightTolerancePercent;
            if (change.Item.Action == CartItemAction.Enter)
            {
                unit.BasketPresence = BasketPresence.Inside;
                unit.BasketTrackId = change.Item.TrackId;
                unit.BasketCameraId = change.Item.CameraId;
                unit.BasketDeviceBootId = movement.DeviceBootId;
                unit.PlacementSequence = movement.Sequence;
            }
            if (change.Effect == CartItemEffect.Removed) RemoveUnit(unit);
        }
        invoice.AddRange(additions);
    }

    public static void RemoveUnit(SessionItem unit)
    {
        unit.RemovedAt = DateTime.UtcNow;
        unit.BasketPresence = BasketPresence.Outside;
        unit.RemovalRequestedAt = null;
    }

    private static IReadOnlyList<SessionItem> Review(Session session, CartMovement movement, string reason, IReadOnlyList<SessionItem> additions)
    {
        session.BasketState = BasketState.NeedsStaff;
        movement.Status = CartMovementStatus.NeedsConfirmation;
        movement.ReasonCode = reason;
        return additions;
    }

    private static SessionItem NewUnit(Guid sessionId, CartMovementItem item, CartVariantData variant, BasketPresence presence) => new()
    {
        Id = Guid.NewGuid(),
        SessionId = sessionId,
        VariantId = variant.Id,
        Barcode = variant.Barcode,
        UnitPriceMinor = variant.PriceMinor,
        ExpectedWeightG = variant.WeightG,
        // Identify ثبت النسبة الفعلية من المنتج أو إعداد السيرفر قبل إنشاء القطعة.
        WeightTolerancePercent = item.WeightTolerancePercent,
        Source = item.Source,
        BasketPresence = presence,
        AddedAt = DateTime.UtcNow
    };

    private static List<CartMovementItem> RoundTrips(IEnumerable<CartMovementItem> items) => items
        .Where(item => item.Action is CartItemAction.Enter or CartItemAction.Exit && !string.IsNullOrWhiteSpace(item.TrackId) && !item.TargetSessionItemId.HasValue)
        .GroupBy(item => (item.CameraId, item.TrackId, item.DetectedBarcode))
        .Where(group => group.Count() == 2 && group.Count(item => item.Action == CartItemAction.Enter) == 1
            && group.Count(item => item.Action == CartItemAction.Exit) == 1).SelectMany(group => group).ToList();
}
