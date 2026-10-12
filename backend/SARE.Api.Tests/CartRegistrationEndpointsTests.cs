using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using SARE.Application.Common.Security;
using SARE.Application.DTOs.Cart;
using SARE.Domain.Enums;

namespace SARE.Api.Tests;

public sealed class CartRegistrationEndpointsTests : IAsyncLifetime
{
    private const string Route = "/api/v1/admin/carts";
    private readonly CatalogApiFactory _factory = new();
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.CreateToken());
        await _factory.InDatabaseAsync(async db => { await db.Database.EnsureCreatedAsync(); });
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("Customer", HttpStatusCode.Forbidden)]
    [InlineData("Staff", HttpStatusCode.Forbidden)]
    [InlineData("Admin", HttpStatusCode.Created)]
    public async Task OnlyAdminsCanRegisterCarts(string? role, HttpStatusCode expected)
    {
        _client.DefaultRequestHeaders.Authorization = role is null
            ? null : new AuthenticationHeaderValue("Bearer", _factory.CreateToken(role));

        var response = await _client.PostAsJsonAsync(Route, new { });

        Assert.Equal(expected, response.StatusCode);
        await _factory.InDatabaseAsync(async db =>
            Assert.Equal(expected == HttpStatusCode.Created ? 1 : 0, await db.Carts.CountAsync()));
    }

    [Fact]
    public async Task RegistrationReturnsCredentialsButStoresOnlyTheHashAndNoFakeTelemetry()
    {
        var response = await _client.PostAsJsonAsync(Route, new CreateCartRequest("  cart-v1  "));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(json.TryGetProperty("tokenHash", out _));
        var registered = json.Deserialize<CartRegistrationResponse>(new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        Assert.True(Guid.TryParseExact(registered.CartId, "N", out var id));
        Assert.NotEqual(Guid.Empty, id);
        Assert.Equal(32, registered.CartId.Length);
        Assert.Equal(32, Convert.FromHexString(registered.HardwareToken).Length);
        Assert.Equal("active", registered.Status);
        Assert.Equal("cart-v1", registered.SwVersion);

        await _factory.InDatabaseAsync(async db =>
        {
            var cart = await db.Carts.SingleAsync();
            Assert.Equal(registered.CartId, cart.Id);
            Assert.Equal(TokenHasher.HashToken(registered.HardwareToken), cart.TokenHash);
            Assert.NotEqual(registered.HardwareToken, cart.TokenHash);
            Assert.Equal(CartStatus.Active, cart.Status);
            Assert.Equal("cart-v1", cart.SwVersion);
            Assert.Null(cart.BatteryPct);
            Assert.Null(cart.LastSeenAt);
        });
    }

    [Fact]
    public async Task EachRegistrationHasItsOwnDeviceIdentityAndToken()
    {
        var first = await RegisterAsync();
        var second = await RegisterAsync();

        Assert.NotEqual(first.CartId, second.CartId);
        Assert.NotEqual(first.HardwareToken, second.HardwareToken);
        Assert.Null(first.SwVersion);
        await _factory.InDatabaseAsync(async db => Assert.Equal(2, await db.Carts.CountAsync()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SoftwareVersionIsOptional(string? swVersion)
    {
        var response = await _client.PostAsJsonAsync(Route, new CreateCartRequest(swVersion));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Null((await response.Content.ReadFromJsonAsync<CartRegistrationResponse>())!.SwVersion);
    }

    [Fact]
    public async Task InvalidSoftwareVersionDoesNotCreateACart()
    {
        var response = await _client.PostAsJsonAsync(Route, new CreateCartRequest(new string('x', 33)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(error.GetProperty("errors").TryGetProperty("swVersion", out _));
        await _factory.InDatabaseAsync(async db => Assert.Empty(await db.Carts.ToListAsync()));
    }

    [Fact]
    public async Task ProvisionedDeviceCanJoinAndStartASessionButAnotherCartsTokenIsRejected()
    {
        var registered = await RegisterAsync();
        var other = await RegisterAsync();
        await using var hub = new HubConnectionBuilder().WithUrl("http://localhost/hubs/cart", options =>
        {
            options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            options.Transports = HttpTransportType.LongPolling;
        }).Build();
        await hub.StartAsync();

        await Assert.ThrowsAsync<HubException>(() => hub.InvokeAsync<CartSummaryResponse?>(
            "JoinCartGroup", registered.CartId, other.HardwareToken));

        Assert.Null(await hub.InvokeAsync<CartSummaryResponse?>(
            "JoinCartGroup", registered.CartId, registered.HardwareToken));
        var session = await hub.InvokeAsync<CartSummaryResponse>("StartSession", new CartStartSessionRequest());

        Assert.Equal(registered.CartId, session.CartId);
        Assert.Equal("open", session.Status);
        Assert.Empty(session.Items);
        var recovered = await hub.InvokeAsync<CartSummaryResponse?>(
            "JoinCartGroup", registered.CartId, registered.HardwareToken);
        Assert.Equal(session.SessionId, recovered!.SessionId);
    }

    private async Task<CartRegistrationResponse> RegisterAsync()
    {
        var response = await _client.PostAsJsonAsync(Route, new { });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CartRegistrationResponse>())!;
    }

    [Fact]
    public async Task DeviceCanRegisterWithoutJwtPersistCredentialsAndReconnectToTheSameCart()
    {
        CartRegistrationResponse registered;
        Guid sessionId;
        await using (var hub = CreateHub())
        {
            await hub.StartAsync();
            registered = await hub.InvokeAsync<CartRegistrationResponse>("RegisterCart", new CreateCartRequest("  pi-v1  "));
            Assert.Equal("pi-v1", registered.SwVersion);
            Assert.Equal("active", registered.Status);

            // Registration returns credentials, but joining is an explicit step after saving them.
            var notJoined = await Assert.ThrowsAsync<HubException>(() =>
                hub.InvokeAsync<CartSummaryResponse>("StartSession", new CartStartSessionRequest()));
            Assert.Contains("CART_NOT_JOINED", notJoined.Message);
            Assert.Null(await hub.InvokeAsync<CartSummaryResponse?>(
                "JoinCartGroup", registered.CartId, registered.HardwareToken));
            var session = await hub.InvokeAsync<CartSummaryResponse>("StartSession", new CartStartSessionRequest());
            Assert.Equal(registered.CartId, session.CartId);
            sessionId = session.SessionId;

            var alreadyJoined = await Assert.ThrowsAsync<HubException>(() =>
                hub.InvokeAsync<CartRegistrationResponse>("RegisterCart", new CreateCartRequest()));
            Assert.Contains("CART_ALREADY_JOINED", alreadyJoined.Message);
        }

        // Emulate loading the saved credentials on a new connection, without registering again.
        await using var reconnected = CreateHub();
        await reconnected.StartAsync();
        var recovered = await reconnected.InvokeAsync<CartSummaryResponse?>(
            "JoinCartGroup", registered.CartId, registered.HardwareToken);
        Assert.Equal(sessionId, recovered!.SessionId);
        await _factory.InDatabaseAsync(async db =>
        {
            var cart = await db.Carts.SingleAsync();
            Assert.Equal(registered.CartId, cart.Id);
            Assert.Equal(TokenHasher.HashToken(registered.HardwareToken), cart.TokenHash);
            Assert.NotEqual(registered.HardwareToken, cart.TokenHash);
            Assert.Single(await db.Sessions.ToListAsync());
        });
    }

    [Fact]
    public async Task InvalidHubRegistrationDoesNotSaveAnythingAndAValidRetryCanSucceed()
    {
        await using var hub = CreateHub();
        await hub.StartAsync();
        var invalidVersion = await Assert.ThrowsAsync<HubException>(() => hub.InvokeAsync<CartRegistrationResponse>(
            "RegisterCart", new CreateCartRequest(new string('x', 33))));
        Assert.Contains("VALIDATION_FAILED", invalidVersion.Message);
        var missingRequest = await Assert.ThrowsAsync<HubException>(() => hub.InvokeAsync<CartRegistrationResponse>(
            "RegisterCart", (CreateCartRequest?)null));
        Assert.Contains("VALIDATION_FAILED", missingRequest.Message);
        await _factory.InDatabaseAsync(async db => Assert.Empty(await db.Carts.ToListAsync()));

        var registered = await hub.InvokeAsync<CartRegistrationResponse>("RegisterCart", new { });
        Assert.Null(registered.SwVersion);
        Assert.Null(await hub.InvokeAsync<CartSummaryResponse?>(
            "JoinCartGroup", registered.CartId, registered.HardwareToken));
    }

    private HubConnection CreateHub() => new HubConnectionBuilder().WithUrl("http://localhost/hubs/cart", options =>
    {
        options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
        options.Transports = HttpTransportType.LongPolling;
    }).Build();
}
