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
        if (report.ExitPendingWeightG > 0) return true; // نفس الباركود قد يخص قطعة غير محسوبة داخل المعلق.
        var remaining = selected.ToList();
        foreach (var observation in IdentifiedExits(report))
        {
            var unit = remaining.FirstOrDefault(item => item.Barcode == observation.DetectedBarcode
                && (!observation.TargetSessionItemId.HasValue || item.Id == observation.TargetSessionItemId));
            if (unit is null) return false;
            remaining.Remove(unit);
        }
        return true;
    }

    private static List<CartMovementItem> IdentifiedExits(CartMovement report) => report.Items
        .Where(item => item.Action == CartItemAction.Exit && item.Effect == CartItemEffect.None && item.VariantId.HasValue
            && item.ReasonCode is null or "WEIGHT_SUPPORTED" or "INVOICE_OR_PENDING").ToList();

    public static bool SelectionLeavesLaterEvidence(IEnumerable<SessionItem> invoice, CartMovement report,
        IReadOnlyList<SessionItem> selected, IEnumerable<CartMovement> queue)
    {
        var pendingPart = Math.Clamp(report.UnconfirmedExitWeightG!.Value
            - selected.Sum(item => (long)item.ExpectedWeightG!.Value), 0, report.ExitPendingWeightG);
        var remaining = invoice.Where(item => item.RemovedAt == null && item.BasketPresence == BasketPresence.Inside
            && !selected.Any(unit => unit.Id == item.Id)).ToList();
        var requiredCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var later in queue.Where(item => IsOpen(item) && item.Sequence > report.Sequence).OrderBy(item => item.Sequence))
        {
            // المعلق قد يحتوي نفس الصنف؛ نستخدم الرؤية اللاحقة كقيد على الفاتورة عندما لا يبقى معلق عندها.
            if (later.ExitPendingWeightG - pendingPart > 0) continue;
            foreach (var observation in IdentifiedExits(later))
            {
                var barcode = observation.DetectedBarcode!;
                requiredCounts[barcode] = requiredCounts.GetValueOrDefault(barcode) + 1;
                var available = remaining.Where(item => item.Barcode == barcode && item.PlacementSequence <= later.Sequence).ToList();
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
        var observations = report.Items.Where(item => item.Action == CartItemAction.Exit && item.Effect == CartItemEffect.None).ToList();
        var identified = IdentifiedExits(report);
        var weight = report.UnconfirmedExitWeightG!.Value;
        var pending = report.ExitPendingWeightG;
        var required = new List<Guid>();

        // بسلة بلا Pending، التعرف الواضح يحدد القطع؛ الكاميرا لا تتحول لاختيار باركود آخر بنفس الوزن.
        if (pending == 0)
        {
            foreach (var observation in identified.OrderByDescending(item => item.TargetSessionItemId.HasValue))
            {
                var candidates = eligible.Where(item => item.Barcode == observation.DetectedBarcode && !required.Contains(item.Id))
                    .Where(item => SelectionLeavesLaterEvidence(all, report,
                        eligible.Where(unit => required.Contains(unit.Id)).Append(item).ToList(), queue)).ToList();
                // أزرار الإزالة التي ضغطت لاحقًا لا تغير ترتيب أسعار خروج مسجل بالفعل.
                var unit = CartMovementProcessor.ChooseExit(observation, report, candidates, preferRemovalIntent: false);
                if (unit is null) return ([], true, false);
                required.Add(unit.Id);
            }
            var known = eligible.Where(item => required.Contains(item.Id)).ToList();
            if (identified.Count > 0 && identified.Count == observations.Count
                && CartWeightPolicy.Sum(known.Select(CartMovementProcessor.UnitRange)).Expand(report.ExitWeightUncertaintyG).Contains(weight))
                return (SelectionLeavesLaterEvidence(all, report, known, queue)
                    ? [new(required, known.Sum(item => item.ExpectedWeightG!.Value), 0)] : [], true, false);
        }

        var matches = CartExitChoiceBuilder.Find(eligible, weight, pending,
            report.ExitWeightUncertaintyG, report.ExitPendingUncertaintyG, required,
            ids => SelectionLeavesLaterEvidence(all, report, eligible.Where(item => ids.Contains(item.Id)).ToList(), queue));
        var choices = matches.Choices;
        var knownUnbilled = identified.Count > 0 && identified.Count == observations.Count
            && identified.All(observation => !eligible.Any(item => item.Barcode == observation.DetectedBarcode));
        var pendingOnly = pending > 0 && weight <= (long)pending + report.ExitPendingUncertaintyG + report.ExitWeightUncertaintyG
            && (knownUnbilled || matches.Complete && choices.Count > 0 && choices.All(choice => choice.SessionItemIds.Count == 0));
        return (choices, matches.Complete, pendingOnly);
    }
}
