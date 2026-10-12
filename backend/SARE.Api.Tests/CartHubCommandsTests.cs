using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using SARE.Application.Common.Security;
using SARE.Application.DTOs.Cart;
using SARE.Domain.Cart;
using SARE.Domain.Catalog;
using SARE.Domain.Enums;

namespace SARE.Api.Tests;

public sealed class CartHubCommandsTests : IAsyncLifetime
{
    private const string CartId = "SOCKET-CART";
    private const string OtherCartId = "OTHER-CART";
    private const string Token = "socket-device-secret";
    private readonly CatalogApiFactory _factory = new();
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-Cart-Token", Token);
        await _factory.InDatabaseAsync(async db =>
        {
            await db.Database.EnsureCreatedAsync();
            db.Carts.AddRange(
                new Domain.Cart.Cart { Id = CartId, TokenHash = TokenHasher.HashToken(Token), BatteryPct = 90 },
                new Domain.Cart.Cart { Id = OtherCartId, TokenHash = TokenHasher.HashToken(Token), BatteryPct = 90 });
            var category = new Category { Id = Guid.NewGuid(), NameAr = "تصنيف", NameEn = "Category" };
            var product = new Product { Id = Guid.NewGuid(), CategoryId = category.Id, NameAr = "منتج", NameEn = "Product" };
            db.Categories.Add(category);
            db.Products.Add(product);
            db.ProductVariants.Add(new ProductVariant
            {
                Id = Guid.NewGuid(), ProductId = product.Id, Barcode = "SOCKET-ITEM", WeightG = 50, PriceMinor = 100
            });
            await db.SaveChangesAsync();
        });
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task JoinReturnsNullWhenThereIsNoOpenSessionIncludingAfterClosure()
    {
        await using var hub = CreateHub();
        await hub.StartAsync();
        Assert.Null(await JoinAsync(hub));
        var session = await StartAsync(hub);
        await hub.InvokeAsync<CartSummaryResponse>("CloseSession", session.SessionId);
        Assert.Null(await JoinAsync(hub));
    }

    [Fact]
    public async Task NewConnectionRecoversFullActiveSessionAndItsPriceSnapshots()
    {
        Guid sessionId;
        await using (var first = CreateHub())
        {
            await first.StartAsync();
            await JoinAsync(first);
            sessionId = (await StartAsync(first)).SessionId;
            await DetectAsync(first, sessionId, Guid.NewGuid());
        }

        await _factory.InDatabaseAsync(async db =>
        {
            var variant = await db.ProductVariants.SingleAsync();
            variant.PriceMinor = 999;
            variant.WeightG = 80;
            await db.SaveChangesAsync();
        });
        await using var reconnected = CreateHub();
        await reconnected.StartAsync();
        var snapshot = await JoinAsync(reconnected);
        Assert.NotNull(snapshot);
        Assert.Equal(sessionId, snapshot.SessionId);
        Assert.Equal("open", snapshot.Status);
        Assert.Equal(100, snapshot.TotalMinor);
        Assert.Equal(100, Assert.Single(snapshot.Items).UnitPriceMinor);
        Assert.Equal(80, snapshot.Items[0].ExpectedWeightG);
    }

    [Fact]
    public async Task SocketCommandsUseExistingServicesAndPublishFullSessionsToCartAndAdmin()
    {
        await using var cart = CreateHub();
        await using var admin = CreateHub("Admin");
        var cartUpdates = Listen(cart);
        var adminUpdates = Listen(admin);
        await cart.StartAsync();
        await admin.StartAsync();
        await JoinAsync(cart);
        await admin.InvokeAsync("JoinSessionsGroup");

        var session = await StartAsync(cart);
        Assert.Equal(session.SessionId, (await ReadAsync(cartUpdates)).SessionId);
        Assert.Equal(session.SessionId, (await ReadAsync(adminUpdates)).SessionId);
        var detected = await DetectAsync(cart, session.SessionId, Guid.NewGuid());
        Assert.True(detected.Success);
        Assert.NotNull(detected.Session);
        var updated = await ReadAsync(cartUpdates);
        Assert.Equal(100, updated.TotalMinor);
        Assert.Equal(JsonSerializer.Serialize(updated), JsonSerializer.Serialize(detected.Session));
        Assert.Equal(JsonSerializer.Serialize(updated), JsonSerializer.Serialize(await ReadAsync(adminUpdates)));
        Assert.Equal(50, Assert.Single(updated.Items).ExpectedWeightG);
        Assert.Equal(DetectionSource.Scanner, updated.Items[0].Source);
        Assert.Null(updated.Items[0].ImageUrl);

        var telemetry = await cart.InvokeAsync<CartTelemetryResponse>("UpdateCartTelemetry",
            new UpdateCartTelemetryRequest(80, "socket-v1"));
        Assert.Equal(CartId, telemetry.CartId);
        Assert.Equal((short)80, telemetry.BatteryPct);
        Assert.Equal(120, telemetry.NextReportIntervalSeconds);

        var closed = await cart.InvokeAsync<CartSummaryResponse>("CloseSession", session.SessionId);
        Assert.Equal("closed", closed.Status);
        Assert.Single(closed.Items);
        Assert.Equal("closed", (await ReadAsync(cartUpdates)).Status);
        Assert.Equal("closed", (await ReadAsync(adminUpdates)).Status);
    }

