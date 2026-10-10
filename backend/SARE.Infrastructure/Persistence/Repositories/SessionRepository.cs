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

    public async Task<CartSessionResponseDto?> GetLegacySessionByIdAsync(Guid sessionId, CancellationToken ct = default)
    {
        var session = await db.Sessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == sessionId, ct);

        if (session == null) return null;

        var items = await db.SessionItems
            .AsNoTracking()
            .Where(i => i.SessionId == sessionId && i.RemovedAt == null)
            .ToListAsync(ct);

        var variantIds = items.Select(i => i.VariantId).Distinct().ToList();

        var variants = await db.ProductVariants
            .AsNoTracking()
            .Where(v => variantIds.Contains(v.Id))
            .ToDictionaryAsync(v => v.Id, v => v, ct);

        var productIds = variants.Values.Select(v => v.ProductId).Distinct().ToList();

        var products = await db.Products
            .AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.NameEn, ct);

        var itemDtos = items.Select(i =>
        {
            var variant = variants.GetValueOrDefault(i.VariantId);
            string name = variant != null && products.TryGetValue(variant.ProductId, out var n) ? n : "Unknown Item";
            return new LegacySessionItemDto(
                i.Id,
                i.VariantId,
                name,
                variant?.Barcode ?? string.Empty,
                i.UnitPriceMinor,
                variant?.WeightG ?? 0,
                1,
                i.AddedAt
            );
        }).ToList();

        return new CartSessionResponseDto(
            session.Id,
            session.CartId,
            session.Status,
            session.TotalMinor,
            session.StartedAt,
            itemDtos
        );
    }

    public async Task<CartSessionResponseDto?> GetActiveLegacySessionByCartIdAsync(string cartId, CancellationToken ct = default)
    {
        var session = await db.Sessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.CartId == cartId && s.Status == SessionStatus.Open, ct);

        if (session == null) return null;

        return await GetLegacySessionByIdAsync(session.Id, ct);
    }

    public async Task AddDetectionEventAsync(DetectionEvent detectionEvent, CancellationToken ct = default)
    {
        await db.DetectionEvents.AddAsync(detectionEvent, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task AddSessionItemWithEventAsync(SessionItem sessionItem, DetectionEvent detectionEvent, Session session, CancellationToken ct = default)
    {
        db.SessionItems.Add(sessionItem);
        db.DetectionEvents.Add(detectionEvent);
        db.Sessions.Update(session);
        await db.SaveChangesAsync(ct);
    }
}
