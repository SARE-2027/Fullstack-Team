using SARE.Application.DTOs.Cart;

namespace SARE.Application.Common.Interfaces;

public interface ICartNotificationService
{
    Task NotifySessionStartedAsync(string cartId, CartSummaryResponse session, CancellationToken ct = default);
    Task NotifySessionClosedAsync(string cartId, CartSummaryResponse session, CancellationToken ct = default);
    Task NotifyLowBatteryAsync(string cartId, short batteryPct, CancellationToken ct = default);
    Task NotifySessionStartedAsync(string cartId, Guid sessionId);
    Task NotifyCartUpdatedAsync(string cartId, Guid sessionId, int totalMinor, int itemCount, string latestItemName);
    Task NotifyFraudAlertAsync(string cartId, Guid sessionId, string barcode, int weightDiffG, string message);
    Task NotifyCheckoutSuccessAsync(string cartId, Guid sessionId, int totalPaidMinor, string receiptHash);
}
