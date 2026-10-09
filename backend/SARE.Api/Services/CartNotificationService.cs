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
