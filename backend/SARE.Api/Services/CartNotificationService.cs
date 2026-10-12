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
    public async Task NotifySessionUpdatedAsync(CartSummaryResponse session, CancellationToken ct = default)
    {
        try
        {
            // Admin and cart session groups have separate membership to avoid duplicate updates.
            await hubContext.Clients.Groups([CartHub.SessionAdminsGroup, CartHub.SessionCartGroup(session.CartId)])
                .SendAsync("SessionUpdated", session, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "SessionUpdated notification failed for session {SessionId} after saving.", session.SessionId);
        }
    }

    public async Task NotifyFraudAlertAsync(string cartId, Guid sessionId, string barcode, int weightDiffG, string message)
    {
        var alert = new { cartId, sessionId, barcode, weightDiffG, message, timestamp = DateTime.UtcNow };
        await SendAsync("StoreDashboard", "FraudAlert", alert);
        await SendAsync($"cart_{cartId}", "WeightMismatchWarning", alert);
    }

    public Task NotifyCheckoutSuccessAsync(string cartId, Guid sessionId, int totalPaidMinor, string receiptHash) =>
        SendAsync($"cart_{cartId}", "CheckoutSuccess", new { cartId, sessionId, totalPaidMinor, receiptHash, timestamp = DateTime.UtcNow });

    private async Task SendAsync(string group, string eventName, object payload, CancellationToken ct = default)
    {
        try
        {
            await hubContext.Clients.Group(group).SendAsync(eventName, payload, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "SignalR notification {EventName} failed for {Group} after saving.", eventName, group);
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
