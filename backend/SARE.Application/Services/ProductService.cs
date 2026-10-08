using FluentValidation;
using FluentValidation.Results;
using SARE.Application.Common.DTOs;
using SARE.Application.Common.Exceptions;
using SARE.Application.Common.Interfaces;
using SARE.Application.DTOs.Catalog;
using SARE.Domain.Catalog;

namespace SARE.Application.Services;

public sealed class ProductService(
    IProductRepository repository,
    IProductImageStore imageStore,
    IValidator<ProductRequest> requestValidator,
    IValidator<ProductStatusRequest> statusValidator,
    IValidator<ProductQuery> queryValidator,
    IValidator<AdminProductQuery> adminQueryValidator)
{
    public async Task<PagedResponse<ProductResponse>> GetPageAsync(AdminProductQuery query, CancellationToken cancellationToken)
    {
        query = query with { Search = query.Search?.Trim(), SortBy = query.SortBy?.Trim() };
        await adminQueryValidator.ValidateAndThrowAsync(query, cancellationToken);
        return await repository.GetPageAsync(query, cancellationToken);
    }

    public async Task<ProductDetailResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await repository.GetDetailsAsync(id, cancellationToken) ?? throw new NotFoundException("Product not found.");

    public async Task<PagedResponse<ProductSummaryResponse>> GetPublicPageAsync(ProductQuery query, CancellationToken cancellationToken)
    {
        query = query with { Search = query.Search?.Trim(), SortBy = query.SortBy?.Trim() };
        await queryValidator.ValidateAndThrowAsync(query, cancellationToken);
        return await repository.GetPublicPageAsync(query, cancellationToken);
    }

    public async Task<ProductPublicDetailResponse> GetPublicByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await repository.GetPublicDetailsAsync(id, cancellationToken) ?? throw new NotFoundException("Product not found.");

    public Task<ProductDashboardResponse> GetDashboardAsync(CancellationToken cancellationToken) =>
        repository.GetDashboardAsync(cancellationToken);

    public async Task<ProductResponse> CreateAsync(ProductRequest request, CancellationToken cancellationToken)
    {
        request = await ValidateRequestAsync(request, cancellationToken);
        ValidateManagedImageUrl(request.ImageUrl, null);
        var product = new Product
        {
            Id = Guid.NewGuid(), CategoryId = request.CategoryId,
            NameAr = request.NameAr!, NameEn = request.NameEn!,
            ImageUrl = request.ImageUrl, IsActive = request.IsActive
        };
        repository.Add(product);
        await repository.SaveChangesAsync(cancellationToken);
        return await GetSummaryAsync(product.Id, cancellationToken);
    }

    public async Task<ProductResponse> UpdateAsync(Guid id, ProductRequest request, CancellationToken cancellationToken)
    {
        request = await ValidateRequestAsync(request, cancellationToken);
        var product = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Product not found.");
        ValidateManagedImageUrl(request.ImageUrl, product.ImageUrl);
        var previousImage = product.ImageUrl;
        product.CategoryId = request.CategoryId;
        product.NameAr = request.NameAr!;
        product.NameEn = request.NameEn!;
        product.ImageUrl = request.ImageUrl;
        product.IsActive = request.IsActive;
        await repository.SaveChangesAsync(cancellationToken);
        if (previousImage != product.ImageUrl) await imageStore.DeleteAsync(previousImage, CancellationToken.None);
        return await GetSummaryAsync(id, cancellationToken);
    }

    public async Task<ProductResponse> SetStatusAsync(Guid id, ProductStatusRequest request, CancellationToken cancellationToken)
    {
        await statusValidator.ValidateAndThrowAsync(request, cancellationToken);
        var product = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Product not found.");
        product.IsActive = request.IsActive!.Value;
        await repository.SaveChangesAsync(cancellationToken);
        return await GetSummaryAsync(id, cancellationToken);
    }

    private async Task<ProductResponse> GetSummaryAsync(Guid id, CancellationToken cancellationToken) =>
        await repository.GetSummaryAsync(id, cancellationToken) ?? throw new NotFoundException("Product not found.");

    private async Task<ProductRequest> ValidateRequestAsync(ProductRequest request, CancellationToken cancellationToken)
    {
        request = request with
        {
            NameAr = request.NameAr?.Trim(), NameEn = request.NameEn?.Trim(),
            ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim()
        };
        await requestValidator.ValidateAndThrowAsync(request, cancellationToken);
        if (!await repository.CategoryExistsAsync(request.CategoryId, cancellationToken))
            throw new ValidationException([new ValidationFailure(nameof(ProductRequest.CategoryId), "Category does not exist.")]);
        return request;
    }

    private static void ValidateManagedImageUrl(string? imageUrl, string? currentImageUrl)
    {
        if (imageUrl?.StartsWith(IProductImageStore.UrlPrefix, StringComparison.OrdinalIgnoreCase) == true
            && imageUrl != currentImageUrl)
            throw new ValidationException([new ValidationFailure(nameof(ProductRequest.ImageUrl),
                "Use the image upload endpoint to assign a managed product image.")]);
    }
}
