using SARE.Application.DTOs.Cart;

namespace SARE.Application.Common.Interfaces;

public interface ICartNotificationService
{
    Task NotifySessionUpdatedAsync(CartSummaryResponse session, CancellationToken ct = default);
    Task NotifyLowBatteryAsync(string cartId, short batteryPct, CancellationToken ct = default);
    Task NotifyCheckoutSuccessAsync(string cartId, Guid sessionId, int totalPaidMinor, string receiptHash);
}
