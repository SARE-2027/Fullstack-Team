using Microsoft.EntityFrameworkCore;
using SARE.Application.Common.Interfaces;
using SARE.Application.DTOs.Catalog;
using SARE.Domain.Catalog;
using SARE.Infrastructure.Persistence;

namespace SARE.Infrastructure.Services;

public class ProductService(AppDbContext context) : IProductService
{
    public async Task<List<ProductResponseDto>> GetAllProductsAsync(CancellationToken cancellationToken = default)
    {
        var products = await context.Products
            .AsNoTracking()
            .Where(p => p.IsActive)
            .ToListAsync(cancellationToken);

        var variants = await context.ProductVariants
            .AsNoTracking()
            .Where(v => v.IsActive)
            .ToListAsync(cancellationToken);

        var categories = await context.Categories
            .AsNoTracking()
            .ToDictionaryAsync(c => c.Id, c => c.NameEn, cancellationToken);

        return products.Select(p => new ProductResponseDto(
            p.Id,
            p.NameAr,
            p.NameEn,
            p.CategoryId,
            categories.GetValueOrDefault(p.CategoryId),
            p.ImageUrl,
            p.IsActive,
            variants.Where(v => v.ProductId == p.Id)
                    .Select(v => new ProductVariantDto(v.Id, v.Barcode, v.PriceMinor, v.WeightG, v.IsActive))
                    .ToList()
        )).ToList();
    }

    public async Task<ProductResponseDto?> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (product == null) return null;

        var variants = await context.ProductVariants
            .AsNoTracking()
            .Where(v => v.ProductId == id && v.IsActive)
            .Select(v => new ProductVariantDto(v.Id, v.Barcode, v.PriceMinor, v.WeightG, v.IsActive))
            .ToListAsync(cancellationToken);

        var category = await context.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == product.CategoryId, cancellationToken);

        return new ProductResponseDto(
            product.Id,
            product.NameAr,
            product.NameEn,
            product.CategoryId,
            category?.NameEn,
            product.ImageUrl,
            product.IsActive,
            variants
        );
    }

    public async Task<ProductResponseDto?> GetProductByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        var variant = await context.ProductVariants
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Barcode == barcode, cancellationToken);

        if (variant == null) return null;

        return await GetProductByIdAsync(variant.ProductId, cancellationToken);
    }

    public async Task<ProductResponseDto> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            CategoryId = request.CategoryId,
            NameAr = request.NameAr,
            NameEn = request.NameEn,
            ImageUrl = request.ImageUrl,
            IsActive = true,
            UpdatedAt = DateTime.UtcNow
        };

        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            Barcode = request.Barcode,
            PriceMinor = request.PriceMinor,
            WeightG = request.WeightG,
            IsActive = true,
            UpdatedAt = DateTime.UtcNow
        };

        context.Products.Add(product);
        context.ProductVariants.Add(variant);
        await context.SaveChangesAsync(cancellationToken);

        return new ProductResponseDto(
            product.Id,
            product.NameAr,
            product.NameEn,
            product.CategoryId,
            null,
            product.ImageUrl,
            product.IsActive,
            [new ProductVariantDto(variant.Id, variant.Barcode, variant.PriceMinor, variant.WeightG, variant.IsActive)]
        );
    }
}
