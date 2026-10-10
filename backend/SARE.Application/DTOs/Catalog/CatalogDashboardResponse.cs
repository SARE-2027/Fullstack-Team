namespace SARE.Application.DTOs.Catalog;

public sealed record CatalogDashboardResponse(
    int TotalCategories, int TotalProducts, int ActiveProducts, int InactiveProducts,
    int AvailableProducts, int ProductsWithoutActiveVariants,
    int TotalVariants, int ActiveVariants, int InactiveVariants, int AvailableVariants,
    int TotalOptions, int TotalOptionValues);
