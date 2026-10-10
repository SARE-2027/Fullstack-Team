using FluentValidation;
using ValidationException = FluentValidation.ValidationException;
using SARE.Application.Common.DTOs;
using SARE.Application.Common.Exceptions;
using SARE.Application.Common.Interfaces;
using SARE.Application.DTOs.Catalog;
using SARE.Domain.Catalog;

namespace SARE.Application.Services;

public sealed class CategoryService(
    ICategoryRepository repository,
    IValidator<CategoryRequest> requestValidator,
    IValidator<CategoryQuery> queryValidator)
{
    public async Task<PagedResponse<CategoryResponse>> GetPageAsync(CategoryQuery query, CancellationToken cancellationToken)
    {
        query = await ValidateQueryAsync(query, cancellationToken);
        return await repository.GetPageAsync(query, cancellationToken);
    }

    public async Task<CategoryResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await repository.GetDetailsAsync(id, cancellationToken)
        ?? throw new NotFoundException("Category not found.");

    public async Task<PagedResponse<CategorySummaryResponse>> GetPublicPageAsync(CategoryQuery query, CancellationToken cancellationToken)
    {
        query = await ValidateQueryAsync(query, cancellationToken);
        return await repository.GetPublicPageAsync(query, cancellationToken);
    }

    public async Task<CategorySummaryResponse> GetPublicByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await repository.GetPublicDetailsAsync(id, cancellationToken)
        ?? throw new NotFoundException("Category not found.");

    public async Task<CategoryResponse> CreateAsync(CategoryRequest request, CancellationToken cancellationToken)
    {
        request = await ValidateAsync(request, cancellationToken);
        var category = new Category
        {
            Id = Guid.NewGuid(), NameAr = request.NameAr!, NameEn = request.NameEn!
        };
        repository.Add(category);
        await repository.SaveChangesAsync(cancellationToken);
        return new CategoryResponse(category.Id, category.NameAr, category.NameEn, 0);
    }

    public async Task<CategoryResponse> UpdateAsync(Guid id, CategoryRequest request, CancellationToken cancellationToken)
    {
        request = await ValidateAsync(request, cancellationToken);
        var category = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Category not found.");
        category.NameAr = request.NameAr!;
        category.NameEn = request.NameEn!;
        await repository.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var category = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Category not found.");
        if (await repository.HasProductsAsync(id, cancellationToken))
            throw new ConflictException("Category cannot be deleted while it contains products. Move the products to another category first.");

        repository.Remove(category);
        await repository.SaveChangesAsync(cancellationToken);
    }

    private async Task<CategoryQuery> ValidateQueryAsync(CategoryQuery query, CancellationToken cancellationToken)
    {
        query = query with { Search = query.Search?.Trim() };
        await queryValidator.ValidateAndThrowAsync(query, cancellationToken);
        return query;
    }

    private async Task<CategoryRequest> ValidateAsync(CategoryRequest request, CancellationToken cancellationToken)
    {
        request = request with { NameAr = request.NameAr?.Trim(), NameEn = request.NameEn?.Trim() };
        await requestValidator.ValidateAndThrowAsync(request, cancellationToken);
        return request;
    }
}
