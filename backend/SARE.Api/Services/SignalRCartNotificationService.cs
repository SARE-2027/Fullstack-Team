using Microsoft.AspNetCore.SignalR;
using SARE.Api.Hubs;
using SARE.Application.Common.Interfaces;

namespace SARE.Api.Services;

public class SignalRCartNotificationService(IHubContext<CartHub> hubContext) : ICartNotificationService
{
    public async Task NotifySessionStartedAsync(string cartId, Guid sessionId)
    {
        await hubContext.Clients.Group($"Cart_{cartId}")
            .SendAsync("SessionStarted", new { cartId, sessionId, timestamp = DateTime.UtcNow });
    }

    public async Task NotifyCartUpdatedAsync(string cartId, Guid sessionId, int totalMinor, int itemCount, string latestItemName)
    {
        await hubContext.Clients.Group($"Cart_{cartId}")
            .SendAsync("CartUpdated", new
            {
                cartId,
                sessionId,
                totalMinor,
                itemCount,
                latestItemName,
                timestamp = DateTime.UtcNow
            });
    }

    public async Task NotifyFraudAlertAsync(string cartId, Guid sessionId, string barcode, int weightDiffG, string message)
    {
        // Broadcast to store staff dashboard
        await hubContext.Clients.Group("StoreDashboard")
            .SendAsync("FraudAlert", new
            {
                cartId,
                sessionId,
                barcode,
                weightDiffG,
                message,
                timestamp = DateTime.UtcNow
            });

        // Also notify the cart screen that an inspection or correction is needed
        await hubContext.Clients.Group($"Cart_{cartId}")
            .SendAsync("WeightMismatchWarning", new
            {
                cartId,
                sessionId,
                barcode,
                weightDiffG,
                message
            });
    }

    public async Task NotifyCheckoutSuccessAsync(string cartId, Guid sessionId, int totalPaidMinor, string receiptHash)
    {
        await hubContext.Clients.Group($"Cart_{cartId}")
            .SendAsync("CheckoutSuccess", new
            {
                cartId,
                sessionId,
                totalPaidMinor,
                receiptHash,
                timestamp = DateTime.UtcNow
            });
    }
}
