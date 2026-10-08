using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SARE.Application.Common.DTOs;
using SARE.Application.Common.Exceptions;
using SARE.Application.Common.Interfaces;
using SARE.Application.DTOs.Catalog;
using SARE.Domain.Catalog;

namespace SARE.Infrastructure.Persistence.Repositories;

public sealed class ProductRepository(AppDbContext context) : IProductRepository
{
    public Task<PagedResponse<ProductResponse>> GetPageAsync(AdminProductQuery query, CancellationToken cancellationToken)
    {
        var products = context.Products.AsNoTracking();
        if (query.IsActive.HasValue) products = products.Where(product => product.IsActive == query.IsActive.Value);
        return GetPageAsync(products, query, AdminProjection(), includeInactiveVariants: true, cancellationToken);
    }

    public Task<PagedResponse<ProductSummaryResponse>> GetPublicPageAsync(ProductQuery query, CancellationToken cancellationToken) =>
        GetPageAsync(AvailableProducts(), query, PublicProjection(), includeInactiveVariants: false, cancellationToken);

    public async Task<ProductDetailResponse?> GetDetailsAsync(Guid id, CancellationToken cancellationToken)
    {
        var product = await GetSummaryAsync(id, cancellationToken);
        if (product is null) return null;

        var variants = await context.ProductVariants.AsNoTracking().Where(variant => variant.ProductId == id)
            .OrderBy(variant => variant.Barcode).ThenBy(variant => variant.Id).ToListAsync(cancellationToken);
        var links = await GetLinksAsync(variants.Select(variant => variant.Id).ToArray(), cancellationToken);
        var options = await GetOptionsAsync(id, null, cancellationToken);
        return new ProductDetailResponse(product, options, variants.Select(variant => new ProductVariantResponse(
            variant.Id, variant.ProductId, variant.Barcode, variant.PriceMinor, variant.WeightG,
            variant.IsActive, variant.UpdatedAt, links.Where(link => link.VariantId == variant.Id)
                .Select(link => link.OptionValueId).Order().ToArray())).ToList());
    }

    public Task<ProductResponse?> GetSummaryAsync(Guid id, CancellationToken cancellationToken) =>
        context.Products.AsNoTracking().Where(product => product.Id == id)
            .Select(AdminProjection()).SingleOrDefaultAsync(cancellationToken);

    public async Task<ProductPublicDetailResponse?> GetPublicDetailsAsync(Guid id, CancellationToken cancellationToken)
    {
        var product = await AvailableProducts().Where(product => product.Id == id)
            .Select(PublicProjection()).SingleOrDefaultAsync(cancellationToken);
        if (product is null) return null;

        var variants = await context.ProductVariants.AsNoTracking()
            .Where(variant => variant.ProductId == id && variant.IsActive)
            .OrderBy(variant => variant.Barcode).ThenBy(variant => variant.Id).ToListAsync(cancellationToken);
        if (variants.Count == 0) return null;
        var links = await GetLinksAsync(variants.Select(variant => variant.Id).ToArray(), cancellationToken);
        var options = await GetOptionsAsync(id, links.Select(link => link.OptionValueId).Distinct().ToArray(), cancellationToken);
        return new ProductPublicDetailResponse(product, options,
            variants.Select(variant => new ProductVariantSummaryResponse(
                variant.Id, variant.Barcode, variant.PriceMinor, variant.WeightG,
                links.Where(link => link.VariantId == variant.Id).Select(link => link.OptionValueId).Order().ToArray())).ToList());
    }

    public async Task<ProductDashboardResponse> GetDashboardAsync(CancellationToken cancellationToken)
    {
        // Scalar subqueries share one database statement/snapshot, including during concurrent edits.
        var counts = await context.Products.Select(product => new
        {
            Total = context.Products.Count(),
            Active = context.Products.Count(item => item.IsActive),
            WithoutActiveVariants = context.Products.Count(item =>
                !context.ProductVariants.Any(variant => variant.ProductId == item.Id && variant.IsActive)),
            Available = context.Products.Count(item => item.IsActive &&
                context.ProductVariants.Any(variant => variant.ProductId == item.Id && variant.IsActive))
        }).FirstOrDefaultAsync(cancellationToken);

        return counts is null ? new ProductDashboardResponse(0, 0, 0, 0, 0)
            : new ProductDashboardResponse(counts.Total, counts.Active, counts.Total - counts.Active,
                counts.WithoutActiveVariants, counts.Available);
    }

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Products.SingleOrDefaultAsync(product => product.Id == id, cancellationToken);

    public Task<bool> CategoryExistsAsync(Guid id, CancellationToken cancellationToken) =>
        context.Categories.AnyAsync(category => category.Id == id, cancellationToken);

