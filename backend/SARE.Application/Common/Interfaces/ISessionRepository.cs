using SARE.Application.DTOs.Cart;
using SARE.Domain.Cart;

namespace SARE.Application.Common.Interfaces;

public interface ISessionRepository
{
    Task<Session?> GetActiveByCartIdAsync(string cartId, CancellationToken ct = default);
    Task<Session?> GetByIdAsync(Guid sessionId, CancellationToken ct = default);
    Task<(Session? Session, IReadOnlyList<SessionItemDto> Items)> GetWithItemsAsync(Guid sessionId, CancellationToken ct = default);
    Task<(Session? Session, IReadOnlyList<SessionItemDto> Items)> GetActiveWithItemsByCartIdAsync(string cartId, CancellationToken ct = default);
    Task AddAsync(Session session, CancellationToken ct = default);
    Task UpdateAsync(Session session, CancellationToken ct = default);
    Task<CartSessionResponseDto?> GetLegacySessionByIdAsync(Guid sessionId, CancellationToken ct = default);
    Task<CartSessionResponseDto?> GetActiveLegacySessionByCartIdAsync(string cartId, CancellationToken ct = default);
    Task AddDetectionEventAsync(DetectionEvent detectionEvent, CancellationToken ct = default);
    Task AddSessionItemWithEventAsync(SessionItem sessionItem, DetectionEvent detectionEvent, Session session, CancellationToken ct = default);
}
