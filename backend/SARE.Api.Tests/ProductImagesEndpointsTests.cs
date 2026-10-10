using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SARE.Application.Common.Exceptions;
using SARE.Application.Common.Interfaces;
using SARE.Application.DTOs.Catalog;
using SARE.Domain.Catalog;

namespace SARE.Api.Tests;

public sealed class ProductImagesEndpointsTests : IAsyncLifetime
{
    private readonly CatalogApiFactory _factory = new();
    private HttpClient _client = null!;
    private readonly Guid _productId = Guid.NewGuid();
    private readonly Guid _categoryId = Guid.NewGuid();
    private string Route => $"/api/v1/admin/products/{_productId}/image";
    private static readonly byte[] Png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aRGoAAAAASUVORK5CYII=");

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient(); SetRole("admin");
        await _factory.InDatabaseAsync(async db =>
        {
            await db.Database.EnsureCreatedAsync();
            db.Categories.Add(new Category { Id = _categoryId, NameAr = "تصنيف", NameEn = "Category" });
            db.Products.Add(new Product { Id = _productId, CategoryId = _categoryId, NameAr = "منتج", NameEn = "Product" });
            await db.SaveChangesAsync();
        });
    }
    public async Task DisposeAsync() { _client.Dispose(); await _factory.DisposeAsync(); }

    [Fact]
    public async Task UploadReadReplaceAndDeleteUseGeneratedNamesAndUpdateProduct()
    {
        var response = await UploadAsync(Png, "../../something.exe", "application/octet-stream");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var first = (await response.Content.ReadFromJsonAsync<ProductResponse>())!;
        Assert.StartsWith(IProductImageStore.UrlPrefix, first.ImageUrl);
        Assert.EndsWith(".png", first.ImageUrl);
        SetRole(null);
        var image = await _client.GetAsync(first.ImageUrl);
        Assert.Equal(HttpStatusCode.OK, image.StatusCode);
        Assert.Equal("image/png", image.Content.Headers.ContentType!.MediaType);
        Assert.Equal("nosniff", image.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal(Png, await image.Content.ReadAsByteArrayAsync());
        SetRole("admin");
        var second = (await (await UploadAsync(Png)).Content.ReadFromJsonAsync<ProductResponse>())!;
        Assert.NotEqual(first.ImageUrl, second.ImageUrl);
        Assert.True(second.UpdatedAt > first.UpdatedAt);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync(first.ImageUrl)).StatusCode);
        Assert.Single(Directory.GetFiles(_factory.ImageRoot));
        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync(Route)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync(second.ImageUrl)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync(Route)).StatusCode);
        await _factory.InDatabaseAsync(async db => Assert.Null((await db.Products.SingleAsync()).ImageUrl));
    }

    [Fact]
    public async Task InvalidAndMissingUploadsDoNotChangeExistingImage()
    {
        var original = (await (await UploadAsync(Png)).Content.ReadFromJsonAsync<ProductResponse>())!;
        Assert.Equal(HttpStatusCode.BadRequest, (await UploadAsync("<svg/>"u8.ToArray(), "fake.png", "image/png")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await UploadAsync([])).StatusCode);
        using var empty = new MultipartFormDataContent();
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsync(Route, empty)).StatusCode);
        var huge = new byte[5 * 1024 * 1024 + 1]; Png.CopyTo(huge, 0);
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, (await UploadAsync(huge)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await UploadAsync(Png, route: $"/api/v1/admin/products/{Guid.NewGuid()}/image")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.DeleteAsync($"/api/v1/admin/products/{Guid.NewGuid()}/image")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync(original.ImageUrl)).StatusCode);
        Assert.Single(Directory.GetFiles(_factory.ImageRoot));
    }

    [Fact]
    public async Task ConcurrencyConflictKeepsOldImageAndCleansNewUpload()
    {
        var original = (await (await UploadAsync(Png)).Content.ReadFromJsonAsync<ProductResponse>())!;
        _factory.SimulateCatalogWriteRace = true;
        Assert.Equal(HttpStatusCode.Conflict, (await UploadAsync(Png)).StatusCode);
        Assert.Single(Directory.GetFiles(_factory.ImageRoot));
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync(original.ImageUrl)).StatusCode);
        await _factory.InDatabaseAsync(async db => Assert.Equal(original.ImageUrl, (await db.Products.SingleAsync()).ImageUrl));
        _factory.SimulateCatalogWriteRace = true;
        Assert.Equal(HttpStatusCode.Conflict, (await _client.DeleteAsync(Route)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync(original.ImageUrl)).StatusCode);
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("customer", HttpStatusCode.Forbidden)]
    [InlineData("staff", HttpStatusCode.Forbidden)]
    public async Task ImageWritesRequireAdmin(string? role, HttpStatusCode expected)
    {
        SetRole(role);
        Assert.Equal(expected, (await UploadAsync(Png)).StatusCode);
        Assert.Equal(expected, (await _client.DeleteAsync(Route)).StatusCode);
    }

    [Fact]
    public async Task ManagedFilesCannotBeReassignedThroughProductMetadata()
    {
        var product = (await (await UploadAsync(Png)).Content.ReadFromJsonAsync<ProductResponse>())!;
        var request = new ProductRequest(_categoryId, "منتج", "Product", product.ImageUrl);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsJsonAsync("/api/v1/admin/products", request)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.PutAsJsonAsync($"/api/v1/admin/products/{_productId}", request)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.PutAsJsonAsync($"/api/v1/admin/products/{_productId}", request with { ImageUrl = "https://example.com/product.png" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync(product.ImageUrl)).StatusCode);
        Assert.Empty(Directory.GetFiles(_factory.ImageRoot));
    }

    [Theory]
    [InlineData("/9j/AA==", "image/jpeg", ".jpg")]
    [InlineData("UklGRgAAAABXRUJQ", "image/webp", ".webp")]
    public async Task ImageResponseTypeComesFromFileSignature(string base64, string type, string extension)
    {
        var response = await UploadAsync(Convert.FromBase64String(base64), "misleading.png", "image/png");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var product = (await response.Content.ReadFromJsonAsync<ProductResponse>())!;
        Assert.EndsWith(extension, product.ImageUrl);
        Assert.Equal(type, (await _client.GetAsync(product.ImageUrl)).Content.Headers.ContentType!.MediaType);
    }

    [Fact]
    public async Task StoreEnforcesStreamSizeAndRejectsUnsafePaths()
    {
        var store = _factory.Services.GetRequiredService<IProductImageStore>();
        var huge = new byte[5 * 1024 * 1024 + 1]; Png.CopyTo(huge, 0);
        await Assert.ThrowsAsync<PayloadTooLargeException>(() => store.SaveAsync(new MemoryStream(huge), default));
        Assert.Empty(Directory.GetFiles(_factory.ImageRoot));
        foreach (var name in new[] { "../../appsettings.json", "file.png", "", new string('a', 32) + ".svg" })
            Assert.Null(await store.OpenAsync(name, default));
    }

    private async Task<HttpResponseMessage> UploadAsync(byte[] bytes, string fileName = "image.png", string type = "image/png", string? route = null)
    {
        using var form = new MultipartFormDataContent();
        var content = new ByteArrayContent(bytes); content.Headers.ContentType = new MediaTypeHeaderValue(type);
        form.Add(content, "file", fileName);
        return await _client.PostAsync(route ?? Route, form);
    }
    private void SetRole(string? role) => _client.DefaultRequestHeaders.Authorization =
        role is null ? null : new AuthenticationHeaderValue("Bearer", _factory.CreateToken(role));
}
