using FluentValidation;
using SARE.Application.Common.Exceptions;
using SARE.Application.Common.Interfaces;
using SARE.Application.DTOs.Catalog;
using SARE.Domain.Catalog;

namespace SARE.Application.Services;

public sealed class ProductOptionService(IProductOptionRepository repository, ProductService products,
    IValidator<ProductOptionRequest> optionValidator, IValidator<ProductOptionValueRequest> valueValidator, TimeProvider clock)
{
    public async Task<IReadOnlyList<ProductOptionResponse>> GetOptionsAsync(Guid productId, CancellationToken ct)
    {
        await RequireProductAsync(productId, ct);
        return await repository.GetOptionsAsync(productId, ct);
    }

    public async Task<ProductOptionResponse> GetOptionAsync(Guid productId, Guid optionId, CancellationToken ct)
    {
        var option = await RequireOptionAsync(productId, optionId, ct);
        return new(option.Id, option.NameAr, option.NameEn, await repository.GetValuesAsync(optionId, ct));
    }

    public async Task<IReadOnlyList<ProductOptionValueResponse>> GetValuesAsync(Guid productId, Guid optionId, CancellationToken ct)
    {
        await RequireOptionAsync(productId, optionId, ct);
        return await repository.GetValuesAsync(optionId, ct);
    }

    public async Task<ProductOptionValueResponse> GetValueAsync(Guid productId, Guid optionId, Guid valueId, CancellationToken ct)
    {
        await RequireOptionAsync(productId, optionId, ct);
        return Map(await RequireValueAsync(optionId, valueId, ct));
    }

    public async Task<IReadOnlyList<ProductOptionResponse>> GetPublicOptionsAsync(Guid productId, CancellationToken ct) =>
        (await products.GetPublicByIdAsync(productId, ct)).Options;

    public async Task<ProductOptionResponse> GetPublicOptionAsync(Guid productId, Guid optionId, CancellationToken ct) =>
        (await GetPublicOptionsAsync(productId, ct)).SingleOrDefault(option => option.Id == optionId)
        ?? throw new NotFoundException("Option not found.");

    public async Task<IReadOnlyList<ProductOptionValueResponse>> GetPublicValuesAsync(Guid productId, Guid optionId, CancellationToken ct) =>
        (await GetPublicOptionAsync(productId, optionId, ct)).Values;

    public async Task<ProductOptionValueResponse> GetPublicValueAsync(Guid productId, Guid optionId, Guid valueId, CancellationToken ct) =>
        (await GetPublicValuesAsync(productId, optionId, ct)).SingleOrDefault(value => value.Id == valueId)
        ?? throw new NotFoundException("Option value not found.");

    public async Task<ProductOptionResponse> CreateOptionAsync(Guid productId, ProductOptionRequest request, CancellationToken ct)
    {
        request = await ValidateOptionAsync(request, ct);
        var product = await RequireProductAsync(productId, ct);
        var option = new ProductOption { Id = Guid.NewGuid(), ProductId = productId, NameAr = request.NameAr!, NameEn = request.NameEn! };
        repository.Add(option);
        Touch(product);
        await repository.SaveAsync(ct);
        return new(option.Id, option.NameAr, option.NameEn, []);
    }

    public async Task<ProductOptionResponse> UpdateOptionAsync(Guid productId, Guid optionId, ProductOptionRequest request, CancellationToken ct)
    {
        request = await ValidateOptionAsync(request, ct);
        var product = await RequireProductAsync(productId, ct);
        var option = await RequireOptionAsync(productId, optionId, ct);
        option.NameAr = request.NameAr!;
        option.NameEn = request.NameEn!;
        Touch(product);
        await repository.SaveAsync(ct);
        return new(option.Id, option.NameAr, option.NameEn, await repository.GetValuesAsync(optionId, ct));
    }

    public async Task DeleteOptionAsync(Guid productId, Guid optionId, CancellationToken ct)
    {
        var product = await RequireProductAsync(productId, ct);
        var option = await RequireOptionAsync(productId, optionId, ct);
        if (await repository.OptionIsUsedAsync(optionId, ct)) throw new ConflictException("Option is used by a variant. Remove its variant associations first.");
        repository.Remove(option);
        Touch(product);
        await repository.SaveAsync(ct);
    }

    public async Task<ProductOptionValueResponse> CreateValueAsync(Guid productId, Guid optionId, ProductOptionValueRequest request, CancellationToken ct)
    {
        request = await ValidateValueAsync(request, ct);
        var product = await RequireProductAsync(productId, ct);
        await RequireOptionAsync(productId, optionId, ct);
        var value = new ProductOptionValue { Id = Guid.NewGuid(), ProductOptionId = optionId, ValueAr = request.ValueAr!, ValueEn = request.ValueEn! };
        repository.Add(value);
        Touch(product);
        await repository.SaveAsync(ct);
        return Map(value);
    }

    public async Task<ProductOptionValueResponse> UpdateValueAsync(Guid productId, Guid optionId, Guid valueId, ProductOptionValueRequest request, CancellationToken ct)
    {
        request = await ValidateValueAsync(request, ct);
        var product = await RequireProductAsync(productId, ct);
        await RequireOptionAsync(productId, optionId, ct);
        var value = await RequireValueAsync(optionId, valueId, ct);
        value.ValueAr = request.ValueAr!;
        value.ValueEn = request.ValueEn!;
        Touch(product);
        await repository.SaveAsync(ct);
        return Map(value);
    }

    public async Task DeleteValueAsync(Guid productId, Guid optionId, Guid valueId, CancellationToken ct)
    {
        var product = await RequireProductAsync(productId, ct);
        await RequireOptionAsync(productId, optionId, ct);
        var value = await RequireValueAsync(optionId, valueId, ct);
        if (await repository.ValueIsUsedAsync(valueId, ct)) throw new ConflictException("Option value is used by a variant. Remove its variant associations first.");
        repository.Remove(value);
        Touch(product);
        await repository.SaveAsync(ct);
    }

    private async Task<Product> RequireProductAsync(Guid id, CancellationToken ct) =>
        await repository.GetProductAsync(id, ct) ?? throw new NotFoundException("Product not found.");
    private async Task<ProductOption> RequireOptionAsync(Guid productId, Guid optionId, CancellationToken ct)
    {
        await RequireProductAsync(productId, ct);
        return await repository.FindOptionAsync(productId, optionId, ct) ?? throw new NotFoundException("Option not found.");
    }
    private async Task<ProductOptionValue> RequireValueAsync(Guid optionId, Guid valueId, CancellationToken ct) =>
        await repository.FindValueAsync(optionId, valueId, ct) ?? throw new NotFoundException("Option value not found.");
    private async Task<ProductOptionRequest> ValidateOptionAsync(ProductOptionRequest request, CancellationToken ct)
    {
        request = request with { NameAr = request.NameAr?.Trim(), NameEn = request.NameEn?.Trim() };
        await optionValidator.ValidateAndThrowAsync(request, ct);
        return request;
    }
    private async Task<ProductOptionValueRequest> ValidateValueAsync(ProductOptionValueRequest request, CancellationToken ct)
    {
        request = request with { ValueAr = request.ValueAr?.Trim(), ValueEn = request.ValueEn?.Trim() };
        await valueValidator.ValidateAndThrowAsync(request, ct);
        return request;
    }
    private void Touch(Product product) => product.UpdatedAt = clock.GetUtcNow().UtcDateTime;
    private static ProductOptionValueResponse Map(ProductOptionValue value) => new(value.Id, value.ValueAr, value.ValueEn);
}
