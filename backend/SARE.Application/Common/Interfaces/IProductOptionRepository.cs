using SARE.Application.DTOs.Catalog;
using SARE.Domain.Catalog;

namespace SARE.Application.Common.Interfaces;

public interface IProductOptionRepository
{
    Task<Product?> GetProductAsync(Guid productId, CancellationToken ct);
    Task<List<ProductOptionResponse>> GetOptionsAsync(Guid productId, CancellationToken ct);
    Task<ProductOption?> FindOptionAsync(Guid productId, Guid optionId, CancellationToken ct);
    Task<List<ProductOptionValueResponse>> GetValuesAsync(Guid optionId, CancellationToken ct);
    Task<ProductOptionValue?> FindValueAsync(Guid optionId, Guid valueId, CancellationToken ct);
    Task<bool> OptionIsUsedAsync(Guid optionId, CancellationToken ct);
    Task<bool> ValueIsUsedAsync(Guid valueId, CancellationToken ct);
    void Add<TEntity>(TEntity entity) where TEntity : class;
    void Remove<TEntity>(TEntity entity) where TEntity : class;
    Task SaveAsync(CancellationToken ct);
}
