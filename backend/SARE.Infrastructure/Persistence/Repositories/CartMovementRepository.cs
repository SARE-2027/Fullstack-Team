using Microsoft.EntityFrameworkCore;
using SARE.Application.Common.Exceptions;
using SARE.Application.Common.Interfaces;
using SARE.Domain.Cart;
using SARE.Domain.Enums;

namespace SARE.Infrastructure.Persistence.Repositories;

public sealed class CartMovementRepository(AppDbContext db) : ICartMovementRepository
{
    public Task<CartEventReceipt?> GetReceiptAsync(Guid eventId, CancellationToken ct) =>
        db.CartEventReceipts.AsNoTracking().SingleOrDefaultAsync(x => x.Id == eventId, ct);

    public Task<CartMovement?> GetAsync(Guid movementId, CancellationToken ct) =>
        db.CartMovements.Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == movementId, ct);

    public Task<List<CartMovement>> GetUnresolvedAsync(Guid sessionId, CancellationToken ct) =>
        db.CartMovements.Include(x => x.Items).Where(x => x.SessionId == sessionId
            && x.Status == CartMovementStatus.NeedsConfirmation)
            .OrderBy(x => x.Sequence).ToListAsync(ct);

    public Task<List<SessionItem>> GetSessionItemsAsync(Guid sessionId, CancellationToken ct) =>
        db.SessionItems.Where(x => x.SessionId == sessionId).ToListAsync(ct);

    public Task<CartBasketAudit?> GetAuditAsync(Guid sessionId, CancellationToken ct) =>
        db.CartBasketAudits.Include(audit => audit.Items)
            .SingleOrDefaultAsync(audit => audit.SessionId == sessionId && audit.CompletedAtUtc == null, ct);

    public void AddAudit(CartBasketAudit audit) => db.CartBasketAudits.Add(audit);

    public async Task<bool> TrySaveAsync(Session session, CartMovement movement, CartEventReceipt receipt,
        IReadOnlyList<SessionItem> newItems, CancellationToken ct)
    {
        if (db.Entry(movement).State == EntityState.Detached) db.CartMovements.Add(movement);
        db.CartEventReceipts.Add(receipt);
        db.SessionItems.AddRange(newItems);
        db.Sessions.Update(session);
        try
        {
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException exception)
        {
            db.ChangeTracker.Clear();
            if (await db.CartEventReceipts.AnyAsync(x => x.Id == receipt.Id, ct)) return false;
            if (exception is DbUpdateConcurrencyException || await db.CartEventReceipts.AnyAsync(
                x => x.SessionId == receipt.SessionId && x.Sequence == receipt.Sequence, ct))
                throw new ConflictException("Session changed concurrently. Rejoin and retry the same eventId.");
            throw;
        }
    }
}
