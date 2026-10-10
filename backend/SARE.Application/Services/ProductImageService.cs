using SARE.Application.Common.Exceptions;
using SARE.Application.Common.Interfaces;
using SARE.Application.DTOs.Catalog;

namespace SARE.Application.Services;

public sealed class ProductImageService(IProductRepository repository, IProductImageStore store)
{
    public async Task<ProductResponse> UploadAsync(Guid productId, Stream content, CancellationToken cancellationToken)
    {
        var product = await repository.GetByIdAsync(productId, cancellationToken)
            ?? throw new NotFoundException("Product not found.");
        var previous = product.ImageUrl;
        var imageUrl = await store.SaveAsync(content, cancellationToken);
        product.ImageUrl = imageUrl;
        try
        {
            await repository.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await store.DeleteAsync(imageUrl, CancellationToken.None);
            throw;
        }
        await store.DeleteAsync(previous, CancellationToken.None);
        return (await repository.GetSummaryAsync(productId, cancellationToken))!;
    }

    public async Task DeleteAsync(Guid productId, CancellationToken cancellationToken)
    {
        var product = await repository.GetByIdAsync(productId, cancellationToken)
            ?? throw new NotFoundException("Product not found.");
        var previous = product.ImageUrl;
        product.ImageUrl = null;
        await repository.SaveChangesAsync(cancellationToken);
        await store.DeleteAsync(previous, CancellationToken.None);
    }
}