    [Fact]
    public async Task HttpAdministrativeCloseReachesCartAndRejectsFurtherNewScans()
    {
        await using var cart = CreateHub();
        var updates = Listen(cart);
        await cart.StartAsync();
        await JoinAsync(cart);
        var session = await StartAsync(cart);
        await ReadAsync(updates);
        var response = await _client.PostAsJsonAsync($"/api/sessions/{session.SessionId}/close",
            new CloseSessionRequest(Reason: CloseReason.StaffClosed));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("closed", (await ReadAsync(updates)).Status);
        Assert.False((await DetectAsync(cart, session.SessionId, Guid.NewGuid())).Success);
        await AssertCountsAsync(0, 0, 0);
    }

    [Fact]
    public async Task CartCannotReadOrModifyAnotherCartsSession()
    {
        await using var first = CreateHub();
        await using var other = CreateHub();
        await first.StartAsync();
        await other.StartAsync();
        await JoinAsync(first);
        Assert.Null(await JoinAsync(other, OtherCartId));
        var session = await StartAsync(first);
        var scanError = await Assert.ThrowsAsync<HubException>(() => DetectAsync(other, session.SessionId, Guid.NewGuid()));
        Assert.Contains("SESSION_NOT_ACCESSIBLE", scanError.Message);
        var closeError = await Assert.ThrowsAsync<HubException>(() => other.InvokeAsync<CartSummaryResponse>("CloseSession", session.SessionId));
        Assert.Contains("SESSION_NOT_ACCESSIBLE", closeError.Message);
        Assert.Null(await JoinAsync(other, OtherCartId));
        await AssertCountsAsync(0, 0, 0);
    }

    [Theory]
    [InlineData("StartSession")]
    [InlineData("ProcessItemDetection")]
    [InlineData("UpdateCartTelemetry")]
    [InlineData("CloseSession")]
    public async Task CommandsRequireAnAuthenticatedCartSubscription(string method)
    {
        await using var hub = CreateHub();
        await hub.StartAsync();
        var error = await Assert.ThrowsAsync<HubException>(async () =>
        {
            switch (method)
            {
                case "StartSession": await StartAsync(hub); break;
                case "ProcessItemDetection": await DetectAsync(hub, Guid.NewGuid(), Guid.NewGuid()); break;
                case "UpdateCartTelemetry": await hub.InvokeAsync(method, new UpdateCartTelemetryRequest(80, null)); break;
                case "CloseSession": await hub.InvokeAsync(method, Guid.NewGuid()); break;
            }
        });
        Assert.Contains("CART_NOT_JOINED", error.Message);
    }

    [Fact]
    public async Task SwitchingCartUnsubscribesTheOldCartAndLeavingRemovesCommandAccess()
    {
        await using var hub = CreateHub();
        var updates = Listen(hub);
        await hub.StartAsync();
        await JoinAsync(hub);
        await JoinAsync(hub, OtherCartId);
        var oldStart = await _client.PostAsJsonAsync("/api/sessions", new StartSessionRequest(CartId));
        Assert.Equal(HttpStatusCode.Created, oldStart.StatusCode);
        var current = await StartAsync(hub);
        Assert.Equal(OtherCartId, current.CartId);
        Assert.Equal(current.SessionId, (await ReadAsync(updates)).SessionId);
        Assert.False(updates.Reader.TryRead(out _));
        await hub.InvokeAsync("LeaveCartGroup", OtherCartId);
        var error = await Assert.ThrowsAsync<HubException>(() => StartAsync(hub));
        Assert.Contains("CART_NOT_JOINED", error.Message);
    }

