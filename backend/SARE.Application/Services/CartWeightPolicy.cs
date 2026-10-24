using Microsoft.Extensions.Options;
using SARE.Application.Common;

namespace SARE.Application.Services;

// النسبة تخص وزن كل قطعة، وليس صافي فرق الميزان عند دخول وخروج قطع معًا.
public readonly record struct CartWeightRange(long Min, long Max)
{
    public static CartWeightRange For(int? expected, decimal tolerancePercent)
    {
        var weight = expected ?? 0;
        var allowance = (long)decimal.Ceiling(weight * tolerancePercent / 100m);
        return new(Math.Max(0, weight - allowance), weight + allowance);
    }

    public static CartWeightRange operator +(CartWeightRange a, CartWeightRange b) => new(a.Min + b.Min, a.Max + b.Max);
    public static CartWeightRange operator -(CartWeightRange a, CartWeightRange b) => new(a.Min - b.Max, a.Max - b.Min);
    public CartWeightRange Negative() => new(-Max, -Min);
    public CartWeightRange Expand(int grams) => new(Min - grams, Max + grams);
    public bool Contains(long grams) => grams >= Min && grams <= Max;
}

public sealed class CartWeightPolicy(IOptions<CartMovementOptions> options)
{
    public decimal ResolvePercent(decimal? productPercent) => productPercent ?? options.Value.DefaultWeightTolerancePercent;

    public static CartWeightRange Sum(IEnumerable<CartWeightRange> ranges) =>
        ranges.Aggregate(new CartWeightRange(0, 0), (total, range) => total + range);
}
