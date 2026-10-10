using Microsoft.AspNetCore.SignalR;

namespace SARE.Api.Hubs;

public class CartHub : Hub
{
    private readonly ILogger<CartHub> _logger;

    public CartHub(ILogger<CartHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Join group for a specific cart to receive live session and item updates
    /// </summary>
    public async Task JoinCartGroup(string cartId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Cart_{cartId}");
        _logger.LogInformation("Connection {ConnectionId} joined Cart_{CartId}", Context.ConnectionId, cartId);
    }

    /// <summary>
    /// Join group for the store manager / cashier dashboard for fraud and status logs
    /// </summary>
    public async Task JoinDashboardGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "StoreDashboard");
        _logger.LogInformation("Connection {ConnectionId} joined StoreDashboard", Context.ConnectionId);
    }
}
