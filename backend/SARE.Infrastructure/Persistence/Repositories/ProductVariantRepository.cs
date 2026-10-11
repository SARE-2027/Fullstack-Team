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
    public Task<Product?> GetProductAsync(Guid id, CancellationToken ct) =>
        context.Products.SingleOrDefaultAsync(product => product.Id == id, ct);

    public Task<ProductVariant?> FindAsync(Guid id, CancellationToken ct) =>
        context.ProductVariants.SingleOrDefaultAsync(variant => variant.Id == id, ct);

    public Task<ProductVariant?> FindByBarcodeAsync(string barcode, CancellationToken ct) =>
        context.ProductVariants.SingleOrDefaultAsync(variant => variant.Barcode == barcode, ct);

    public Task<List<ProductOptionValue>> GetProductValuesAsync(Guid productId, CancellationToken ct) =>
        context.ProductOptionValues
            .AsNoTracking()
            .Where(value => context.ProductOptions.Any(option =>
                option.Id == value.ProductOptionId && option.ProductId == productId))
            .ToListAsync(ct);

    public Task<List<VariantOptionValue>> GetLinksAsync(Guid variantId, CancellationToken ct) =>
        context.VariantOptionValues
            .Where(link => link.VariantId == variantId)
            .ToListAsync(ct);

    public async Task<PagedResponse<ProductVariantResponse>> GetPageAsync(AdminVariantQuery query, CancellationToken ct)
    {
        var variants = context.ProductVariants.AsNoTracking();
        if (query.IsActive.HasValue)
        {
            variants = variants.Where(variant => variant.IsActive == query.IsActive.Value);
        }

        variants = Filter(variants, query);
        var total = await variants.CountAsync(ct);
        var items = await Page(variants, query).ToListAsync(ct);
        var links = await PageLinksAsync(items, ct);
        var optionsByProduct = await ReadOptionsAsync(items, publicOnly: false, ct);
        var responses = items.Select(variant =>
        {
            var productOptions = optionsByProduct.GetValueOrDefault(variant.ProductId, []);
            return MapVariant(variant, links, productOptions);
        }).ToList();

        return new PagedResponse<ProductVariantResponse>(responses, total, query.Page, query.PageSize);
    }

    public async Task<PagedResponse<VariantLookupResponse>> GetPublicPageAsync(VariantQuery query, CancellationToken ct)
    {
        var variants = Filter(AvailableVariants(), query);
        var total = await variants.CountAsync(ct);
        var items = await Page(variants, query).ToListAsync(ct);
        var links = await PageLinksAsync(items, ct);
        var optionsByProduct = await ReadOptionsAsync(items, publicOnly: true, ct);
        var productIds = items.Select(variant => variant.ProductId).Distinct().ToArray();
        var productsById = await context.Products
            .AsNoTracking()
            .Where(product => productIds.Contains(product.Id))
            .ToDictionaryAsync(product => product.Id, ct);

        var responses = items.Select(variant =>
        {
            var product = productsById[variant.ProductId];
            var productOptions = optionsByProduct.GetValueOrDefault(variant.ProductId, []);
            return MapPublicVariant(variant, product, links, productOptions);
        }).ToList();

        return new PagedResponse<VariantLookupResponse>(responses, total, query.Page, query.PageSize);
    }

    public async Task<ProductVariantResponse?> GetDetailsAsync(Guid id, CancellationToken ct)
    {
        var variant = await context.ProductVariants.AsNoTracking().SingleOrDefaultAsync(variant => variant.Id == id, ct);
        if (variant is null)
        {
            return null;
        }

        var optionsByProduct = await ReadOptionsAsync([variant], publicOnly: false, ct);
        var links = await GetLinksAsync(id, ct);
        var productOptions = optionsByProduct.GetValueOrDefault(variant.ProductId, []);

        return MapVariant(variant, links, productOptions);
    }

    public async Task<VariantLookupResponse?> GetPublicDetailsAsync(Guid id, CancellationToken ct)
    {
        var variant = await AvailableVariants().SingleOrDefaultAsync(variant => variant.Id == id, ct);
        if (variant is null)
        {
            return null;
        }

        var product = await context.Products
            .AsNoTracking()
            .SingleAsync(product => product.Id == variant.ProductId, ct);
        var optionsByProduct = await ReadOptionsAsync([variant], publicOnly: true, ct);
        var links = await GetLinksAsync(id, ct);
        var productOptions = optionsByProduct.GetValueOrDefault(variant.ProductId, []);

        return MapPublicVariant(variant, product, links, productOptions);
    }

    public void Add<TEntity>(TEntity entity) where TEntity : class => context.Add(entity);

    public void Remove(VariantOptionValue link) => context.Remove(link);

    public async Task SaveAsync(CancellationToken ct)
    {
        try
        {
            await context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("Product data changed. Reload it and try again.");
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation
        })
        {
            throw new ConflictException("Barcode or variant association already exists.", exception);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.ForeignKeyViolation
        })
        {
            throw new ConflictException("Related catalog data changed. Reload the product and try again.", exception);
        }
    }

    private IQueryable<ProductVariant> AvailableVariants() =>
        context.ProductVariants
            .AsNoTracking()
            .Where(variant => variant.IsActive && context.Products.Any(product =>
                product.Id == variant.ProductId && product.IsActive));

    private IQueryable<ProductVariant> Filter(IQueryable<ProductVariant> variants, VariantQuery query)
    {
        if (query.ProductId.HasValue)
        {
            variants = variants.Where(variant => variant.ProductId == query.ProductId.Value);
        }

        if (query.CategoryId.HasValue)
        {
            variants = variants.Where(variant => context.Products.Any(product =>
                product.Id == variant.ProductId && product.CategoryId == query.CategoryId.Value));
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.ToLowerInvariant();
            variants = variants.Where(variant =>
                variant.Barcode.ToLower().Contains(search) ||
                context.Products.Any(product =>
                    product.Id == variant.ProductId &&
                    (product.NameAr.ToLower().Contains(search) || product.NameEn.ToLower().Contains(search))));
        }

        return variants;
    }

    private static IQueryable<ProductVariant> Page(IQueryable<ProductVariant> variants, VariantQuery query) =>
        variants
            .OrderBy(variant => variant.Barcode)
            .ThenBy(variant => variant.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize);

    private Task<List<VariantOptionValue>> PageLinksAsync(List<ProductVariant> variants, CancellationToken ct)
    {
        var variantIds = variants.Select(variant => variant.Id).ToArray();
        if (variantIds.Length == 0)
        {
            return Task.FromResult(new List<VariantOptionValue>());
        }

        return context.VariantOptionValues
            .AsNoTracking()
            .Where(link => variantIds.Contains(link.VariantId))
            .ToListAsync(ct);
    }

    private static Guid[] GetSelectedValueIds(Guid variantId, List<VariantOptionValue> links) =>
        links
            .Where(link => link.VariantId == variantId)
            .Select(link => link.OptionValueId)
            .Order()
            .ToArray();

    private Task<Dictionary<Guid, IReadOnlyList<ProductOptionResponse>>> ReadOptionsAsync(
        IReadOnlyList<ProductVariant> variants,
        bool publicOnly,
        CancellationToken ct)
    {
        var productIds = variants
            .Select(variant => variant.ProductId)
            .Distinct()
            .ToArray();

        return ProductRepository.ReadOptionsAsync(context, productIds, publicOnly, ct);
    }

    private static IReadOnlyList<VariantOptionResponse> MapOptions(
        IReadOnlyList<ProductOptionResponse> options,
        Guid[] selectedValueIds)
    {
        return options.Select(option =>
        {
            var selectedValue = option.Values.FirstOrDefault(value => selectedValueIds.Contains(value.Id));

            return new VariantOptionResponse(
                Id: option.Id,
                NameAr: option.NameAr,
                NameEn: option.NameEn,
                SelectedValueId: selectedValue?.Id,
                Values: option.Values);
        }).ToArray();
    }

    private static ProductVariantResponse MapVariant(
        ProductVariant variant,
        List<VariantOptionValue> links,
        IReadOnlyList<ProductOptionResponse> options)
    {
        var selectedValueIds = GetSelectedValueIds(variant.Id, links);
        var variantOptions = MapOptions(options, selectedValueIds);

        return new ProductVariantResponse(
            Id: variant.Id,
            ProductId: variant.ProductId,
            Barcode: variant.Barcode,
            PriceMinor: variant.PriceMinor,
            WeightG: variant.WeightG,
            IsActive: variant.IsActive,
            UpdatedAt: variant.UpdatedAt,
            OptionValueIds: selectedValueIds,
            Options: variantOptions);
    }

    private static VariantLookupResponse MapPublicVariant(
        ProductVariant variant,
        Product product,
        List<VariantOptionValue> links,
        IReadOnlyList<ProductOptionResponse> options)
    {
        var selectedValueIds = GetSelectedValueIds(variant.Id, links);
        var variantOptions = MapOptions(options, selectedValueIds);

        return new VariantLookupResponse(
            Id: variant.Id,
            ProductId: product.Id,
            CategoryId: product.CategoryId,
            ProductNameAr: product.NameAr,
            ProductNameEn: product.NameEn,
            Barcode: variant.Barcode,
            PriceMinor: variant.PriceMinor,
            WeightG: variant.WeightG,
            OptionValueIds: selectedValueIds,
            Options: variantOptions);
    }
}
