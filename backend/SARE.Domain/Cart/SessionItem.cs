using SARE.Domain.Common;
using SARE.Domain.Enums;

namespace SARE.Domain.Cart;

public class SessionItem : BaseEntity
{
    public Guid SessionId { get; set; }
    public Guid VariantId { get; set; }
    public int UnitPriceMinor { get; set; }
    public int? ExpectedWeightG { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public BasketPresence BasketPresence { get; set; } = BasketPresence.Unconfirmed;
    public DateTime? RemovalRequestedAt { get; set; }
    public string? BasketTrackId { get; set; }
    public string? BasketCameraId { get; set; }
    public Guid? BasketDeviceBootId { get; set; }
    public CartItemSource Source { get; set; }
    public DateTime AddedAt { get; set; }
    public DateTime? RemovedAt { get; set; }

}
