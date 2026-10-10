using SARE.Application.Common.DTOs;
using SARE.Application.DTOs.Catalog;
using SARE.Domain.Catalog;

namespace SARE.Application.Common.Interfaces;

public interface IProductVariantRepository
{
    Task<Product?> GetProductAsync(Guid id, CancellationToken ct);
    Task<ProductVariant?> FindAsync(Guid id, CancellationToken ct);
    Task<ProductVariant?> FindByBarcodeAsync(string barcode, CancellationToken ct);
    Task<List<ProductOptionValue>> GetProductValuesAsync(Guid productId, CancellationToken ct);
    Task<List<VariantOptionValue>> GetLinksAsync(Guid variantId, CancellationToken ct);
    Task<PagedResponse<ProductVariantResponse>> GetPageAsync(AdminVariantQuery query, CancellationToken ct);
    Task<PagedResponse<VariantLookupResponse>> GetPublicPageAsync(VariantQuery query, CancellationToken ct);
    Task<ProductVariantResponse?> GetDetailsAsync(Guid id, CancellationToken ct);
    Task<VariantLookupResponse?> GetPublicDetailsAsync(Guid id, CancellationToken ct);
    void Add<TEntity>(TEntity entity) where TEntity : class;
    void Remove(VariantOptionValue link);
    Task SaveAsync(CancellationToken ct);
}
