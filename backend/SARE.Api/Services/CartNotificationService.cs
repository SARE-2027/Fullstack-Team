using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SARE.Api.Hubs;
using SARE.Application.Common.Interfaces;
using SARE.Application.DTOs.Cart;

namespace SARE.Api.Services;

public class CartNotificationService(
    IHubContext<CartHub> hubContext,
    ILogger<CartNotificationService> logger) : ICartNotificationService
{
    public Task NotifySessionStartedAsync(string cartId, Guid sessionId) =>
        SendAsync($"cart_{cartId}", "SessionStarted", new { cartId, sessionId, timestamp = DateTime.UtcNow });

    public Task NotifyCartUpdatedAsync(string cartId, Guid sessionId, int totalMinor, int itemCount, string latestItemName) =>
        SendAsync($"cart_{cartId}", "CartUpdated", new { cartId, sessionId, totalMinor, itemCount, latestItemName, timestamp = DateTime.UtcNow });

    public async Task NotifyFraudAlertAsync(string cartId, Guid sessionId, string barcode, int weightDiffG, string message)
    {
        var alert = new { cartId, sessionId, barcode, weightDiffG, message, timestamp = DateTime.UtcNow };
        await SendAsync("StoreDashboard", "FraudAlert", alert);
        await SendAsync($"cart_{cartId}", "WeightMismatchWarning", alert);
    }

    public Task NotifyCheckoutSuccessAsync(string cartId, Guid sessionId, int totalPaidMinor, string receiptHash) =>
        SendAsync($"cart_{cartId}", "CheckoutSuccess", new { cartId, sessionId, totalPaidMinor, receiptHash, timestamp = DateTime.UtcNow });

    private async Task SendAsync(string group, string eventName, object payload)
    {
        try { await hubContext.Clients.Group(group).SendAsync(eventName, payload); }
        catch (Exception ex) { logger.LogWarning(ex, "SignalR notification {EventName} failed for {Group} after saving.", eventName, group); }
    }

    public async Task NotifySessionStartedAsync(string cartId, CartSummaryResponse session, CancellationToken ct = default)
    {
        try
        {
            await hubContext.Clients.Group($"cart_{cartId}").SendAsync("SessionStarted", session, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "تعذر إرسال إشعار بدء الجلسة لحظياً للعربة {CartId} عبر SignalR. العملية مستمرة بنجاح في قاعدة البيانات.", cartId);
        }
    }

    public async Task NotifySessionClosedAsync(string cartId, CartSummaryResponse session, CancellationToken ct = default)
    {
        try
        {
            await hubContext.Clients.Group($"cart_{cartId}").SendAsync("SessionClosed", session, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "تعذر إرسال إشعار إغلاق الجلسة لحظياً للعربة {CartId} عبر SignalR. العملية مستمرة بنجاح في قاعدة البيانات.", cartId);
        }
    }

    public async Task NotifyLowBatteryAsync(string cartId, short batteryPct, CancellationToken ct = default)
    {
        try
        {
            await hubContext.Clients.Group($"cart_{cartId}").SendAsync("LowBatteryWarning", new { CartId = cartId, BatteryPct = batteryPct }, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "تعذر إرسال تحذير انخفاض البطارية للعربة {CartId} عبر SignalR.", cartId);
        }
    }
}
