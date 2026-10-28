using Microsoft.EntityFrameworkCore;
using SARE.Application.Common.Exceptions;
using SARE.Application.Common.Interfaces;
using SARE.Application.DTOs.Cart;
using SARE.Domain.Cart;
using SARE.Domain.Enums;
using SARE.Application.Services;

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
                    select new { Unit = item, ProductId = product.Id, product.NameAr, product.NameEn, product.ImageUrl }
                ).ToList(),
                Audit = db.CartBasketAudits.Where(audit => audit.SessionId == session.Id && audit.CompletedAtUtc == null)
                    .Select(audit => new CartAuditResponse(audit.Id, audit.StartedByUserId, audit.StartedAtUtc,
                        audit.LastStableWeightG,
                        (from draft in db.CartBasketAuditItems
                         where draft.AuditId == audit.Id && !draft.IsRemoved
                         join variant in db.ProductVariants on draft.VariantId equals variant.Id
                         join product in db.Products on variant.ProductId equals product.Id
                         orderby draft.ScannedAtUtc, draft.Id
                         select new CartAuditItemResponse(draft.Id, draft.VariantId, draft.Barcode,
                             product.NameAr, product.NameEn, draft.UnitPriceMinor, draft.ExpectedWeightG,
                             draft.WeightTolerancePercent)).ToList())).FirstOrDefault(),
                Pending = db.CartMovements.Where(movement => movement.SessionId == session.Id
                    && movement.Status == CartMovementStatus.NeedsConfirmation)
                    .OrderBy(movement => movement.Sequence)
                    .Select(movement => new { Movement = movement, Details = movement.Items.OrderBy(item => item.Position).ToList() }).ToList()
            }
        ).ToListAsync(ct);

        return rows.Select(row =>
        {
            var items = row.Items.Select(entry => new SessionItemDto(entry.Unit.Id, entry.Unit.VariantId,
                entry.ProductId, entry.Unit.Barcode, entry.NameAr, entry.NameEn, entry.ImageUrl,
                entry.Unit.UnitPriceMinor, entry.Unit.ExpectedWeightG, entry.Unit.Source, entry.Unit.AddedAt)
            {
                BasketPresence = entry.Unit.BasketPresence, RemovalRequestedAt = entry.Unit.RemovalRequestedAt,
                WeightTolerancePercent = entry.Unit.WeightTolerancePercent
            }).ToList();
            foreach (var entry in row.Pending) entry.Movement.Items = entry.Details;
            var head = CartExitQueue.First(row.Pending.Select(entry => entry.Movement));
            return new CartSummaryResponse(
                SessionId: row.Session.Id,
                CartId: row.Session.CartId,
                UserId: row.Session.UserId,
                UserName: row.UserName,
                Status: row.Session.Status.ToString().ToLowerInvariant(),
                TotalMinor: row.Session.TotalMinor,
                ItemsCount: row.Items.Count,
                StartedAt: row.Session.StartedAt,
                Items: items)
            {
                UpdatedAt = row.Session.LastActivityAt,
                ClosedAt = row.Session.ClosedAt,
                ClosedByUserId = row.Session.ClosedBy,
                LastMovementSequence = row.Session.LastMovementSequence,
                BasketWeightG = row.Session.BasketWeightG,
                PendingWeightG = row.Session.PendingWeightG,
                PendingWeightUncertaintyG = row.Session.PendingWeightUncertaintyG,
                StaffRequestedAt = row.Session.StaffRequestedAt,
                StaffRequestReason = row.Session.StaffRequestReason,
                Audit = row.Audit,
                BasketState = row.Session.BasketState,
                ExitChoices = row.Session.BasketState != BasketState.Pending || head is null ? []
                    : CartExitQueue.Find(row.Items.Select(entry => entry.Unit), head,
                        row.Pending.Select(entry => entry.Movement)).Choices,
                PendingMovements = row.Pending.Select(entry => CartMovementService.ToResponse(entry.Movement)).ToList(),
                CloseReason = row.Session.CloseReason switch
                {
                    SARE.Domain.Enums.CloseReason.StaffClosed => "staff_closed",
                    SARE.Domain.Enums.CloseReason.Abandoned => "abandoned",
                    _ => null
                }
            };
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
        await db.Sessions.AnyAsync(session => session.Id == sessionId
            && (session.BasketState != BasketState.Verified || session.PendingWeightG != 0), ct)
        || await db.CartMovements.AnyAsync(x => x.SessionId == sessionId
            && x.Status == CartMovementStatus.NeedsConfirmation, ct)
        || await db.SessionItems.AnyAsync(x => x.SessionId == sessionId && x.RemovedAt == null
            && (x.RemovalRequestedAt != null || x.BasketPresence == BasketPresence.Inside && x.ExpectedWeightG == null), ct);

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
