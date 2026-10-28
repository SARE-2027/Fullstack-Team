using SARE.Application.DTOs.Cart;

namespace SARE.Application.Common.Interfaces;

public interface ICartMovementService
{
    Task<CartMovementResult> ReportAsync(Guid sessionId, CartMovementRequest request, CancellationToken ct = default);
    Task<CartMovementResult> ConfirmExitAsync(Guid sessionId, ConfirmExitChoiceRequest request, CancellationToken ct = default);
    Task<CartMovementResult> RequestStaffAsync(Guid sessionId, CartAuditCommand request, CancellationToken ct = default);
    Task<CartMovementResult> ManageAuditAsync(Guid sessionId, CartAuditAction action, Guid adminUserId,
        CartAuditCommand request, CancellationToken ct = default);
    Task<CartSummaryResponse> SetRemovalRequestAsync(Guid sessionId, Guid itemId, bool requested, CancellationToken ct = default);
}
