namespace SARE.Application.Common;

public sealed class CartMovementOptions
{
    public double MinimumVisionConfidence { get; set; } = 0.80;
    public int WeightToleranceGrams { get; set; } = 15;
}
