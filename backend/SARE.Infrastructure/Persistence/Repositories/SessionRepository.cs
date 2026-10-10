using Microsoft.EntityFrameworkCore;
using SARE.Application.Common.Interfaces;
using SARE.Application.DTOs.Cart;
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

    public async Task<(Session? Session, IReadOnlyList<SessionItemDto> Items)> GetWithItemsAsync(
        Guid sessionId,
        CancellationToken ct = default)
    {
        var session = await db.Sessions.FirstOrDefaultAsync(s => s.Id == sessionId, ct);
        if (session is null)
            return (null, []);

        var items = await (
            from item in db.SessionItems
            where item.SessionId == sessionId && item.RemovedAt == null
            join variant in db.ProductVariants on item.VariantId equals variant.Id
            join product in db.Products on variant.ProductId equals product.Id
            select new SessionItemDto(
                item.Id,
                item.VariantId,
                variant.Barcode,
                product.NameAr,
                product.NameEn,
                item.UnitPriceMinor,
                item.AddedAt
            )
        ).ToListAsync(ct);

        return (session, items);
    }

    public async Task<(Session? Session, IReadOnlyList<SessionItemDto> Items)> GetActiveWithItemsByCartIdAsync(
        string cartId,
        CancellationToken ct = default)
    {
        var session = await db.Sessions
            .FirstOrDefaultAsync(s => s.CartId == cartId && s.Status == SessionStatus.Open, ct);

        if (session is null)
            return (null, []);

        return await GetWithItemsAsync(session.Id, ct);
    }

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