    public void Add(Product product) => context.Products.Add(product);

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
            throw new ConflictException("The selected category no longer exists. Select an existing category and try again.", exception);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("Product data changed or no longer exists. Reload it and try again.");
        }
    }

    private IQueryable<Product> AvailableProducts() => context.Products.AsNoTracking().Where(product =>
        product.IsActive && context.ProductVariants.Any(variant => variant.ProductId == product.Id && variant.IsActive));

    private async Task<PagedResponse<T>> GetPageAsync<T>(IQueryable<Product> products, ProductQuery query,
        Expression<Func<Product, T>> projection, bool includeInactiveVariants, CancellationToken cancellationToken)
    {
        if (query.CategoryId.HasValue) products = products.Where(product => product.CategoryId == query.CategoryId.Value);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.ToLowerInvariant();
            products = products.Where(product => product.NameAr.ToLower().Contains(search)
                || product.NameEn.ToLower().Contains(search)
                || context.ProductVariants.Any(variant => variant.ProductId == product.Id
                    && variant.Barcode.ToLower().Contains(search)
                    && (includeInactiveVariants || variant.IsActive)));
        }

        var totalCount = await products.CountAsync(cancellationToken);
        IOrderedQueryable<Product> ordered = (query.SortBy!.ToLowerInvariant(), query.SortDescending) switch
        {
            ("namear", false) => products.OrderBy(product => product.NameAr),
            ("namear", true) => products.OrderByDescending(product => product.NameAr),
            ("updatedat", false) => products.OrderBy(product => product.UpdatedAt),
            ("updatedat", true) => products.OrderByDescending(product => product.UpdatedAt),
            (_, false) => products.OrderBy(product => product.NameEn),
            (_, true) => products.OrderByDescending(product => product.NameEn)
        };
        var items = await ordered.ThenBy(product => product.Id).Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize).Select(projection).ToListAsync(cancellationToken);
        return new PagedResponse<T>(items, totalCount, query.Page, query.PageSize);
    }

    private Expression<Func<Product, ProductResponse>> AdminProjection() => product => new ProductResponse(
        product.Id, product.CategoryId, product.NameAr, product.NameEn, product.ImageUrl, product.IsActive, product.UpdatedAt,
        context.Categories.Where(category => category.Id == product.CategoryId).Select(category => category.NameAr).First(),
        context.Categories.Where(category => category.Id == product.CategoryId).Select(category => category.NameEn).First(),
        context.ProductVariants.Count(variant => variant.ProductId == product.Id),
        context.ProductVariants.Count(variant => variant.ProductId == product.Id && variant.IsActive));

    private Expression<Func<Product, ProductSummaryResponse>> PublicProjection() => product => new ProductSummaryResponse(
        product.Id, product.CategoryId, product.NameAr, product.NameEn, product.ImageUrl,
        context.Categories.Where(category => category.Id == product.CategoryId).Select(category => category.NameAr).First(),
        context.Categories.Where(category => category.Id == product.CategoryId).Select(category => category.NameEn).First(),
        context.ProductVariants.Where(variant => variant.ProductId == product.Id && variant.IsActive)
            .Select(variant => (int?)variant.PriceMinor).Min(),
        context.ProductVariants.Where(variant => variant.ProductId == product.Id && variant.IsActive)
            .Select(variant => (int?)variant.PriceMinor).Max());

    private Task<List<VariantOptionValue>> GetLinksAsync(Guid[] variantIds, CancellationToken cancellationToken) =>
        variantIds.Length == 0 ? Task.FromResult(new List<VariantOptionValue>())
            : context.VariantOptionValues.AsNoTracking().Where(link => variantIds.Contains(link.VariantId)).ToListAsync(cancellationToken);

    private async Task<IReadOnlyList<ProductOptionResponse>> GetOptionsAsync(Guid productId,
        Guid[]? selectedValueIds, CancellationToken cancellationToken)
    {
        var options = await context.ProductOptions.AsNoTracking().Where(option => option.ProductId == productId)
            .OrderBy(option => option.NameEn).ThenBy(option => option.Id).ToListAsync(cancellationToken);
        if (options.Count == 0) return [];
        var optionIds = options.Select(option => option.Id).ToArray();
        var valueQuery = context.ProductOptionValues.AsNoTracking().Where(value => optionIds.Contains(value.ProductOptionId));
        if (selectedValueIds is not null) valueQuery = valueQuery.Where(value => selectedValueIds.Contains(value.Id));
        var values = await valueQuery.OrderBy(value => value.ValueEn).ThenBy(value => value.Id).ToListAsync(cancellationToken);
        return options.Where(option => selectedValueIds is null || values.Any(value => value.ProductOptionId == option.Id))
            .Select(option => new ProductOptionResponse(option.Id, option.NameAr, option.NameEn,
                values.Where(value => value.ProductOptionId == option.Id)
                    .Select(value => new ProductOptionValueResponse(value.Id, value.ValueAr, value.ValueEn)).ToList())).ToList();
    }
}
