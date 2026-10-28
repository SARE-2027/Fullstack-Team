using SARE.Domain.Enums;

namespace SARE.Application.DTOs.Cart;

public record SessionItemDto(
    Guid Id,
    Guid VariantId,
    Guid ProductId,
    string Barcode,
    string NameAr,
    string NameEn,
    string? ImageUrl,
    int UnitPriceMinor,
    int? ExpectedWeightG,
    CartItemSource Source,
    DateTime AddedAt
)
{
    public SARE.Domain.Enums.BasketPresence BasketPresence { get; init; }
    public DateTime? RemovalRequestedAt { get; init; }
    public decimal WeightTolerancePercent { get; init; }
}
