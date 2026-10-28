using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
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
public sealed class CartMovementProcessingTests : IAsyncLifetime
{
    private readonly CatalogApiFactory _factory = new();
    private readonly Guid _boot = Guid.NewGuid();
    private const string Token = "movement-device-secret";
    private Guid _sessionId;
    private int _sequence;
    private HubConnection _cart = null!;
    public async Task InitializeAsync()
    {
        _factory.CreateClient();
        await _factory.InDatabaseAsync(async db =>
        {
            await db.Database.EnsureCreatedAsync();
            db.Carts.AddRange(new Cart { Id = "CART", TokenHash = TokenHasher.HashToken(Token) },
                new Cart { Id = "OTHER", TokenHash = TokenHasher.HashToken(Token) });
            var category = new Category { Id = Guid.NewGuid(), NameAr = "تصنيف", NameEn = "Category" };
            var product = new Product { Id = Guid.NewGuid(), CategoryId = category.Id, NameAr = "منتج", NameEn = "Product", WeightTolerancePercent = 0m };
            db.Categories.Add(category); db.Products.Add(product);
            foreach (var (barcode, weight, price) in new[] { ("A", 50, 100), ("B", 100, 250), ("C", 50, 200) })
                db.ProductVariants.Add(new ProductVariant { Id = Guid.NewGuid(), ProductId = product.Id,
                    Barcode = barcode, WeightG = weight, PriceMinor = price });
            await db.SaveChangesAsync();
        });
        _cart = await ConnectAsync();
        _sessionId = (await _cart.InvokeAsync<CartSummaryResponse>("StartSession", new CartStartSessionRequest())).SessionId;
    }
    public async Task DisposeAsync() { await _cart.DisposeAsync(); await _factory.DisposeAsync(); }
    [Fact]
    public async Task ScannerChargesWithoutWeightAndCanCloseWhileHeldOutside()
    {
        var result = await SendAsync([Scan("A")]);
        Assert.Equal("Accepted", result.Movement.Status);
        Assert.Equal(100, result.Session.TotalMinor);
        Assert.Equal(0, result.Session.ExpectedBasketWeightG);
        Assert.Equal(BasketPresence.Unconfirmed, Assert.Single(result.Session.Items).BasketPresence);
        Assert.NotEqual(Guid.Empty, result.Movement.Items[0].ItemId);
        Assert.NotEqual(Guid.Empty, result.Session.Items[0].Id);
        Assert.Equal("closed", (await _cart.InvokeAsync<CartSummaryResponse>("CloseSession", _sessionId)).Status);
    }
    [Fact]
    public async Task LaterPlacementKeepsServerInvoiceIdAndDoesNotChargeTwice()
    {
        var scan = await SendAsync([Scan("A")]);
        var placed = await SendAsync([Enter("A")], new(0, 50, true));
        Assert.Equal("Accepted", placed.Movement.Status);
        Assert.Equal("PlacementConfirmed", placed.Movement.Items[0].Effect);
        Assert.Equal(100, placed.Session.TotalMinor);
        Assert.Equal(scan.Session.Items[0].Id, placed.Session.Items[0].Id);
        Assert.Equal(BasketPresence.Inside, Assert.Single(placed.Session.Items).BasketPresence);
        Assert.Equal(50, placed.Session.ExpectedBasketWeightG);
    }
    [Fact]
    public async Task SamePacketPreservesObservationOrderAndGeneratesDistinctServerIds()
    {
        var result = await SendAsync([Scan("A"), Enter("A")], new(0, 50, true));
        Assert.Equal("Accepted", result.Movement.Status);
        Assert.Equal(100, result.Session.TotalMinor);
        Assert.Single(result.Session.Items);
        Assert.Equal(new[] { 0, 1 }, result.Movement.Items.Select(item => item.Position));
        Assert.Equal(result.Movement.Items[0].SessionItemId, result.Movement.Items[1].SessionItemId);
        Assert.Equal(2, result.Movement.Items.Select(item => item.ItemId).Distinct().Count());
    }
    [Fact]
    public async Task EveryScanAddsANewInvoiceUnitEvenWhenSameBarcodeIsAlreadyInside()
    {
        var entered = await SendAsync([Enter("A")], new(0, 50, true));
        var result = await SendAsync([Scan("A")]);
        Assert.Equal("Added", result.Movement.Items[0].Effect);
        Assert.Equal(200, result.Session.TotalMinor);
        Assert.NotEqual(entered.Session.Items[0].Id, result.Movement.Items[0].SessionItemId);
        Assert.Single(result.Session.Items, item => item.BasketPresence == BasketPresence.Unconfirmed);
        Assert.Single(result.Session.Items, item => item.BasketPresence == BasketPresence.Inside);
    }
    [Fact]
    public async Task MixedScannerVisionAndExitUseOnlyPhysicalPiecesForWeight()
    {
        var initial = await SendAsync([Enter("A")], new(0, 50, true));
        var result = await SendAsync([Exit("A") with { SessionItemId = initial.Session.Items[0].Id }, Enter("B"), Scan("C")], new(50, 100, true));
        Assert.Equal("Accepted", result.Movement.Status);
        Assert.Equal(450, result.Session.TotalMinor);
        Assert.Equal(2, result.Session.ItemsCount);
        Assert.Equal(100, result.Session.ExpectedBasketWeightG);
    }
    [Fact]
    public async Task RepeatedSkuMeansDistinctServerUnitsAndOneSelectedRemoval()
    {
        var added = await SendAsync([Enter("A"), Enter("A"), Enter("A")], new(0, 150, true));
        Assert.Equal(3, added.Session.Items.Select(item => item.Id).Distinct().Count());
        var target = added.Session.Items[1].Id;
        Assert.Equal(300, (await _cart.InvokeAsync<CartSummaryResponse>("RemoveSessionItem", _sessionId, target)).TotalMinor);
        var request = Request([Exit("A") with { SessionItemId = target }], new(150, 100, true));
        Assert.Equal(200, (await SendAsync(request)).Session.TotalMinor);
        Assert.Equal(200, (await SendAsync(request)).Session.TotalMinor);
        Assert.Equal(200, (await _cart.InvokeAsync<CartSummaryResponse>("RemoveSessionItem", _sessionId, target)).TotalMinor);
    }
    [Fact]
    public async Task AutomaticPlacementConsumesHeldScanThenNextPieceIsCharged()
    {
        var scan = await SendAsync([Scan("A")]);
        var request = Request([Enter("A")], new(0, 50, true));
        var placed = await SendAsync(request);
        Assert.Equal("Accepted", placed.Movement.Status);
        Assert.Equal("PlacementConfirmed", placed.Movement.Items[0].Effect);
        Assert.Equal(scan.Session.Items[0].Id, Assert.Single(placed.Session.Items).Id);
        Assert.Equal(BasketPresence.Inside, placed.Session.Items[0].BasketPresence);
        Assert.Equal(100, placed.Session.TotalMinor);
        Assert.Equal(100, (await SendAsync(request)).Session.TotalMinor);

        var next = await SendAsync([Enter("A")], new(50, 100, true));
        Assert.Equal("Added", next.Movement.Items[0].Effect);
        Assert.Equal(200, next.Session.TotalMinor);
        Assert.Equal(2, next.Session.ItemsCount);
        Assert.All(next.Session.Items, item => Assert.Equal(BasketPresence.Inside, item.BasketPresence));
    }
    [Fact]
    public async Task SamePacketScanAndEntryMatchAutomaticallyWithoutItemReferences()
    {
        // ترتيب observations لا يشترط أن تصل ملاحظة السكان قبل الدخول في القائمة.
        var result = await SendAsync([Enter("A"), Scan("A")], new(0, 50, true));
        Assert.Equal("Accepted", result.Movement.Status);
        Assert.Equal(100, result.Session.TotalMinor);
        Assert.Equal(BasketPresence.Inside, Assert.Single(result.Session.Items).BasketPresence);
        Assert.Equal("PlacementConfirmed", result.Movement.Items[0].Effect);
        Assert.Equal(result.Movement.Items[0].SessionItemId, result.Movement.Items[1].SessionItemId);
    }
    [Fact]
    public async Task MultipleEntriesConsumeDifferentWaitingScansThenCreateOnlyTheExtraPiece()
    {
        var scanned = await SendAsync([Scan("A"), Scan("A")]);
        var result = await SendAsync([Enter("A"), Enter("A"), Enter("A")], new(0, 150, true));
        Assert.Equal("Accepted", result.Movement.Status);
        Assert.Equal(new[] { "PlacementConfirmed", "PlacementConfirmed", "Added" }, result.Movement.Items.Select(item => item.Effect));
        Assert.Equal(3, result.Movement.Items.Select(item => item.SessionItemId).Distinct().Count());
        Assert.All(scanned.Session.Items, scan => Assert.Contains(result.Session.Items, item => item.Id == scan.Id && item.BasketPresence == BasketPresence.Inside));
        Assert.Equal(300, result.Session.TotalMinor);
        Assert.Equal(150, result.Session.ExpectedBasketWeightG);
    }
    [Fact]
    public async Task AutomaticPlacementUsesOldestPurchaseSnapshotAfterCatalogChanges()
    {
        var oldest = await SendAsync([Scan("A")]);
        await _factory.InDatabaseAsync(async db =>
        {
            var variant = await db.ProductVariants.SingleAsync(item => item.Barcode == "A");
            variant.PriceMinor = 200;
            variant.WeightG = 60;
            await db.SaveChangesAsync();
        });
        var newer = await SendAsync([Scan("A")]);
        await _factory.InDatabaseAsync(async db =>
        {
            (await db.ProductVariants.SingleAsync(item => item.Barcode == "A")).IsActive = false;
            await db.SaveChangesAsync();
        });

        var first = await SendAsync([Enter("A")], new(0, 50, true));
        Assert.Equal("Accepted", first.Movement.Status);
        Assert.Equal(oldest.Session.Items[0].Id, first.Movement.Items[0].SessionItemId);
        Assert.Equal(50, first.Movement.Items[0].ExpectedWeightG);
        Assert.Equal(100, first.Movement.Items[0].UnitPriceMinor);
        Assert.Single(first.Session.Items, item => item.BasketPresence == BasketPresence.Unconfirmed);

        var second = await SendAsync([Enter("A")], new(50, 110, true));
        Assert.Equal("Accepted", second.Movement.Status);
        Assert.Equal(newer.Movement.Items[0].SessionItemId, second.Movement.Items[0].SessionItemId);
        Assert.Equal(300, second.Session.TotalMinor);
        Assert.Equal(110, second.Session.ExpectedBasketWeightG);
    }
    [Fact]
    public async Task AutomaticPlacementMatchesBarcodeInsteadOfAnotherProductWithTheSameWeight()
    {
        var scan = await SendAsync([Scan("A")]);
        var other = await SendAsync([Enter("C")], new(0, 50, true));
        Assert.Equal("Added", other.Movement.Items[0].Effect);
        Assert.Equal(BasketPresence.Unconfirmed, other.Session.Items.Single(item => item.Id == scan.Session.Items[0].Id).BasketPresence);
        Assert.Equal(300, other.Session.TotalMinor);
        var result = await SendAsync([Enter("A")], new(50, 100, true));
        Assert.Equal("PlacementConfirmed", result.Movement.Items[0].Effect);
        Assert.Equal(300, result.Session.TotalMinor);
    }
    [Fact]
    public async Task RecognizedPlacementAndScanAreAcceptedWhileOnlyUnexplainedMassIsPending()
    {
        var scan = await SendAsync([Scan("A")]);
        var result = await SendAsync([Enter("A"), Scan("B")], new(0, 90, true));
        Assert.Equal("UNIDENTIFIED_WEIGHT", result.Movement.ReasonCode);
        Assert.Equal(40, result.Session.PendingWeightG);
        Assert.Equal(350, result.Session.TotalMinor);
        Assert.Equal(scan.Session.Items[0].Id, result.Movement.Items[0].SessionItemId);
        Assert.Equal(BasketPresence.Inside, result.Session.Items.Single(item => item.Barcode == "A").BasketPresence);
        Assert.All(result.Movement.Items, item => Assert.Equal("Accepted", item.Outcome));
        Assert.False(result.Session.CanCheckout);
    }

