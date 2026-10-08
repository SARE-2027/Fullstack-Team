using FluentValidation;
using FluentValidation.Results;
using SARE.Application.Common.DTOs;
using SARE.Application.Common.Exceptions;
using SARE.Application.Common.Interfaces;
using SARE.Application.DTOs.Catalog;
using SARE.Domain.Catalog;

namespace SARE.Application.Services;

public sealed class ProductVariantService(IProductVariantRepository repository, ProductService products,
    IValidator<VariantRequest> requestValidator, IValidator<VariantStatusRequest> statusValidator,
    IValidator<VariantQuery> queryValidator, IValidator<AdminVariantQuery> adminQueryValidator, TimeProvider clock)
{
    public async Task<PagedResponse<ProductVariantResponse>> GetPageAsync(AdminVariantQuery query, CancellationToken ct)
    {
        query = query with { Search = query.Search?.Trim() };
        await adminQueryValidator.ValidateAndThrowAsync(query, ct);
        return await repository.GetPageAsync(query, ct);
    }
    public async Task<PagedResponse<VariantLookupResponse>> GetPublicPageAsync(VariantQuery query, CancellationToken ct)
    {
        query = query with { Search = query.Search?.Trim() };
        await queryValidator.ValidateAndThrowAsync(query, ct);
        return await repository.GetPublicPageAsync(query, ct);
    }
    public async Task<PagedResponse<ProductVariantResponse>> GetForProductAsync(Guid productId, AdminVariantQuery query, CancellationToken ct)
    {
        await RequireProductAsync(productId, ct);
        return await GetPageAsync(query with { ProductId = productId }, ct);
    }
    public async Task<PagedResponse<VariantLookupResponse>> GetPublicForProductAsync(Guid productId, VariantQuery query, CancellationToken ct)
    {
        await products.GetPublicByIdAsync(productId, ct);
        return await GetPublicPageAsync(query with { ProductId = productId }, ct);
    }
    public async Task<ProductVariantResponse> GetByIdAsync(Guid id, CancellationToken ct) =>
        await repository.GetDetailsAsync(id, ct) ?? throw new NotFoundException("Variant not found.");
    public async Task<VariantLookupResponse> GetPublicByIdAsync(Guid id, CancellationToken ct) =>
        await repository.GetPublicDetailsAsync(id, ct) ?? throw new NotFoundException("Variant not found.");
    public async Task<ProductVariantResponse> GetByBarcodeAsync(string barcode, CancellationToken ct)
    {
        var variant = await RequireBarcodeAsync(barcode, ct);
        return await GetByIdAsync(variant.Id, ct);
    }
    public async Task<VariantLookupResponse> GetPublicByBarcodeAsync(string barcode, CancellationToken ct)
    {
        var variant = await RequireBarcodeAsync(barcode, ct);
        return await GetPublicByIdAsync(variant.Id, ct);
    }
    public async Task<ProductVariantResponse> CreateAsync(Guid productId, VariantRequest request, CancellationToken ct)
    {
        var product = await RequireProductAsync(productId, ct);
        request = await ValidateAsync(productId, null, request, ct);
        var variant = new ProductVariant { Id = Guid.NewGuid(), ProductId = productId,
            Barcode = request.Barcode!, PriceMinor = request.PriceMinor, WeightG = request.WeightG, IsActive = request.IsActive };
        repository.Add(variant);
        foreach (var id in request.OptionValueIds!) repository.Add(new VariantOptionValue { VariantId = variant.Id, OptionValueId = id });
        Touch(product);
        await repository.SaveAsync(ct);
        return await GetByIdAsync(variant.Id, ct);
    }
    public async Task<ProductVariantResponse> UpdateAsync(Guid productId, Guid variantId, VariantRequest request, CancellationToken ct)
    {
        var product = await RequireProductAsync(productId, ct);
        var variant = await RequireVariantAsync(productId, variantId, ct);
        request = await ValidateAsync(productId, variantId, request, ct);
        variant.Barcode = request.Barcode!; variant.PriceMinor = request.PriceMinor;
        variant.WeightG = request.WeightG; variant.IsActive = request.IsActive;
        var links = await repository.GetLinksAsync(variantId, ct);
        foreach (var link in links.Where(link => !request.OptionValueIds!.Contains(link.OptionValueId))) repository.Remove(link);
        foreach (var id in request.OptionValueIds!.Where(id => links.All(link => link.OptionValueId != id)))
            repository.Add(new VariantOptionValue { VariantId = variantId, OptionValueId = id });
        Touch(product);
        await repository.SaveAsync(ct);
        return await GetByIdAsync(variantId, ct);
    }
    public async Task<ProductVariantResponse> SetStatusAsync(Guid productId, Guid variantId, VariantStatusRequest request, CancellationToken ct)
    {
        await statusValidator.ValidateAndThrowAsync(request, ct);
        var product = await RequireProductAsync(productId, ct);
        var variant = await RequireVariantAsync(productId, variantId, ct);
        variant.IsActive = request.IsActive!.Value;
        Touch(product);
        await repository.SaveAsync(ct);
        return await GetByIdAsync(variantId, ct);
    }
    private async Task<VariantRequest> ValidateAsync(Guid productId, Guid? variantId, VariantRequest request, CancellationToken ct)
    {
        request = request with { Barcode = request.Barcode?.Trim() };
        await requestValidator.ValidateAndThrowAsync(request, ct);
        var owner = await repository.FindByBarcodeAsync(request.Barcode!, ct);
        if (owner is not null && owner.Id != variantId) throw new ConflictException("Barcode already exists.");
        var values = await repository.GetProductValuesAsync(productId, ct);
        var selected = values.Where(value => request.OptionValueIds!.Contains(value.Id)).ToList();
        if (selected.Count != request.OptionValueIds!.Count)
            throw InvalidValues("All selected option values must belong to this product.");
        if (selected.GroupBy(value => value.ProductOptionId).Any(group => group.Count() > 1))
            throw InvalidValues("A variant can select only one value per option.");
        return request;
    }
    private async Task<Product> RequireProductAsync(Guid id, CancellationToken ct) =>
        await repository.GetProductAsync(id, ct) ?? throw new NotFoundException("Product not found.");
    private async Task<ProductVariant> RequireVariantAsync(Guid productId, Guid id, CancellationToken ct)
    {
        var variant = await repository.FindAsync(id, ct);
        return variant?.ProductId == productId ? variant : throw new NotFoundException("Variant not found.");
    }
    private async Task<ProductVariant> RequireBarcodeAsync(string barcode, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(barcode) || barcode.Trim().Length > 64)
            throw new ValidationException([new ValidationFailure("Barcode", "Barcode is required and must be at most 64 characters.")]);
        return await repository.FindByBarcodeAsync(barcode.Trim(), ct) ?? throw new NotFoundException("Variant not found.");
    }
    private void Touch(Product product) => product.UpdatedAt = clock.GetUtcNow().UtcDateTime;
    private static ValidationException InvalidValues(string message) => new([new ValidationFailure("OptionValueIds", message)]);
}
