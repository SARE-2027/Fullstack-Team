namespace SARE.Application.Common;

public sealed class CartMovementOptions
{
    public float MinimumVisionConfidence { get; set; } = 0.90f;
    public decimal DefaultWeightTolerancePercent { get; set; } = 3m;
}
