namespace SARE.Application.DTOs.Catalog;

public sealed record ProductDetailResponse(
    ProductResponse Product, IReadOnlyList<ProductOptionResponse> Options);

public sealed record ProductPublicDetailResponse(
    ProductSummaryResponse Product, IReadOnlyList<ProductOptionResponse> Options);
