using SARE.Domain.Common;

namespace SARE.Domain.Cart;

// Technical deduplication only. Business data belongs to the movement and its items.
public class CartEventReceipt : BaseEntity
{
    public Guid SessionId { get; set; }
    public Guid MovementId { get; set; }
    public int Sequence { get; set; }
    public Guid DeviceBootId { get; set; }
    public string PayloadHash { get; set; } = string.Empty;
    public DateTime ReceivedAtUtc { get; set; }
}
