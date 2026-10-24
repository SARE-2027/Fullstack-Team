using SARE.Application.DTOs.Cart;
using SARE.Domain.Cart;
using SARE.Domain.Enums;

namespace SARE.Application.Services;

// نفس قواعد الاختيار للرد وللحسم؛ الأسئلة لا تستخدم قطعًا دخلت بعد حركتها.
public static class CartExitQueue
{
    public static bool IsOpen(CartMovement report) => report.Status == CartMovementStatus.NeedsConfirmation
        && report.UnconfirmedExitWeightG.HasValue;

    public static CartMovement? First(IEnumerable<CartMovement> reports) => reports.Where(IsOpen)
        .OrderBy(report => report.Sequence).FirstOrDefault();

    public static List<SessionItem> Eligible(IEnumerable<SessionItem> invoice, CartMovement report) => invoice
        .Where(item => item.RemovedAt == null && item.BasketPresence == BasketPresence.Inside
            && item.PlacementSequence <= report.Sequence).ToList();

    public static bool SelectionMatchesIdentification(IReadOnlyList<SessionItem> selected, CartMovement report)
    {
        var identified = IdentifiedExits(report);
        var remaining = selected.ToList();
        long identifiedPendingMinimum = 0;
        foreach (var observation in identified.OrderByDescending(item => item.TargetSessionItemId.HasValue))
        {
            var unit = remaining.FirstOrDefault(item => item.Barcode == observation.DetectedBarcode
                && (!observation.TargetSessionItemId.HasValue || item.Id == observation.TargetSessionItemId));
            if (unit is not null) remaining.Remove(unit);
            else if (report.ExitPendingWeightG == 0) return false;
            else identifiedPendingMinimum += CartWeightRange.For(observation.ExpectedWeightG, observation.WeightTolerancePercent).Min;
        }

        // لو كل الخروج معروف ووزنه مفسَّر، لا نعرض أصنافًا أخرى لمجرد تساوي وزنها.
        var knownRange = CartWeightPolicy.Sum(identified.Select(item =>
            CartWeightRange.For(item.ExpectedWeightG, item.WeightTolerancePercent)));
        if (identified.Count > 0 && identified.Count == OpenExitObservations(report).Count
            && knownRange.Expand(report.ExitWeightUncertaintyG).Contains(report.UnconfirmedExitWeightG!.Value)
            && remaining.Count > 0)
            return false;

        // أي جزء معلوم لم نخصمه من الفاتورة يجب أن تتسع له كتلة المعلق الخارجة فعليًا.
        var invoiceRange = CartWeightPolicy.Sum(selected.Select(CartMovementProcessor.UnitRange));
        var remainingExitMaximum = (long)report.UnconfirmedExitWeightG!.Value + report.ExitWeightUncertaintyG - invoiceRange.Min;
        return identifiedPendingMinimum <= Math.Min(remainingExitMaximum,
            report.ExitPendingWeightG > 0 ? (long)report.ExitPendingWeightG + report.ExitPendingUncertaintyG : 0);
    }

    public static bool IsKnownInvoiceExit(IReadOnlyList<SessionItem> selected, CartMovement report)
    {
        var identified = IdentifiedExits(report);
        if (identified.Count == 0 || identified.Count != OpenExitObservations(report).Count || selected.Count != identified.Count)
            return false;
        var remaining = selected.ToList();
        foreach (var observation in identified.OrderByDescending(item => item.TargetSessionItemId.HasValue))
        {
            var unit = remaining.FirstOrDefault(item => item.Barcode == observation.DetectedBarcode
                && (!observation.TargetSessionItemId.HasValue || item.Id == observation.TargetSessionItemId));
            if (unit is null) return false;
            remaining.Remove(unit);
        }
        return CartWeightPolicy.Sum(selected.Select(CartMovementProcessor.UnitRange))
            .Expand(report.ExitWeightUncertaintyG).Contains(report.UnconfirmedExitWeightG!.Value);
    }

    public static int PendingPart(IReadOnlyList<SessionItem> selected, CartMovement report)
    {
        // خروج الصنف المعروف المطابق لا يستهلك المعلق بسبب فرق عبوة داخل السماحية.
        if (IsKnownInvoiceExit(selected, report)) return 0;
        return (int)Math.Clamp(report.UnconfirmedExitWeightG!.Value
            - selected.Sum(item => (long)item.ExpectedWeightG!.Value), 0, report.ExitPendingWeightG);
    }

    private static List<CartMovementItem> OpenExitObservations(CartMovement report) => report.Items
        .Where(item => item.Action == CartItemAction.Exit && item.Effect == CartItemEffect.None).ToList();

    private static List<CartMovementItem> IdentifiedExits(CartMovement report) => report.Items
        .Where(item => item.Action == CartItemAction.Exit && item.Effect == CartItemEffect.None && item.VariantId.HasValue
            && item.ReasonCode is null or "WEIGHT_SUPPORTED" or "INVOICE_OR_PENDING").ToList();

