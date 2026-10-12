using Microsoft.Extensions.Logging;
using FluentValidation;
using SARE.Application.Common.Exceptions;
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
    ISessionService sessionService,
    IValidator<CartAddItemRequest> detectionValidator) : ICartService
{
    private const int DefaultWeightToleranceGrams = 15;

    public async Task<CartItemProcessResult> ProcessItemDetectionAsync(Guid sessionId, CartAddItemRequest request, CancellationToken cancellationToken = default)
    {
        await detectionValidator.ValidateAndThrowAsync(request, cancellationToken);
        if (request.EventId.HasValue)
        {
            var existing = await sessionRepository.GetDetectionEventAsync(request.EventId.Value, cancellationToken);
            if (existing is not null)
                return await ReplayDetectionAsync(existing, sessionId, request, cancellationToken);
        }

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

        var detectionEvent = new DetectionEvent
        {
            Id = request.EventId ?? Guid.NewGuid(),
            SessionId = session.Id,
            Source = request.Source,
            DetectedBarcode = request.Barcode,
            WeightDeltaG = request.MeasuredWeightG,
            Outcome = isWeightValid ? DetectionOutcome.Accepted : DetectionOutcome.Rejected,
            FinalBarcode = request.Barcode,
            CreatedAt = DateTime.UtcNow
        };

        if (!isWeightValid)
        {
            // Record Mismatch / Potential Fraud
            if (!await sessionRepository.TryAddDetectionEventAsync(detectionEvent, cancellationToken))
                return await ReplayCommittedDetectionAsync(detectionEvent.Id, sessionId, request, cancellationToken);

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

        detectionEvent.SessionItemId = sessionItem.Id;

        session.TotalMinor += variant.PriceMinor;
        session.LastActivityAt = DateTime.UtcNow;

        if (!await sessionRepository.TryAddSessionItemWithEventAsync(sessionItem, detectionEvent, session, cancellationToken))
            return await ReplayCommittedDetectionAsync(detectionEvent.Id, sessionId, request, cancellationToken);

        // Broadcast the full committed session, including all active invoice items.
        var summary = await sessionService.GetSessionSummaryAsync(session.Id, cancellationToken);
        await notifications.NotifySessionUpdatedAsync(summary, cancellationToken);

        return new CartItemProcessResult(
            Success: true,
            Message: "Item added successfully.",
            Session: summary
        );
    }

    private async Task<CartItemProcessResult> ReplayCommittedDetectionAsync(
        Guid eventId, Guid sessionId, CartAddItemRequest request, CancellationToken ct)
    {
        var existing = await sessionRepository.GetDetectionEventAsync(eventId, ct)
            ?? throw new ConflictException("Detection changed concurrently. Retry with the same eventId.");
        return await ReplayDetectionAsync(existing, sessionId, request, ct);
    }

    private async Task<CartItemProcessResult> ReplayDetectionAsync(
        DetectionEvent existing, Guid sessionId, CartAddItemRequest request, CancellationToken ct)
    {
        if (existing.SessionId != sessionId || existing.DetectedBarcode != request.Barcode
            || existing.Source != request.Source || existing.WeightDeltaG != request.MeasuredWeightG)
        {
            throw new ConflictException("This eventId was already used for a different detection.", "EVENT_ID_REUSED");
        }

        // Replays acknowledge the stored outcome without changing items or sending another alert.
        // Expected weight was not stored on the event; do not invent a historical value.
        return existing.Outcome switch
        {
            DetectionOutcome.Accepted => new CartItemProcessResult(true, "Detection already processed.",
                Session: await sessionService.GetSessionSummaryAsync(sessionId, ct)),
            DetectionOutcome.Rejected => new CartItemProcessResult(false, "Detection already rejected.",
                IsWeightMismatch: true, MeasuredWeightG: request.MeasuredWeightG),
            _ => throw new ConflictException("This event has a different outcome.", "EVENT_ID_REUSED")
        };
    }

    public async Task<CheckoutResponseDto> CheckoutSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var summary = await sessionService.CloseSessionAsync(sessionId, ct: cancellationToken);
        var closedAt = summary.ClosedAt!.Value;
        var receiptHash = $"RECEIPT_{summary.SessionId}_{closedAt.Ticks}";
        await notifications.NotifyCheckoutSuccessAsync(summary.CartId, summary.SessionId, summary.TotalMinor, receiptHash);
        return new CheckoutResponseDto(summary.SessionId, summary.CartId, summary.TotalMinor, receiptHash, closedAt);
    }
}
