using Microsoft.EntityFrameworkCore;
using Npgsql;
using SARE.Application.Common.Exceptions;
using SARE.Application.Common.Interfaces;
using SARE.Application.DTOs.Catalog;
using SARE.Domain.Catalog;

namespace SARE.Infrastructure.Persistence.Repositories;

public sealed class ProductOptionRepository(AppDbContext context) : IProductOptionRepository
{
    public Task<Product?> GetProductAsync(Guid productId, CancellationToken ct) =>
        context.Products.SingleOrDefaultAsync(product => product.Id == productId, ct);
    public async Task<List<ProductOptionResponse>> GetOptionsAsync(Guid productId, CancellationToken ct)
    {
        var options = await context.ProductOptions.AsNoTracking().Where(option => option.ProductId == productId)
            .OrderBy(option => option.NameEn).ThenBy(option => option.Id).ToListAsync(ct);
        var values = await context.ProductOptionValues.AsNoTracking().Where(value =>
            context.ProductOptions.Any(option => option.Id == value.ProductOptionId && option.ProductId == productId))
            .OrderBy(value => value.ValueEn).ThenBy(value => value.Id).ToListAsync(ct);
        return options.Select(option => new ProductOptionResponse(option.Id, option.NameAr, option.NameEn,
            values.Where(value => value.ProductOptionId == option.Id)
                .Select(value => new ProductOptionValueResponse(value.Id, value.ValueAr, value.ValueEn)).ToList())).ToList();
    }
    public Task<ProductOption?> FindOptionAsync(Guid productId, Guid optionId, CancellationToken ct) =>
        context.ProductOptions.SingleOrDefaultAsync(option => option.Id == optionId && option.ProductId == productId, ct);
    public Task<List<ProductOptionValueResponse>> GetValuesAsync(Guid optionId, CancellationToken ct) =>
        context.ProductOptionValues.AsNoTracking().Where(value => value.ProductOptionId == optionId)
            .OrderBy(value => value.ValueEn).ThenBy(value => value.Id)
            .Select(value => new ProductOptionValueResponse(value.Id, value.ValueAr, value.ValueEn)).ToListAsync(ct);
    public Task<ProductOptionValue?> FindValueAsync(Guid optionId, Guid valueId, CancellationToken ct) =>
        context.ProductOptionValues.SingleOrDefaultAsync(value => value.Id == valueId && value.ProductOptionId == optionId, ct);
    public Task<bool> OptionIsUsedAsync(Guid optionId, CancellationToken ct) => context.VariantOptionValues.AnyAsync(link =>
        context.ProductOptionValues.Any(value => value.Id == link.OptionValueId && value.ProductOptionId == optionId), ct);
    public Task<bool> ValueIsUsedAsync(Guid valueId, CancellationToken ct) =>
        context.VariantOptionValues.AnyAsync(link => link.OptionValueId == valueId, ct);
    public void Add<TEntity>(TEntity entity) where TEntity : class => context.Add(entity);
    public void Remove<TEntity>(TEntity entity) where TEntity : class => context.Remove(entity);
    public async Task SaveAsync(CancellationToken ct)
    {
        try { await context.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { throw new ConflictException("Product data changed. Reload it and try again."); }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
        { throw new ConflictException("The option is in use or its parent was removed. Reload the product and try again.", exception); }
    }
}
