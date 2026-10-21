using SARE.Application.DTOs.Cart;

namespace SARE.Application.Common.Interfaces;

public interface ICartMovementService
{
    Task<CartMovementResult> ReportAsync(Guid sessionId, CartMovementRequest request, CancellationToken ct = default);
    Task<CartMovementResult> ReconcileAsync(Guid sessionId, ReconcileBasketRequest request, CancellationToken ct = default);
    Task<CartSummaryResponse> SetRemovalRequestAsync(Guid sessionId, Guid itemId, bool requested, CancellationToken ct = default);
}
