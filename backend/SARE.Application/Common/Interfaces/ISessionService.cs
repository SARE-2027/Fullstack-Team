using SARE.Application.DTOs.Cart;

namespace SARE.Application.Common.Interfaces;

public interface ISessionService
{
    Task<CartTelemetryResponse> UpdateCartTelemetryAsync(
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

    Task<CartSummaryResponse> CloseSessionAsync(
        Guid sessionId,
        CloseSessionRequest? request = null,
        CancellationToken ct = default);
}
