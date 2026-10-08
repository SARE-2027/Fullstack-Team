using SARE.Application.Common.Interfaces;
using SARE.Application.DTOs.Cart;

namespace SARE.Application.Services;

public class SessionService(ICartRepository carts) : ISessionService
{
    public async Task UpdateCartTelemetryAsync(
        string cartId,
        UpdateCartTelemetryRequest request,
        CancellationToken ct = default)
    {
        var cart = await carts.GetByIdAsync(cartId, ct)
            ?? throw new KeyNotFoundException($"العربة {cartId} غير موجودة");

        if (request.BatteryPct is not null)
            cart.BatteryPct = request.BatteryPct;

        if (request.SwVersion is not null)
            cart.SwVersion = request.SwVersion;

        cart.LastSeenAt = DateTime.UtcNow;

        await carts.UpdateAsync(cart, ct);
    }
}
