using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SARE.Application.DTOs.Catalog;
using SARE.Domain.Catalog;

namespace SARE.Api.Tests;

public sealed class ProductOptionsEndpointsTests : IAsyncLifetime
{
    private readonly CatalogApiFactory _factory = new();
    private HttpClient _client = null!;
    private readonly Guid _productId = Guid.NewGuid();
    private string Route => $"/api/v1/admin/products/{_productId}/options";
    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient();
        SetRole("admin");
        await _factory.InDatabaseAsync(async db =>
        {
            await db.Database.EnsureCreatedAsync();
            var category = new Category { Id = Guid.NewGuid(), NameAr = "سناكس", NameEn = "Snacks" };
            db.Categories.Add(category);
            db.Products.Add(new Product { Id = _productId, CategoryId = category.Id, NameAr = "شيبسي", NameEn = "Chips" });
            await db.SaveChangesAsync();
        });
    }
    public async Task DisposeAsync() { _client.Dispose(); await _factory.DisposeAsync(); }

    [Fact]
    public async Task OptionsAndValuesSupportFullLifecycleAndLocations()
    {
        var create = await _client.PostAsJsonAsync(Route, new ProductOptionRequest(" الحجم ", " Size "));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var option = (await create.Content.ReadFromJsonAsync<ProductOptionResponse>())!;
        Assert.Equal("Size", option.NameEn);
        Assert.Equal($"{Route}/{option.Id}", create.Headers.Location!.AbsolutePath);
        Assert.Equal(option.Id, (await _client.GetFromJsonAsync<ProductOptionResponse>(create.Headers.Location))!.Id);
        Assert.Single((await _client.GetFromJsonAsync<List<ProductOptionResponse>>(Route))!);
        Assert.Equal(HttpStatusCode.OK, (await _client.PutAsJsonAsync($"{Route}/{option.Id}", new ProductOptionRequest("حجم العبوة", "Pack size"))).StatusCode);
        var valuesRoute = $"{Route}/{option.Id}/values";
        var valueCreate = await _client.PostAsJsonAsync(valuesRoute, new ProductOptionValueRequest(" صغير ", " Small "));
        Assert.Equal(HttpStatusCode.Created, valueCreate.StatusCode);
        var value = (await valueCreate.Content.ReadFromJsonAsync<ProductOptionValueResponse>())!;
        Assert.Equal("Small", value.ValueEn);
        Assert.Equal($"{valuesRoute}/{value.Id}", valueCreate.Headers.Location!.AbsolutePath);
        Assert.Equal(value, await _client.GetFromJsonAsync<ProductOptionValueResponse>(valueCreate.Headers.Location));
        Assert.Single((await _client.GetFromJsonAsync<List<ProductOptionValueResponse>>(valuesRoute))!);
        Assert.Equal(HttpStatusCode.OK, (await _client.PutAsJsonAsync($"{valuesRoute}/{value.Id}", new ProductOptionValueRequest("كبير", "Large"))).StatusCode);
        Assert.Equal("Large", (await _client.GetFromJsonAsync<ProductOptionValueResponse>($"{valuesRoute}/{value.Id}"))!.ValueEn);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"{valuesRoute}/{value.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"{valuesRoute}/{value.Id}")).StatusCode);
        // Removing an unused option also removes its remaining values through the configured cascade.
        await _client.PostAsJsonAsync(valuesRoute, new ProductOptionValueRequest("وسط", "Medium"));
        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"{Route}/{option.Id}")).StatusCode);
        await _factory.InDatabaseAsync(async db => Assert.Equal(0, await db.ProductOptionValues.CountAsync()));
    }

    [Fact]
    public async Task OwnershipUsedValueDeletionAndPublicVisibilityAreEnforced()
    {
        var option = (await (await _client.PostAsJsonAsync(Route, new ProductOptionRequest("الحجم", "Size"))).Content.ReadFromJsonAsync<ProductOptionResponse>())!;
        var valuesRoute = $"{Route}/{option.Id}/values";
        var used = (await (await _client.PostAsJsonAsync(valuesRoute, new ProductOptionValueRequest("صغير", "Small"))).Content.ReadFromJsonAsync<ProductOptionValueResponse>())!;
        var unused = (await (await _client.PostAsJsonAsync(valuesRoute, new ProductOptionValueRequest("كبير", "Large"))).Content.ReadFromJsonAsync<ProductOptionValueResponse>())!;
        await _factory.InDatabaseAsync(async db =>
        {
            var variant = new ProductVariant { Id = Guid.NewGuid(), ProductId = _productId, Barcode = "chips", PriceMinor = 500, WeightG = 100 };
            db.ProductVariants.Add(variant);
            db.VariantOptionValues.Add(new VariantOptionValue { VariantId = variant.Id, OptionValueId = used.Id });
            var parent = await db.Products.FindAsync(_productId);
            db.Products.Add(new Product { Id = Guid.NewGuid(), CategoryId = parent!.CategoryId, NameAr = "آخر", NameEn = "Other" });
            await db.SaveChangesAsync();
        });
        Assert.Equal(HttpStatusCode.Conflict, (await _client.DeleteAsync($"{valuesRoute}/{used.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await _client.DeleteAsync($"{Route}/{option.Id}")).StatusCode);
        Guid otherId = default;
        await _factory.InDatabaseAsync(async db => { otherId = (await db.Products.FirstAsync(product => product.Id != _productId)).Id; });
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/v1/admin/products/{otherId}/options/{option.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.DeleteAsync($"/api/v1/admin/products/{otherId}/options/{option.Id}/values/{used.Id}")).StatusCode);
        SetRole(null);
        var publicRoute = $"/api/v1/products/{_productId}/options";
        Assert.Single((await _client.GetFromJsonAsync<List<ProductOptionResponse>>(publicRoute))!);
        Assert.Single((await _client.GetFromJsonAsync<ProductOptionResponse>($"{publicRoute}/{option.Id}"))!.Values);
        Assert.Single((await _client.GetFromJsonAsync<List<ProductOptionValueResponse>>($"{publicRoute}/{option.Id}/values"))!);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync($"{publicRoute}/{option.Id}/values/{used.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"{publicRoute}/{option.Id}/values/{unused.Id}")).StatusCode);
        SetRole("staff");
        var staffRoute = $"/api/v1/staff/products/{_productId}/options";
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync(staffRoute)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync($"{staffRoute}/{option.Id}")).StatusCode);
        Assert.Equal(2, (await _client.GetFromJsonAsync<List<ProductOptionValueResponse>>($"{staffRoute}/{option.Id}/values"))!.Count);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync($"{staffRoute}/{option.Id}/values/{unused.Id}")).StatusCode);
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("customer", HttpStatusCode.Forbidden)]
    [InlineData("staff", HttpStatusCode.Forbidden)]
    public async Task EveryManagementRouteRequiresAdmin(string? role, HttpStatusCode expected)
    {
        SetRole(role);
        var optionId = Guid.NewGuid(); var valueId = Guid.NewGuid();
        var options = new[] { ("GET", Route), ("GET", $"{Route}/{optionId}"), ("POST", Route), ("PUT", $"{Route}/{optionId}"), ("DELETE", $"{Route}/{optionId}") };
        var values = new[] { ("GET", $"{Route}/{optionId}/values"), ("GET", $"{Route}/{optionId}/values/{valueId}"), ("POST", $"{Route}/{optionId}/values"), ("PUT", $"{Route}/{optionId}/values/{valueId}"), ("DELETE", $"{Route}/{optionId}/values/{valueId}") };
        foreach (var (method, path) in options.Concat(values))
        {
            using var request = new HttpRequestMessage(new HttpMethod(method), path);
            if (method is "POST" or "PUT") request.Content = JsonContent.Create(new { nameAr = "حجم", nameEn = "Size", valueAr = "صغير", valueEn = "Small" });
            Assert.Equal(expected, (await _client.SendAsync(request)).StatusCode);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    public async Task InvalidOptionAndValueNamesAreRejected(string? name)
    {
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsJsonAsync(Route, new ProductOptionRequest(name, "Size"))).StatusCode);
        var option = (await (await _client.PostAsJsonAsync(Route, new ProductOptionRequest("الحجم", "Size"))).Content.ReadFromJsonAsync<ProductOptionResponse>())!;
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PutAsJsonAsync($"{Route}/{option.Id}", new ProductOptionRequest("الحجم", name))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsJsonAsync($"{Route}/{option.Id}/values", new ProductOptionValueRequest(name, "Small"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsJsonAsync(Route, new ProductOptionRequest(new string('a', 101), "Size"))).StatusCode);
    }
    private void SetRole(string? role) => _client.DefaultRequestHeaders.Authorization = role is null ? null : new AuthenticationHeaderValue("Bearer", _factory.CreateToken(role));
}
