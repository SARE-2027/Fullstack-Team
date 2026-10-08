using Microsoft.EntityFrameworkCore;
using SARE.Application.Common.Interfaces;
using SARE.Application.DTOs.Catalog;

namespace SARE.Infrastructure.Persistence.Repositories;

public sealed class CatalogDashboardRepository(AppDbContext context) : ICatalogDashboardRepository
{
    public async Task<CatalogDashboardResponse> GetSummaryAsync(CancellationToken cancellationToken)
    {
        // One statement keeps all counters in the same database snapshot.
        var counts = await context.Categories.Select(category => new
        {
            Categories = context.Categories.Count(),
            Products = context.Products.Count(),
            ActiveProducts = context.Products.Count(product => product.IsActive),
            AvailableProducts = context.Products.Count(product => product.IsActive &&
                context.ProductVariants.Any(variant => variant.ProductId == product.Id && variant.IsActive)),
            WithoutActiveVariants = context.Products.Count(product =>
                !context.ProductVariants.Any(variant => variant.ProductId == product.Id && variant.IsActive)),
            Variants = context.ProductVariants.Count(),
            ActiveVariants = context.ProductVariants.Count(variant => variant.IsActive),
            AvailableVariants = context.ProductVariants.Count(variant => variant.IsActive &&
                context.Products.Any(product => product.Id == variant.ProductId && product.IsActive)),
            Options = context.ProductOptions.Count(),
            Values = context.ProductOptionValues.Count()
        }).FirstOrDefaultAsync(cancellationToken);

        return counts is null ? new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)
            : new(counts.Categories, counts.Products, counts.ActiveProducts, counts.Products - counts.ActiveProducts,
                counts.AvailableProducts, counts.WithoutActiveVariants, counts.Variants, counts.ActiveVariants,
                counts.Variants - counts.ActiveVariants, counts.AvailableVariants, counts.Options, counts.Values);
    }
}
