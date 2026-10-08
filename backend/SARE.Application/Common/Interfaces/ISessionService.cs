using SARE.Application.DTOs.Cart;

namespace SARE.Application.Common.Interfaces;

public interface ISessionService
{
    Task UpdateCartTelemetryAsync(
        string cartId,
        UpdateCartTelemetryRequest request,
        CancellationToken ct = default);
}
