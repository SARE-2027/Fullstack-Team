using SARE.Domain.Enums;

namespace SARE.Application.DTOs.Cart;

public record BasketWeightDto(int BeforeG, int AfterG, bool IsStable);

public record CartMovementItemRequest(
    CartItemAction Action, string? Barcode,
    CartItemSource Source = CartItemSource.Vision, float? Confidence = null,
    Guid? SessionItemId = null,
    string? TrackId = null, string? CameraId = null);

public record CartMovementRequest(
    Guid EventId, Guid MovementId, int Sequence, Guid DeviceBootId,
    DateTime StartedAtUtc, DateTime EndedAtUtc,
    IReadOnlyList<CartMovementItemRequest> Items, BasketWeightDto? Weight = null,
    bool IsComplete = true, string? ModelVersion = null)
{
    // المسحات أثناء الجرد تشير للجرد الظاهر حاليًا على شاشة العربية.
    public Guid? AuditId { get; init; }
}

public record CartMovementItemResponse(
    Guid ItemId, string Action, string? Barcode, Guid? VariantId,
    Guid? SessionItemId, string Outcome, string Effect, string? ReasonCode,
    CartItemSource Source, float? Confidence,
    string? TrackId, string? CameraId, int? ExpectedWeightG, int? UnitPriceMinor)
{
    public Guid? TargetSessionItemId { get; init; }
    public int Position { get; init; }
}

public record CartMovementResponse(
    Guid MovementId, string Status, string? ReasonCode,
    int? WeightBeforeG, int? WeightAfterG, IReadOnlyList<CartMovementItemResponse> Items)
{
    public int Sequence { get; init; }
    public Guid DeviceBootId { get; init; }
    public DateTime StartedAtUtc { get; init; }
    public DateTime EndedAtUtc { get; init; }
    public string? ModelVersion { get; init; }
    public int PendingWeightBeforeG { get; init; }
    public int PendingWeightAfterG { get; init; }
    public int? UnconfirmedExitWeightG { get; init; }
    public Guid? RelatedMovementId { get; init; }
    public Guid? ResolvedByMovementId { get; init; }
    public bool ConfirmedByCustomer { get; init; }
    public Guid? AuditId { get; init; }
}

public record CartMovementResult(Guid EventId, int NextSequence, CartMovementResponse Movement, CartSummaryResponse Session);

public record CartVariantData(Guid Id, string Barcode, int PriceMinor, int WeightG, bool IsAvailable)
{
    public decimal? WeightTolerancePercent { get; init; }
}

public record ConfirmExitChoiceRequest(Guid EventId, Guid MovementId, int Sequence, Guid DeviceBootId,
    Guid PendingMovementId, string ExpectedVersion, IReadOnlyList<Guid> SessionItemIds)
{
    public bool NeedsStaff { get; init; }
}

public record CartExitChoiceResponse(IReadOnlyList<Guid> SessionItemIds, int InvoiceWeightG, int PendingWeightG);
