namespace SARE.Application.DTOs.Catalog;

public sealed record CreateProductOptionWithValuesRequest(
    string? NameAr,
    string? NameEn,
    IReadOnlyList<ProductOptionValueRequest>? Values);
