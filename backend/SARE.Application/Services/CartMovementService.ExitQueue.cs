using SARE.Application.DTOs.Cart;
using SARE.Domain.Cart;
using SARE.Domain.Enums;

namespace SARE.Application.Services;

public sealed partial class CartMovementService
{
    private static void ResolveExitQueue(Session session, List<CartMovement> reports,
        List<SessionItem> invoice, CartMovement decision, bool allowUniqueWithPending = false)
    {
        while (session.BasketState != BasketState.NeedsStaff && CartExitQueue.First(reports) is { } head)
        {
            var matches = CartExitQueue.Find(invoice, head, reports);
            if (matches.PendingOnly)
                SettleExit(session, head, [], reports, decision, "PENDING_WEIGHT_REMOVED");
            else if (matches.Complete && matches.Choices.Count == 1
                && (head.ExitPendingWeightG == 0 || allowUniqueWithPending))
            {
                var ids = matches.Choices[0].SessionItemIds;
                SettleExit(session, head, invoice.Where(item => ids.Contains(item.Id)).ToList(),
                    reports, decision, "UNIQUE_WEIGHT_MATCH");
            }
            else
            {
                if (matches.Complete && matches.Choices.Count == 0)
                {
                    session.BasketState = BasketState.NeedsStaff;
                    session.StaffRequestReason = decision.ReasonCode = head.ReasonCode = "NO_EXIT_MATCH";
                }
                break; // ننتظر إجابة أول سؤال فقط؛ لا نحسم سؤالًا متأخرًا قبله.
            }
        }
        RefreshBasketState(session, reports, decision);
    }

    private static void SettleExit(Session session, CartMovement report, IReadOnlyList<SessionItem> selected,
        List<CartMovement> reports, CartMovement decision, string reason)
    {
        var invoiceWeight = selected.Sum(item => (long)item.ExpectedWeightG!.Value);
        var pendingPart = (int)Math.Clamp(report.UnconfirmedExitWeightG!.Value - invoiceWeight, 0, report.ExitPendingWeightG);
        var range = CartWeightPolicy.Sum(selected.Select(CartMovementProcessor.UnitRange));
        var uncertainty = report.ExitWeightUncertaintyG
            + (int)Math.Max(Math.Abs(range.Min - invoiceWeight), Math.Abs(range.Max - invoiceWeight));

        foreach (var item in selected)
        {
            CartMovementProcessor.RemoveUnit(item);
            decision.Items.Add(new CartMovementItem
            {
                Id = Guid.NewGuid(), MovementId = decision.Id, Position = decision.Items.Count,
                Action = CartItemAction.Exit, Source = CartItemSource.Manual,
                Outcome = CartItemOutcome.Accepted, Effect = CartItemEffect.Removed,
                SessionItemId = item.Id, VariantId = item.VariantId, DetectedBarcode = item.Barcode,
                UnitPriceMinor = item.UnitPriceMinor, ExpectedWeightG = item.ExpectedWeightG,
                WeightTolerancePercent = item.WeightTolerancePercent, ReasonCode = reason
            });
        }
        session.PendingWeightG -= pendingPart;
        session.PendingWeightUncertaintyG = session.PendingWeightG == 0 ? 0
            : Math.Min(1_000_000, session.PendingWeightUncertaintyG + uncertainty);

        // الأسئلة اللاحقة كانت تحتوي الرصيد الأقدم؛ نطرح المستهلك دون إضافة مجهول دخل بعدها لسؤال سابق.
        foreach (var later in reports.Where(item => CartExitQueue.IsOpen(item) && item.Sequence > report.Sequence))
        {
            later.ExitPendingWeightG -= pendingPart;
            later.ExitPendingUncertaintyG = later.ExitPendingWeightG == 0 ? 0
                : Math.Min(1_000_000, later.ExitPendingUncertaintyG + uncertainty);
        }
        foreach (var observation in report.Items.Where(item => item.Action == CartItemAction.Exit && item.Effect == CartItemEffect.None))
        {
            observation.Outcome = CartItemOutcome.Accepted;
            observation.Effect = selected.Count == 0 ? CartItemEffect.PendingRemoved : CartItemEffect.NoChange;
        }
        report.Status = report.Id == decision.Id ? CartMovementStatus.Accepted : CartMovementStatus.Cancelled;
        report.ReasonCode = reason;
        report.ResolvedByMovementId = report.Id == decision.Id ? null : decision.Id;
        if (report.Id == decision.Id) report.UnconfirmedExitWeightG = null;
    }

    private static void RefreshBasketState(Session session, List<CartMovement> reports, CartMovement decision)
    {
        if (session.BasketState == BasketState.NeedsStaff) return;
        session.BasketState = session.PendingWeightG > 0 || reports.Any(CartExitQueue.IsOpen)
            ? BasketState.Pending : BasketState.Verified;
        if (session.BasketState != BasketState.Verified) return;
        foreach (var report in reports.Where(item => item.Status == CartMovementStatus.NeedsConfirmation))
        {
            CancelUnresolved(report, "RESOLVED_BY_EXIT");
            report.ResolvedByMovementId = decision.Id;
        }
    }
}