    [Fact]
    public async Task RotatingHardwareTokenRevokesCommandsOnAnExistingConnection()
    {
        await using var hub = CreateHub();
        await hub.StartAsync();
        await JoinAsync(hub);
        await _factory.InDatabaseAsync(async db =>
        {
            (await db.Carts.SingleAsync(cart => cart.Id == CartId)).TokenHash = TokenHasher.HashToken("replacement");
            await db.SaveChangesAsync();
        });
        var error = await Assert.ThrowsAsync<HubException>(() => StartAsync(hub));
        Assert.Contains("INVALID_CART_TOKEN", error.Message);
    }

    [Fact]
    public async Task AcceptedDetectionRetryDoesNotDuplicateItemsTotalsOrNotificationsEvenAfterClose()
    {
        await using var hub = CreateHub();
        var updates = Listen(hub);
        await hub.StartAsync();
        await JoinAsync(hub);
        var session = await StartAsync(hub);
        await ReadAsync(updates);
        var eventId = Guid.NewGuid();
        Assert.True((await DetectAsync(hub, session.SessionId, eventId)).Success);
        await ReadAsync(updates);
        Assert.True((await DetectAsync(hub, session.SessionId, eventId)).Success);
        Assert.False(updates.Reader.TryRead(out _));
        await hub.InvokeAsync("CloseSession", session.SessionId);
        await ReadAsync(updates);
        var closedReplay = await DetectAsync(hub, session.SessionId, eventId);
        Assert.True(closedReplay.Success);
        Assert.NotNull(closedReplay.Session);
        Assert.Equal("closed", closedReplay.Session.Status);
        Assert.NotNull(closedReplay.Session.ClosedAt);
        Assert.Equal(100, Assert.Single(closedReplay.Session.Items).UnitPriceMinor);
        Assert.False(updates.Reader.TryRead(out _));
        await AssertCountsAsync(1, 1, 100);
    }

    [Fact]
    public async Task RejectedRetryKeepsOriginalOutcomeEvenIfProductWeightChanges()
    {
        await using var hub = CreateHub();
        var alerts = Channel.CreateUnbounded<string>();
        hub.On<System.Text.Json.JsonElement>("WeightMismatchWarning", payload => alerts.Writer.TryWrite(payload.ToString()));
        await hub.StartAsync();
        await JoinAsync(hub);
        var session = await StartAsync(hub);
        var eventId = Guid.NewGuid();
        var first = await DetectAsync(hub, session.SessionId, eventId, weight: 500);
        Assert.True(first.IsWeightMismatch);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await alerts.Reader.ReadAsync(timeout.Token);
        await _factory.InDatabaseAsync(async db =>
        {
            (await db.ProductVariants.SingleAsync()).WeightG = 500;
            await db.SaveChangesAsync();
        });
        var retry = await DetectAsync(hub, session.SessionId, eventId, weight: 500);
        Assert.False(retry.Success);
        Assert.True(retry.IsWeightMismatch);
        Assert.Null(retry.ExpectedWeightG);
        Assert.False(alerts.Reader.TryRead(out _));
        await AssertCountsAsync(0, 1, 0);
    }

    [Fact]
    public async Task ReusingEventIdWithDifferentPayloadOrNewSessionIsRejected()
    {
        await using var hub = CreateHub();
        await hub.StartAsync();
        await JoinAsync(hub);
        var first = await StartAsync(hub);
        var eventId = Guid.NewGuid();
        await DetectAsync(hub, first.SessionId, eventId);
        var changed = await Assert.ThrowsAsync<HubException>(() => DetectAsync(hub, first.SessionId, eventId, weight: 51));
        Assert.Contains("EVENT_ID_REUSED", changed.Message);
        await hub.InvokeAsync("CloseSession", first.SessionId);
        var second = await StartAsync(hub);
        var reused = await Assert.ThrowsAsync<HubException>(() => DetectAsync(hub, second.SessionId, eventId));
        Assert.Contains("EVENT_ID_REUSED", reused.Message);
        Assert.True((await DetectAsync(hub, first.SessionId, eventId)).Success);
        Assert.Empty((await JoinAsync(hub))!.Items);
        await AssertCountsAsync(1, 1, 100);
    }

    [Theory]
    [InlineData(null, 50, null)]
    [InlineData("SOCKET-ITEM", -1, "valid")]
    [InlineData("", 50, "valid")]
    [InlineData("SOCKET-ITEM", 50, null)]
    [InlineData("SOCKET-ITEM", 50, "empty")]
    public async Task InvalidDetectionsAreRejectedBeforeWriting(string? barcode, int weight, string? eventKind)
    {
        await using var hub = CreateHub();
        await hub.StartAsync();
        await JoinAsync(hub);
        var session = await StartAsync(hub);
        Guid? eventId = eventKind == "valid" ? Guid.NewGuid() : eventKind == "empty" ? Guid.Empty : null;
        var error = await Assert.ThrowsAsync<HubException>(() => hub.InvokeAsync<CartItemProcessResult>(
            "ProcessItemDetection", session.SessionId, new CartAddItemRequest(barcode!, weight, EventId: eventId)));
        Assert.Contains("VALIDATION_FAILED", error.Message);
        await AssertCountsAsync(0, 0, 0);
    }

