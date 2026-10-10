using Microsoft.EntityFrameworkCore;
using Npgsql;
using SARE.Application.Common.DTOs;
using SARE.Application.Common.Exceptions;
using SARE.Application.Common.Interfaces;
using SARE.Application.DTOs.Catalog;
using SARE.Domain.Catalog;

namespace SARE.Infrastructure.Persistence.Repositories;

public sealed class ProductVariantRepository(AppDbContext context) : IProductVariantRepository
{
    public Task<Product?> GetProductAsync(Guid id, CancellationToken ct) => context.Products.SingleOrDefaultAsync(product => product.Id == id, ct);
    public Task<ProductVariant?> FindAsync(Guid id, CancellationToken ct) => context.ProductVariants.SingleOrDefaultAsync(variant => variant.Id == id, ct);
    public Task<ProductVariant?> FindByBarcodeAsync(string barcode, CancellationToken ct) => context.ProductVariants.SingleOrDefaultAsync(variant => variant.Barcode == barcode, ct);
    public Task<List<ProductOptionValue>> GetProductValuesAsync(Guid productId, CancellationToken ct) => context.ProductOptionValues.AsNoTracking()
        .Where(value => context.ProductOptions.Any(option => option.Id == value.ProductOptionId && option.ProductId == productId)).ToListAsync(ct);
    public Task<List<VariantOptionValue>> GetLinksAsync(Guid variantId, CancellationToken ct) => context.VariantOptionValues.Where(link => link.VariantId == variantId).ToListAsync(ct);
    public async Task<PagedResponse<ProductVariantResponse>> GetPageAsync(AdminVariantQuery query, CancellationToken ct)
    {
        var variants = context.ProductVariants.AsNoTracking();
        if (query.IsActive.HasValue) variants = variants.Where(variant => variant.IsActive == query.IsActive.Value);
        variants = Filter(variants, query);
        var total = await variants.CountAsync(ct);
        var items = await Page(variants, query).ToListAsync(ct);
        var links = await PageLinksAsync(items, ct);
        return new(items.Select(variant => Map(variant, links)).ToList(), total, query.Page, query.PageSize);
    }
    public async Task<PagedResponse<VariantLookupResponse>> GetPublicPageAsync(VariantQuery query, CancellationToken ct)
    {
        var variants = Filter(AvailableVariants(), query);
        var total = await variants.CountAsync(ct);
        var items = await Page(variants, query).ToListAsync(ct);
        var links = await PageLinksAsync(items, ct);
        var ids = items.Select(variant => variant.ProductId).Distinct().ToArray();
        var products = await context.Products.AsNoTracking().Where(product => ids.Contains(product.Id)).ToDictionaryAsync(product => product.Id, ct);
        return new(items.Select(variant => MapPublic(variant, products[variant.ProductId], links)).ToList(), total, query.Page, query.PageSize);
    }
    public async Task<ProductVariantResponse?> GetDetailsAsync(Guid id, CancellationToken ct)
    {
        var variant = await context.ProductVariants.AsNoTracking().SingleOrDefaultAsync(variant => variant.Id == id, ct);
        return variant is null ? null : Map(variant, await GetLinksAsync(id, ct));
    }
    public async Task<VariantLookupResponse?> GetPublicDetailsAsync(Guid id, CancellationToken ct)
    {
        var variant = await AvailableVariants().SingleOrDefaultAsync(variant => variant.Id == id, ct);
        if (variant is null) return null;
        var product = await context.Products.AsNoTracking().SingleAsync(product => product.Id == variant.ProductId, ct);
        return MapPublic(variant, product, await GetLinksAsync(id, ct));
    }
    public void Add<TEntity>(TEntity entity) where TEntity : class => context.Add(entity);
    public void Remove(VariantOptionValue link) => context.Remove(link);
    public async Task SaveAsync(CancellationToken ct)
    {
        try { await context.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { throw new ConflictException("Product data changed. Reload it and try again."); }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        { throw new ConflictException("Barcode or variant association already exists.", exception); }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
        { throw new ConflictException("Related catalog data changed. Reload the product and try again.", exception); }
    }
    private IQueryable<ProductVariant> AvailableVariants() => context.ProductVariants.AsNoTracking().Where(variant => variant.IsActive
        && context.Products.Any(product => product.Id == variant.ProductId && product.IsActive));
    private IQueryable<ProductVariant> Filter(IQueryable<ProductVariant> variants, VariantQuery query)
    {
        if (query.ProductId.HasValue) variants = variants.Where(variant => variant.ProductId == query.ProductId.Value);
        if (query.CategoryId.HasValue) variants = variants.Where(variant => context.Products.Any(product => product.Id == variant.ProductId && product.CategoryId == query.CategoryId.Value));
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.ToLowerInvariant();
            variants = variants.Where(variant => variant.Barcode.ToLower().Contains(search)
                || context.Products.Any(product => product.Id == variant.ProductId
                    && (product.NameAr.ToLower().Contains(search) || product.NameEn.ToLower().Contains(search))));
        }
        return variants;
    }
    private static IQueryable<ProductVariant> Page(IQueryable<ProductVariant> variants, VariantQuery query) => variants.OrderBy(variant => variant.Barcode)
        .ThenBy(variant => variant.Id).Skip((query.Page - 1) * query.PageSize).Take(query.PageSize);
    private Task<List<VariantOptionValue>> PageLinksAsync(List<ProductVariant> variants, CancellationToken ct)
    {
        var ids = variants.Select(variant => variant.Id).ToArray();
        return ids.Length == 0 ? Task.FromResult(new List<VariantOptionValue>()) : context.VariantOptionValues.AsNoTracking().Where(link => ids.Contains(link.VariantId)).ToListAsync(ct);
    }
    private static Guid[] Values(Guid id, List<VariantOptionValue> links) => links.Where(link => link.VariantId == id).Select(link => link.OptionValueId).Order().ToArray();
    private static ProductVariantResponse Map(ProductVariant variant, List<VariantOptionValue> links) => new(variant.Id, variant.ProductId, variant.Barcode,
        variant.PriceMinor, variant.WeightG, variant.IsActive, variant.UpdatedAt, Values(variant.Id, links));
    private static VariantLookupResponse MapPublic(ProductVariant variant, Product product, List<VariantOptionValue> links) => new(variant.Id, product.Id, product.CategoryId,
        product.NameAr, product.NameEn, variant.Barcode, variant.PriceMinor, variant.WeightG, Values(variant.Id, links));
}
