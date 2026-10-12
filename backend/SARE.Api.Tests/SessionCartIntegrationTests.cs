using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Channels;
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
    private readonly Guid _productId = Guid.NewGuid();

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
            var product = new Product
            {
                Id = _productId, CategoryId = category.Id, NameAr = "منتج", NameEn = "Product",
                ImageUrl = "/images/session-product.jpg"
            };
            db.Categories.Add(category); db.Products.Add(product);
            db.ProductVariants.Add(new ProductVariant { Id = _variantId, ProductId = product.Id, Barcode = "ITEM-1", PriceMinor = 100, WeightG = 50 });
            await db.SaveChangesAsync();
        });
    }
    public async Task DisposeAsync() { _client.Dispose(); await _factory.DisposeAsync(); }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("Customer", HttpStatusCode.Forbidden)]
    [InlineData("Staff", HttpStatusCode.Forbidden)]
    [InlineData("Admin", HttpStatusCode.OK)]
    public async Task OnlyAdminsCanLoadAllSessions(string? role, HttpStatusCode expected)
    {
        if (role is not null)
        {
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.CreateToken(role));
        }
        var response = await _client.GetAsync("/api/sessions");
        Assert.Equal(expected, response.StatusCode);
        if (expected == HttpStatusCode.OK)
        {
            Assert.Empty((await response.Content.ReadFromJsonAsync<List<CartSummaryResponse>>())!);
        }
    }

    [Fact]
    public async Task SessionRoutesAndDetectionAcknowledgementsUseTheSameFullContract()
    {
        await using var hub = CreateHub();
        var updates = ListenForSessionUpdates(hub);
        await hub.StartAsync();
        await hub.InvokeAsync("JoinCartGroup", CartId, CartToken);

        var startResponse = await _client.PostAsJsonAsync("/api/cart/session/start",
            new LegacyStartSessionRequest(CartId, _userId));
        Assert.Equal(HttpStatusCode.OK, startResponse.StatusCode);
        var started = (await startResponse.Content.ReadFromJsonAsync<CartSummaryResponse>())!;
        Assert.Equal(_userId, started.UserId);
        Assert.Equal("Customer", started.UserName);
        Assert.Equal(JsonSerializer.Serialize(started), JsonSerializer.Serialize(await ReadUpdateAsync(updates)));

        var request = new CartAddItemRequest("ITEM-1", 50, EventId: Guid.NewGuid());
        var addResponse = await _client.PostAsJsonAsync($"/api/cart/{started.SessionId}/items", request);
        Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);
        var result = (await addResponse.Content.ReadFromJsonAsync<CartItemProcessResult>())!;
        var summary = result.Session;
        Assert.True(result.Success);
        Assert.NotNull(summary);
        var item = Assert.Single(summary.Items);
        Assert.Equal(_productId, item.ProductId);
        Assert.Equal(_variantId, item.VariantId);
        Assert.Equal("/images/session-product.jpg", item.ImageUrl);
        Assert.Equal(50, item.ExpectedWeightG);
        Assert.Equal(DetectionSource.Scanner, item.Source);
        Assert.Equal(100, item.UnitPriceMinor);
        var expected = JsonSerializer.Serialize(summary);
        Assert.Equal(expected, JsonSerializer.Serialize(await ReadUpdateAsync(updates)));

        foreach (var route in new[]
        {
            $"/api/sessions/{started.SessionId}", $"/api/cart/{started.SessionId}",
            $"/api/carts/{CartId}/session", $"/api/cart/active/{CartId}"
        })
        {
            Assert.Equal(expected, JsonSerializer.Serialize(await _client.GetFromJsonAsync<CartSummaryResponse>(route)));
        }
        var joined = await hub.InvokeAsync<CartSummaryResponse?>("JoinCartGroup", CartId, CartToken);
        Assert.Equal(expected, JsonSerializer.Serialize(joined));
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.CreateToken("Admin"));
        var all = (await _client.GetFromJsonAsync<List<CartSummaryResponse>>("/api/sessions"))!;
        Assert.Equal(expected, JsonSerializer.Serialize(Assert.Single(all)));

        var retry = await _client.PostAsJsonAsync($"/api/cart/{started.SessionId}/items", request);
        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
        Assert.Equal(expected, JsonSerializer.Serialize((await retry.Content.ReadFromJsonAsync<CartItemProcessResult>())!.Session));

        var closeResponse = await _client.PostAsJsonAsync($"/api/sessions/{started.SessionId}/close",
            new CloseSessionRequest(_userId, CloseReason.StaffClosed));
        Assert.Equal(HttpStatusCode.OK, closeResponse.StatusCode);
        var closed = (await closeResponse.Content.ReadFromJsonAsync<CartSummaryResponse>())!;
        Assert.Equal("closed", closed.Status);
        Assert.Equal(_userId, closed.ClosedByUserId);
        Assert.Equal(JsonSerializer.Serialize(closed), JsonSerializer.Serialize(await ReadUpdateAsync(updates)));
        var closedRetry = await _client.PostAsJsonAsync($"/api/cart/{started.SessionId}/items", request);
        Assert.Equal(HttpStatusCode.OK, closedRetry.StatusCode);
        Assert.Equal(JsonSerializer.Serialize(closed),
            JsonSerializer.Serialize((await closedRetry.Content.ReadFromJsonAsync<CartItemProcessResult>())!.Session));
        Assert.False(updates.Reader.TryRead(out _));
    }

    [Fact]
    public async Task CloseRecalculatesOnlyActiveItemsUsingStoredPrices()
    {
        var session = await StartAsync(new StartSessionRequest(CartId));
        for (var index = 0; index < 2; index++)
        {
            Assert.Equal(HttpStatusCode.OK,
                (await _client.PostAsJsonAsync($"/api/cart/{session.SessionId}/items", new CartAddItemRequest("ITEM-1", 50))).StatusCode);
        }
        await _factory.InDatabaseAsync(async db =>
        {
            (await db.SessionItems.OrderBy(item => item.AddedAt).FirstAsync()).RemovedAt = DateTime.UtcNow;
            (await db.ProductVariants.SingleAsync()).PriceMinor = 999;
            await db.SaveChangesAsync();
        });
        var response = await _client.PostAsJsonAsync($"/api/sessions/{session.SessionId}/close", new CloseSessionRequest());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var closed = (await response.Content.ReadFromJsonAsync<CartSummaryResponse>())!;
        Assert.Equal(100, closed.TotalMinor);
        Assert.Equal(1, closed.ItemsCount);
        Assert.Equal(100, Assert.Single(closed.Items).UnitPriceMinor);
    }

    [Fact]
    public async Task AllSessionsRequestIncludesEveryCartItsItemsAndClosureMetadata()
    {
        await AddOtherCartAsync();
        var first = await StartAsync(new StartSessionRequest(CartId, "NFC-TEST"));
        Assert.Equal(HttpStatusCode.OK,
            (await _client.PostAsJsonAsync($"/api/cart/{first.SessionId}/items", new CartAddItemRequest("ITEM-1", 50))).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await _client.PostAsJsonAsync($"/api/sessions/{first.SessionId}/close",
                new CloseSessionRequest(_userId, CloseReason.Abandoned))).StatusCode);
        var second = await StartAsync(new StartSessionRequest("C-OTHER"));

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.CreateToken("Admin"));
        var sessions = (await _client.GetFromJsonAsync<List<CartSummaryResponse>>("/api/sessions"))!;
        Assert.Equal(2, sessions.Count);
        Assert.Equal(second.SessionId, sessions[0].SessionId);
        var closed = sessions.Single(session => session.SessionId == first.SessionId);
        Assert.Equal("abandoned", closed.Status);
        Assert.Equal("abandoned", closed.CloseReason);
        Assert.Equal(_userId, closed.ClosedByUserId);
        Assert.NotNull(closed.ClosedAt);
        Assert.Equal("Customer", closed.UserName);
        Assert.Equal(100, closed.TotalMinor);
        Assert.Equal(1, closed.ItemsCount);
        Assert.Equal(_variantId, Assert.Single(closed.Items).VariantId);
        Assert.Equal(100, closed.Items[0].UnitPriceMinor);
        Assert.Equal("منتج", closed.Items[0].NameAr);
        Assert.True(closed.UpdatedAt >= closed.StartedAt);
        Assert.Empty(sessions[0].Items);
        var detail = await _client.GetFromJsonAsync<CartSummaryResponse>($"/api/sessions/{first.SessionId}");
        Assert.Equal(JsonSerializer.Serialize(detail), JsonSerializer.Serialize(closed));
    }

    [Fact]
    public async Task AdminReceivesEveryFullSessionAndEachCartReceivesOnlyItsOwnChanges()
    {
        await AddOtherCartAsync();
        await using var adminHub = CreateHub("Admin");
        await using var cartHub = CreateHub();
        await using var otherCartHub = CreateHub();
        var adminUpdates = ListenForSessionUpdates(adminHub);
        var cartUpdates = ListenForSessionUpdates(cartHub);
        var otherCartUpdates = ListenForSessionUpdates(otherCartHub);
        await adminHub.StartAsync();
        await cartHub.StartAsync();
        await otherCartHub.StartAsync();
        await adminHub.InvokeAsync("JoinSessionsGroup");
        // Membership of both groups must still produce only one SessionUpdated event.
        await adminHub.InvokeAsync("JoinCartGroup", CartId, CartToken);
        await cartHub.InvokeAsync("JoinCartGroup", CartId, CartToken);
        await otherCartHub.InvokeAsync("JoinCartGroup", "C-OTHER", CartToken);

        var first = await StartAsync(new StartSessionRequest(CartId));
        Assert.Equal(first.SessionId, (await ReadUpdateAsync(adminUpdates)).SessionId);
        Assert.Equal(first.SessionId, (await ReadUpdateAsync(cartUpdates)).SessionId);
        var secondResponse = await _client.PostAsJsonAsync("/api/cart/session/start", new LegacyStartSessionRequest("C-OTHER", null));
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        var second = (await secondResponse.Content.ReadFromJsonAsync<CartSummaryResponse>())!;
        Assert.Equal(second.SessionId, (await ReadUpdateAsync(adminUpdates)).SessionId);
        Assert.Equal(second.SessionId, (await ReadUpdateAsync(otherCartUpdates)).SessionId);

        Assert.Equal(HttpStatusCode.OK,
            (await _client.PostAsJsonAsync($"/api/cart/{first.SessionId}/items", new CartAddItemRequest("ITEM-1", 50))).StatusCode);
        var added = await ReadUpdateAsync(adminUpdates);
        Assert.Equal(first.SessionId, added.SessionId);
        Assert.Equal(100, added.TotalMinor);
        Assert.Equal(1, added.ItemsCount);
        Assert.Equal(_variantId, Assert.Single(added.Items).VariantId);
        Assert.Equal(100, added.Items[0].UnitPriceMinor);
        Assert.Equal("منتج", added.Items[0].NameAr);
        Assert.True(added.UpdatedAt >= first.UpdatedAt);
        Assert.True(long.Parse(added.Version) > long.Parse(first.Version));
        Assert.Equal(JsonSerializer.Serialize(added), JsonSerializer.Serialize(await ReadUpdateAsync(cartUpdates)));
        var current = await _client.GetFromJsonAsync<CartSummaryResponse>($"/api/sessions/{first.SessionId}");
        Assert.Equal(JsonSerializer.Serialize(current), JsonSerializer.Serialize(added));

        Assert.Equal(HttpStatusCode.OK,
            (await _client.PostAsJsonAsync($"/api/sessions/{first.SessionId}/close",
                new CloseSessionRequest(Reason: CloseReason.Abandoned))).StatusCode);
        var abandoned = await ReadUpdateAsync(adminUpdates);
        Assert.Equal("abandoned", abandoned.Status);
        Assert.Single(abandoned.Items);
        Assert.NotNull(abandoned.ClosedAt);
        Assert.Equal("abandoned", (await ReadUpdateAsync(cartUpdates)).Status);

        Assert.Equal(HttpStatusCode.OK,
            (await _client.PostAsync($"/api/cart/{second.SessionId}/checkout", null)).StatusCode);
        var closed = await ReadUpdateAsync(adminUpdates);
        Assert.Equal(second.SessionId, closed.SessionId);
        Assert.Equal("closed", closed.Status);
        Assert.Equal(second.SessionId, (await ReadUpdateAsync(otherCartUpdates)).SessionId);
        Assert.False(adminUpdates.Reader.TryRead(out _));
        Assert.False(cartUpdates.Reader.TryRead(out _));
        Assert.False(otherCartUpdates.Reader.TryRead(out _));
    }

    [Fact]
    public async Task FailedSessionWritesDoNotPublishAnUncommittedSession()
    {
        await using var hub = CreateHub("Admin");
        var updates = ListenForSessionUpdates(hub);
        await hub.StartAsync();
        await hub.InvokeAsync("JoinSessionsGroup");
        var session = await StartAsync(new StartSessionRequest(CartId));
        await ReadUpdateAsync(updates);

        Assert.Equal(HttpStatusCode.Conflict,
            (await _client.PostAsJsonAsync("/api/sessions", new StartSessionRequest(CartId))).StatusCode);
        _factory.SimulateSessionWriteRace = true;
        Assert.Equal(HttpStatusCode.Conflict,
            (await _client.PostAsJsonAsync($"/api/cart/{session.SessionId}/items", new CartAddItemRequest("ITEM-1", 50))).StatusCode);
        await _factory.InDatabaseAsync(async db => Assert.Empty(await db.SessionItems.ToListAsync()));

        Assert.Equal(HttpStatusCode.OK,
            (await _client.PostAsJsonAsync($"/api/cart/{session.SessionId}/items", new CartAddItemRequest("ITEM-1", 50))).StatusCode);
        var added = await ReadUpdateAsync(updates);
        Assert.Equal(100, added.TotalMinor);
        Assert.Single(added.Items);

        _factory.SimulateSessionWriteRace = true;
        Assert.Equal(HttpStatusCode.Conflict,
            (await _client.PostAsJsonAsync($"/api/sessions/{session.SessionId}/close", new CloseSessionRequest())).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await _client.PostAsJsonAsync($"/api/sessions/{session.SessionId}/close", new CloseSessionRequest())).StatusCode);
        Assert.Equal("closed", (await ReadUpdateAsync(updates)).Status);
        Assert.False(updates.Reader.TryRead(out _));
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("Customer", false)]
    [InlineData("Staff", false)]
    [InlineData("Admin", true)]
    public async Task AllSessionsSubscriptionRequiresAdmin(string? role, bool allowed)
    {
        await using var hub = CreateHub(role);
        await hub.StartAsync();
        if (allowed)
        {
            await hub.InvokeAsync("JoinSessionsGroup");
            await hub.InvokeAsync("LeaveSessionsGroup");
        }
        else
        {
            await Assert.ThrowsAsync<HubException>(() => hub.InvokeAsync("JoinSessionsGroup"));
        }
    }

    private async Task AddOtherCartAsync()
    {
        await _factory.InDatabaseAsync(async db =>
        {
            db.Carts.Add(new SARE.Domain.Cart.Cart
            {
                Id = "C-OTHER",
                TokenHash = TokenHasher.HashToken(CartToken),
                BatteryPct = 90
            });
            await db.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task CartWithoutAConfiguredTokenDoesNotExposeSessionUpdates()
    {
        await _factory.InDatabaseAsync(async db =>
        {
            db.Carts.Add(new SARE.Domain.Cart.Cart { Id = "UNPROVISIONED" });
            await db.SaveChangesAsync();
        });
        await using var hub = CreateHub();
        await hub.StartAsync();
        await Assert.ThrowsAsync<HubException>(() => hub.InvokeAsync("JoinCartGroup", "UNPROVISIONED", ""));
    }

    private static Channel<CartSummaryResponse> ListenForSessionUpdates(HubConnection hub)
    {
        var updates = Channel.CreateUnbounded<CartSummaryResponse>();
        hub.On<CartSummaryResponse>("SessionUpdated", session => { updates.Writer.TryWrite(session); });
        return updates;
    }

    private static Task<CartSummaryResponse> ReadUpdateAsync(Channel<CartSummaryResponse> updates)
    {
        return updates.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));
    }

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
        var session = (await response.Content.ReadFromJsonAsync<CartSummaryResponse>())!;
        Assert.Equal(_userId, (await _client.GetFromJsonAsync<CartSummaryResponse>($"/api/sessions/{session.SessionId}"))!.UserId);
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsJsonAsync($"/api/sessions/{session.SessionId}/close", new CloseSessionRequest(Reason: CloseReason.Abandoned))).StatusCode);
        Assert.Equal("abandoned", (await _client.GetFromJsonAsync<CartSummaryResponse>($"/api/cart/{session.SessionId}"))!.Status);
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
    public async Task CartReceivesOnlySessionUpdatedForSessionChangesAndKeepsCheckoutSuccess()
    {
        await using var hub = CreateHub();
        var updates = ListenForSessionUpdates(hub);
        var obsoleteEvents = Channel.CreateUnbounded<string>();
        foreach (var eventName in new[] { "SessionStarted", "CartUpdated", "SessionClosed" })
        {
            hub.On<JsonElement>(eventName, _ => { obsoleteEvents.Writer.TryWrite(eventName); });
        }
        var checkoutEvent = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        hub.On<JsonElement>("CheckoutSuccess", value => checkoutEvent.TrySetResult(value));
        await hub.StartAsync();
        await Assert.ThrowsAsync<HubException>(() => hub.InvokeAsync("JoinCartGroup", CartId, "wrong"));
        await hub.InvokeAsync("JoinCartGroup", CartId, CartToken);
        var session = await StartAsync(new StartSessionRequest(CartId));
        var started = await ReadUpdateAsync(updates);
        Assert.Equal(session.SessionId, started.SessionId);
        Assert.Equal("open", started.Status);
        Assert.Empty(started.Items);
        Assert.Equal(HttpStatusCode.OK,
            (await _client.PostAsJsonAsync($"/api/cart/{session.SessionId}/items", new CartAddItemRequest("ITEM-1", 50))).StatusCode);
        var firstUpdate = await ReadUpdateAsync(updates);
        Assert.Equal(session.SessionId, firstUpdate.SessionId);
        Assert.Equal(CartId, firstUpdate.CartId);
        Assert.Equal(100, firstUpdate.TotalMinor);
        Assert.Equal(1, firstUpdate.ItemsCount);
        Assert.Equal(_variantId, Assert.Single(firstUpdate.Items).VariantId);

        Assert.Equal(HttpStatusCode.OK,
            (await _client.PostAsJsonAsync($"/api/cart/{session.SessionId}/items", new CartAddItemRequest("ITEM-1", 50))).StatusCode);
        var secondUpdate = await ReadUpdateAsync(updates);
        Assert.Equal(200, secondUpdate.TotalMinor);
        Assert.Equal(2, secondUpdate.ItemsCount);
        Assert.Equal(2, secondUpdate.Items.Count);
        Assert.All(secondUpdate.Items, item => Assert.Equal("منتج", item.NameAr));
        var current = await _client.GetFromJsonAsync<CartSummaryResponse>($"/api/sessions/{session.SessionId}");
        Assert.Equal(JsonSerializer.Serialize(current), JsonSerializer.Serialize(secondUpdate));
        Assert.Equal(HttpStatusCode.OK,
            (await _client.PostAsync($"/api/cart/{session.SessionId}/checkout", null)).StatusCode);
        var closed = await ReadUpdateAsync(updates);
        Assert.Equal("closed", closed.Status);
        Assert.Equal(200, closed.TotalMinor);
        Assert.Equal(2, closed.Items.Count);
        var checkout = await checkoutEvent.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(session.SessionId, checkout.GetProperty("sessionId").GetGuid());
        Assert.Equal(200, checkout.GetProperty("totalPaidMinor").GetInt32());
        Assert.False(obsoleteEvents.Reader.TryRead(out _));
        Assert.False(updates.Reader.TryRead(out _));
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
