using SARE.Application.DTOs.Cart;

namespace SARE.Application.Common.Interfaces;

public interface ICartNotificationService
{
    Task NotifySessionStartedAsync(string cartId, CartSummaryResponse session, CancellationToken ct = default);
    Task NotifySessionClosedAsync(string cartId, CartSummaryResponse session, CancellationToken ct = default);
    Task NotifyLowBatteryAsync(string cartId, short batteryPct, CancellationToken ct = default);
}