    [Theory]
    [InlineData(50, true, 1, 100)]
    [InlineData(500, false, 0, 0)]
    public async Task ConcurrentRetryRollsBackLosingWritesAndAcknowledgesWinningEvent(int weight, bool success, int itemCount, int total)
    {
        await using var hub = CreateHub();
        await hub.StartAsync();
        await JoinAsync(hub);
        var session = await StartAsync(hub);
        _factory.SimulateDetectionRace = true;
        var result = await DetectAsync(hub, session.SessionId, Guid.NewGuid(), weight);
        Assert.Equal(success, result.Success);
        Assert.Contains("already", result.Message);
        await AssertCountsAsync(itemCount, 1, total);
    }

    [Fact]
    public async Task DatabaseRejectsTwoOpenSessionsButAllowsClosedHistory()
    {
        await _factory.InDatabaseAsync(async db =>
        {
            var now = DateTime.UtcNow;
            db.Sessions.AddRange(
                new Session { Id = Guid.NewGuid(), CartId = CartId, StartedAt = now, LastActivityAt = now },
                new Session { Id = Guid.NewGuid(), CartId = CartId, StartedAt = now, LastActivityAt = now });
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        });
        await using var hub = CreateHub();
        await hub.StartAsync();
        await JoinAsync(hub);
        var first = await StartAsync(hub);
        await hub.InvokeAsync("CloseSession", first.SessionId);
        var second = await StartAsync(hub);
        Assert.NotEqual(first.SessionId, second.SessionId);
        await _factory.InDatabaseAsync(async db => Assert.Equal(1, await db.Sessions.CountAsync(session => session.Status == SessionStatus.Open)));
    }

    [Fact]
    public async Task ConcurrentStartReturnsConflictAndDoesNotPublishAnUncommittedSession()
    {
        await using var hub = CreateHub();
        var updates = Listen(hub);
        await hub.StartAsync();
        await JoinAsync(hub);
        _factory.SimulateSessionStartRace = true;
        var error = await Assert.ThrowsAsync<HubException>(() => StartAsync(hub));
        Assert.Contains("CONFLICT", error.Message);
        Assert.False(updates.Reader.TryRead(out _));
        Assert.NotNull(await JoinAsync(hub));
        await _factory.InDatabaseAsync(async db => Assert.Equal(1, await db.Sessions.CountAsync()));
    }

    private HubConnection CreateHub(string? role = null)
    {
        return new HubConnectionBuilder().WithUrl("http://localhost/hubs/cart", options =>
        {
            options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            options.Transports = HttpTransportType.LongPolling;
            if (role is not null) options.AccessTokenProvider = () => Task.FromResult<string?>(_factory.CreateToken(role));
        }).Build();
    }

    private static Task<CartSummaryResponse?> JoinAsync(HubConnection hub, string cartId = CartId)
        => hub.InvokeAsync<CartSummaryResponse?>("JoinCartGroup", cartId, Token);

    private static Task<CartSummaryResponse> StartAsync(HubConnection hub)
        => hub.InvokeAsync<CartSummaryResponse>("StartSession", new CartStartSessionRequest());

    private static Task<CartItemProcessResult> DetectAsync(HubConnection hub, Guid sessionId, Guid eventId, int weight = 50)
        => hub.InvokeAsync<CartItemProcessResult>("ProcessItemDetection", sessionId,
            new CartAddItemRequest("SOCKET-ITEM", weight, EventId: eventId));

    private static Channel<CartSummaryResponse> Listen(HubConnection hub)
    {
        var channel = Channel.CreateUnbounded<CartSummaryResponse>();
        hub.On<CartSummaryResponse>("SessionUpdated", session => channel.Writer.TryWrite(session));
        return channel;
    }

    private static async Task<CartSummaryResponse> ReadAsync(Channel<CartSummaryResponse> channel)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        return await channel.Reader.ReadAsync(timeout.Token);
    }

    private Task AssertCountsAsync(int items, int detections, int total) => _factory.InDatabaseAsync(async db =>
    {
        Assert.Equal(items, await db.SessionItems.CountAsync());
        Assert.Equal(detections, await db.DetectionEvents.CountAsync());
        Assert.Equal(total, await db.Sessions.SumAsync(session => session.TotalMinor));
    });
}
