namespace SARE.Application.DTOs.Catalog;

public sealed record ProductDashboardResponse(
    int TotalProducts, int ActiveProducts, int InactiveProducts,
    int ProductsWithoutActiveVariants, int AvailableProducts);
