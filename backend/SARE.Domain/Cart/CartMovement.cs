using SARE.Domain.Common;
using SARE.Domain.Enums;

namespace SARE.Domain.Cart;

public class CartMovement : BaseEntity
{
    public Guid SessionId { get; set; }
    public int Sequence { get; set; }
    public Guid DeviceBootId { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime EndedAtUtc { get; set; }
    public int? WeightBeforeG { get; set; }
    public int? WeightAfterG { get; set; }
    public int WeightToleranceG { get; set; }
    public bool HasBasketChange { get; set; }
    public string? ModelVersion { get; set; }
    public CartMovementStatus Status { get; set; } = CartMovementStatus.NeedsConfirmation;
    public string? ReasonCode { get; set; }
    public ICollection<CartMovementItem> Items { get; set; } = new List<CartMovementItem>();
}