    [Fact]
    public async Task SamePacketScansArePairedInTheirOriginalInputOrder()
    {
        var result = await SendAsync([Scan("A"), Scan("A"), Enter("A"), Enter("A")], new(0, 100, true));
        Assert.Equal("Accepted", result.Movement.Status);
        Assert.Equal(result.Movement.Items[0].SessionItemId, result.Movement.Items[2].SessionItemId);
        Assert.Equal(result.Movement.Items[1].SessionItemId, result.Movement.Items[3].SessionItemId);
        Assert.Equal(200, result.Session.TotalMinor);
    }
    [Fact]
    public async Task CancellationCannotReferenceAnInvoiceUnitFromAnotherCartSession()
    {
        var scan = await SendAsync([Scan("A")]);
        await using var other = await ConnectAsync("OTHER");
        var session = await other.InvokeAsync<CartSummaryResponse>("StartSession", new CartStartSessionRequest());
        var request = Request([Scan("A") with { Action = CartItemAction.CancelScan, SessionItemId = scan.Session.Items[0].Id }]) with { Sequence = 1 };
        var result = await other.InvokeAsync<CartMovementResult>("ReportCartMovement", session.SessionId, request);
        Assert.Equal("ITEM_NOT_IN_SESSION", result.Movement.ReasonCode);
        Assert.Empty(result.Session.Items);
        Assert.Equal(100, (await SnapshotAsync()).TotalMinor);
    }
    [Fact]
    public async Task CancelledScanIsNotConsumedByAutomaticEntryInSamePacket()
    {
        var scanned = await SendAsync([Scan("A")]);
        var id = scanned.Session.Items[0].Id;
        var result = await SendAsync([Enter("A"), Scan("A") with { Action = CartItemAction.CancelScan, SessionItemId = id }], new(0, 50, true));
        Assert.Equal("Accepted", result.Movement.Status);
        Assert.Equal("Added", result.Movement.Items[0].Effect);
        Assert.NotEqual(id, Assert.Single(result.Session.Items).Id);
        Assert.Equal(100, result.Session.TotalMinor);
    }
    [Fact]
    public async Task SamePieceRoundTripHasNoInvoiceEffect()
    {
        var result = await SendAsync([Enter("A") with { TrackId = "same" }, Exit("A") with { TrackId = "same" }], new(0, 0, true));
        Assert.Equal("Accepted", result.Movement.Status);
        Assert.Empty(result.Session.Items);
        Assert.All(result.Movement.Items, item => Assert.Equal("NoChange", item.Effect));
    }
    [Fact]
    public async Task ZeroWeightSwapUpdatesDifferentProducts()
    {
        await SendAsync([Enter("A")], new(0, 50, true));
        var result = await SendAsync([Exit("A"), Enter("C")], new(50, 50, true));
        Assert.Equal("Accepted", result.Movement.Status);
        Assert.Equal("C", Assert.Single(result.Session.Items).Barcode);
        Assert.Equal(200, result.Session.TotalMinor);
    }
    [Theory]
    [InlineData(null, 0.99f, "ITEM_NOT_IDENTIFIED")]
    [InlineData("UNKNOWN", 0.99f, "UNKNOWN_BARCODE")]
    [InlineData("A", 0.4f, "LOW_CONFIDENCE")]
    public async Task StaffRecountClearsAnUnidentifiedEntryWithoutReplayingItsEvidence(string? barcode, float confidence, string reason)
    {
        var request = Request([Enter(barcode) with { Confidence = confidence }], new(0, reason == "LOW_CONFIDENCE" ? 90 : 50, true));
        var pending = await SendAsync(request);
        Assert.Equal(reason, pending.Movement.ReasonCode);
        Assert.Empty(pending.Session.Items);
        Assert.Single(pending.Session.PendingMovements[0].Items);
        Assert.Empty((await RecoverAsync()).Session.PendingMovements);
        await _factory.InDatabaseAsync(async db =>
        {
            var original = await db.CartMovementItems.SingleAsync();
            Assert.Equal(barcode, original.DetectedBarcode);
            Assert.Equal(confidence, original.Confidence);
            Assert.Equal(CartItemOutcome.Rejected, original.Outcome);
            Assert.Empty(await db.SessionItems.ToListAsync());
        });
        Assert.Equal("Cancelled", (await SendAsync(request)).Movement.Status);
    }
    [Fact]
    public async Task WrongWeightNeedsSupervisorAndExistingMovementCannotBeResaved()
    {
        var request = Request([Enter("A")], new(90, 140, true));
        Assert.Equal("WEIGHT_CONTINUITY_MISMATCH", (await SendAsync(request)).Movement.ReasonCode);
        var changed = request with { EventId = Guid.NewGuid(), Sequence = ++_sequence, Weight = new(0, 50, true) };
        Assert.Contains("MOVEMENT_ALREADY_RECORDED", (await ErrorAsync(changed)).Message);
        _sequence--;
        await RecoverAsync();
        Assert.Equal(200, (await SendAsync([Enter("A")], new(50, 100, true))).Session.TotalMinor);
    }
    [Fact]
    public async Task OldUnknownReportsAreNotReplayedWhileNewKnownShoppingContinues()
    {
        var original = Request([Enter(null)], new(0, 50, true));
        await SendAsync(original);
        var later = await SendAsync([Enter("B")], new(50, 150, true));
        Assert.Equal("Accepted", later.Movement.Status);
        Assert.Equal("B", Assert.Single(later.Session.Items).Barcode);
        Assert.Equal(50, later.Session.PendingWeightG);
        Assert.Single(later.Session.PendingMovements);
        var scanned = await SendAsync([Scan("C")]);
        Assert.Equal(450, scanned.Session.TotalMinor);
        Assert.Single(scanned.Session.PendingMovements);
        var recovered = await RecoverAsync();
        Assert.Equal(450, recovered.Session.TotalMinor);
        Assert.Empty(recovered.Session.PendingMovements);
        Assert.Equal(700, (await SendAsync([Enter("B")], new(150, 250, true))).Session.TotalMinor);
        Assert.Equal("Cancelled", (await SendAsync(original)).Movement.Status);
    }

