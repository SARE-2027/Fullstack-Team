namespace SARE.Application.DTOs.Cart;

public record SessionItemDto(
    Guid Id,
    Guid VariantId,
    string Barcode,
    string NameAr,
    string NameEn,
    int UnitPriceMinor,
    DateTime AddedAt
);
