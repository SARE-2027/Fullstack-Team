using SARE.Application.DTOs.Cart;
using SARE.Domain.Cart;

namespace SARE.Application.Common.Interfaces;

public interface ISessionRepository
{
    Task<IReadOnlyList<CartSummaryResponse>> GetAllSummariesAsync(CancellationToken ct = default);
    Task<CartSummaryResponse?> GetSummaryAsync(Guid sessionId, CancellationToken ct = default);
    Task<CartSummaryResponse?> GetActiveSummaryByCartIdAsync(string cartId, CancellationToken ct = default);
    Task<Session?> GetActiveByCartIdAsync(string cartId, CancellationToken ct = default);
    Task<Session?> GetByIdAsync(Guid sessionId, CancellationToken ct = default);
    Task<int> GetActiveItemsTotalAsync(Guid sessionId, CancellationToken ct = default);
    Task AddAsync(Session session, CancellationToken ct = default);
    Task UpdateAsync(Session session, CancellationToken ct = default);
    Task<DetectionEvent?> GetDetectionEventAsync(Guid eventId, CancellationToken ct = default);
    Task<bool> TryAddDetectionEventAsync(DetectionEvent detectionEvent, CancellationToken ct = default);
    Task<bool> TryAddSessionItemWithEventAsync(SessionItem sessionItem, DetectionEvent detectionEvent, Session session, CancellationToken ct = default);
}
