using Microsoft.Extensions.Options;
using SARE.Application.Common;
using SARE.Application.Services;

namespace SARE.Api.Tests;

public sealed class CartWeightPolicyTests
{
    [Fact]
    public void ProductOverrideAndZeroTakePrecedenceOverTheConfiguredDefault()
    {
        var policy = new CartWeightPolicy(Options.Create(new CartMovementOptions
        {
            DefaultWeightTolerancePercent = 7.5m
        }));
        Assert.Equal(7.5m, policy.ResolvePercent(null));
        Assert.Equal(3m, policy.ResolvePercent(3m));
        Assert.Equal(0m, policy.ResolvePercent(0m));
    }

    [Theory]
    [InlineData(100, 97, 103)]
    [InlineData(500, 485, 515)]
    [InlineData(50, 48, 52)]
    public void TheSameProductPercentScalesWithEachVariantWeight(int weight, long min, long max)
    {
        var range = CartWeightRange.For(weight, 3m);
        Assert.Equal(min, range.Min);
        Assert.Equal(max, range.Max);
    }

    [Fact]
    public void SimultaneousEnterAndExitKeepBothAllowancesInsteadOfUsingTheNetDelta()
    {
        var range = CartWeightRange.For(500, 3m) + CartWeightRange.For(100, 3m).Negative();
        // صافي الوزن 400g، لكن السماحية 15 + 3 وليست 3% من 400.
        Assert.True(range.Contains(382));
        Assert.True(range.Contains(418));
        Assert.False(range.Contains(419));
        Assert.False(range.Contains(433));
    }
}
