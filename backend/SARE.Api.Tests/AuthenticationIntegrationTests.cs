using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SARE.Application.DTOs.Auth;
using SARE.Application.DTOs.Catalog;
using SARE.Domain.Users;

namespace SARE.Api.Tests;

public sealed class AuthenticationIntegrationTests : IAsyncLifetime
{
    private readonly CatalogApiFactory _factory = new() { UseApplicationJwt = true };
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient();
        await _factory.InDatabaseAsync(db => db.Database.EnsureCreatedAsync());
        await using var scope = _factory.Services.CreateAsyncScope();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
        foreach (var role in UserRoles.All) Assert.True((await roles.CreateAsync(new Role(role))).Succeeded);
    }
    public async Task DisposeAsync() { _client.Dispose(); await _factory.DisposeAsync(); }

    [Fact]
    public async Task RegistrationLoginNfcRefreshAndRevokeWorkThroughTheCombinedPipeline()
    {
        var registration = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("Customer", "customer@example.com", "Password123!", "NFC-123"));
        Assert.Equal(HttpStatusCode.OK, registration.StatusCode);
        var auth = (await registration.Content.ReadFromJsonAsync<AuthResponse>())!;
        Assert.Contains(UserRoles.Customer, auth.Roles);
        SetToken(auth.AccessToken);
        Assert.Equal(auth.Id, (await _client.GetFromJsonAsync<UserDto>("/api/auth/me"))!.Id);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.GetAsync("/api/v1/admin/categories")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.PostAsJsonAsync("/api/v1/admin/products", new ProductRequest(Guid.NewGuid(), "Name", "Name", null))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest("customer@example.com", "wrong"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsJsonAsync("/api/auth/login-nfc", new NfcLoginRequest("NFC-123"))).StatusCode);
        var refresh = await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest(auth.AccessToken, auth.RefreshToken));
        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
        var next = (await refresh.Content.ReadFromJsonAsync<AuthResponse>())!;
        Assert.NotEqual(auth.RefreshToken, next.RefreshToken);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest(auth.AccessToken, auth.RefreshToken))).StatusCode);
        SetToken(next.AccessToken);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.PostAsJsonAsync("/api/auth/revoke", next.RefreshToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest(next.AccessToken, next.RefreshToken))).StatusCode);
    }

    [Theory]
    [InlineData(UserRoles.Admin, HttpStatusCode.OK)]
    [InlineData(UserRoles.Staff, HttpStatusCode.Forbidden)]
    public async Task IdentityRoleTokensCanAccessTheCorrectCatalogRoutes(string role, HttpStatusCode managementStatus)
    {
        var auth = await CreateRoleUserAsync(role);
        SetToken(auth.AccessToken);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/api/v1/staff/products")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/api/v1/dashboard/summary")).StatusCode);
        Assert.Equal(managementStatus, (await _client.GetAsync("/api/v1/admin/categories")).StatusCode);
    }


    private async Task<AuthResponse> CreateRoleUserAsync(string role)
    {
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("Team", "team@example.com", "Password123!"));
        await using var scope = _factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var user = (await users.FindByEmailAsync("team@example.com"))!;
        Assert.True((await users.AddToRoleAsync(user, role)).Succeeded);
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest("team@example.com", "Password123!"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }
    private void SetToken(string token) => _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
}
