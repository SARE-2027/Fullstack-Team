using SARE.Application.Common.DTOs;
using SARE.Application.DTOs.Catalog;
using SARE.Domain.Catalog;

namespace SARE.Application.Common.Interfaces;

public interface ICategoryRepository
{
    Task<PagedResponse<CategoryResponse>> GetPageAsync(CategoryQuery query, CancellationToken cancellationToken);
    Task<CategoryResponse?> GetDetailsAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedResponse<CategorySummaryResponse>> GetPublicPageAsync(CategoryQuery query, CancellationToken cancellationToken);
    Task<CategorySummaryResponse?> GetPublicDetailsAsync(Guid id, CancellationToken cancellationToken);
    Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> HasProductsAsync(Guid id, CancellationToken cancellationToken);
    void Add(Category category);
    void Remove(Category category);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
