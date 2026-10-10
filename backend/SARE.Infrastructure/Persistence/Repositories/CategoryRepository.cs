using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SARE.Application.Common.DTOs;
using SARE.Application.Common.Exceptions;
using SARE.Application.Common.Interfaces;
using SARE.Application.DTOs.Catalog;
using SARE.Domain.Catalog;

namespace SARE.Infrastructure.Persistence.Repositories;

public sealed class CategoryRepository(AppDbContext context) : ICategoryRepository
{
    public Task<PagedResponse<CategoryResponse>> GetPageAsync(CategoryQuery query, CancellationToken cancellationToken) =>
        GetPageAsync(query, category => new CategoryResponse(category.Id, category.NameAr, category.NameEn,
            context.Products.Count(product => product.CategoryId == category.Id)), cancellationToken);

    public Task<PagedResponse<CategorySummaryResponse>> GetPublicPageAsync(CategoryQuery query, CancellationToken cancellationToken) =>
        GetPageAsync(query, category => new CategorySummaryResponse(category.Id, category.NameAr, category.NameEn), cancellationToken);

    private async Task<PagedResponse<T>> GetPageAsync<T>(CategoryQuery query,
        Expression<Func<Category, T>> projection, CancellationToken cancellationToken)
    {
        var categories = context.Categories.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.ToLowerInvariant();
            categories = categories.Where(category =>
                category.NameAr.ToLower().Contains(search) || category.NameEn.ToLower().Contains(search));
        }

        var totalCount = await categories.CountAsync(cancellationToken);
        var page = categories.OrderBy(category => category.NameEn).ThenBy(category => category.Id)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize);
        var items = await page.Select(projection).ToListAsync(cancellationToken);
        return new PagedResponse<T>(items, totalCount, query.Page, query.PageSize);
    }

    public Task<CategoryResponse?> GetDetailsAsync(Guid id, CancellationToken cancellationToken) =>
        SelectDetails(context.Categories.AsNoTracking().Where(category => category.Id == id))
            .SingleOrDefaultAsync(cancellationToken);

    public Task<CategorySummaryResponse?> GetPublicDetailsAsync(Guid id, CancellationToken cancellationToken) =>
        context.Categories.AsNoTracking().Where(category => category.Id == id)
            .Select(category => new CategorySummaryResponse(category.Id, category.NameAr, category.NameEn))
            .SingleOrDefaultAsync(cancellationToken);

    public Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Categories.SingleOrDefaultAsync(category => category.Id == id, cancellationToken);

    public Task<bool> HasProductsAsync(Guid id, CancellationToken cancellationToken) =>
        context.Products.AnyAsync(product => product.CategoryId == id, cancellationToken);

    public void Add(Category category) => context.Categories.Add(category);
    public void Remove(Category category) => context.Categories.Remove(category);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.ForeignKeyViolation,
            ConstraintName: "fk_products_categories_category_id"
        })
        {
            // A product may have been added after the application's existence check.
            throw new ConflictException("Category cannot be deleted while it contains products. Move the products to another category first.", exception);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new NotFoundException("Category no longer exists.");
        }
    }

    private IQueryable<CategoryResponse> SelectDetails(IQueryable<Category> categories) =>
        categories.Select(category => new CategoryResponse(category.Id, category.NameAr, category.NameEn,
            context.Products.Count(product => product.CategoryId == category.Id)));
}
