using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.EntityFrameworkCore;
using SARE.Application.Common.Security;
using SARE.Application.DTOs.Cart;
using SARE.Domain.Cart;
using SARE.Domain.Catalog;
using SARE.Domain.Enums;
using SARE.Domain.Users;

namespace SARE.Api.Tests;

public sealed class SessionCartIntegrationTests : IAsyncLifetime
{
    private readonly CatalogApiFactory _factory = new();
    private HttpClient _client = null!;
    private const string CartId = "C-TEST";
    private const string CartToken = "test-device-secret";
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _variantId = Guid.NewGuid();

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-Cart-Token", CartToken);
        await _factory.InDatabaseAsync(async db =>
        {
            await db.Database.EnsureCreatedAsync();
            db.Carts.Add(new SARE.Domain.Cart.Cart { Id = CartId, TokenHash = TokenHasher.HashToken(CartToken), BatteryPct = 90 });
            db.Users.Add(new User { Id = _userId, Name = "Customer", UserName = "customer", NfcUid = "NFC-TEST" });
            var category = new Category { Id = Guid.NewGuid(), NameAr = "تصنيف", NameEn = "Category" };
            var product = new Product { Id = Guid.NewGuid(), CategoryId = category.Id, NameAr = "منتج", NameEn = "Product" };
            db.Categories.Add(category); db.Products.Add(product);
            db.ProductVariants.Add(new ProductVariant { Id = _variantId, ProductId = product.Id, Barcode = "ITEM-1", PriceMinor = 100, WeightG = 50 });
            await db.SaveChangesAsync();
        });
    }
    public async Task DisposeAsync() { _client.Dispose(); await _factory.DisposeAsync(); }

    [Fact]
    public async Task NewStartLegacyDetectionNewSummaryAndLegacyCheckoutShareOneLifecycle()
    {
        var started = await StartAsync(new StartSessionRequest(CartId, "NFC-TEST"));
        Assert.Equal(_userId, started.UserId);
        Assert.Equal(HttpStatusCode.Conflict, (await _client.PostAsJsonAsync("/api/cart/session/start", new LegacyStartSessionRequest(CartId, null))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await _client.PostAsJsonAsync("/api/sessions", new StartSessionRequest(CartId))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync($"/api/cart/active/{CartId}")).StatusCode);
        var added = await _client.PostAsJsonAsync($"/api/cart/{started.SessionId}/items", new CartAddItemRequest("ITEM-1", 50));
        Assert.Equal(HttpStatusCode.OK, added.StatusCode);
        var summary = (await _client.GetFromJsonAsync<CartSummaryResponse>($"/api/sessions/{started.SessionId}"))!;
        Assert.Equal(100, summary.TotalMinor); Assert.Equal("منتج", Assert.Single(summary.Items).NameAr);
        await _factory.InDatabaseAsync(async db =>
        {
            (await db.ProductVariants.SingleAsync()).PriceMinor = 999;
            await db.SaveChangesAsync();
        });
        var close = await _client.PostAsync($"/api/cart/{started.SessionId}/checkout", null);
        Assert.Equal(HttpStatusCode.OK, close.StatusCode);
        Assert.Equal(100, (await close.Content.ReadFromJsonAsync<CheckoutResponseDto>())!.TotalPaidMinor);
        Assert.Equal("closed", (await _client.GetFromJsonAsync<CartSummaryResponse>($"/api/sessions/{started.SessionId}"))!.Status);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/carts/{CartId}/session")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await _client.PostAsJsonAsync($"/api/sessions/{started.SessionId}/close", new CloseSessionRequest())).StatusCode);
        await _factory.InDatabaseAsync(async db =>
        {
            Assert.Equal(100, (await db.SessionItems.SingleAsync()).UnitPriceMinor);
            Assert.Equal(DetectionOutcome.Accepted, (await db.DetectionEvents.SingleAsync()).Outcome);
        });
    }

    [Fact]
    public async Task LegacyUserIdStartCanBeClosedThroughNewApi()
    {
        var response = await _client.PostAsJsonAsync("/api/cart/session/start", new LegacyStartSessionRequest(CartId, _userId));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var session = (await response.Content.ReadFromJsonAsync<CartSessionResponseDto>())!;
        Assert.Equal(_userId, (await _client.GetFromJsonAsync<CartSummaryResponse>($"/api/sessions/{session.SessionId}"))!.UserId);
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsJsonAsync($"/api/sessions/{session.SessionId}/close", new CloseSessionRequest(Reason: CloseReason.Abandoned))).StatusCode);
        Assert.Equal(SessionStatus.Abandoned, (await _client.GetFromJsonAsync<CartSessionResponseDto>($"/api/cart/{session.SessionId}"))!.Status);
    }

    [Fact]
    public async Task LowBatteryAllowsCurrentShopperToFinishThenDisablesTheCart()
    {
        Assert.Equal(300, (await TelemetryAsync(90)).NextReportIntervalSeconds);
        var started = await StartAsync(new StartSessionRequest(CartId));
        Assert.Equal(120, (await TelemetryAsync(80)).NextReportIntervalSeconds);
        var low = await TelemetryAsync(8);
        Assert.Equal("active", low.Status); Assert.Equal(60, low.NextReportIntervalSeconds);
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsync($"/api/cart/{started.SessionId}/checkout", null)).StatusCode);
        await _factory.InDatabaseAsync(async db => Assert.Equal(CartStatus.Disabled, (await db.Carts.SingleAsync()).Status));
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsJsonAsync("/api/sessions", new StartSessionRequest(CartId))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsJsonAsync("/api/cart/session/start", new LegacyStartSessionRequest(CartId, null))).StatusCode);
    }

    [Fact]
    public async Task IdleCriticalBatteryDisablesCartAndExplicitStatusCanReenableAfterCharging()
    {
        Assert.Equal("disabled", (await TelemetryAsync(9)).Status);
        Assert.Equal(HttpStatusCode.OK, (await _client.PatchAsJsonAsync($"/api/carts/{CartId}", new UpdateCartTelemetryRequest(95, "v2", CartStatus.Active))).StatusCode);
        await StartAsync(new StartSessionRequest(CartId));
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PatchAsJsonAsync($"/api/carts/{CartId}", new UpdateCartTelemetryRequest(101, null))).StatusCode);
    }

    [Fact]
    public async Task TelemetryVerifiesTheExistingHardwareTokenContract()
    {
        var session = await StartAsync(new StartSessionRequest(CartId));
        _client.DefaultRequestHeaders.Remove("X-Cart-Token");
        foreach (var token in new[] { "wrong", "" })
        {
            _client.DefaultRequestHeaders.Remove("X-Cart-Token"); _client.DefaultRequestHeaders.Add("X-Cart-Token", token);
            Assert.Equal(HttpStatusCode.Unauthorized, (await _client.PatchAsJsonAsync($"/api/carts/{CartId}", new UpdateCartTelemetryRequest(90, null))).StatusCode);
        }
        _client.DefaultRequestHeaders.Remove("X-Cart-Token");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.CreateToken("Staff"));
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync($"/api/sessions/{session.SessionId}")).StatusCode);
    }

    [Fact]
    public async Task WeightMismatchAndUnavailableParentDoNotAddInvoiceItems()
    {
        var session = await StartAsync(new StartSessionRequest(CartId));
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsJsonAsync($"/api/cart/{session.SessionId}/items", new CartAddItemRequest("ITEM-1", 100))).StatusCode);
        await _factory.InDatabaseAsync(async db =>
        {
            Assert.Empty(await db.SessionItems.ToListAsync());
            Assert.Equal(DetectionOutcome.Rejected, (await db.DetectionEvents.SingleAsync()).Outcome);
            (await db.Products.SingleAsync()).IsActive = false;
            await db.SaveChangesAsync();
        });
        Assert.Equal(HttpStatusCode.NotFound, (await _client.PostAsJsonAsync($"/api/cart/{session.SessionId}/items", new CartAddItemRequest("ITEM-1", 50))).StatusCode);
        Assert.Equal(0, (await _client.GetFromJsonAsync<CartSummaryResponse>($"/api/sessions/{session.SessionId}"))!.TotalMinor);
    }

    [Fact]
    public async Task SessionRequestValidationIsPreservedAcrossMergedDtos()
    {
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsJsonAsync("/api/sessions", new StartSessionRequest(CartId, "NFC-TEST", _userId))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.PostAsJsonAsync("/api/cart/session/start", new LegacyStartSessionRequest("missing", null))).StatusCode);
        var started = await StartAsync(new StartSessionRequest(CartId));
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsJsonAsync($"/api/sessions/{started.SessionId}/close", new CloseSessionRequest(Reason: (CloseReason)999))).StatusCode);
    }

    [Fact]
    public async Task CartSignalRGroupReceivesBothSessionAndLegacyDetectionNotifications()
    {
        await using var hub = CreateHub();
        var startedEvent = new TaskCompletionSource<CartSummaryResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        var updatedEvent = new TaskCompletionSource<System.Text.Json.JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        var closedEvent = new TaskCompletionSource<CartSummaryResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        hub.On<CartSummaryResponse>("SessionStarted", value => startedEvent.TrySetResult(value));
        hub.On<System.Text.Json.JsonElement>("CartUpdated", value => updatedEvent.TrySetResult(value));
        hub.On<CartSummaryResponse>("SessionClosed", value => closedEvent.TrySetResult(value));
        await hub.StartAsync();
        await Assert.ThrowsAsync<HubException>(() => hub.InvokeAsync("JoinCartGroup", CartId, "wrong"));
        await hub.InvokeAsync("JoinCartGroup", CartId, CartToken);
        var session = await StartAsync(new StartSessionRequest(CartId));
        Assert.Equal(session.SessionId, (await startedEvent.Task.WaitAsync(TimeSpan.FromSeconds(10))).SessionId);
        await _client.PostAsJsonAsync($"/api/cart/{session.SessionId}/items", new CartAddItemRequest("ITEM-1", 50));
        Assert.Equal(100, (await updatedEvent.Task.WaitAsync(TimeSpan.FromSeconds(10))).GetProperty("totalMinor").GetInt32());
        await _client.PostAsync($"/api/cart/{session.SessionId}/checkout", null);
        Assert.Equal("closed", (await closedEvent.Task.WaitAsync(TimeSpan.FromSeconds(10))).Status);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("Customer", false)]
    [InlineData("Staff", true)]
    [InlineData("Admin", true)]
    public async Task SignalRDashboardSubscriptionChecksJwtRoles(string? role, bool allowed)
    {
        await using var hub = CreateHub(role);
        await hub.StartAsync();
        if (allowed) await hub.InvokeAsync("JoinDashboardGroup");
        else await Assert.ThrowsAsync<HubException>(() => hub.InvokeAsync("JoinDashboardGroup"));
    }

    private HubConnection CreateHub(string? role = null) => new HubConnectionBuilder()
        .WithUrl("http://localhost/hubs/cart", options =>
        {
            options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            options.Transports = HttpTransportType.LongPolling;
            if (role is not null) options.AccessTokenProvider = () => Task.FromResult<string?>(_factory.CreateToken(role));
        }).Build();

    [Fact]
    public async Task FailedCloseRollsBackCartAndSessionTogether()
    {
        var session = await StartAsync(new StartSessionRequest(CartId));
        await TelemetryAsync(8);
        _factory.SimulateSessionWriteRace = true;
        Assert.Equal(HttpStatusCode.Conflict, (await _client.PostAsJsonAsync($"/api/sessions/{session.SessionId}/close", new CloseSessionRequest())).StatusCode);
        await _factory.InDatabaseAsync(async db =>
        {
            Assert.Equal(SessionStatus.Open, (await db.Sessions.SingleAsync()).Status);
            Assert.Equal(CartStatus.Active, (await db.Carts.SingleAsync()).Status);
        });
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsJsonAsync($"/api/sessions/{session.SessionId}/close", new CloseSessionRequest())).StatusCode);
    }

    private async Task<CartSummaryResponse> StartAsync(StartSessionRequest request)
    {
        var response = await _client.PostAsJsonAsync("/api/sessions", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CartSummaryResponse>())!;
    }
    private async Task<CartTelemetryResponse> TelemetryAsync(short battery)
    {
        var response = await _client.PatchAsJsonAsync($"/api/carts/{CartId}", new UpdateCartTelemetryRequest(battery, null));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CartTelemetryResponse>())!;
    }
}
