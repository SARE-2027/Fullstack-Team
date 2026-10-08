using SARE.Application.Common.DTOs;
using SARE.Application.DTOs.Catalog;
using SARE.Domain.Catalog;

namespace SARE.Application.Common.Interfaces;

public interface IProductRepository
{
    Task<PagedResponse<ProductResponse>> GetPageAsync(AdminProductQuery query, CancellationToken cancellationToken);
    Task<ProductDetailResponse?> GetDetailsAsync(Guid id, CancellationToken cancellationToken);
    Task<ProductResponse?> GetSummaryAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedResponse<ProductSummaryResponse>> GetPublicPageAsync(ProductQuery query, CancellationToken cancellationToken);
    Task<ProductPublicDetailResponse?> GetPublicDetailsAsync(Guid id, CancellationToken cancellationToken);
    Task<ProductDashboardResponse> GetDashboardAsync(CancellationToken cancellationToken);
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> CategoryExistsAsync(Guid id, CancellationToken cancellationToken);
    void Add(Product product);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
