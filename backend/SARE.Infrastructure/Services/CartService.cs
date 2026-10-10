using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SARE.Application.Common.Interfaces;
using SARE.Application.DTOs.Cart;
using SARE.Domain.Cart;
using SARE.Domain.Enums;
using SARE.Infrastructure.Persistence;

namespace SARE.Infrastructure.Services;

public class CartService(
    AppDbContext context,
    ICartNotificationService notifications,
    ILogger<CartService> logger) : ICartService
{
    private const int DefaultWeightToleranceGrams = 15;

    public async Task<CartSessionResponseDto> StartSessionAsync(StartSessionRequest request, CancellationToken cancellationToken = default)
    {
        // 1. Mark existing open sessions on this cart as abandoned
        var openSessions = await context.Sessions
            .Where(s => s.CartId == request.CartId && s.Status == SessionStatus.Open)
            .ToListAsync(cancellationToken);

        foreach (var oldSession in openSessions)
        {
            oldSession.Status = SessionStatus.Abandoned;
            oldSession.CloseReason = CloseReason.Abandoned;
            oldSession.ClosedAt = DateTime.UtcNow;
        }

        // 2. Ensure cart exists or register it
        var cart = await context.Carts.FindAsync([request.CartId], cancellationToken);
        if (cart == null)
        {
            cart = new SARE.Domain.Cart.Cart
            {
                Id = request.CartId,
                Status = CartStatus.Active,
                BatteryPct = 100,
                LastSeenAt = DateTime.UtcNow
            };
            context.Carts.Add(cart);
        }
        else
        {
            cart.Status = CartStatus.Active;
            cart.LastSeenAt = DateTime.UtcNow;
        }

        // 3. Create new Session
        var session = new Session
        {
            Id = Guid.NewGuid(),
            CartId = request.CartId,
            UserId = request.UserId,
            Status = SessionStatus.Open,
            TotalMinor = 0,
            StartedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow
        };

        context.Sessions.Add(session);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Started cart session {SessionId} on Cart {CartId}", session.Id, request.CartId);

        // Notify Cart screen via SignalR
        await notifications.NotifySessionStartedAsync(request.CartId, session.Id);

        return new CartSessionResponseDto(
            session.Id,
            session.CartId,
            session.Status,
            session.TotalMinor,
            session.StartedAt,
            []
        );
    }

    public async Task<CartItemProcessResult> ProcessItemDetectionAsync(Guid sessionId, CartAddItemRequest request, CancellationToken cancellationToken = default)
    {
        var session = await context.Sessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.Status == SessionStatus.Open, cancellationToken);

        if (session == null)
        {
            return new CartItemProcessResult(false, "Active session not found.");
        }

        var variant = await context.ProductVariants
            .FirstOrDefaultAsync(v => v.Barcode == request.Barcode && v.IsActive, cancellationToken);

        if (variant == null)
        {
            return new CartItemProcessResult(false, $"No active product matching barcode '{request.Barcode}'.");
        }

        var product = await context.Products
            .FirstOrDefaultAsync(p => p.Id == variant.ProductId, cancellationToken);

        string productName = product?.NameEn ?? "Unknown Product";

        // 4. Weight verification logic
        int weightDelta = Math.Abs(request.MeasuredWeightG - variant.WeightG);
        bool isWeightValid = weightDelta <= DefaultWeightToleranceGrams;

        if (!isWeightValid)
        {
            // Record Mismatch / Potential Fraud
            var fraudEvent = new DetectionEvent
            {
                Id = Guid.NewGuid(),
                SessionId = session.Id,
                Source = request.Source,
                DetectedBarcode = request.Barcode,
                WeightDeltaG = request.MeasuredWeightG,
                Outcome = DetectionOutcome.Rejected,
                FinalBarcode = request.Barcode,
                CreatedAt = DateTime.UtcNow
            };

            context.DetectionEvents.Add(fraudEvent);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogWarning("Weight mismatch on cart {CartId}: Expected {Expected}g, Measured {Measured}g (Diff: {Diff}g)",
                session.CartId, variant.WeightG, request.MeasuredWeightG, weightDelta);

            await notifications.NotifyFraudAlertAsync(
                session.CartId,
                session.Id,
                request.Barcode,
                weightDelta,
                $"Weight mismatch! Expected {variant.WeightG}g but measured {request.MeasuredWeightG}g on item: {productName}"
            );

            return new CartItemProcessResult(
                Success: false,
                Message: "Weight tolerance exceeded.",
                IsWeightMismatch: true,
                ExpectedWeightG: variant.WeightG,
                MeasuredWeightG: request.MeasuredWeightG
            );
        }

        // 5. Weight matches: Add Item and Record Accepted Detection
        var sessionItem = new SessionItem
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            VariantId = variant.Id,
            UnitPriceMinor = variant.PriceMinor,
            Source = request.Source,
            AddedAt = DateTime.UtcNow
        };

        var acceptedEvent = new DetectionEvent
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            SessionItemId = sessionItem.Id,
            Source = request.Source,
            DetectedBarcode = request.Barcode,
            WeightDeltaG = request.MeasuredWeightG,
            Outcome = DetectionOutcome.Accepted,
            FinalBarcode = request.Barcode,
            CreatedAt = DateTime.UtcNow
        };

        session.TotalMinor += variant.PriceMinor;
        session.LastActivityAt = DateTime.UtcNow;

        context.SessionItems.Add(sessionItem);
        context.DetectionEvents.Add(acceptedEvent);

        await context.SaveChangesAsync(cancellationToken);

        // Fetch all active items in session to broadcast updated receipt
        var sessionDto = await GetSessionByIdAsync(session.Id, cancellationToken);

        await notifications.NotifyCartUpdatedAsync(
            session.CartId,
            session.Id,
            session.TotalMinor,
            sessionDto?.Items.Count ?? 1,
            productName
        );

        return new CartItemProcessResult(
            Success: true,
            Message: "Item added successfully.",
            Session: sessionDto
        );
    }

    public async Task<CheckoutResponseDto> CheckoutSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await context.Sessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.Status == SessionStatus.Open, cancellationToken);

        if (session == null)
        {
            throw new InvalidOperationException("Active session not found or already closed.");
        }

        session.Status = SessionStatus.Closed;
        session.ClosedAt = DateTime.UtcNow;

        var cart = await context.Carts.FindAsync([session.CartId], cancellationToken);
        if (cart != null)
        {
            cart.Status = CartStatus.Active;
            cart.LastSeenAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync(cancellationToken);

        string receiptHash = $"RECEIPT_{session.Id}_{DateTime.UtcNow.Ticks}";

        await notifications.NotifyCheckoutSuccessAsync(
            session.CartId,
            session.Id,
            session.TotalMinor,
            receiptHash
        );

        return new CheckoutResponseDto(
            session.Id,
            session.CartId,
            session.TotalMinor,
            receiptHash,
            session.ClosedAt.Value
        );
    }

    public async Task<CartSessionResponseDto?> GetActiveSessionByCartIdAsync(string cartId, CancellationToken cancellationToken = default)
    {
        var session = await context.Sessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.CartId == cartId && s.Status == SessionStatus.Open, cancellationToken);

        if (session == null) return null;

        return await GetSessionByIdAsync(session.Id, cancellationToken);
    }

    public async Task<CartSessionResponseDto?> GetSessionByIdAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await context.Sessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

        if (session == null) return null;

        var items = await context.SessionItems
            .AsNoTracking()
            .Where(i => i.SessionId == sessionId && i.RemovedAt == null)
            .ToListAsync(cancellationToken);

        var variantIds = items.Select(i => i.VariantId).Distinct().ToList();

        var variants = await context.ProductVariants
            .AsNoTracking()
            .Where(v => variantIds.Contains(v.Id))
            .ToDictionaryAsync(v => v.Id, v => v, cancellationToken);

        var productIds = variants.Values.Select(v => v.ProductId).Distinct().ToList();

        var products = await context.Products
            .AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.NameEn, cancellationToken);

        var itemDtos = items.Select(i =>
        {
            var variant = variants.GetValueOrDefault(i.VariantId);
            string name = variant != null && products.TryGetValue(variant.ProductId, out var n) ? n : "Unknown Item";
            return new SessionItemDto(
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
}