    public static bool SelectionLeavesLaterEvidence(IEnumerable<SessionItem> invoice, CartMovement report,
        IReadOnlyList<SessionItem> selected, IEnumerable<CartMovement> queue)
    {
        var pendingPart = PendingPart(selected, report);
        var remaining = invoice.Where(item => item.RemovedAt == null && item.BasketPresence == BasketPresence.Inside
            && !selected.Any(unit => unit.Id == item.Id)).ToList();
        var requiredCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var later in queue.Where(item => IsOpen(item) && item.Sequence > report.Sequence).OrderBy(item => item.Sequence))
        {
            var laterPending = Math.Max(0, later.ExitPendingWeightG - pendingPart);
            foreach (var observation in IdentifiedExits(later))
            {
                var barcode = observation.DetectedBarcode!;
                var available = remaining.Where(item => item.Barcode == barcode && item.PlacementSequence <= later.Sequence).ToList();
                var unit = CartMovementProcessor.ChooseExit(observation, later, available, preferRemovalIntent: false);
                // نحتفظ أيضًا بالصنف اللاحق الذي لا يغطيه المعلق المتبقي، حتى لو لم يصبح رصيده صفرًا.
                if (!RequiresInvoiceUnit(observation, later, unit, laterPending)) continue;
                requiredCounts[barcode] = requiredCounts.GetValueOrDefault(barcode) + 1;
                if (available.Count < requiredCounts[barcode]
                    || observation.TargetSessionItemId.HasValue && available.All(item => item.Id != observation.TargetSessionItemId))
                    return false;
            }
        }
        return true;
    }

    public static (IReadOnlyList<CartExitChoiceResponse> Choices, bool Complete, bool PendingOnly) Find(
        IEnumerable<SessionItem> invoice, CartMovement report, IEnumerable<CartMovement> queue)
    {
        var all = invoice.ToList();
        var eligible = Eligible(all, report);
        var observations = OpenExitObservations(report);
        var identified = IdentifiedExits(report);
        var weight = report.UnconfirmedExitWeightG!.Value;
        var pending = report.ExitPendingWeightG;
        var required = new List<Guid>();

        // نختار وحدة الفاتورة بترتيب الشراء إذا كان المعلق لا يستطيع تفسير خروجها.
        foreach (var observation in identified.OrderByDescending(item => item.TargetSessionItemId.HasValue))
        {
            var candidates = eligible.Where(item => item.Barcode == observation.DetectedBarcode && !required.Contains(item.Id))
                .Where(item => SelectionLeavesLaterEvidence(all, report,
                    eligible.Where(unit => required.Contains(unit.Id)).Append(item).ToList(), queue)).ToList();
            // أزرار الإزالة التي ضغطت لاحقًا لا تغير ترتيب أسعار خروج مسجل بالفعل.
            var unit = CartMovementProcessor.ChooseExit(observation, report, candidates, preferRemovalIntent: false);
            if (!RequiresInvoiceUnit(observation, report, unit, pending)) continue;
            if (unit is null) return ([], true, false);
            required.Add(unit.Id);
        }
        var known = eligible.Where(item => required.Contains(item.Id)).ToList();
        if (IsKnownInvoiceExit(known, report))
            return (SelectionLeavesLaterEvidence(all, report, known, queue)
                ? [new(required, known.Sum(item => item.ExpectedWeightG!.Value), 0)] : [], true, false);

        var matches = CartExitChoiceBuilder.Find(eligible, weight, pending,
            report.ExitWeightUncertaintyG, report.ExitPendingUncertaintyG, required,
            ids =>
            {
                var selected = eligible.Where(item => ids.Contains(item.Id)).ToList();
                return SelectionMatchesIdentification(selected, report) && SelectionLeavesLaterEvidence(all, report, selected, queue);
            });
        var choices = matches.Choices.Select(choice => choice with
        {
            PendingWeightG = PendingPart(eligible.Where(item => choice.SessionItemIds.Contains(item.Id)).ToList(), report)
        }).ToList();
        var knownUnbilled = identified.Count > 0 && identified.Count == observations.Count
            && identified.All(observation => !eligible.Any(item => item.Barcode == observation.DetectedBarcode));
        var pendingOnly = pending > 0 && weight <= (long)pending + report.ExitPendingUncertaintyG + report.ExitWeightUncertaintyG
            && (knownUnbilled || matches.Complete && choices.Count > 0 && choices.All(choice => choice.SessionItemIds.Count == 0));
        return (choices, matches.Complete, pendingOnly);
    }

    private static bool RequiresInvoiceUnit(CartMovementItem observation, CartMovement report, SessionItem? unit, int pending)
    {
        if (pending == 0) return true;
        var range = unit is not null ? CartMovementProcessor.UnitRange(unit)
            : CartWeightRange.For(observation.ExpectedWeightG, observation.WeightTolerancePercent);
        var pendingMaximum = (long)pending + report.ExitPendingUncertaintyG + report.ExitWeightUncertaintyG;
        return range.Min > pendingMaximum || IdentifiedExits(report).Count == 1 && OpenExitObservations(report).Count == 1
            && report.UnconfirmedExitWeightG!.Value > pendingMaximum
            && range.Expand(report.ExitWeightUncertaintyG).Contains(report.UnconfirmedExitWeightG.Value);
    }
}
