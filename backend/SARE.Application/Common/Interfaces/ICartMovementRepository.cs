using SARE.Domain.Cart;

namespace SARE.Application.Common.Interfaces;

public interface ICartMovementRepository
{
    Task<CartEventReceipt?> GetReceiptAsync(Guid eventId, CancellationToken ct);
    Task<CartMovement?> GetAsync(Guid movementId, CancellationToken ct);
    Task<List<CartMovement>> GetUnresolvedAsync(Guid sessionId, CancellationToken ct);
    Task<List<SessionItem>> GetSessionItemsAsync(Guid sessionId, CancellationToken ct);
    Task<CartBasketAudit?> GetAuditAsync(Guid sessionId, CancellationToken ct);
    void AddAudit(CartBasketAudit audit);
    Task<bool> TrySaveAsync(Session session, CartMovement movement, CartEventReceipt receipt,
        IReadOnlyList<SessionItem> newItems, CancellationToken ct);
}
