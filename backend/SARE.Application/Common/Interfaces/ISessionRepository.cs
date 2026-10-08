using SARE.Domain.Cart;

namespace SARE.Application.Common.Interfaces;

public interface ISessionRepository
{
    Task<Session?> GetActiveByCartIdAsync(string cartId, CancellationToken ct = default);
    Task<Session?> GetByIdAsync(Guid sessionId, CancellationToken ct = default);
    Task AddAsync(Session session, CancellationToken ct = default);
    Task UpdateAsync(Session session, CancellationToken ct = default);
}
