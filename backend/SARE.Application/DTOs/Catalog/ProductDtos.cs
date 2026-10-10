namespace SARE.Application.DTOs.Catalog;

public record ProductVariantDto(
    Guid Id,
    string Barcode,
    int PriceMinor,
    int WeightG,
    bool IsActive
);

public record ProductResponseDto(
    Guid Id,
    string NameAr,
    string NameEn,
    Guid CategoryId,
    string? CategoryName,
    string? ImageUrl,
    bool IsActive,
    List<ProductVariantDto> Variants
);

public record CreateProductRequest(
    string NameAr,
    string NameEn,
    Guid CategoryId,
    string Barcode,
    int PriceMinor,
    int WeightG,
    string? ImageUrl
);
