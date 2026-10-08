namespace SARE.Application.Common.Interfaces;

public sealed record StoredProductImage(Stream Content, string ContentType);

public interface IProductImageStore
{
    const string UrlPrefix = "/api/v1/product-images/";
    Task<string> SaveAsync(Stream content, CancellationToken cancellationToken);
    Task<StoredProductImage?> OpenAsync(string fileName, CancellationToken cancellationToken);
    Task DeleteAsync(string? imageUrl, CancellationToken cancellationToken);
}
