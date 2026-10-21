using SARE.Domain.Common;
using SARE.Domain.Enums;

namespace SARE.Domain.Cart;

public class CartMovementItem : BaseEntity
{
    public Guid MovementId { get; set; }
    public int Position { get; set; }
    public CartItemAction Action { get; set; }
    public CartItemEffect Effect { get; set; }
    public Guid? VariantId { get; set; }
    public int? ExpectedWeightG { get; set; }
    public int? UnitPriceMinor { get; set; }
    public Guid? SessionItemId { get; set; }
    public Guid? TargetSessionItemId { get; set; }
    public CartItemSource Source { get; set; }
    public string? DetectedBarcode { get; set; }
    public float? Confidence { get; set; }
    public string? TrackId { get; set; }
    public string? CameraId { get; set; }
    public string? ReasonCode { get; set; }
    public CartItemOutcome Outcome { get; set; } = CartItemOutcome.Unknown;
    public DateTime CreatedAt { get; set; }
}
