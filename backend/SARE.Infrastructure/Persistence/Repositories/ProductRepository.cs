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
    public async Task<PagedResponse<ProductResponse>> GetPageAsync(AdminProductQuery query, CancellationToken cancellationToken)
    {
        var products = context.Products.AsNoTracking();
        if (query.IsActive.HasValue)
        {
            products = products.Where(product => product.IsActive == query.IsActive.Value);
        }

        var page = await GetPageAsync(products, query, AdminProjection(), includeInactiveVariants: true, cancellationToken);
        var productIds = page.Items.Select(product => product.Id).ToArray();
        var optionsByProduct = await ReadOptionsAsync(context, productIds, publicOnly: false, cancellationToken);
        var items = page.Items.Select(product => product with
        {
            Options = optionsByProduct.GetValueOrDefault(product.Id, [])
        }).ToArray();

        return page with { Items = items };
    }

    public async Task<PagedResponse<ProductSummaryResponse>> GetPublicPageAsync(ProductQuery query, CancellationToken cancellationToken)
    {
        var page = await GetPageAsync(AvailableProducts(), query, PublicProjection(), includeInactiveVariants: false, cancellationToken);
        var productIds = page.Items.Select(product => product.Id).ToArray();
        var optionsByProduct = await ReadOptionsAsync(context, productIds, publicOnly: true, cancellationToken);
        var items = page.Items.Select(product => product with
        {
            Options = optionsByProduct.GetValueOrDefault(product.Id, [])
        }).ToArray();

        return page with { Items = items };
    }

    public async Task<ProductDetailResponse?> GetDetailsAsync(Guid id, CancellationToken cancellationToken)
    {
        var product = await GetSummaryAsync(id, cancellationToken);
        if (product is null) return null;

        var options = await ReadOptionsAsync(context, [id], false, cancellationToken);
        return new ProductDetailResponse(product, options.GetValueOrDefault(id, []));
    }

    public Task<ProductResponse?> GetSummaryAsync(Guid id, CancellationToken cancellationToken) =>
        context.Products.AsNoTracking().Where(product => product.Id == id)
            .Select(AdminProjection()).SingleOrDefaultAsync(cancellationToken);

    public async Task<ProductPublicDetailResponse?> GetPublicDetailsAsync(Guid id, CancellationToken cancellationToken)
    {
        var product = await AvailableProducts().Where(product => product.Id == id)
            .Select(PublicProjection()).SingleOrDefaultAsync(cancellationToken);
        if (product is null) return null;

        var options = await ReadOptionsAsync(context, [id], true, cancellationToken);
        return new ProductPublicDetailResponse(product, options.GetValueOrDefault(id, []));
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

    /// <summary>
    /// يجلب كل الـ Options والـ Values لمجموعة منتجات دفعة واحدة (Batch Loading) في استعلامين SQL فقط لمنع مشكلة N+1 Query.
    /// </summary>
    internal static async Task<Dictionary<Guid, IReadOnlyList<ProductOptionResponse>>> ReadOptionsAsync(
        AppDbContext context,
        Guid[] productIds,
        bool publicOnly,
        CancellationToken ct)
    {
        // 1. فحص الحراسة: لو مفيش منتجات مبعوثة، نرجع فوراً بديكشنري فاضي بدون الذهاب للداتابيز
        if (productIds.Length == 0)
        {
            return [];
        }

        // 2. [الاستعلام الأول]: جلب جميع الـ Options التابعة لهذه المنتجات دفعة واحدة
        var options = await context.ProductOptions
            .AsNoTracking()
            .Where(option => productIds.Contains(option.ProductId))
            .OrderBy(option => option.NameEn)
            .ThenBy(option => option.Id)
            .ToListAsync(ct);

        if (options.Count == 0)
        {
            return [];
        }

        // 3. تحضير الاستعلام الثاني لجلب الـ Values التابعة للـ Options التي تم جلبها
        var optionIds = options.Select(option => option.Id).ToArray();
        var valueQuery = context.ProductOptionValues
            .AsNoTracking()
            .Where(value => optionIds.Contains(value.ProductOptionId));

        // 4. فلترة العرض العام (publicOnly):
        // في حالة العرض للزبون، نستبعد أي قيمة غير مستخدمة في فاريانت نشط (Active Variant)
        // حتى لا يظهر للزبون خيار في القائمة ليس له منتج متاح للبيع في المخزن.
        if (publicOnly)
        {
            valueQuery = valueQuery.Where(value => context.VariantOptionValues.Any(link =>
                link.OptionValueId == value.Id &&
                context.ProductVariants.Any(variant =>
                    variant.Id == link.VariantId &&
                    variant.IsActive &&
                    context.ProductOptions.Any(option =>
                        option.Id == value.ProductOptionId && option.ProductId == variant.ProductId))));
        }

        // 5. [الاستعلام الثاني]: تنفيذ استعلام الـ Values وترتيبها
        var values = await valueQuery
            .OrderBy(value => value.ValueEn)
            .ThenBy(value => value.Id)
            .ToListAsync(ct);

        // 6. تجميع القيم في الذاكرة (ToLookup): خريطة Multi-Map حيث كل OptionId يشاور على قائمة الـ Values التابعة له بأمان
        var valuesByOption = values.ToLookup(value => value.ProductOptionId);

        // 7. تجميع الـ Options لكل منتج (GroupBy ProductId):
        // إذا كنا في وضع العرض العام، نستبعد أي أوبشن ليس له أي قيم متاحة للبيع
        var optionsByProduct = options
            .Where(option => !publicOnly || valuesByOption[option.Id].Any())
            .GroupBy(option => option.ProductId);

        // 8. بناء الديكشنري النهائي:
        // المفتاح: ProductId (الـ Key الخاص بكل جروب)
        // القيمة: تحويل كل أوبشن ومصفوفة قيمه إلى ProductOptionResponse
        return optionsByProduct.ToDictionary(
            productOptions => productOptions.Key,
            productOptions => (IReadOnlyList<ProductOptionResponse>)productOptions.Select(option =>
            {
                // تحويل كل Value تابعة لهذا الأوبشن إلى DTO
                var optionValues = valuesByOption[option.Id]
                    .Select(value => new ProductOptionValueResponse(value.Id, value.ValueAr, value.ValueEn))
                    .ToArray();

                return new ProductOptionResponse(option.Id, option.NameAr, option.NameEn, optionValues);
            }).ToArray());
    }
}
