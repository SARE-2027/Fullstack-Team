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
    int ExpectedWeightG,
    DetectionSource Source,
    DateTime AddedAt
);
