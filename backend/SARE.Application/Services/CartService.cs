using Microsoft.Extensions.Logging;
using SARE.Application.Common.Interfaces;
using SARE.Application.DTOs.Cart;
using SARE.Domain.Cart;
using SARE.Domain.Enums;

namespace SARE.Application.Services;

public class CartService(
    ISessionRepository sessionRepository,
    IProductVariantRepository variantRepository,
    ICartNotificationService notifications,
    ILogger<CartService> logger,
    ISessionService sessionService) : ICartService
{
    private const int DefaultWeightToleranceGrams = 15;

    public async Task<CartSessionResponseDto> StartSessionAsync(LegacyStartSessionRequest request, CancellationToken cancellationToken = default)
    {
        var session = await sessionService.StartSessionAsync(new StartSessionRequest(request.CartId, UserId: request.UserId), cancellationToken);
        return (await GetSessionByIdAsync(session.SessionId, cancellationToken))!;
    }

    public async Task<CartItemProcessResult> ProcessItemDetectionAsync(Guid sessionId, CartAddItemRequest request, CancellationToken cancellationToken = default)
    {
        var session = await sessionRepository.GetByIdAsync(sessionId, cancellationToken);

        if (session == null || session.Status != SessionStatus.Open)
        {
            return new CartItemProcessResult(false, "Active session not found.");
        }

        var variant = await variantRepository.FindByBarcodeAsync(request.Barcode, cancellationToken);

        if (variant == null || !variant.IsActive)
        {
            return new CartItemProcessResult(false, $"No active product matching barcode '{request.Barcode}'.");
        }

        var product = await variantRepository.GetProductAsync(variant.ProductId, cancellationToken);

        if (product is null || !product.IsActive)
        {
            return new CartItemProcessResult(false, "Product is not available.");
        }

        string productName = product.NameEn;

        // Weight verification logic
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

            await sessionRepository.AddDetectionEventAsync(fraudEvent, cancellationToken);

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

        // Weight matches: Add Item and Record Accepted Detection
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

        await sessionRepository.AddSessionItemWithEventAsync(sessionItem, acceptedEvent, session, cancellationToken);

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
        var summary = await sessionService.CloseSessionAsync(sessionId, ct: cancellationToken);
        var session = await sessionRepository.GetByIdAsync(sessionId, cancellationToken)
            ?? throw new InvalidOperationException($"Session '{sessionId}' not found.");
        var receiptHash = $"RECEIPT_{session.Id}_{session.ClosedAt!.Value.Ticks}";
        await notifications.NotifyCheckoutSuccessAsync(session.CartId, session.Id, summary.TotalMinor, receiptHash);
        return new CheckoutResponseDto(session.Id, session.CartId, summary.TotalMinor, receiptHash, session.ClosedAt.Value);
    }

    public async Task<CartSessionResponseDto?> GetActiveSessionByCartIdAsync(string cartId, CancellationToken cancellationToken = default)
    {
        return await sessionRepository.GetActiveLegacySessionByCartIdAsync(cartId, cancellationToken);
    }

    public async Task<CartSessionResponseDto?> GetSessionByIdAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return await sessionRepository.GetLegacySessionByIdAsync(sessionId, cancellationToken);
    }
}
