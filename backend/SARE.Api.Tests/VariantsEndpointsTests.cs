using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SARE.Application.Common.DTOs;
using SARE.Application.DTOs.Catalog;
using SARE.Domain.Catalog;

namespace SARE.Api.Tests;

public sealed class VariantsEndpointsTests : IAsyncLifetime
{
    private readonly CatalogApiFactory _factory = new();
    private HttpClient _client = null!;
    private readonly Guid _productId = Guid.NewGuid();
    private readonly Guid _categoryId = Guid.NewGuid();
    private string Route => $"/api/v1/admin/products/{_productId}/variants";
    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient(); SetRole("admin");
        await _factory.InDatabaseAsync(async db =>
        {
            await db.Database.EnsureCreatedAsync();
            db.Categories.Add(new Category { Id = _categoryId, NameAr = "سناكس", NameEn = "Snacks" });
            db.Products.Add(new Product { Id = _productId, CategoryId = _categoryId, NameAr = "شيبسي", NameEn = "Chips" });
            await db.SaveChangesAsync();
        });
    }
    public async Task DisposeAsync() { _client.Dispose(); await _factory.DisposeAsync(); }

    [Fact]
    public async Task VariantLifecycleUpdatesLinksAndSupportsEveryReadRoute()
    {
        var (option, small, large) = await CreateOptionAsync();
        var response = await _client.PostAsJsonAsync(Route, Request(" chips-1 ", [small]));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var variant = (await response.Content.ReadFromJsonAsync<ProductVariantResponse>())!;
        Assert.Equal("chips-1", variant.Barcode); Assert.Equal(small, Assert.Single(variant.OptionValueIds));
        Assert.Equal($"/api/v1/admin/variants/{variant.Id}", response.Headers.Location!.AbsolutePath);
        Assert.Equal(variant.Id, (await _client.GetFromJsonAsync<ProductVariantResponse>(response.Headers.Location))!.Id);
        Assert.Equal(variant.Id, (await _client.GetFromJsonAsync<ProductVariantResponse>("/api/v1/admin/variants/by-barcode/chips-1"))!.Id);
        Assert.Equal(variant.Id, (await _client.GetFromJsonAsync<ProductVariantResponse>("/api/v1/admin/variants/by-barcode?barcode=chips-1"))!.Id);
        Assert.Single((await _client.GetFromJsonAsync<PagedResponse<ProductVariantResponse>>(Route))!.Items);
        Assert.Single((await _client.GetFromJsonAsync<PagedResponse<ProductVariantResponse>>($"/api/v1/admin/variants?search=CHIPS&categoryId={_categoryId}"))!.Items);

        var update = await _client.PutAsJsonAsync($"{Route}/{variant.Id}", Request("code/2", [large]) with { PriceMinor = 2000, WeightG = 300 });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var updated = (await update.Content.ReadFromJsonAsync<ProductVariantResponse>())!;
        Assert.Equal(2000, updated.PriceMinor); Assert.Equal(large, Assert.Single(updated.OptionValueIds));
        Assert.True(updated.UpdatedAt > variant.UpdatedAt);
        Assert.Equal(HttpStatusCode.OK, (await _client.PutAsJsonAsync($"{Route}/{variant.Id}", Request("code/2", [large]))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync("/api/v1/admin/variants/by-barcode/chips-1")).StatusCode);
        SetRole(null);
        Assert.Equal(variant.Id, (await _client.GetFromJsonAsync<VariantLookupResponse>("/api/v1/variants/by-barcode?barcode=code%2F2"))!.Id);
        Assert.Equal(variant.Id, (await _client.GetFromJsonAsync<VariantLookupResponse>($"/api/v1/variants/{variant.Id}"))!.Id);
        Assert.Single((await _client.GetFromJsonAsync<PagedResponse<VariantLookupResponse>>($"/api/v1/products/{_productId}/variants"))!.Items);
        Assert.Single((await _client.GetFromJsonAsync<PagedResponse<VariantLookupResponse>>("/api/v1/variants?search=شيبسي"))!.Items);
        SetRole("staff");
        foreach (var path in new[] { "/api/v1/staff/variants", $"/api/v1/staff/variants/{variant.Id}", "/api/v1/staff/variants/by-barcode?barcode=code%2F2", $"/api/v1/staff/products/{_productId}/variants" })
            Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync(path)).StatusCode);
        SetRole("admin");
        Assert.Equal(HttpStatusCode.OK, (await _client.PatchAsJsonAsync($"{Route}/{variant.Id}/status", new VariantStatusRequest(false))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/v1/variants/{variant.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync("/api/v1/variants/by-barcode?barcode=code%2F2")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await _client.DeleteAsync($"/api/v1/admin/products/{_productId}/options/{option}/values/{large}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.PatchAsJsonAsync($"{Route}/{variant.Id}/status", new VariantStatusRequest(true))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync($"/api/v1/variants/{variant.Id}")).StatusCode);
        await _client.PatchAsJsonAsync($"/api/v1/admin/products/{_productId}/status", new ProductStatusRequest(false));
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/v1/variants/{variant.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync($"/api/v1/staff/variants/{variant.Id}")).StatusCode);
    }

    [Fact]
    public async Task DuplicateBarcodeAndInvalidOptionMappingsAreRejectedWithoutPartialWrites()
    {
        var (_, small, large) = await CreateOptionAsync();
        var created = await _client.PostAsJsonAsync(Route, Request("unique", [small]));
        var variant = (await created.Content.ReadFromJsonAsync<ProductVariantResponse>())!;
        Assert.Equal(HttpStatusCode.Conflict, (await _client.PostAsJsonAsync(Route, Request("unique", [large]))).StatusCode);
        foreach (var ids in new[] { new[] { small, large }, new[] { small, small }, new[] { Guid.NewGuid() }, new[] { Guid.Empty } })
        {
            Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsJsonAsync(Route, Request("other", ids))).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await _client.PutAsJsonAsync($"{Route}/{variant.Id}", Request("other", ids))).StatusCode);
        }
        var otherProduct = Guid.NewGuid();
        var foreignValue = Guid.NewGuid();
        await _factory.InDatabaseAsync(async db =>
        {
            db.Products.Add(new Product { Id = otherProduct, CategoryId = _categoryId, NameAr = "آخر", NameEn = "Other" });
            var option = new ProductOption { Id = Guid.NewGuid(), ProductId = otherProduct, NameAr = "حجم", NameEn = "Size" };
            db.ProductOptions.Add(option);
            db.ProductOptionValues.Add(new ProductOptionValue { Id = foreignValue, ProductOptionId = option.Id, ValueAr = "صغير", ValueEn = "Small" });
            await db.SaveChangesAsync();
        });
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsJsonAsync(Route, Request("foreign", [foreignValue]))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.PutAsJsonAsync($"/api/v1/admin/products/{otherProduct}/variants/{variant.Id}", Request("unique", []))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.PatchAsJsonAsync($"/api/v1/admin/products/{otherProduct}/variants/{variant.Id}/status", new VariantStatusRequest(false))).StatusCode);
        await _factory.InDatabaseAsync(async db =>
        {
            Assert.Equal(1, await db.ProductVariants.CountAsync());
            Assert.Equal(small, (await db.VariantOptionValues.SingleAsync()).OptionValueId);
        });
    }

    [Fact]
    public async Task ConcurrentEditReturnsConflictAndRollsBackVariantAndLinks()
    {
        var (_, small, large) = await CreateOptionAsync();
        var variant = (await (await _client.PostAsJsonAsync(Route, Request("race", [small]))).Content.ReadFromJsonAsync<ProductVariantResponse>())!;
        _factory.SimulateCatalogWriteRace = true;
        var update = await _client.PutAsJsonAsync($"{Route}/{variant.Id}", Request("changed", [large]) with { PriceMinor = 9000 });
        Assert.Equal(HttpStatusCode.Conflict, update.StatusCode);
        await _factory.InDatabaseAsync(async db =>
        {
            var stored = await db.ProductVariants.SingleAsync();
            Assert.Equal("race", stored.Barcode); Assert.Equal(1000, stored.PriceMinor);
            Assert.Equal(small, (await db.VariantOptionValues.SingleAsync()).OptionValueId);
            Assert.Equal("Concurrent edit", (await db.Products.FindAsync(_productId))!.NameEn);
        });
    }

    [Theory]
    [InlineData(null, 100, 100)]
    [InlineData(" ", 100, 100)]
    [InlineData("bad", -1, 100)]
    [InlineData("bad", 100, 0)]
    [InlineData("bad", 100, -1)]
    public async Task InvalidVariantFieldsReturnBadRequest(string? barcode, int price, int weight)
    {
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsJsonAsync(Route, new VariantRequest(barcode, price, weight, true, []))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsJsonAsync(Route, Request(new string('a', 65), []))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsJsonAsync(Route, Request("null-values", null))).StatusCode);
    }

    [Theory]
    [InlineData("page=0")]
    [InlineData("pageSize=101")]
    [InlineData("page=2147483647&pageSize=100")]
    [InlineData("productId=00000000-0000-0000-0000-000000000000")]
    public async Task InvalidQueriesAreRejectedForAllAudiences(string query)
    {
        foreach (var path in new[] { "/api/v1/variants", "/api/v1/admin/variants", "/api/v1/staff/variants" })
            Assert.Equal(HttpStatusCode.BadRequest, (await _client.GetAsync($"{path}?{query}")).StatusCode);
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("customer", HttpStatusCode.Forbidden)]
    [InlineData("staff", HttpStatusCode.Forbidden)]
    public async Task AllManagementRoutesRequireAdmin(string? role, HttpStatusCode expected)
    {
        SetRole(role); var id = Guid.NewGuid();
        foreach (var path in new[] { "/api/v1/admin/variants", $"/api/v1/admin/variants/{id}", "/api/v1/admin/variants/by-barcode/test", "/api/v1/admin/variants/by-barcode?barcode=test", Route })
            Assert.Equal(expected, (await _client.GetAsync(path)).StatusCode);
        Assert.Equal(expected, (await _client.PostAsJsonAsync(Route, Request("test", []))).StatusCode);
        Assert.Equal(expected, (await _client.PutAsJsonAsync($"{Route}/{id}", Request("test", []))).StatusCode);
        Assert.Equal(expected, (await _client.PatchAsJsonAsync($"{Route}/{id}/status", new VariantStatusRequest(false))).StatusCode);
    }

    [Fact]
    public async Task PublicReadsCannotBypassAvailabilityAndNestedParentFilters()
    {
        var variant = (await (await _client.PostAsJsonAsync(Route, Request("zero-price", []) with { PriceMinor = 0 })).Content.ReadFromJsonAsync<ProductVariantResponse>())!;
        var page = (await _client.GetFromJsonAsync<PagedResponse<VariantLookupResponse>>($"/api/v1/products/{_productId}/variants?productId={Guid.NewGuid()}"))!;
        Assert.Equal(variant.Id, Assert.Single(page.Items).Id);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/v1/products/{Guid.NewGuid()}/variants")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.GetAsync("/api/v1/variants/by-barcode?barcode=" )).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync("/api/v1/variants/by-barcode/missing")).StatusCode);
    }
    private async Task<(Guid Option, Guid Small, Guid Large)> CreateOptionAsync()
    {
        var route = $"/api/v1/admin/products/{_productId}/options";
        var option = (await (await _client.PostAsJsonAsync(route, new ProductOptionRequest("الحجم", "Size"))).Content.ReadFromJsonAsync<ProductOptionResponse>())!;
        var small = (await (await _client.PostAsJsonAsync($"{route}/{option.Id}/values", new ProductOptionValueRequest("صغير", "Small"))).Content.ReadFromJsonAsync<ProductOptionValueResponse>())!;
        var large = (await (await _client.PostAsJsonAsync($"{route}/{option.Id}/values", new ProductOptionValueRequest("كبير", "Large"))).Content.ReadFromJsonAsync<ProductOptionValueResponse>())!;
        return (option.Id, small.Id, large.Id);
    }
    private static VariantRequest Request(string barcode, IReadOnlyList<Guid>? ids) => new(barcode, 1000, 100, true, ids);
    private void SetRole(string? role) => _client.DefaultRequestHeaders.Authorization = role is null ? null : new AuthenticationHeaderValue("Bearer", _factory.CreateToken(role));
}
