using SARE.Application.DTOs.Cart;
using SARE.Domain.Cart;
using SARE.Domain.Enums;

namespace SARE.Application.Services;

// ترشيحات محدودة؛ توقف البحث لا يثبت أن التوليفة وحيدة أو أن الخارج كله Pending.
public static class CartExitChoiceBuilder
{
    private sealed record Unit(Guid Id, Guid VariantId, int Price, int Weight, decimal TolerancePercent, DateTime AddedAt);

    public static (IReadOnlyList<CartExitChoiceResponse> Choices, bool Complete) Find(IEnumerable<SessionItem> items,
        int weight, int pending, int exitWeightUncertainty, int pendingUncertainty, IReadOnlyList<Guid> requiredIds,
        Func<IReadOnlyList<Guid>, bool> allowedSelection) => Search(items
            .Where(item => item.RemovedAt == null && item.BasketPresence == BasketPresence.Inside && item.ExpectedWeightG > 0)
            .Select(item => new Unit(item.Id, item.VariantId, item.UnitPriceMinor, item.ExpectedWeightG!.Value,
                item.WeightTolerancePercent, item.AddedAt)), weight, pending, exitWeightUncertainty, pendingUncertainty,
                requiredIds, allowedSelection);

    private static (IReadOnlyList<CartExitChoiceResponse> Choices, bool Complete) Search(IEnumerable<Unit> units,
        int weight, int pending, int exitWeightUncertainty, int pendingUncertainty, IReadOnlyList<Guid> requiredIds,
        Func<IReadOnlyList<Guid>, bool> allowedSelection)
    {
        var all = units.ToArray();
        var selected = all.Where(item => requiredIds.Contains(item.Id)).ToList();
        if (selected.Count != requiredIds.Count) return ([], true);
        var available = all.Where(item => !requiredIds.Contains(item.Id))
            .OrderByDescending(item => item.Weight).ThenBy(item => item.AddedAt).ThenBy(item => item.Id).ToArray();
        var choices = new List<CartExitChoiceResponse>();
        var signatures = new HashSet<string>(StringComparer.Ordinal);
        var steps = 0;
        var complete = true;
        Visit(0, selected.Sum(item => item.Weight),
            CartWeightPolicy.Sum(selected.Select(item => CartWeightRange.For(item.Weight, item.TolerancePercent))));
        return (choices, complete);

        void Visit(int start, int total, CartWeightRange range)
        {
            if (++steps > 10_000 || choices.Count >= 5)
            {
                complete = false;
                return;
            }
            var pendingPart = weight - total;
            var possibleWeight = range + new CartWeightRange(0, pending > 0 ? (long)pending + pendingUncertainty : 0);
            if ((selected.Count > 0 || pending > 0)
                && possibleWeight.Expand(exitWeightUncertainty).Contains(weight))
            {
                // الوحدات المتطابقة كميةً وسعرًا ووزنًا تمثل اختيارًا ماليًا واحدًا.
                var key = string.Join(";", selected.GroupBy(item => (item.VariantId, item.Price, item.Weight, item.TolerancePercent))
                    .OrderBy(group => group.Key).Select(group => $"{group.Key}:{group.Count()}"));
                var ids = selected.Select(item => item.Id).ToArray();
                // نفحص صلاحية القطع قبل دمج الاختيارات المتساوية ماليًا؛ قد تكون إحداها محجوزة لدليل لاحق.
                if (allowedSelection(ids) && signatures.Add(key))
                    choices.Add(new(ids, total, Math.Clamp(pendingPart, 0, pending)));
            }
            if (selected.Count >= 10)
            {
                if (start < available.Length) complete = false;
                return;
            }
            for (var index = start; index < available.Length; index++)
            {
                if (steps >= 10_000 || choices.Count >= 5)
                {
                    complete = false;
                    break;
                }
                var next = (long)total + available[index].Weight;
                var nextRange = range + CartWeightRange.For(available[index].Weight, available[index].TolerancePercent);
                if (nextRange.Min > (long)weight + exitWeightUncertainty) continue;
                selected.Add(available[index]);
                Visit(index + 1, (int)next, nextRange);
                selected.RemoveAt(selected.Count - 1);
            }
        }
    }
}
