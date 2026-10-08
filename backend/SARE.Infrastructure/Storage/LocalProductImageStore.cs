using System.Buffers;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using SARE.Application.Common.Exceptions;
using SARE.Application.Common.Interfaces;

namespace SARE.Infrastructure.Storage;

public sealed class LocalProductImageStore(string rootPath, ILogger<LocalProductImageStore> logger) : IProductImageStore
{
    public const int MaxBytes = 5 * 1024 * 1024;
    public const string UrlPrefix = IProductImageStore.UrlPrefix;
    private readonly string _root = Path.GetFullPath(rootPath);

    public async Task<string> SaveAsync(Stream content, CancellationToken cancellationToken)
    {
        var header = new byte[12];
        var length = await content.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, cancellationToken);
        var extension = DetectExtension(header.AsSpan(0, length));
        if (extension is null)
            throw new ValidationException([new ValidationFailure("File", "Upload a PNG, JPEG, or WebP image.")]);
        Directory.CreateDirectory(_root);
        var fileName = Guid.NewGuid().ToString("N") + extension;
        var path = Path.Combine(_root, fileName);
        var buffer = ArrayPool<byte>.Shared.Rent(81920);
        try
        {
            await using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
            await output.WriteAsync(header.AsMemory(0, length), cancellationToken);
            var total = length;
            int read;
            while ((read = await content.ReadAsync(buffer, cancellationToken)) != 0)
            {
                total += read;
                if (total > MaxBytes) throw new PayloadTooLargeException("Product images must not exceed 5 MiB.");
                await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            }
        }
        catch
        {
            TryDelete(path);
            throw;
        }
        finally { ArrayPool<byte>.Shared.Return(buffer); }
        return UrlPrefix + fileName;
    }

    public Task<StoredProductImage?> OpenAsync(string fileName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var contentType = ContentType(fileName);
        if (contentType is null) return Task.FromResult<StoredProductImage?>(null);
        try
        {
            Stream stream = new FileStream(Path.Combine(_root, fileName), FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete, 81920, true);
            return Task.FromResult<StoredProductImage?>(new(stream, contentType));
        }
        catch (FileNotFoundException) { return Task.FromResult<StoredProductImage?>(null); }
        catch (DirectoryNotFoundException) { return Task.FromResult<StoredProductImage?>(null); }
    }

    public Task DeleteAsync(string? imageUrl, CancellationToken cancellationToken)
    {
        // Only server-generated local paths can be removed. External image URLs are never file paths.
        if (imageUrl?.StartsWith(UrlPrefix, StringComparison.Ordinal) == true)
        {
            var fileName = imageUrl[UrlPrefix.Length..];
            if (ContentType(fileName) is not null) TryDelete(Path.Combine(_root, fileName));
        }
        return Task.CompletedTask;
    }

    private void TryDelete(string path)
    {
        try { File.Delete(path); }
        catch (IOException ex) { logger.LogWarning(ex, "Could not clean up product image {ImagePath}.", path); }
        catch (UnauthorizedAccessException ex) { logger.LogWarning(ex, "Could not clean up product image {ImagePath}.", path); }
    }

    private static string? ContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        if (fileName.Length != 32 + extension.Length || !Guid.TryParseExact(fileName[..32], "N", out _)) return null;
        return extension switch { ".png" => "image/png", ".jpg" => "image/jpeg", ".webp" => "image/webp", _ => null };
    }

    private static string? DetectExtension(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length >= 8 && bytes[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })) return ".png";
        if (bytes.Length >= 3 && bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255) return ".jpg";
        if (bytes.Length >= 12 && bytes[..4].SequenceEqual("RIFF"u8) && bytes[8..12].SequenceEqual("WEBP"u8)) return ".webp";
        return null;
    }
}
