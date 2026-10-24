using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Channels;
using SARE.Application.Common.Security;
using SARE.Application.DTOs.Cart;
using SARE.Domain.Cart;
using SARE.Domain.Catalog;
using SARE.Domain.Enums;

namespace SARE.Api.Tests;

// اختبار الفلو من الاتصال الحقيقي: الجرد له قائمة، والفاتورة لا تتغير قبل اعتماد الأدمن.
public sealed class CartAuditFlowTests : IAsyncLifetime
{
    private readonly CatalogApiFactory _factory = new();
    private readonly Guid _boot = Guid.NewGuid();
    private HubConnection _cart = null!;
    private HubConnection _admin = null!;
    private HttpClient _adminClient = null!;
    private Guid _session;

    public async Task InitializeAsync()
    {
        _factory.CreateClient();
        await _factory.InDatabaseAsync(async db =>
        {
            await db.Database.EnsureCreatedAsync();
            db.Carts.Add(new Cart { Id = "AUDIT-CART", TokenHash = TokenHasher.HashToken("audit-token") });
            var category = new Category { Id = Guid.NewGuid(), NameAr = "تصنيف", NameEn = "Category" };
            var product = new Product { Id = Guid.NewGuid(), CategoryId = category.Id, NameAr = "منتج", NameEn = "Product", WeightTolerancePercent = 10m };
            db.Categories.Add(category);
            db.Products.Add(product);
            db.ProductVariants.AddRange(
                new ProductVariant { Id = Guid.NewGuid(), ProductId = product.Id, Barcode = "A", PriceMinor = 1000,
                    WeightG = 100 },
                new ProductVariant { Id = Guid.NewGuid(), ProductId = product.Id, Barcode = "B", PriceMinor = 2000,
                    WeightG = 200 });
            await db.SaveChangesAsync();
        });
        _cart = await ConnectAsync();
        _admin = await ConnectAsync(admin: true);
        _adminClient = _factory.CreateClient();
        _adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.CreateToken());
        _session = (await _cart.InvokeAsync<CartSummaryResponse>("StartSession", new CartStartSessionRequest())).SessionId;
    }

    public async Task DisposeAsync()
    {
        await _cart.DisposeAsync();
        await _admin.DisposeAsync();
        _adminClient.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task StaffRecountClosesTheWholeExitQueueAndResumesNormalShopping()
    {
        await ReportAsync([Enter("A"), Enter("B")], new(0, 300, true));
        await ReportAsync([Enter(null)], new(300, 400, true));
        await ReportAsync([Exit("A")], new(400, 300, true));
        var queued = await ReportAsync([Exit("B")], new(300, 100, true));
        Assert.Equal(2, queued.Session.PendingMovements.Count(item => item.UnconfirmedExitWeightG.HasValue));
        var audit = await StartAsync();
        Assert.Null((await SnapshotAsync()).CurrentExitMovementId);
        await ReportAsync([Scan("A")], audit: audit);
        await ReportAsync([], new(0, 100, true), audit);
        var committed = await AdminAsync("commit", audit);
        Assert.Empty(committed.Session.PendingMovements);
        Assert.Equal(0, committed.Session.PendingWeightG);
        Assert.Equal(0, committed.Session.UnconfirmedExitWeightG);
        Assert.True(committed.Session.CanCheckout);
        var next = await ReportAsync([Enter("B")], new(100, 300, true));
        Assert.Equal(3000, next.Session.TotalMinor);
        Assert.True(next.Session.CanCheckout);
    }

    [Fact]
    public async Task FullAuditReplacesTheInvoiceOnlyAtCommitAndClosesOldProblems()
    {
        await ReportAsync([Enter("A"), Enter(null)], new(0, 300, true));
        var audit = await StartAsync();
        // الموظف يخرج كل المحتويات: أحداث الكاميرا لا تزيل بند A من الفاتورة.
        await ReportAsync([Exit("A"), Exit(null)], new(300, 0, true), audit);
        await ReportAsync([Scan("A"), Scan("B")], audit: audit);
        var counting = await SnapshotAsync();
        Assert.Equal(1000, counting.TotalMinor);
        Assert.Equal(3000, counting.Audit!.TotalMinor);
        Assert.Equal(2, counting.Audit.Items.Count);
        Assert.False(counting.CanCheckout);
        var placement = await ReportAsync([Enter("A"), Enter("B")], new(0, 310, true), audit);
        Assert.Equal(1000, placement.Session.TotalMinor);
        Assert.Equal(2, placement.Session.Audit!.Items.Count);
        var request = await CommandAsync(audit);
        var result = await SendAdminAsync("audit/commit", request);
        Assert.Equal(3000, result.Session.TotalMinor);
        Assert.Equal(2, result.Session.Items.Count);
        Assert.All(result.Session.Items, item => Assert.Equal(BasketPresence.Inside, item.BasketPresence));
        Assert.Equal(0, result.Session.PendingWeightG);
        Assert.Empty(result.Session.PendingMovements);
        Assert.Null(result.Session.Audit);
        Assert.Null(result.Session.StaffRequestedAt);
        Assert.True(result.Session.CanCheckout);
        var replay = await SendAdminAsync("audit/commit", request);
        Assert.Equal(result.Session.Version, replay.Session.Version);
        await _factory.InDatabaseAsync(async db =>
        {
            var saved = await db.CartBasketAudits.Include(a => a.Items).SingleAsync();
            Assert.NotNull(saved.CompletedByUserId);
            Assert.NotNull(saved.CompletedAtUtc);
            Assert.Equal(2, saved.Items.Count);
        });
    }

    [Fact]
    public async Task NeedsStaffFreezesScansAndRemovalRequestsAsWellAsPhysicalChanges()
    {
        await ReportAsync([Scan("A")]);
        await _cart.InvokeAsync<CartMovementResult>("RequestStaff", _session, await CommandAsync());
        var result = await ReportAsync([Scan("B"), Enter("A")], new(0, 100, true));
        Assert.Equal(1000, result.Session.TotalMinor);
        Assert.Single(result.Session.Items);
        Assert.Equal(BasketPresence.Unconfirmed, result.Session.Items[0].BasketPresence);
        Assert.NotNull(result.Session.StaffRequestedAt);
        await Assert.ThrowsAsync<HubException>(() => _cart.InvokeAsync<CartSummaryResponse>(
            "RemoveSessionItem", _session, result.Session.Items[0].Id));
        await Assert.ThrowsAsync<HubException>(() => _cart.InvokeAsync<CartSummaryResponse>("CloseSession", _session));
    }

    [Fact]
    public async Task CartCannotStartAnAuditAndAdminCannotCommitUncountedContents()
    {
        await _cart.InvokeAsync<CartMovementResult>("RequestStaff", _session, await CommandAsync());
        var unauthorizedCommand = await CommandAsync();
        await Assert.ThrowsAsync<HubException>(() => _cart.InvokeAsync<CartMovementResult>(
            "BeginCartAudit", _session, unauthorizedCommand));
        var audit = (await AdminAsync("start")).Session.Audit!.Id;
        await ReportAsync([], new(100, 100, true), audit);
        await Assert.ThrowsAsync<HttpRequestException>(() => AdminAsync("commit", audit));
        Assert.Equal(BasketState.Auditing, (await SnapshotAsync()).BasketState);
    }

    [Fact]
    public async Task ScanningStartsImmediatelyWithCurrentAuditIdAndRetryNeverCountsTwice()
    {
        var audit = await StartAsync();
        await ReportAsync([Scan("B")]); // مسحة تسوق قديمة بلا معرف الجرد.
        Assert.Empty((await SnapshotAsync()).Audit!.Items);
        var request = await MovementAsync([Scan("A")], audit: audit);
        await _cart.InvokeAsync<CartMovementResult>("ReportCartMovement", _session, request);
        await _cart.InvokeAsync<CartMovementResult>("ReportCartMovement", _session, request);
        Assert.Single((await SnapshotAsync()).Audit!.Items);
        Assert.Empty((await SnapshotAsync()).Items);
    }

    [Fact]
    public async Task CameraCrossingsAndCancelScanCannotChangeTheAuditList()
    {
        var audit = await StartAsync();
        var scanned = await ReportAsync([Scan("A")], audit: audit);
        await ReportAsync([Enter("A")], new(0, 100, true), audit);
        var result = await ReportAsync([
            Enter("B"), Exit("A"),
            new(CartItemAction.CancelScan, "A", CartItemSource.Scanner,
                SessionItemId: scanned.Session.Audit!.Items[0].Id)
        ], new(100, 200, true), audit);
        Assert.Equal("A", Assert.Single(result.Session.Audit!.Items).Barcode);
        Assert.Empty(result.Session.Items);
        Assert.Equal(0, result.Session.TotalMinor);
        Assert.Equal(0, result.Session.PendingWeightG);
        Assert.All(result.Movement.Items, item => Assert.Equal("NoChange", item.Effect));
        await Assert.ThrowsAsync<HttpRequestException>(() => AdminAsync("commit", audit));
    }

    [Fact]
    public async Task FinalWeightMustComeAfterTheScansAndAContradictionKeepsAuditOpen()
    {
        var audit = await StartAsync();
        await ReportAsync([Scan("A")], audit: audit);
        await Assert.ThrowsAsync<HttpRequestException>(() => AdminAsync("commit", audit));
        await ReportAsync([], new(0, 500, true), audit);
        await Assert.ThrowsAsync<HttpRequestException>(() => AdminAsync("commit", audit));
        var snapshot = await SnapshotAsync();
        Assert.Equal(BasketState.Auditing, snapshot.BasketState);
        Assert.Empty(snapshot.Items);
        Assert.Single(snapshot.Audit!.Items);
        await ReportAsync([], new(500, 105, true), audit);
        Assert.True((await AdminAsync("commit", audit)).Session.CanCheckout);
    }

    [Fact]
    public async Task AdminCanCorrectADuplicateScanAndItRequiresAFreshFinalWeight()
    {
        var audit = await StartAsync();
        await ReportAsync([Scan("A"), Scan("A")], new(0, 100, true), audit);
        var snapshot = await SnapshotAsync();
        var command = (await CommandAsync(audit)) with { AuditItemId = snapshot.Audit!.Items[1].Id };
        await SendAdminAsync("audit/remove-item", command);
        await Assert.ThrowsAsync<HttpRequestException>(() => AdminAsync("commit", audit));
        await ReportAsync([], new(100, 100, true), audit);
        Assert.Equal(1000, (await AdminAsync("commit", audit)).Session.TotalMinor);
    }

    [Fact]
    public async Task DelayedAuditAndCameraReportsNeverReplayAsShoppingAfterCommit()
    {
        var audit = await StartAsync();
        await ReportAsync([Scan("A")], audit: audit);
        var old = await MovementAsync([Enter("B")], new(0, 200, true));
        var oldScan = await MovementAsync([Scan("B")], audit: audit);
        await ReportAsync([], new(0, 100, true), audit);
        await AdminAsync("commit", audit);
        var current = await SnapshotAsync();
        old = old with { Sequence = current.LastMovementSequence + 1 };
        var ignored = await _cart.InvokeAsync<CartMovementResult>("ReportCartMovement", _session, old);
        Assert.Equal(1000, ignored.Session.TotalMinor);
        Assert.Equal(100, ignored.Session.BasketWeightG);
        oldScan = oldScan with { Sequence = ignored.Session.LastMovementSequence + 1 };
        var lateScan = await _cart.InvokeAsync<CartMovementResult>("ReportCartMovement", _session, oldScan);
        Assert.Equal(1000, lateScan.Session.TotalMinor);
        var ordinary = await ReportAsync([Enter("B")], new(100, 300, true));
        Assert.Equal(3000, ordinary.Session.TotalMinor);
    }

    [Fact]
    public async Task FreshShoppingWithAnExpiredAuditContextCannotSilentlyBypassBilling()
    {
        var audit = await StartAsync();
        await ReportAsync([], new(0, 0, true), audit);
        await AdminAsync("commit", audit);
        var result = await ReportAsync([Scan("B"), Enter("B")], new(0, 200, true), audit);
        Assert.Equal(BasketState.NeedsStaff, result.Session.BasketState);
        Assert.Equal("STALE_AUDIT_CONTEXT", result.Session.StaffRequestReason);
        Assert.False(result.Session.CanCheckout);
        await Assert.ThrowsAsync<HubException>(() => _cart.InvokeAsync<CartSummaryResponse>("CloseSession", _session));
    }

    [Theory]
    [InlineData(false, "AUDIT_BOUNDARY_MOVEMENT")]
    [InlineData(true, "STALE_AUDIT_CONTEXT")]
    public async Task AReportSpanningAuditApprovalRequiresReviewInsteadOfSilentlyUnlockingCheckout(
        bool includeAuditId, string reason)
    {
        var audit = await StartAsync();
        await ReportAsync([], new(0, 0, true), audit);
        var startedDuringAudit = DateTime.UtcNow;
        await AdminAsync("commit", audit);
        var request = (await MovementAsync([Enter("B")], new(0, 200, true))) with
        {
            StartedAtUtc = startedDuringAudit,
            AuditId = includeAuditId ? audit : null
        };
        var result = await _cart.InvokeAsync<CartMovementResult>("ReportCartMovement", _session, request);
        Assert.Equal(BasketState.NeedsStaff, result.Session.BasketState);
        Assert.Equal(reason, result.Session.StaffRequestReason);
        Assert.Equal(200, result.Session.BasketWeightG);
        Assert.Equal(0, result.Session.TotalMinor);
        Assert.False(result.Session.CanCheckout);
        await Assert.ThrowsAsync<HubException>(() => _cart.InvokeAsync<CartSummaryResponse>("CloseSession", _session));
        var retry = await _cart.InvokeAsync<CartMovementResult>("ReportCartMovement", _session, request);
        Assert.Equal(result.Session.LastMovementSequence, retry.Session.LastMovementSequence);
        Assert.Equal(result.Session.Version, retry.Session.Version);
    }

    [Fact]
    public async Task RecountPreservesExistingUnitPricesButPricesExtraPiecesFromCatalog()
    {
        await ReportAsync([Scan("A")]);
        await _factory.InDatabaseAsync(async db =>
        {
            (await db.ProductVariants.SingleAsync(v => v.Barcode == "A")).PriceMinor = 1500;
            await db.SaveChangesAsync();
        });
        var audit = await StartAsync();
        await ReportAsync([Scan("A"), Scan("A")], audit: audit);
        Assert.Equal(2500, (await SnapshotAsync()).Audit!.TotalMinor);
        await ReportAsync([], new(0, 210, true), audit);
        var result = await AdminAsync("commit", audit);
        Assert.Equal(new[] { 1000, 1500 }, result.Session.Items.Select(i => i.UnitPriceMinor).Order().ToArray());
    }

    [Fact]
    public async Task ChangingProductToleranceDoesNotRewriteOpenInvoiceOrAuditSnapshots()
    {
        var entered = await ReportAsync([Enter("A")], new(0, 110, true));
        Assert.Equal(10m, Assert.Single(entered.Session.Items).WeightTolerancePercent);
        await _factory.InDatabaseAsync(async db =>
        {
            (await db.Products.SingleAsync()).WeightTolerancePercent = 0m;
            await db.SaveChangesAsync();
        });
        var next = await ReportAsync([Enter("B")], new(110, 310, true));
        Assert.Equal(10m, next.Session.Items.Single(item => item.Barcode == "A").WeightTolerancePercent);
        Assert.Equal(0m, next.Session.Items.Single(item => item.Barcode == "B").WeightTolerancePercent);
        var audit = await StartAsync();
        await ReportAsync([Scan("A"), Scan("B")], audit: audit);
        var draft = (await SnapshotAsync()).Audit!;
        Assert.Equal(10m, draft.Items.Single(item => item.Barcode == "A").WeightTolerancePercent);
        Assert.Equal(0m, draft.Items.Single(item => item.Barcode == "B").WeightTolerancePercent);
        await ReportAsync([], new(0, 310, true), audit);
        var committed = await AdminAsync("commit", audit);
        Assert.True(committed.Session.CanCheckout);
        Assert.Equal(10m, committed.Session.Items.Single(item => item.Barcode == "A").WeightTolerancePercent);
    }

    [Fact]
    public async Task WholeBasketUsesTheSavedPercentageForEveryPhysicalPiece()
    {
        await ReportAsync([Enter("A")], new(0, 110, true));
        var second = await ReportAsync([Enter("A")], new(110, 220, true));
        Assert.Equal(BasketState.Verified, second.Session.BasketState);
        Assert.Equal(0, second.Session.PendingWeightG);
        await _cart.InvokeAsync<CartSummaryResponse>("CloseSession", _session);
    }

    [Fact]
    public async Task WeakIdentityUsesProductPercentOrTheGlobalDefault()
    {
        var supported = await ReportAsync([Enter("A") with { Confidence = 0.5f }], new(0, 110, true));
        Assert.Equal("WEIGHT_SUPPORTED", supported.Movement.Items[0].ReasonCode);
        await _factory.InDatabaseAsync(async db =>
        {
            (await db.Products.SingleAsync()).WeightTolerancePercent = null;
            await db.SaveChangesAsync();
        });
        var fallback = await ReportAsync([Enter("B") with { Confidence = 0.5f }], new(110, 316, true));
        Assert.Equal("WEIGHT_SUPPORTED", fallback.Movement.Items[0].ReasonCode);
        Assert.Equal(3m, fallback.Session.Items.Single(item => item.Barcode == "B").WeightTolerancePercent);
        var unknown = await ReportAsync([Enter("B") with { Confidence = 0.5f }], new(316, 566, true));
        Assert.Equal(250, unknown.Session.PendingWeightG);
        Assert.Equal(0, unknown.Session.PendingWeightUncertaintyG);
        Assert.Equal(2, unknown.Session.Items.Count);
    }

    [Theory]
    [InlineData(null, 103, true)]
    [InlineData(null, 104, false)]
    [InlineData(0d, 101, false)]
    [InlineData(10d, 110, true)]
    [InlineData(10d, 111, false)]
    public async Task WeakVisionUsesOnlyProductOrDefaultPercentWithoutAnExtraGramAllowance(
        object? productPercentRaw, int actualWeight, bool accepted)
    {
        var productPercent = productPercentRaw == null ? (decimal?)null : Convert.ToDecimal(productPercentRaw);
        await _factory.InDatabaseAsync(async db =>
        {
            (await db.Products.SingleAsync()).WeightTolerancePercent = productPercent;
            await db.SaveChangesAsync();
        });
        var result = await ReportAsync([Enter("A") with { Confidence = 0.5f }], new(0, actualWeight, true));
        if (accepted)
        {
            Assert.Equal("WEIGHT_SUPPORTED", result.Movement.Items[0].ReasonCode);
            Assert.Single(result.Session.Items);
            Assert.Equal(0, result.Session.PendingWeightG);
        }
        else
        {
            Assert.Empty(result.Session.Items);
            Assert.Equal(actualWeight, result.Session.PendingWeightG);
        }
    }

    [Fact]
    public async Task UnknownResidualUncertaintyComesOnlyFromTheRecognizedProductPercent()
    {
        var result = await ReportAsync([Enter("A"), Enter(null)], new(0, 300, true));
        Assert.Equal(200, result.Session.PendingWeightG);
        Assert.Equal(10, result.Session.PendingWeightUncertaintyG);
    }

    [Fact]
    public async Task AuditCommitRejectsWeightOutsideTheProductPercentage()
    {
        var audit = await StartAsync();
        await ReportAsync([Scan("A")], audit: audit);
        await ReportAsync([], new(0, 111, true), audit);
        var rejected = await _adminClient.PostAsJsonAsync($"/api/v1/admin/sessions/{_session}/audit/commit", await CommandAsync(audit));
        Assert.Equal(HttpStatusCode.Conflict, rejected.StatusCode);
        Assert.Equal(BasketState.Auditing, (await SnapshotAsync()).BasketState);
        await ReportAsync([], new(111, 110, true), audit);
        Assert.True((await AdminAsync("commit", audit)).Session.CanCheckout);
    }

    [Fact]
    public async Task OverlappingPackageRangesProduceChoicesInsteadOfInventedUniqueIdentity()
    {
        await ReportAsync([Enter("A"), Enter("A"), Enter("B")], new(0, 400, true));
        var exit = await ReportAsync([Exit(null)], new(400, 200, true));
        Assert.Equal("SELECT_EXIT_ITEMS", exit.Movement.ReasonCode);
        Assert.Equal(2, exit.Session.ExitChoices.Count);
        Assert.Equal(4000, exit.Session.TotalMinor);
    }

    [Fact]
    public async Task DraftSurvivesReconnectAndIsReturnedWithTheActiveSession()
    {
        var audit = await StartAsync();
        await ReportAsync([Scan("A")], audit: audit);
        await _cart.DisposeAsync();
        _cart = await ConnectAsync();
        var summary = await _cart.InvokeAsync<CartSummaryResponse>("JoinCartGroup", "AUDIT-CART", "audit-token");
        Assert.Equal(audit, summary.Audit!.Id);
        Assert.Single(summary.Audit.Items);
        Assert.False(summary.CanCheckout);
    }

    [Fact]
    public async Task HttpAuditUsesAdminIdentityAndStaffRoleCannotOpenIt()
    {
        using var client = _factory.CreateClient();
        var root = $"/api/v1/admin/sessions/{_session}";
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.CreateToken("staff"));
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync($"{root}/audit/start", await CommandAsync())).StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.CreateToken());
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync($"{root}/staff-request", await CommandAsync())).StatusCode);
        var start = await client.PostAsJsonAsync($"{root}/audit/start", await CommandAsync());
        Assert.Equal(HttpStatusCode.OK, start.StatusCode);
        var audit = (await start.Content.ReadFromJsonAsync<CartMovementResult>())!.Session.Audit!.Id;
        await ReportAsync([Scan("A")], audit: audit);
        var sessions = await client.GetFromJsonAsync<List<CartSummaryResponse>>("/api/sessions");
        Assert.Single(sessions!.Single(s => s.SessionId == _session).Audit!.Items);
        await ReportAsync([], new(0, 100, true), audit);
        var commit = await client.PostAsJsonAsync($"{root}/audit/commit", await CommandAsync(audit));
        Assert.Equal(HttpStatusCode.OK, commit.StatusCode);
        Assert.True((await commit.Content.ReadFromJsonAsync<CartMovementResult>())!.Session.CanCheckout);
    }

    [Fact]
    public async Task AdminCommandsAreHttpOnlyAndTheirChangesStillReachSignalRListeners()
    {
        var updates = Channel.CreateUnbounded<CartSummaryResponse>();
        using var subscription = _admin.On<CartSummaryResponse>("SessionUpdated", session => updates.Writer.TryWrite(session));
        await _admin.InvokeAsync("JoinSessionsGroup");
        await _cart.InvokeAsync<CartMovementResult>("RequestStaff", _session, await CommandAsync());
        var requested = await updates.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(BasketState.NeedsStaff, requested.BasketState);
        var command = await CommandAsync();
        foreach (var method in new[] { "BeginCartAudit", "ConfirmAuditEmptyBasket", "RemoveAuditItem", "CommitCartAudit", "ReconcileCartBasket" })
            await Assert.ThrowsAsync<HubException>(() => _admin.InvokeAsync<CartMovementResult>(method, _session, command));
        Assert.Null((await SnapshotAsync()).Audit);
        var started = await AdminAsync("start");
        var notification = await updates.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(BasketState.Auditing, notification.BasketState);
        Assert.Equal(started.Session.Version, notification.Version);
        Assert.Equal(started.Session.Audit!.Id, notification.Audit!.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid-user-id")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task AuditCannotBeRecordedWithoutAValidActorIdentity(string subject)
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            _factory.CreateToken(subject: subject));
        var response = await client.PostAsJsonAsync($"/api/v1/admin/sessions/{_session}/audit/start", await CommandAsync());
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await _factory.InDatabaseAsync(async db => Assert.False(await db.CartBasketAudits.AnyAsync()));
    }

    private async Task<Guid> StartAsync()
    {
        await _cart.InvokeAsync<CartMovementResult>("RequestStaff", _session, await CommandAsync());
        return (await AdminAsync("start")).Session.Audit!.Id;
    }

    private Task<CartSummaryResponse> SnapshotAsync() => _cart.InvokeAsync<CartSummaryResponse>("JoinCartGroup", "AUDIT-CART", "audit-token");

    private async Task<CartAuditCommand> CommandAsync(Guid? audit = null)
    {
        var snapshot = await SnapshotAsync();
        return new(Guid.NewGuid(), Guid.NewGuid(), snapshot.LastMovementSequence + 1, _boot, snapshot.Version) { AuditId = audit };
    }

    private async Task<CartMovementResult> AdminAsync(string action, Guid? audit = null) =>
        await SendAdminAsync($"audit/{action}", await CommandAsync(audit));

    private async Task<CartMovementResult> SendAdminAsync(string route, CartAuditCommand request)
    {
        var response = await _adminClient.PostAsJsonAsync($"/api/v1/admin/sessions/{_session}/{route}", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CartMovementResult>())!;
    }

    private async Task<CartMovementRequest> MovementAsync(IReadOnlyList<CartMovementItemRequest> items,
        BasketWeightDto? weight = null, Guid? audit = null)
    {
        var snapshot = await SnapshotAsync();
        var now = DateTime.UtcNow;
        return new(Guid.NewGuid(), Guid.NewGuid(), snapshot.LastMovementSequence + 1, _boot, now, now, items, weight) { AuditId = audit };
    }

    private async Task<CartMovementResult> ReportAsync(IReadOnlyList<CartMovementItemRequest> items,
        BasketWeightDto? weight = null, Guid? audit = null) =>
        await _cart.InvokeAsync<CartMovementResult>("ReportCartMovement", _session, await MovementAsync(items, weight, audit));

    private async Task<HubConnection> ConnectAsync(bool admin = false)
    {
        var token = admin ? _factory.CreateToken() : null;
        var connection = new HubConnectionBuilder().WithUrl("http://localhost/hubs/cart", options =>
        {
            options.Transports = HttpTransportType.LongPolling;
            options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            if (token is not null) options.AccessTokenProvider = () => Task.FromResult<string?>(token);
        }).Build();
        await connection.StartAsync();
        if (!admin) await connection.InvokeAsync<CartSummaryResponse?>("JoinCartGroup", "AUDIT-CART", "audit-token");
        return connection;
    }

    private static CartMovementItemRequest Scan(string barcode) => new(CartItemAction.Scan, barcode, CartItemSource.Scanner);
    private static CartMovementItemRequest Enter(string? barcode) => new(CartItemAction.Enter, barcode, Confidence: 0.99f);
    private static CartMovementItemRequest Exit(string? barcode) => new(CartItemAction.Exit, barcode, Confidence: 0.99f);
}
