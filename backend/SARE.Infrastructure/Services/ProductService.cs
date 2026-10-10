using Microsoft.EntityFrameworkCore;
using SARE.Application.Common.Interfaces;
using SARE.Application.DTOs.Catalog;
using SARE.Domain.Catalog;
using SARE.Infrastructure.Persistence;
using FluentValidation;
using FluentValidation.Results;
using SARE.Application.Common.Exceptions;
using Npgsql;
using ValidationException = FluentValidation.ValidationException;

namespace SARE.Infrastructure.Services;

public class ProductService(AppDbContext context, IValidator<ProductRequest> productValidator,
    IValidator<VariantRequest> variantValidator) : IProductService
{
    public async Task<List<ProductResponseDto>> GetAllProductsAsync(CancellationToken cancellationToken = default)
    {
        var products = await context.Products
            .AsNoTracking()
            .Where(p => p.IsActive && context.ProductVariants.Any(v => v.ProductId == p.Id && v.IsActive))
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
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive &&
                context.ProductVariants.Any(v => v.ProductId == p.Id && v.IsActive), cancellationToken);

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
            .FirstOrDefaultAsync(v => v.Barcode == barcode && v.IsActive, cancellationToken);

        if (variant == null) return null;

        return await GetProductByIdAsync(variant.ProductId, cancellationToken);
    }

    public async Task<ProductResponseDto> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var productRequest = new ProductRequest(request.CategoryId, request.NameAr?.Trim(), request.NameEn?.Trim(),
            string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim());
        var variantRequest = new VariantRequest(request.Barcode?.Trim(), request.PriceMinor, request.WeightG, true, []);
        await productValidator.ValidateAndThrowAsync(productRequest, cancellationToken);
        await variantValidator.ValidateAndThrowAsync(variantRequest, cancellationToken);
        if (productRequest.ImageUrl?.StartsWith(IProductImageStore.UrlPrefix, StringComparison.OrdinalIgnoreCase) == true)
            throw new ValidationException([new ValidationFailure("ImageUrl", "Use the product image upload endpoint.")]);
        if (!await context.Categories.AnyAsync(c => c.Id == request.CategoryId, cancellationToken))
            throw new ValidationException([new ValidationFailure("CategoryId", "Category does not exist.")]);
        if (await context.ProductVariants.AnyAsync(v => v.Barcode == variantRequest.Barcode, cancellationToken))
            throw new ConflictException("Barcode already exists.");
        var product = new Product
        {
            Id = Guid.NewGuid(),
            CategoryId = request.CategoryId,
            NameAr = productRequest.NameAr!,
            NameEn = productRequest.NameEn!,
            ImageUrl = productRequest.ImageUrl,
            IsActive = true,
            UpdatedAt = DateTime.UtcNow
        };

        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            Barcode = variantRequest.Barcode!,
            PriceMinor = request.PriceMinor,
            WeightG = request.WeightG,
            IsActive = true,
            UpdatedAt = DateTime.UtcNow
        };

        context.Products.Add(product);
        context.ProductVariants.Add(variant);
        try { await context.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation or PostgresErrorCodes.ForeignKeyViolation })
        { throw new ConflictException("Related catalog data changed or the barcode already exists.", ex); }

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
