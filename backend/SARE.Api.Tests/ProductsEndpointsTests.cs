using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SARE.Application.Common.DTOs;
using SARE.Application.DTOs.Catalog;
using SARE.Domain.Cart;
using SARE.Domain.Catalog;
using SARE.Domain.Enums;

namespace SARE.Api.Tests;

public sealed class ProductsEndpointsTests : IAsyncLifetime
{
    private const string AdminRoute = "/api/v1/admin/products";
    private const string PublicRoute = "/api/v1/products";
    private const string StaffRoute = "/api/v1/staff/products";
    private const string DashboardRoute = "/api/v1/dashboard/products/summary";
    private readonly CatalogApiFactory _factory = new();
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient();
        UseRole("admin");
        await _factory.InDatabaseAsync(async context => { await context.Database.EnsureCreatedAsync(); });
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task AdminCanCreateReadAndReplaceProductIncludingItsCategory()
    {
        var category = NewCategory("Snacks");
        var other = NewCategory("Drinks");
        await _factory.InDatabaseAsync(async context =>
        {
            context.Categories.AddRange(category, other);
            await context.SaveChangesAsync();
        });

        var create = await _client.PostAsJsonAsync(AdminRoute,
            new ProductRequest(category.Id, "  شيبسي  ", "  Chips  ", "  https://example.com/chips.jpg  "));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = (await create.Content.ReadFromJsonAsync<ProductResponse>())!;
        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal("شيبسي", created.NameAr);
        Assert.Equal("Chips", created.NameEn);
        Assert.Equal("https://example.com/chips.jpg", created.ImageUrl);
        Assert.Equal(category.NameEn, created.CategoryNameEn);
        Assert.True(created.IsActive);
        Assert.Equal(0, created.VariantCount);
        Assert.True(created.UpdatedAt > DateTime.UtcNow.AddMinutes(-1));
        Assert.Equal($"{AdminRoute}/{created.Id}", create.Headers.Location!.AbsolutePath);

        var details = (await _client.GetFromJsonAsync<ProductDetailResponse>(create.Headers.Location))!;
        Assert.Equal(created, details.Product);
        Assert.Empty(details.Options);
        Assert.Empty(details.Variants);
        // Products are created before their variants; the customer sees them once an active variant exists.
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"{PublicRoute}/{created.Id}")).StatusCode);

        var update = await _client.PutAsJsonAsync($"{AdminRoute}/{created.Id}",
            new ProductRequest(other.Id, "مياه", "Water", "  ", false));
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var updated = (await update.Content.ReadFromJsonAsync<ProductResponse>())!;
        Assert.Equal(created.Id, updated.Id);
        Assert.Equal(other.Id, updated.CategoryId);
        Assert.Equal("Water", updated.NameEn);
        Assert.Null(updated.ImageUrl);
        Assert.False(updated.IsActive);
        Assert.True(updated.UpdatedAt > created.UpdatedAt);
        Assert.Equal(updated, (await _client.GetFromJsonAsync<ProductDetailResponse>($"{AdminRoute}/{created.Id}"))!.Product);
    }

    [Fact]
    public async Task AdminListingSupportsNamesBarcodeFiltersSortingAndPages()
    {
        var seed = await SeedCatalogAsync();
        var page = (await _client.GetFromJsonAsync<PagedResponse<ProductResponse>>($"{AdminRoute}?page=2&pageSize=1"))!;
        Assert.Equal(5, page.TotalCount);
        Assert.Equal(seed.Visible.Id, Assert.Single(page.Items).Id);
        Assert.Equal(3, page.Items[0].VariantCount);
        Assert.Equal(2, page.Items[0].ActiveVariantCount);

        var filtered = (await _client.GetFromJsonAsync<PagedResponse<ProductResponse>>(
            $"{AdminRoute}?categoryId={seed.Category.Id}&isActive=true"))!;
        Assert.Equal(seed.Visible.Id, Assert.Single(filtered.Items).Id);
        var inactive = (await _client.GetFromJsonAsync<PagedResponse<ProductResponse>>($"{AdminRoute}?isActive=false"))!;
        Assert.Equal(seed.Inactive.Id, Assert.Single(inactive.Items).Id);
        foreach (var search in new[] { " cHiP ", "شيبسي", "hidden-barcode" })
        {
            var matches = (await _client.GetFromJsonAsync<PagedResponse<ProductResponse>>(
                $"{AdminRoute}?search={Uri.EscapeDataString(search)}"))!;
            Assert.Equal(seed.Visible.Id, Assert.Single(matches.Items).Id);
        }
        var descending = (await _client.GetFromJsonAsync<PagedResponse<ProductResponse>>(
            $"{AdminRoute}?sortBy=nameEn&sortDescending=true"))!;
        Assert.Equal(seed.OtherVisible.Id, descending.Items[0].Id);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync($"{AdminRoute}?sortBy=updatedAt")).StatusCode);
        var beyond = (await _client.GetFromJsonAsync<PagedResponse<ProductResponse>>($"{AdminRoute}?page=20&pageSize=1"))!;
        Assert.Empty(beyond.Items);
        Assert.Equal(5, beyond.TotalCount);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("customer")]
    [InlineData("staff")]
    [InlineData("admin")]
    public async Task PublicReadShowsOnlyAvailableProductsAndActiveVariantOptions(string? role)
    {
        var seed = await SeedCatalogAsync();
        UseRole(role);
        var page = (await _client.GetFromJsonAsync<PagedResponse<ProductSummaryResponse>>(PublicRoute))!;
        Assert.Equal(2, page.TotalCount);
        Assert.Equal(new[] { seed.Visible.Id, seed.OtherVisible.Id }, page.Items.Select(product => product.Id));
        var item = page.Items[0];
        Assert.Equal(1000, item.MinPriceMinor);
        Assert.Equal(2000, item.MaxPriceMinor);

        var detail = (await _client.GetFromJsonAsync<ProductPublicDetailResponse>($"{PublicRoute}/{seed.Visible.Id}"))!;
        Assert.Equal(2, detail.Variants.Count);
        Assert.DoesNotContain(detail.Variants, variant => variant.Barcode == "hidden-barcode");
        Assert.Equal(2, Assert.Single(detail.Options).Values.Count);
        foreach (var hidden in new[] { seed.Inactive, seed.Empty, seed.NoActiveVariants })
            Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"{PublicRoute}/{hidden.Id}")).StatusCode);

        var hiddenBarcode = (await _client.GetFromJsonAsync<PagedResponse<ProductSummaryResponse>>(
            $"{PublicRoute}?search=hidden-barcode"))!;
        Assert.Empty(hiddenBarcode.Items);
        var filtered = (await _client.GetFromJsonAsync<PagedResponse<ProductSummaryResponse>>(
            $"{PublicRoute}?categoryId={seed.Category.Id}&search={Uri.EscapeDataString("شيبسي")}"))!;
        Assert.Equal(seed.Visible.Id, Assert.Single(filtered.Items).Id);
    }

    [Fact]
    public async Task StaffCanInspectInactiveProductsAndDashboardButCannotModifyThem()
    {
        var seed = await SeedCatalogAsync();
        UseRole("staff");
        var page = (await _client.GetFromJsonAsync<PagedResponse<ProductResponse>>(StaffRoute))!;
        Assert.Equal(5, page.TotalCount);
        var details = (await _client.GetFromJsonAsync<ProductDetailResponse>($"{StaffRoute}/{seed.Visible.Id}"))!;
        Assert.Equal(3, details.Variants.Count);
        Assert.Equal(2, details.Options.Count);
        Assert.Equal(3, details.Options.Single(option => option.NameEn == "Size").Values.Count);
        var summary = (await _client.GetFromJsonAsync<ProductDashboardResponse>(DashboardRoute))!;
        Assert.Equal(new ProductDashboardResponse(5, 4, 1, 2, 2), summary);
        var forbidden = await _client.PatchAsJsonAsync($"{AdminRoute}/{seed.Visible.Id}/status", new ProductStatusRequest(false));
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        await _factory.InDatabaseAsync(async context => Assert.True((await context.Products.FindAsync(seed.Visible.Id))!.IsActive));
    }

    [Fact]
    public async Task DeactivationAndReactivationPreserveVariantsAndInvoiceHistory()
    {
        var seed = await SeedCatalogAsync();
        var session = new Session { Id = Guid.NewGuid(), CartId = "test-cart", TotalMinor = 650 };
        var variantId = Guid.Empty;
        await _factory.InDatabaseAsync(async context =>
        {
            variantId = (await context.ProductVariants.FirstAsync(variant => variant.ProductId == seed.Visible.Id && variant.IsActive)).Id;
            context.Carts.Add(new Cart { Id = session.CartId, TokenHash = "test" });
            context.Sessions.Add(session);
            context.SessionItems.Add(new SessionItem
            {
                Id = Guid.NewGuid(), SessionId = session.Id, VariantId = variantId,
                UnitPriceMinor = 650, Source = DetectionSource.Scanner
            });
            await context.SaveChangesAsync();
        });

        var disabled = await _client.PatchAsJsonAsync($"{AdminRoute}/{seed.Visible.Id}/status", new ProductStatusRequest(false));
        Assert.Equal(HttpStatusCode.OK, disabled.StatusCode);
        Assert.False((await disabled.Content.ReadFromJsonAsync<ProductResponse>())!.IsActive);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"{PublicRoute}/{seed.Visible.Id}")).StatusCode);
        await _factory.InDatabaseAsync(async context =>
        {
            Assert.Equal(3, await context.ProductVariants.CountAsync(variant => variant.ProductId == seed.Visible.Id));
            Assert.Equal(2, await context.ProductVariants.CountAsync(variant => variant.ProductId == seed.Visible.Id && variant.IsActive));
            var invoiceItem = await context.SessionItems.SingleAsync();
            Assert.Equal(variantId, invoiceItem.VariantId);
            Assert.Equal(650, invoiceItem.UnitPriceMinor);
        });
        var enabled = await _client.PatchAsJsonAsync($"{AdminRoute}/{seed.Visible.Id}/status", new ProductStatusRequest(true));
        Assert.Equal(HttpStatusCode.OK, enabled.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync($"{PublicRoute}/{seed.Visible.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, (await _client.DeleteAsync($"{AdminRoute}/{seed.Visible.Id}")).StatusCode);
    }

    public static TheoryData<string?, string?, string?> InvalidFields => new()
    {
        { null, "Chips", null }, { " ", "Chips", null }, { "شيبسي", null, null },
        { new string('a', 151), "Chips", null }, { "شيبسي", new string('a', 151), null },
        { "شيبسي", "Chips", new string('a', 501) }, { "شيبسي", "Chips", "javascript:alert(1)" },
        { "شيبسي", "Chips", "ftp://example.com/a.jpg" }, { "شيبسي", "Chips", "//example.com/a.jpg" }
    };

    [Theory]
    [MemberData(nameof(InvalidFields))]
    public async Task InvalidProductFieldsAreRejectedForCreateAndUpdate(string? nameAr, string? nameEn, string? imageUrl)
    {
        var seed = await SeedCatalogAsync();
        var request = new ProductRequest(seed.Category.Id, nameAr, nameEn, imageUrl);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsJsonAsync(AdminRoute, request)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PutAsJsonAsync($"{AdminRoute}/{seed.Visible.Id}", request)).StatusCode);
        await _factory.InDatabaseAsync(async context =>
        {
            Assert.Equal(5, await context.Products.CountAsync());
            Assert.Equal("Chips", (await context.Products.FindAsync(seed.Visible.Id))!.NameEn);
        });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task MissingOrEmptyCategoryIdIsRejected(bool useEmptyId)
    {
        var seed = await SeedCatalogAsync();
        var request = new ProductRequest(useEmptyId ? Guid.Empty : Guid.NewGuid(), "شيبسي", "Chips", null);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsJsonAsync(AdminRoute, request)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PutAsJsonAsync($"{AdminRoute}/{seed.Visible.Id}", request)).StatusCode);
    }

    [Fact]
    public async Task ConcurrentCategoryRemovalIsReportedAsConflict()
    {
        var category = NewCategory("Snacks");
        await _factory.InDatabaseAsync(async context =>
        {
            context.Categories.Add(category);
            await context.SaveChangesAsync();
        });
        _factory.SimulateProductCategoryRace = true;
        var response = await _client.PostAsJsonAsync(AdminRoute, new ProductRequest(category.Id, "شيبسي", "Chips", null));
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await _factory.InDatabaseAsync(async context => Assert.Equal(0, await context.Products.CountAsync()));
    }

    [Theory]
    [InlineData("page=0")]
    [InlineData("pageSize=101")]
    [InlineData("page=2147483647&pageSize=100")]
    [InlineData("page=abc")]
    [InlineData("categoryId=abc")]
    [InlineData("categoryId=00000000-0000-0000-0000-000000000000")]
    [InlineData("sortBy=price")]
    [InlineData("sortBy=")]
    public async Task InvalidQueriesAreRejectedOnAllReadRoutes(string query)
    {
        foreach (var route in new[] { AdminRoute, StaffRoute, PublicRoute })
            Assert.Equal(HttpStatusCode.BadRequest, (await _client.GetAsync($"{route}?{query}")).StatusCode);
    }

    [Fact]
    public async Task MissingProductsReturnNotFoundOnReadAndWriteRoutes()
    {
        var category = NewCategory("Snacks");
        await _factory.InDatabaseAsync(async context =>
        {
            context.Categories.Add(category);
            await context.SaveChangesAsync();
        });
        var id = Guid.NewGuid();
        foreach (var route in new[] { AdminRoute, StaffRoute, PublicRoute })
            Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"{route}/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await _client.PutAsJsonAsync($"{AdminRoute}/{id}", new ProductRequest(category.Id, "شيبسي", "Chips", null))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await _client.PatchAsJsonAsync($"{AdminRoute}/{id}/status", new ProductStatusRequest(false))).StatusCode);
    }

    [Fact]
    public async Task StatusIsRequiredAndEmptyDashboardReturnsZeros()
    {
        var seed = await SeedCatalogAsync();
        Assert.Equal(HttpStatusCode.BadRequest,
            (await _client.PatchAsJsonAsync($"{AdminRoute}/{seed.Visible.Id}/status", new ProductStatusRequest(null))).StatusCode);
        await _factory.InDatabaseAsync(async context =>
        {
            context.VariantOptionValues.RemoveRange(context.VariantOptionValues);
            context.ProductVariants.RemoveRange(context.ProductVariants);
            context.ProductOptionValues.RemoveRange(context.ProductOptionValues);
            context.ProductOptions.RemoveRange(context.ProductOptions);
            context.Products.RemoveRange(context.Products);
            await context.SaveChangesAsync();
        });
        Assert.Equal(new ProductDashboardResponse(0, 0, 0, 0, 0),
            await _client.GetFromJsonAsync<ProductDashboardResponse>(DashboardRoute));
        Assert.Empty((await _client.GetFromJsonAsync<PagedResponse<ProductSummaryResponse>>(PublicRoute))!.Items);
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("customer", HttpStatusCode.Forbidden)]
    [InlineData("staff", HttpStatusCode.Forbidden)]
    [InlineData("manager", HttpStatusCode.Forbidden)]
    public async Task EveryAdminEndpointRequiresCatalogManagementPermission(string? role, HttpStatusCode expected)
    {
        UseRole(role);
        var id = Guid.NewGuid();
        Assert.Equal(expected, (await _client.GetAsync(AdminRoute)).StatusCode);
        Assert.Equal(expected, (await _client.GetAsync($"{AdminRoute}/{id}")).StatusCode);
        var request = new ProductRequest(Guid.NewGuid(), "شيبسي", "Chips", null);
        Assert.Equal(expected, (await _client.PostAsJsonAsync(AdminRoute, request)).StatusCode);
        Assert.Equal(expected, (await _client.PutAsJsonAsync($"{AdminRoute}/{id}", request)).StatusCode);
        Assert.Equal(expected, (await _client.PatchAsJsonAsync($"{AdminRoute}/{id}/status", new ProductStatusRequest(false))).StatusCode);
        await _factory.InDatabaseAsync(async context => Assert.Equal(0, await context.Products.CountAsync()));
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("customer", HttpStatusCode.Forbidden)]
    [InlineData("staff", HttpStatusCode.OK)]
    [InlineData("admin", HttpStatusCode.OK)]
    public async Task StaffReadsAndDashboardEnforceRolePermissions(string? role, HttpStatusCode expected)
    {
        var seed = await SeedCatalogAsync();
        UseRole(role);
        Assert.Equal(expected, (await _client.GetAsync(StaffRoute)).StatusCode);
        Assert.Equal(expected, (await _client.GetAsync($"{StaffRoute}/{seed.Inactive.Id}")).StatusCode);
        Assert.Equal(expected, (await _client.GetAsync(DashboardRoute)).StatusCode);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task PublicAndStaffRoutesCannotBeUsedToModifyProducts(string method)
    {
        var seed = await SeedCatalogAsync();
        foreach (var route in new[] { PublicRoute, StaffRoute })
        {
            UseRole(route == StaffRoute ? "staff" : null);
            var path = method == "POST" ? route : $"{route}/{seed.Visible.Id}";
            if (method == "PATCH") path += "/status";
            using var request = new HttpRequestMessage(new HttpMethod(method), path);
            request.Content = JsonContent.Create(new ProductRequest(seed.Category.Id, "تغيير", "Changed", null, false));
            var response = await _client.SendAsync(request);
            Assert.Equal(method == "PATCH" ? HttpStatusCode.NotFound : HttpStatusCode.MethodNotAllowed, response.StatusCode);
        }
        await _factory.InDatabaseAsync(async context => Assert.Equal("Chips", (await context.Products.FindAsync(seed.Visible.Id))!.NameEn));
    }

    private void UseRole(string? role) => _client.DefaultRequestHeaders.Authorization = role is null
        ? null : new AuthenticationHeaderValue("Bearer", _factory.CreateToken(role));

    private async Task<Seed> SeedCatalogAsync()
    {
        var category = NewCategory("Snacks");
        var otherCategory = NewCategory("Drinks");
        var visible = NewProduct(category.Id, "Chips", true);
        visible.NameAr = "شيبسي";
        var inactive = NewProduct(category.Id, "Archived", false);
        var empty = NewProduct(otherCategory.Id, "Draft", true);
        var unavailable = NewProduct(otherCategory.Id, "Unavailable", true);
        var otherVisible = NewProduct(otherCategory.Id, "Water", true);
        await _factory.InDatabaseAsync(async context =>
        {
            context.Categories.AddRange(category, otherCategory);
            context.Products.AddRange(visible, inactive, empty, unavailable, otherVisible);
            var small = NewVariant(visible.Id, "chips-small", 1000, true);
            var large = NewVariant(visible.Id, "chips-large", 2000, true);
            var hidden = NewVariant(visible.Id, "hidden-barcode", 50, false);
            context.ProductVariants.AddRange(small, large, hidden,
                NewVariant(inactive.Id, "archived", 700, true),
                NewVariant(unavailable.Id, "unavailable", 500, false),
                NewVariant(otherVisible.Id, "water", 3000, true));
            var size = new ProductOption { Id = Guid.NewGuid(), ProductId = visible.Id, NameAr = "الحجم", NameEn = "Size" };
            var unusedOption = new ProductOption { Id = Guid.NewGuid(), ProductId = visible.Id, NameAr = "النكهة", NameEn = "Flavor" };
            var smallValue = NewValue(size.Id, "Small");
            var largeValue = NewValue(size.Id, "Large");
            var hiddenValue = NewValue(size.Id, "Hidden");
            context.ProductOptions.AddRange(size, unusedOption);
            context.ProductOptionValues.AddRange(smallValue, largeValue, hiddenValue, NewValue(unusedOption.Id, "Cheese"));
            context.VariantOptionValues.AddRange(
                new VariantOptionValue { VariantId = small.Id, OptionValueId = smallValue.Id },
                new VariantOptionValue { VariantId = large.Id, OptionValueId = largeValue.Id },
                new VariantOptionValue { VariantId = hidden.Id, OptionValueId = hiddenValue.Id });
            await context.SaveChangesAsync();
        });
        return new Seed(category, visible, inactive, empty, unavailable, otherVisible);
    }

    private static Category NewCategory(string name) => new() { Id = Guid.NewGuid(), NameAr = name, NameEn = name };
    private static Product NewProduct(Guid categoryId, string name, bool active) => new()
    {
        Id = Guid.NewGuid(), CategoryId = categoryId, NameAr = name, NameEn = name, IsActive = active
    };
    private static ProductVariant NewVariant(Guid productId, string barcode, int price, bool active) => new()
    {
        Id = Guid.NewGuid(), ProductId = productId, Barcode = barcode, PriceMinor = price, WeightG = 100, IsActive = active
    };
    private static ProductOptionValue NewValue(Guid optionId, string name) => new()
    {
        Id = Guid.NewGuid(), ProductOptionId = optionId, ValueAr = name, ValueEn = name
    };
    private sealed record Seed(Category Category, Product Visible, Product Inactive,
        Product Empty, Product NoActiveVariants, Product OtherVisible);
}
