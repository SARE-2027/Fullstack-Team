using SARE.Application.DTOs.Cart;

namespace SARE.Application.Common.Interfaces;

public interface ISessionService
{
    Task UpdateCartTelemetryAsync(
        string cartId,
        UpdateCartTelemetryRequest request,
        CancellationToken ct = default);

    Task<CartSummaryResponse> StartSessionAsync(
        StartSessionRequest request,
        CancellationToken ct = default);

    Task<CartSummaryResponse> GetSessionSummaryAsync(
        Guid sessionId,
        CancellationToken ct = default);

    Task<CartSummaryResponse> GetActiveSessionByCartIdAsync(
        string cartId,
        CancellationToken ct = default);
}
