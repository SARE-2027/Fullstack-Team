using SARE.Domain.Enums;

namespace SARE.Domain.Cart;

public class Cart
{
    public string Id { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public CartStatus Status { get; set; } = CartStatus.Active;
    public short? BatteryPct { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public string? SwVersion { get; set; }

}
