using Microsoft.EntityFrameworkCore;
using SARE.Application.Common.Exceptions;
using SARE.Application.Common.Interfaces;
using SARE.Application.DTOs.Cart;
using SARE.Domain.Cart;
using SARE.Domain.Enums;

namespace SARE.Infrastructure.Persistence.Repositories;

public class SessionRepository(AppDbContext db) : ISessionRepository
{
    public async Task<IReadOnlyList<CartSummaryResponse>> GetAllSummariesAsync(CancellationToken ct = default)
    {
        return await ReadSummariesAsync(db.Sessions.AsNoTracking(), ct);
    }

    public async Task<CartSummaryResponse?> GetSummaryAsync(Guid sessionId, CancellationToken ct = default)
    {
        var sessions = db.Sessions.AsNoTracking()
            .Where(session => session.Id == sessionId);
        var summaries = await ReadSummariesAsync(sessions, ct);
        return summaries.SingleOrDefault();
    }

    public async Task<CartSummaryResponse?> GetActiveSummaryByCartIdAsync(string cartId, CancellationToken ct = default)
    {
        var sessions = db.Sessions.AsNoTracking()
            .Where(session => session.CartId == cartId && session.Status == SessionStatus.Open);
        return (await ReadSummariesAsync(sessions, ct)).SingleOrDefault();
    }

    private async Task<List<CartSummaryResponse>> ReadSummariesAsync(IQueryable<Session> sessions, CancellationToken ct)
    {
        // One database query reads session metadata and its active items together.
        var rows = await (
            from session in sessions
            join user in db.Users on session.UserId equals (Guid?)user.Id into sessionUsers
            from user in sessionUsers.DefaultIfEmpty()
            orderby session.StartedAt descending, session.Id
            select new
            {
                Session = session,
                UserName = user == null ? null : user.Name,
                Items = (
                    from item in db.SessionItems
                    where item.SessionId == session.Id && item.RemovedAt == null
                    join variant in db.ProductVariants on item.VariantId equals variant.Id
                    join product in db.Products on variant.ProductId equals product.Id
                    orderby item.AddedAt, item.Id
                    select new SessionItemDto(
                        item.Id,
                        item.VariantId,
                        product.Id,
                        item.Barcode,
                        product.NameAr,
                        product.NameEn,
                        product.ImageUrl,
                        item.UnitPriceMinor,
                        item.ExpectedWeightG,
                        item.Source,
                        item.AddedAt) { BasketPresence = item.BasketPresence, RemovalRequestedAt = item.RemovalRequestedAt }
                ).ToList(),
                Pending = db.CartMovements.Where(movement => movement.SessionId == session.Id
                    && movement.Status == CartMovementStatus.NeedsConfirmation)
                    .OrderBy(movement => movement.Sequence)
                    .Select(movement => new { Movement = movement, Details = movement.Items.OrderBy(item => item.Position).ToList() }).ToList()
            }
        ).ToListAsync(ct);

        return rows.Select(row => new CartSummaryResponse(
            SessionId: row.Session.Id,
            CartId: row.Session.CartId,
            UserId: row.Session.UserId,
            UserName: row.UserName,
            Status: row.Session.Status.ToString().ToLowerInvariant(),
            TotalMinor: row.Session.TotalMinor,
            ItemsCount: row.Items.Count,
            StartedAt: row.Session.StartedAt,
            Items: row.Items)
        {
            UpdatedAt = row.Session.LastActivityAt,
            ClosedAt = row.Session.ClosedAt,
            ClosedByUserId = row.Session.ClosedBy,
            LastMovementSequence = row.Session.LastMovementSequence,
            BasketWeightG = row.Session.BasketWeightG,
            PendingMovements = row.Pending.Select(entry =>
            {
                entry.Movement.Items = entry.Details;
                return SARE.Application.Services.CartMovementService.ToResponse(entry.Movement);
            }).ToList(),
            CloseReason = row.Session.CloseReason switch
            {
                SARE.Domain.Enums.CloseReason.StaffClosed => "staff_closed",
                SARE.Domain.Enums.CloseReason.Abandoned => "abandoned",
                _ => null
            }
        }).ToList();
    }

    public async Task<Session?> GetActiveByCartIdAsync(string cartId, CancellationToken ct = default)
        => await db.Sessions
            .FirstOrDefaultAsync(s => s.CartId == cartId && s.Status == SessionStatus.Open, ct);

    public async Task<Session?> GetByIdAsync(Guid sessionId, CancellationToken ct = default)
        => await db.Sessions.FirstOrDefaultAsync(s => s.Id == sessionId, ct);

    public async Task<int> GetActiveItemsTotalAsync(Guid sessionId, CancellationToken ct = default)
    {
        return await db.SessionItems
            .Where(item => item.SessionId == sessionId && item.RemovedAt == null)
            .SumAsync(item => (int?)item.UnitPriceMinor, ct) ?? 0;
    }

    public async Task<bool> HasPendingChangesAsync(Guid sessionId, CancellationToken ct = default) =>
        await db.CartMovements.AnyAsync(x => x.SessionId == sessionId
            && x.Status == CartMovementStatus.NeedsConfirmation, ct)
        || await db.SessionItems.AnyAsync(x => x.SessionId == sessionId && x.RemovedAt == null && x.RemovalRequestedAt != null, ct);

    public async Task AddAsync(Session session, CancellationToken ct = default)
    {
        await db.Sessions.AddAsync(session, ct);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            if (session.Status == SessionStatus.Open && await db.Sessions.AsNoTracking()
                .AnyAsync(existing => existing.CartId == session.CartId && existing.Status == SessionStatus.Open, ct))
            {
                throw new ConflictException($"العربة {session.CartId} لديها جلسة نشطة بالفعل");
            }
            throw;
        }
    }

    public async Task UpdateAsync(Session session, CancellationToken ct = default)
    {
        db.Sessions.Update(session);
        await db.SaveChangesAsync(ct);
    }

}