    [Fact]
    public async Task UnknownPhysicalMassDoesNotCancelTheIndependentScannerPurchase()
    {
        var result = await SendAsync([Scan("B"), Enter(null)], new(0, 50, true));
        Assert.Equal("NeedsConfirmation", result.Movement.Status);
        Assert.Equal(50, result.Session.PendingWeightG);
        Assert.Equal(250, result.Session.TotalMinor);
        Assert.Equal(BasketPresence.Unconfirmed, Assert.Single(result.Session.Items).BasketPresence);
        Assert.Equal("Accepted", result.Movement.Items[0].Outcome);
        Assert.Equal("Unknown", result.Movement.Items[1].Outcome);
    }

    [Fact]
    public async Task ContradictoryBasketMassDoesNotDiscardTheRecognizedItemDecisions()
    {
        await SendAsync([Enter("A")], new(0, 50, true));
        var result = await SendAsync([Scan("C"), Exit("A"), Enter("B")], new(50, 70, true));
        // بعد الأجزاء المعروفة يبقى خروج 30g لا يطابق محتويات الفاتورة؛ المراجعة لا تلغي القرارات المستقلة.
        Assert.Equal("NO_EXIT_MATCH", result.Movement.ReasonCode);
        Assert.Equal(BasketState.NeedsStaff, result.Session.BasketState);
        Assert.Equal(450, result.Session.TotalMinor);
        Assert.DoesNotContain(result.Session.Items, item => item.Barcode == "A");
        Assert.All(result.Movement.Items, item => Assert.Equal("Accepted", item.Outcome));
        Assert.Equal(BasketState.NeedsStaff, result.Session.BasketState);
        await Assert.ThrowsAsync<HubException>(() => _cart.InvokeAsync<CartSummaryResponse>("CloseSession", _sessionId));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PartialOrUnstableReportFailsBeforeAnyFinancialChange(bool complete)
    {
        var request = Request([Scan("B"), Enter("A")], new(0, 50, !complete)) with { IsComplete = complete };
        Assert.Contains("VALIDATION_FAILED", (await ErrorAsync(request)).Message);
        await _factory.InDatabaseAsync(async db => Assert.Empty(await db.CartMovements.ToListAsync()));
        Assert.Equal(350, (await SendAsync(request with { IsComplete = true, Weight = new(0, 50, true) })).Session.TotalMinor);
    }
    [Fact]
    public async Task ExitUsesPurchaseSnapshotsAfterPriceWeightAndAvailabilityChange()
    {
        await SendAsync([Enter("A")], new(0, 50, true));
        await _factory.InDatabaseAsync(async db =>
        {
            var variant = await db.ProductVariants.SingleAsync(item => item.Barcode == "A");
            variant.WeightG = 900; variant.PriceMinor = 999; variant.IsActive = false;
            (await db.Products.SingleAsync()).IsActive = false; await db.SaveChangesAsync();
        });
        var result = await SendAsync([Exit("A")], new(50, 0, true));
        Assert.Equal("Accepted", result.Movement.Status);
        Assert.Equal(0, result.Session.TotalMinor);
        Assert.Equal(50, result.Movement.Items[0].ExpectedWeightG);
        Assert.Equal(100, result.Movement.Items[0].UnitPriceMinor);
    }
    [Fact]
    public async Task BarcodeSnapshotAllowsPlacementAndExitAfterBarcodeChanges()
    {
        var scan = await SendAsync([Scan("A")]);
        await _factory.InDatabaseAsync(async db => { (await db.ProductVariants.SingleAsync(item => item.Barcode == "A")).Barcode = "NEW-A"; await db.SaveChangesAsync(); });
        var placed = await SendAsync([Enter("A")], new(0, 50, true));
        Assert.Equal("Accepted", placed.Movement.Status);
        Assert.Equal(scan.Session.Items[0].Id, placed.Movement.Items[0].SessionItemId);
        Assert.Equal("A", Assert.Single(placed.Session.Items).Barcode);
        Assert.Equal(0, (await SendAsync([Exit("A")], new(50, 0, true))).Session.TotalMinor);
    }
    [Fact]
    public async Task KnownExitUsesTheOldestPurchaseSnapshotWhenPricesDiffer()
    {
        var first = await SendAsync([Enter("A")], new(0, 50, true));
        await _factory.InDatabaseAsync(async db =>
        {
            (await db.ProductVariants.SingleAsync(item => item.Barcode == "A")).PriceMinor = 200;
            await db.SaveChangesAsync();
        });
        await SendAsync([Enter("A")], new(50, 100, true));
        var result = await SendAsync([Exit("A")], new(100, 50, true));
        Assert.Equal("Accepted", result.Movement.Status);
        Assert.Equal(first.Session.Items[0].Id, result.Movement.Items[0].SessionItemId);
        Assert.Equal(200, result.Session.TotalMinor);
    }

    [Fact]
    public async Task HeldScanCanBeCancelledButPlacedUnitRequiresExit()
    {
        var scan = await SendAsync([Scan("A")]);
        Assert.Equal(0, (await SendAsync([Scan("A") with { Action = CartItemAction.CancelScan, SessionItemId = scan.Session.Items[0].Id }])).Session.TotalMinor);
        var entered = await SendAsync([Enter("B")], new(0, 100, true));
        var result = await SendAsync([Scan("B") with { Action = CartItemAction.CancelScan, SessionItemId = entered.Session.Items[0].Id }]);
        Assert.Equal("PHYSICAL_EXIT_REQUIRED", result.Movement.ReasonCode);
        Assert.Equal(250, result.Session.TotalMinor);
        Assert.Equal(250, (await SendAsync([Exit("B")], new(100, 0, true))).Session.TotalMinor);
        await RecoverAsync();
        Assert.Equal(0, (await SendAsync([Exit("B")], new(100, 0, true))).Session.TotalMinor);
    }
    [Fact]
    public async Task ExitAndCancellationInSamePacketCannotSubtractTwice()
    {
        var entered = await SendAsync([Enter("A")], new(0, 50, true));
        var id = entered.Session.Items[0].Id;
        var result = await SendAsync([Exit("A") with { SessionItemId = id }, Scan("A") with { Action = CartItemAction.CancelScan, SessionItemId = id }], new(50, 0, true));
        Assert.Equal("Accepted", result.Movement.Status);
        Assert.Equal(0, result.Session.TotalMinor);
        Assert.Contains(result.Movement.Items, item => item.Effect == "NoChange");
    }
    [Fact]
    public async Task RemovalIntentCanBeCancelledAndNeverChangesChargeAlone()
    {
        var entered = await SendAsync([Enter("A")], new(0, 50, true));
        var id = entered.Session.Items[0].Id;
        var requested = await _cart.InvokeAsync<CartSummaryResponse>("RemoveSessionItem", _sessionId, id);
        Assert.Equal(100, requested.TotalMinor);
        Assert.NotNull(requested.Items[0].RemovalRequestedAt);
        await Assert.ThrowsAsync<HubException>(() => _cart.InvokeAsync<CartSummaryResponse>("CloseSession", _sessionId));
        Assert.Null((await _cart.InvokeAsync<CartSummaryResponse>("CancelItemRemoval", _sessionId, id)).Items[0].RemovalRequestedAt);
        Assert.Equal("closed", (await _cart.InvokeAsync<CartSummaryResponse>("CloseSession", _sessionId)).Status);
    }
    [Fact]
    public async Task MultipleExitsInOneMovementRemoveDifferentUnitsOfTheSameBarcode()
    {
        await SendAsync([Enter("A"), Enter("A"), Enter("A")], new(0, 150, true));
        var result = await SendAsync([Exit("A"), Exit("A")], new(150, 50, true));
        Assert.Equal("Accepted", result.Movement.Status);
        Assert.Equal(2, result.Movement.Items.Select(item => item.SessionItemId).Distinct().Count());
        Assert.All(result.Movement.Items, item => Assert.Equal("Removed", item.Effect));
        Assert.Equal(100, result.Session.TotalMinor);
        Assert.Single(result.Session.Items);
    }
    [Fact]
    public async Task ExactReceiptReplayNeverSavesAgainAndWorksAfterClosure()
    {
        var request = Request([Enter("A")], new(0, 50, true));
        await SendAsync(request);
        await SendAsync([Enter("B")], new(50, 150, true));
        var updates = Listen(_cart);
        Assert.Equal(350, (await SendAsync(request)).Session.TotalMinor);
        Assert.False(updates.Reader.TryRead(out _));
        Assert.Contains("MOVEMENT_ALREADY_RECORDED", (await ErrorAsync(request with { EventId = Guid.NewGuid(), Sequence = ++_sequence })).Message);
        _sequence--;
        Assert.Equal(2, (await SnapshotAsync()).LastMovementSequence);
        await _cart.InvokeAsync<CartSummaryResponse>("CloseSession", _sessionId);
        Assert.Equal("closed", (await SendAsync(request)).Session.Status);
        await _factory.InDatabaseAsync(async db => Assert.Equal(2, await db.CartEventReceipts.CountAsync()));
    }
    [Fact]
    public async Task ChangedEventAndCrossCartSessionAreRejected()
    {
        var request = Request([Scan("A")]); await SendAsync(request);
        Assert.Contains("EVENT_ID_REUSED", (await ErrorAsync(request with { Items = [Scan("B")] })).Message);
        await using var other = await ConnectAsync("OTHER");
        Assert.Contains("SESSION_NOT_ACCESSIBLE", (await Assert.ThrowsAsync<HubException>(() => other.InvokeAsync<CartMovementResult>("ReportCartMovement", _sessionId, request))).Message);
    }
    [Fact]
    public async Task SequenceSurvivesReconnectAndBootChangeAndGapsConsumeNothing()
    {
        await SendAsync([Scan("A")]);
        var request = Request([Scan("B")]);
        Assert.Contains("SEQUENCE_GAP", (await ErrorAsync(request with { Sequence = 10 })).Message);
        await using var reconnected = await ConnectAsync();
        Assert.Equal(1, (await reconnected.InvokeAsync<CartSummaryResponse>("JoinCartGroup", "CART", Token)).LastMovementSequence);
        Assert.Equal(350, (await reconnected.InvokeAsync<CartMovementResult>("ReportCartMovement", _sessionId, request with { DeviceBootId = Guid.NewGuid() })).Session.TotalMinor);
    }
    [Fact]
    public async Task FailedSaveRollsBackEveryServerGeneratedItemAndReceipt()
    {
        var request = Request([Scan("A"), Enter("B")], new(0, 100, true));
        var updates = Listen(_cart);
        _factory.SimulateSessionWriteRace = true; await ErrorAsync(request);
        await _factory.InDatabaseAsync(async db =>
        {
            Assert.Empty(await db.CartMovements.ToListAsync()); Assert.Empty(await db.CartMovementItems.ToListAsync());
            Assert.Empty(await db.CartEventReceipts.ToListAsync()); Assert.Empty(await db.SessionItems.ToListAsync());
        });
        Assert.False(updates.Reader.TryRead(out _));
        Assert.Equal(350, (await SendAsync(request)).Session.TotalMinor);
    }
    [Fact]
    public async Task ConcurrentDuplicateCommitsOneSetOfServerIdsAndBroadcastsOnce()
    {
        var request = Request([Scan("A"), Enter("B")], new(0, 100, true));
        _factory.SimulateMovementRace = true;
        var updates = Listen(_cart);
        var result = await SendAsync(request); await ReadAsync(updates);
        Assert.False(updates.Reader.TryRead(out _));
        await _factory.InDatabaseAsync(async db =>
        {
            Assert.Single(await db.CartEventReceipts.ToListAsync()); Assert.Equal(2, await db.SessionItems.CountAsync());
            Assert.Equal(result.Movement.Items.Select(item => item.ItemId).Order(), (await db.CartMovementItems.Select(item => item.Id).ToListAsync()).Order());
        });
    }
    [Fact]
    public async Task RecoveryUsesAdminAuditAndLegacyReconciliationIsNotExposed()
    {
        var pending = await SendAsync([Enter(null)], new(0, 50, true));
        var request = new CartAuditCommand(Guid.NewGuid(), Guid.NewGuid(), _sequence + 1, _boot, pending.Session.Version);
        await Assert.ThrowsAsync<HubException>(() => _cart.InvokeAsync<CartMovementResult>("ReconcileCartBasket", _sessionId, request));
        using var admin = _factory.CreateClient();
        var route = $"/api/v1/admin/sessions/{_sessionId}/audit/start";
        Assert.Equal(HttpStatusCode.Unauthorized, (await admin.PostAsJsonAsync(route, request)).StatusCode);
        admin.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.CreateToken("staff"));
        Assert.Equal(HttpStatusCode.Forbidden, (await admin.PostAsJsonAsync(route, request)).StatusCode);
        admin.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.CreateToken());
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync(route, request)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PostAsJsonAsync(
            $"/api/v1/admin/sessions/{_sessionId}/basket/reconcile", request)).StatusCode);
        Assert.Empty((await RecoverAsync()).Session.PendingMovements);
        Assert.Equal("closed", (await _cart.InvokeAsync<CartSummaryResponse>("CloseSession", _sessionId)).Status);
    }
    [Fact]
    public async Task IdentifiedTinyEntryRequiresAPhysicalWeightChange()
    {
        await _factory.InDatabaseAsync(async db => { var variant = await db.ProductVariants.SingleAsync(item => item.Barcode == "A"); variant.WeightG = 5; await db.SaveChangesAsync(); });
        Assert.Equal("MOVEMENT_WEIGHT_MISMATCH", (await SendAsync([Enter("A")], new(0, 0, true))).Movement.ReasonCode);
    }
    [Fact]
    public async Task ScannedTinyUnitStillRequiresAPhysicalPlacement()
    {
        await _factory.InDatabaseAsync(async db => { var variant = await db.ProductVariants.SingleAsync(item => item.Barcode == "A"); variant.WeightG = 5; await db.SaveChangesAsync(); });
        await SendAsync([Scan("A")]);
        Assert.Equal("MOVEMENT_WEIGHT_MISMATCH", (await SendAsync([Enter("A")], new(0, 0, true))).Movement.ReasonCode);
    }
    [Fact]
    public async Task FullSessionAndUnresolvedEvidenceArePublishedToCartAndAdmin()
    {
        await using var admin = await ConnectAsync(admin: true); await admin.InvokeAsync("JoinSessionsGroup");
        var cartUpdates = Listen(_cart); var adminUpdates = Listen(admin);
        var result = await SendAsync([Enter(null)], new(0, 50, true));
        Assert.Equal(result.Session.Version, (await ReadAsync(cartUpdates)).Version);
        Assert.Equal(result.Session.Version, (await ReadAsync(adminUpdates)).Version);
        Assert.Single(result.Session.PendingMovements[0].Items);
    }
    [Fact]
    public async Task InvalidWeightsRepeatedFramesAndReferencesOnAdditionsFailBeforeWriting()
    {
        var tracked = Enter("A") with { CameraId = "basket", TrackId = "one" };
        var invalid = new[]
        {
            Request([Enter("A")], new(-1, 50, true)), Request([tracked, tracked], new(0, 100, true)),
            Request([Scan("A") with { SessionItemId = Guid.NewGuid() }]),
            Request([Enter("A") with { SessionItemId = Guid.NewGuid() }], new(0, 50, true)),
            Request([Exit("A") with { SessionItemId = Guid.Empty }], new(50, 0, true)),
            Request([Scan("A") with { Source = CartItemSource.Vision }])
        };
        foreach (var request in invalid) Assert.Contains("VALIDATION_FAILED", (await ErrorAsync(request with { Sequence = 1 })).Message);
        await _factory.InDatabaseAsync(async db => Assert.Empty(await db.CartEventReceipts.ToListAsync()));
    }
    [Fact]
    public async Task MissingStoredInvoiceWeightIsReportedAtPhysicalCalculation()
    {
        await SendAsync([Enter("A")], new(0, 50, true));
        await _factory.InDatabaseAsync(async db => { var unit = await db.SessionItems.SingleAsync(); unit.ExpectedWeightG = null; await db.SaveChangesAsync(); });
        var result = await SendAsync([Enter("B")], new(50, 150, true));
        Assert.Equal("WEIGHT_SNAPSHOT_MISSING", result.Movement.ReasonCode);
        Assert.Equal(350, result.Session.TotalMinor); Assert.Equal(2, result.Session.Items.Count);
        Assert.False(result.Session.CanCheckout);
    }
    [Fact]
    public async Task TotalOverflowRollsBackAllChargesAndReceipt()
    {
        await _factory.InDatabaseAsync(async db => { (await db.ProductVariants.SingleAsync(item => item.Barcode == "A")).PriceMinor = int.MaxValue; await db.SaveChangesAsync(); });
        Assert.Contains("TOTAL_LIMIT_EXCEEDED", (await ErrorAsync(Request([Scan("A"), Scan("B")]))).Message);
        await _factory.InDatabaseAsync(async db => { Assert.Empty(await db.CartEventReceipts.ToListAsync()); Assert.Empty(await db.SessionItems.ToListAsync()); });
    }
    [Fact]
    public async Task HttpBoundaryAllowsStaffReadsButNotMutations()
    {
        using var client = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"/api/sessions/{_sessionId}")).StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.CreateToken("staff"));
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/sessions/{_sessionId}")).StatusCode);
        var request = Request([Scan("A")]);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync($"/api/cart/{_sessionId}/movements", request)).StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.CreateToken("admin"));
        var response = await client.PostAsJsonAsync($"/api/cart/{_sessionId}/movements", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(100, (await response.Content.ReadFromJsonAsync<CartMovementResult>())!.Session.TotalMinor);
    }
    [Fact]
    public async Task StoredHashIsNotCredentialAndRotationRevokesCommands()
    {
        await Assert.ThrowsAsync<HubException>(() => _cart.InvokeAsync<CartSummaryResponse?>("JoinCartGroup", "CART", TokenHasher.HashToken(Token)));
        await _factory.InDatabaseAsync(async db => { (await db.Carts.SingleAsync(item => item.Id == "CART")).TokenHash = TokenHasher.HashToken("new"); await db.SaveChangesAsync(); });
        Assert.Contains("INVALID_CART_TOKEN", (await ErrorAsync(Request([Scan("A")]))).Message);
    }
    [Fact]
    public async Task RemovedDetectionRoutesCannotBillItems()
    {
        using var client = _factory.CreateClient(); client.DefaultRequestHeaders.Add("X-Cart-Token", Token);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync($"/api/cart/{_sessionId}/items", new { barcode = "A", measuredWeightG = 50 })).StatusCode);
        await Assert.ThrowsAsync<HubException>(() => _cart.InvokeAsync("ProcessItemDetection", _sessionId, new { eventId = Guid.NewGuid(), barcode = "A", measuredWeightG = 50 }));
        await _factory.InDatabaseAsync(async db => Assert.Empty(await db.SessionItems.ToListAsync()));
    }
    private CartMovementRequest Request(IReadOnlyList<CartMovementItemRequest> items, BasketWeightDto? weight = null)
    { var now = DateTime.UtcNow; return new(Guid.NewGuid(), Guid.NewGuid(), ++_sequence, _boot, now, now, items, weight); }
    private Task<CartMovementResult> SendAsync(IReadOnlyList<CartMovementItemRequest> items, BasketWeightDto? weight = null) => SendAsync(Request(items, weight));
    private Task<CartMovementResult> SendAsync(CartMovementRequest request) => _cart.InvokeAsync<CartMovementResult>("ReportCartMovement", _sessionId, request);
    private Task<HubException> ErrorAsync(CartMovementRequest request) => Assert.ThrowsAsync<HubException>(() => SendAsync(request));
    private Task<CartSummaryResponse> SnapshotAsync() => _cart.InvokeAsync<CartSummaryResponse>("JoinCartGroup", "CART", Token);
    private static CartMovementItemRequest Scan(string barcode) => new(CartItemAction.Scan, barcode, CartItemSource.Scanner);
    private static CartMovementItemRequest Enter(string? barcode) => new(CartItemAction.Enter, barcode, Confidence: 0.95f);
    private static CartMovementItemRequest Exit(string barcode) => Enter(barcode) with { Action = CartItemAction.Exit };
    private async Task<CartMovementResult> RecoverAsync()
    {
        var purchases = (await SnapshotAsync()).Items;
        using var admin = _factory.CreateClient();
        admin.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.CreateToken());
        async Task<CartAuditCommand> CommandAsync(Guid? auditId = null)
        {
            var snapshot = await SnapshotAsync();
            _sequence = snapshot.LastMovementSequence + 1;
            return new(Guid.NewGuid(), Guid.NewGuid(), _sequence, _boot, snapshot.Version) { AuditId = auditId };
        }
        async Task<CartMovementResult> AdminAsync(string action, Guid? auditId = null)
        {
            var response = await admin.PostAsJsonAsync($"/api/v1/admin/sessions/{_sessionId}/audit/{action}", await CommandAsync(auditId));
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<CartMovementResult>())!;
        }

        await _cart.InvokeAsync<CartMovementResult>("RequestStaff", _sessionId, await CommandAsync());
        var audit = (await AdminAsync("start")).Session.Audit!.Id;
        if (purchases.Count > 0)
            await SendAsync(Request(purchases.Select(item => Scan(item.Barcode)).ToList()) with { AuditId = audit });
        var weight = purchases.Sum(item => item.ExpectedWeightG!.Value);
        await SendAsync(Request([], new(0, weight, true)) with { AuditId = audit });
        return await AdminAsync("commit", audit);
    }
    private async Task<HubConnection> ConnectAsync(string cartId = "CART", bool admin = false)
    {
        var connection = new HubConnectionBuilder().WithUrl("http://localhost/hubs/cart", options =>
        {
            options.Transports = HttpTransportType.LongPolling; options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            if (admin) options.AccessTokenProvider = () => Task.FromResult<string?>(_factory.CreateToken());
        }).Build();
        await connection.StartAsync();
        if (!admin) await connection.InvokeAsync<CartSummaryResponse?>("JoinCartGroup", cartId, Token);
        return connection;
    }
    private static Channel<CartSummaryResponse> Listen(HubConnection connection)
    { var channel = Channel.CreateUnbounded<CartSummaryResponse>(); connection.On<CartSummaryResponse>("SessionUpdated", item => channel.Writer.TryWrite(item)); return channel; }
    private static Task<CartSummaryResponse> ReadAsync(Channel<CartSummaryResponse> channel) => channel.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));
}
