using Microsoft.EntityFrameworkCore;
using SARE.Application.Common.Interfaces;
using SARE.Domain.Cart;
using SARE.Domain.Enums;

namespace SARE.Infrastructure.Persistence.Repositories;

public class SessionRepository(AppDbContext db) : ISessionRepository
{
    public async Task<Session?> GetActiveByCartIdAsync(string cartId, CancellationToken ct = default)
        => await db.Sessions
            .FirstOrDefaultAsync(s => s.CartId == cartId && s.Status == SessionStatus.Open, ct);

    public async Task<Session?> GetByIdAsync(Guid sessionId, CancellationToken ct = default)
        => await db.Sessions.FirstOrDefaultAsync(s => s.Id == sessionId, ct);

    public async Task AddAsync(Session session, CancellationToken ct = default)
    {
        await db.Sessions.AddAsync(session, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Session session, CancellationToken ct = default)
    {
        db.Sessions.Update(session);
        await db.SaveChangesAsync(ct);
    }
}
