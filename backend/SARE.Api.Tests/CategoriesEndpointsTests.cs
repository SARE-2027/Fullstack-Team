using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SARE.Application.Common.DTOs;
using SARE.Application.DTOs.Catalog;
using SARE.Domain.Catalog;

namespace SARE.Api.Tests;

public sealed class CategoriesEndpointsTests : IAsyncLifetime
{
    private const string Route = "/api/v1/admin/categories";
    private const string PublicRoute = "/api/v1/categories";
    private readonly CategoriesApiFactory _factory = new();
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.CreateToken());
        await _factory.InDatabaseAsync(async context => { await context.Database.EnsureCreatedAsync(); });
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task CategoryCanBeCreatedReadUpdatedAndDeleted()
    {
        var create = await _client.PostAsJsonAsync(Route, new CategoryRequest("  مشروبات  ", "  Drinks  "));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = (await create.Content.ReadFromJsonAsync<CategoryResponse>())!;
        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal("مشروبات", created.NameAr);
        Assert.Equal("Drinks", created.NameEn);
        Assert.Equal(0, created.ProductCount);
        Assert.Equal($"{Route}/{created.Id}", create.Headers.Location!.AbsolutePath);

        var read = (await _client.GetFromJsonAsync<CategoryResponse>(create.Headers.Location))!;
        Assert.Equal(created, read);
        var update = await _client.PutAsJsonAsync($"{Route}/{created.Id}", new CategoryRequest("سناكس", "Snacks"));
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var updated = (await update.Content.ReadFromJsonAsync<CategoryResponse>())!;
        Assert.Equal(created.Id, updated.Id);
        Assert.Equal("Snacks", updated.NameEn);
        Assert.Equal(updated, await _client.GetFromJsonAsync<CategoryResponse>($"{Route}/{created.Id}"));

        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"{Route}/{created.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"{Route}/{created.Id}")).StatusCode);
    }

    [Fact]
    public async Task ListingSupportsSearchPaginationAndAllProductCounts()
    {
        var dairy = Category("ألبان", "Dairy");
        var drinks = Category("مشروبات", "Drinks");
        var snacks = Category("سناكس", "Snacks");
        await _factory.InDatabaseAsync(async context =>
        {
            context.Categories.AddRange(snacks, dairy, drinks);
            context.Products.AddRange(Product(drinks.Id, true), Product(drinks.Id, false));
            await context.SaveChangesAsync();
        });

        var page = (await _client.GetFromJsonAsync<PagedResponse<CategoryResponse>>($"{Route}?page=2&pageSize=1"))!;
        Assert.Equal(3, page.TotalCount);
        Assert.Equal(2, page.Page);
        Assert.Equal(1, page.PageSize);
        var item = Assert.Single(page.Items);
        Assert.Equal(drinks.Id, item.Id);
        Assert.Equal(2, item.ProductCount);

        var english = (await _client.GetFromJsonAsync<PagedResponse<CategoryResponse>>($"{Route}?search=%20sNaCk%20"))!;
        Assert.Equal(snacks.Id, Assert.Single(english.Items).Id);
        Assert.Equal(1, english.TotalCount);
        var arabic = (await _client.GetFromJsonAsync<PagedResponse<CategoryResponse>>($"{Route}?search={Uri.EscapeDataString("ألبان")}"))!;
        Assert.Equal(dairy.Id, Assert.Single(arabic.Items).Id);
        var details = (await _client.GetFromJsonAsync<CategoryResponse>($"{Route}/{drinks.Id}"))!;
        Assert.Equal(2, details.ProductCount);

        var beyondLastPage = (await _client.GetFromJsonAsync<PagedResponse<CategoryResponse>>($"{Route}?page=5&pageSize=1"))!;
        Assert.Empty(beyondLastPage.Items);
        Assert.Equal(3, beyondLastPage.TotalCount);
    }

    [Fact]
    public async Task SearchTreatsWildcardCharactersAsLiteralText()
    {
        await _factory.InDatabaseAsync(async context =>
        {
            context.Categories.AddRange(Category("خصومات", "100% deals"), Category("ألبان", "Dairy"));
            await context.SaveChangesAsync();
        });
        var page = (await _client.GetFromJsonAsync<PagedResponse<CategoryResponse>>($"{Route}?search=%25"))!;
        Assert.Equal("100% deals", Assert.Single(page.Items).NameEn);
    }

    [Fact]
    public async Task EmptyCatalogReturnsAnEmptyPage()
    {
        var page = (await _client.GetFromJsonAsync<PagedResponse<CategoryResponse>>(Route))!;
        Assert.Empty(page.Items);
        Assert.Equal(0, page.TotalCount);
        Assert.Equal(1, page.Page);
        Assert.Equal(20, page.PageSize);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CategoryWithAnyProductCannotBeDeleted(bool productIsActive)
    {
        var category = Category("مشروبات", "Drinks");
        await _factory.InDatabaseAsync(async context =>
        {
            context.Categories.Add(category);
            context.Products.Add(Product(category.Id, productIsActive));
            await context.SaveChangesAsync();
        });
        var response = await _client.DeleteAsync($"{Route}/{category.Id}");
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        await _factory.InDatabaseAsync(async context =>
        {
            Assert.True(await context.Categories.AnyAsync(item => item.Id == category.Id));
            Assert.Equal(1, await context.Products.CountAsync());
        });
    }

    [Fact]
    public async Task PostgreSqlForeignKeyRaceReturnsConflictAndPreservesCategory()
    {
        var category = Category("مشروبات", "Drinks");
        await _factory.InDatabaseAsync(async context =>
        {
            context.Categories.Add(category);
            await context.SaveChangesAsync();
        });
        _factory.SimulateDeleteRace = true;
        var response = await _client.DeleteAsync($"{Route}/{category.Id}");
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync($"{Route}/{category.Id}")).StatusCode);
    }

    public static TheoryData<string?, string?> InvalidNames => new()
    {
        { null, "Drinks" }, { "   ", "Drinks" }, { "مشروبات", null }, { "مشروبات", "\t" },
        { new string('a', 101), "Drinks" }, { "مشروبات", new string('a', 101) }
    };

    [Theory]
    [MemberData(nameof(InvalidNames))]
    public async Task InvalidNamesAreRejectedForBothCreateAndUpdate(string? nameAr, string? nameEn)
    {
        var category = Category("مشروبات", "Drinks");
        await _factory.InDatabaseAsync(async context =>
        {
            context.Categories.Add(category);
            await context.SaveChangesAsync();
        });
        var request = new CategoryRequest(nameAr, nameEn);
        var create = await _client.PostAsJsonAsync(Route, request);
        var update = await _client.PutAsJsonAsync($"{Route}/{category.Id}", request);
        Assert.Equal(HttpStatusCode.BadRequest, create.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, update.StatusCode);
        using var problem = await JsonDocument.ParseAsync(await create.Content.ReadAsStreamAsync());
        Assert.True(problem.RootElement.TryGetProperty("errors", out _));
        await _factory.InDatabaseAsync(async context =>
        {
            var unchanged = await context.Categories.SingleAsync();
            Assert.Equal("Drinks", unchanged.NameEn);
        });
    }

    [Theory]
    [InlineData("page=0")]
    [InlineData("page=-1")]
    [InlineData("pageSize=0")]
    [InlineData("pageSize=101")]
    [InlineData("pageSize=-1")]
    [InlineData("page=2147483647&pageSize=100")]
    [InlineData("page=abc")]
    public async Task InvalidPaginationReturnsBadRequest(string query)
    {
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.GetAsync($"{Route}?{query}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.GetAsync($"{PublicRoute}?{query}")).StatusCode);
    }

    [Fact]
    public async Task OversizedSearchReturnsBadRequest()
    {
        Assert.Equal(HttpStatusCode.BadRequest,
            (await _client.GetAsync($"{Route}?search={new string('a', 101)}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await _client.GetAsync($"{PublicRoute}?search={new string('a', 101)}")).StatusCode);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task MissingCategoryReturnsNotFound(string method)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), $"{Route}/{Guid.NewGuid()}");
        if (method == "PUT") request.Content = JsonContent.Create(new CategoryRequest("مشروبات", "Drinks"));
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("GET", false)]
    [InlineData("GET", true)]
    [InlineData("POST", false)]
    [InlineData("PUT", true)]
    [InlineData("DELETE", true)]
    public async Task EveryAdminEndpointRequiresAuthentication(string method, bool hasId)
    {
        _client.DefaultRequestHeaders.Authorization = null;
        using var request = new HttpRequestMessage(new HttpMethod(method), hasId ? $"{Route}/{Guid.NewGuid()}" : Route);
        if (method is "POST" or "PUT") request.Content = JsonContent.Create(new CategoryRequest("مشروبات", "Drinks"));
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains(response.Headers.WwwAuthenticate, header => header.Scheme == "Bearer");
    }

    [Theory]
    [InlineData("staff")]
    [InlineData("customer")]
    public async Task NonAdminUsersAreForbidden(string role)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.CreateToken(role));
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.GetAsync(Route)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await _client.PostAsJsonAsync(Route, new CategoryRequest("مشروبات", "Drinks"))).StatusCode);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task ExpiredOrIncorrectlySignedTokensAreRejected(bool expired, bool invalidSignature)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            _factory.CreateToken(expired: expired, invalidSignature: invalidSignature));
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync(Route)).StatusCode);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("{")]
    [InlineData("{}")]
    public async Task InvalidJsonBodiesAreRejected(string body)
    {
        var response = await _client.PostAsync(Route, new StringContent(body, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("customer")]
    [InlineData("staff")]
    [InlineData("admin")]
    public async Task UsersAndGuestsCanReadCategoriesWithoutAdminPermissions(string? role)
    {
        _client.DefaultRequestHeaders.Authorization = role is null ? null
            : new AuthenticationHeaderValue("Bearer", _factory.CreateToken(role));
        var category = Category("مشروبات", "Drinks");
        await _factory.InDatabaseAsync(async context =>
        {
            context.Categories.Add(category);
            await context.SaveChangesAsync();
        });

        var page = (await _client.GetFromJsonAsync<PagedResponse<CategorySummaryResponse>>(PublicRoute))!;
        Assert.Equal(category.Id, Assert.Single(page.Items).Id);
        var response = await _client.GetAsync($"{PublicRoute}/{category.Id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var details = (await response.Content.ReadFromJsonAsync<CategorySummaryResponse>())!;
        Assert.Equal(category.Id, details.Id);
        Assert.Equal("مشروبات", details.NameAr);
        Assert.Equal("Drinks", details.NameEn);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.False(body.RootElement.TryGetProperty("productCount", out _));
    }

    [Fact]
    public async Task PublicCategoriesSupportSearchPaginationAndNotFound()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var drinks = Category("مشروبات", "Drinks");
        var snacks = Category("سناكس", "Snacks");
        await _factory.InDatabaseAsync(async context =>
        {
            context.Categories.AddRange(snacks, drinks);
            await context.SaveChangesAsync();
        });
        var page = (await _client.GetFromJsonAsync<PagedResponse<CategorySummaryResponse>>($"{PublicRoute}?page=2&pageSize=1"))!;
        Assert.Equal(2, page.TotalCount);
        Assert.Equal(snacks.Id, Assert.Single(page.Items).Id);
        var english = (await _client.GetFromJsonAsync<PagedResponse<CategorySummaryResponse>>($"{PublicRoute}?search=%20dRiNk%20"))!;
        Assert.Equal(drinks.Id, Assert.Single(english.Items).Id);
        var arabic = (await _client.GetFromJsonAsync<PagedResponse<CategorySummaryResponse>>(
            $"{PublicRoute}?search={Uri.EscapeDataString("سناكس")}"))!;
        Assert.Equal(snacks.Id, Assert.Single(arabic.Items).Id);
        Assert.Equal(HttpStatusCode.NotFound,
            (await _client.GetAsync($"{PublicRoute}/{Guid.NewGuid()}")).StatusCode);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task PublicCategoryRoutesDoNotAllowWrites(string method)
    {
        _client.DefaultRequestHeaders.Authorization = null;
        using var request = new HttpRequestMessage(new HttpMethod(method),
            method == "POST" ? PublicRoute : $"{PublicRoute}/{Guid.NewGuid()}");
        if (method is "POST" or "PUT") request.Content = JsonContent.Create(new CategoryRequest("مشروبات", "Drinks"));
        Assert.Equal(HttpStatusCode.MethodNotAllowed, (await _client.SendAsync(request)).StatusCode);
        await _factory.InDatabaseAsync(async context => Assert.Equal(0, await context.Categories.CountAsync()));
    }

    private static Category Category(string nameAr, string nameEn) => new()
    {
        Id = Guid.NewGuid(), NameAr = nameAr, NameEn = nameEn
    };

    private static Product Product(Guid categoryId, bool isActive) => new()
    {
        Id = Guid.NewGuid(), CategoryId = categoryId, NameAr = "منتج", NameEn = "Product", IsActive = isActive
    };
}
