namespace SARE.Application.DTOs.Catalog;

public sealed record VariantOptionResponse(
    Guid Id, string NameAr, string NameEn, Guid? SelectedValueId,
    IReadOnlyList<ProductOptionValueResponse> Values);
