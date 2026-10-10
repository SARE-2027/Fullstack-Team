using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using SARE.Application.DTOs.Catalog;
using SARE.Domain.Catalog;

namespace SARE.Api.Tests;

public sealed class DashboardEndpointsTests : IAsyncLifetime
{
    private readonly CatalogApiFactory _factory = new();
    private HttpClient _client = null!;
    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient();
        await _factory.InDatabaseAsync(db => db.Database.EnsureCreatedAsync());
    }
    public async Task DisposeAsync() { _client.Dispose(); await _factory.DisposeAsync(); }

    [Theory]
    [InlineData("admin")]
    [InlineData("staff")]
    public async Task EmptyCatalogHasZeroCountersForDashboardRoles(string role)
    {
        SetRole(role);
        Assert.Equal(new CatalogDashboardResponse(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
            await _client.GetFromJsonAsync<CatalogDashboardResponse>("/api/v1/dashboard/summary"));
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("customer", HttpStatusCode.Forbidden)]
    public async Task DashboardIsRestricted(string? role, HttpStatusCode expected)
    {
        SetRole(role);
        Assert.Equal(expected, (await _client.GetAsync("/api/v1/dashboard/summary")).StatusCode);
    }

    [Fact]
    public async Task SummaryDistinguishesActiveFlagsFromPublicAvailabilityAndTracksChanges()
    {
        var category = Guid.NewGuid();
        var available = Guid.NewGuid(); var hidden = Guid.NewGuid(); var empty = Guid.NewGuid();
        var option = Guid.NewGuid(); var value = Guid.NewGuid(); var variant = Guid.NewGuid();
        await _factory.InDatabaseAsync(async db =>
        {
            db.Categories.AddRange(new Category { Id = category, NameAr = "تصنيف", NameEn = "Category" },
                new Category { Id = Guid.NewGuid(), NameAr = "فارغ", NameEn = "Empty" });
            db.Products.AddRange(
                new Product { Id = available, CategoryId = category, NameAr = "ظاهر", NameEn = "Available" },
                new Product { Id = hidden, CategoryId = category, NameAr = "مخفي", NameEn = "Hidden", IsActive = false },
                new Product { Id = empty, CategoryId = category, NameAr = "فارغ", NameEn = "Empty" });
            db.ProductVariants.AddRange(
                new ProductVariant { Id = variant, ProductId = available, Barcode = "v1", WeightG = 100 },
                new ProductVariant { Id = Guid.NewGuid(), ProductId = available, Barcode = "v2", WeightG = 100, IsActive = false },
                new ProductVariant { Id = Guid.NewGuid(), ProductId = hidden, Barcode = "v3", WeightG = 100 });
            db.ProductOptions.Add(new ProductOption { Id = option, ProductId = available, NameAr = "حجم", NameEn = "Size" });
            db.ProductOptionValues.AddRange(
                new ProductOptionValue { Id = value, ProductOptionId = option, ValueAr = "صغير", ValueEn = "Small" },
                new ProductOptionValue { Id = Guid.NewGuid(), ProductOptionId = option, ValueAr = "كبير", ValueEn = "Large" });
            db.VariantOptionValues.Add(new VariantOptionValue { VariantId = variant, OptionValueId = value });
            await db.SaveChangesAsync();
        });
        SetRole("staff");
        Assert.Equal(new CatalogDashboardResponse(2, 3, 2, 1, 1, 1, 3, 2, 1, 1, 1, 2),
            await _client.GetFromJsonAsync<CatalogDashboardResponse>("/api/v1/dashboard/summary"));
        SetRole("admin");
        Assert.Equal(HttpStatusCode.OK, (await _client.PatchAsJsonAsync($"/api/v1/admin/products/{hidden}/status", new ProductStatusRequest(true))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.PatchAsJsonAsync($"/api/v1/admin/products/{available}/variants/{variant}/status", new VariantStatusRequest(false))).StatusCode);
        var summary = (await _client.GetFromJsonAsync<CatalogDashboardResponse>("/api/v1/dashboard/summary"))!;
        Assert.Equal(new CatalogDashboardResponse(2, 3, 3, 0, 1, 2, 3, 1, 2, 1, 1, 2), summary);
        var products = (await _client.GetFromJsonAsync<ProductDashboardResponse>("/api/v1/dashboard/products/summary"))!;
        Assert.Equal(products.AvailableProducts, summary.AvailableProducts);
        Assert.Equal(products.ProductsWithoutActiveVariants, summary.ProductsWithoutActiveVariants);
    }
    private void SetRole(string? role) => _client.DefaultRequestHeaders.Authorization =
        role is null ? null : new AuthenticationHeaderValue("Bearer", _factory.CreateToken(role));
}
