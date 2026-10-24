using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;
using SARE.Application.Common.Security;
using SARE.Application.DTOs.Cart;
using SARE.Domain.Cart;
using SARE.Domain.Catalog;
using SARE.Domain.Enums;

namespace SARE.Api.Tests;

// السيناريوهات المتفق عليها تُختبر من Hub حقيقي مع الحفظ والرد الكامل للسيشن.
public sealed class CartPendingFlowTests : IAsyncLifetime
{
    private const string CartId = "PENDING-CART";
    private const string Token = "pending-cart-token";
    private readonly CatalogApiFactory _factory = new();
    private readonly Guid _boot = Guid.NewGuid();
    private HubConnection _cart = null!;
    private Guid _session;

    public async Task InitializeAsync()
    {
        _factory.CreateClient();
        await _factory.InDatabaseAsync(async db =>
        {
            await db.Database.EnsureCreatedAsync();
            db.Carts.Add(new Cart { Id = CartId, TokenHash = TokenHasher.HashToken(Token) });
            var category = new Category { Id = Guid.NewGuid(), NameAr = "تصنيف", NameEn = "Category" };
            var product = new Product { Id = Guid.NewGuid(), CategoryId = category.Id, NameAr = "منتج", NameEn = "Product", WeightTolerancePercent = 0m };
            db.Categories.Add(category);
            db.Products.Add(product);
            foreach (var (barcode, weight, price) in new[]
            {
                ("A", 100, 1000), ("B", 200, 2000), ("C", 300, 3000), ("D", 100, 1500), ("E", 60, 600), ("MILK", 1000, 5000)
            })
                db.ProductVariants.Add(new ProductVariant
                { Id = Guid.NewGuid(), ProductId = product.Id, Barcode = barcode, WeightG = weight,
                    PriceMinor = price });
            await db.SaveChangesAsync();
        });
        _cart = await ConnectAsync();
        _session = (await _cart.InvokeAsync<CartSummaryResponse>("StartSession", new CartStartSessionRequest())).SessionId;
    }

    public async Task DisposeAsync()
    {
        await _cart.DisposeAsync();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task ScanAndTwoKnownEntriesLeaveOnlyTheWeakCandidateForResidualWeight()
    {
        var result = await ReportAsync([Scan("D"), Enter("A"), Enter("B"), Enter("C") with { Confidence = 0.5f }], new(0, 600, true));
        Assert.Equal(7500, result.Session.TotalMinor);
        Assert.Equal(600, result.Session.ExpectedBasketWeightG);
        Assert.Equal(0, result.Session.PendingWeightG);
        Assert.Equal(BasketPresence.Unconfirmed, result.Session.Items.Single(item => item.Barcode == "D").BasketPresence);
        var supported = result.Movement.Items.Single(item => item.Barcode == "C");
        Assert.Equal("WEIGHT_SUPPORTED", supported.ReasonCode);
        Assert.Equal(0.5f, supported.Confidence);
        Assert.True(result.Session.CanCheckout);
    }

    [Fact]
    public async Task FailedWeakCandidateOnlyAddsItsActualResidualMassToPending()
    {
        var result = await ReportAsync([Scan("D"), Enter("A"), Enter("B"), Enter("C") with { Confidence = 0.5f }], new(0, 650, true));
        Assert.Equal(4500, result.Session.TotalMinor);
        Assert.Equal(350, result.Session.PendingWeightG);
        Assert.DoesNotContain(result.Session.Items, item => item.Barcode == "C");
        Assert.All(result.Movement.Items.Take(3), item => Assert.Equal("Accepted", item.Outcome));
        Assert.False(result.Session.CanCheckout);
    }

    [Theory]
    [InlineData(0.90f, true)]
    [InlineData(0.899f, false)]
    public async Task TheConfiguredConfidenceBoundaryIsInclusiveWithoutRoundingItBelowTheThreshold(
        float confidence, bool identified)
    {
        var result = await ReportAsync([Enter("A") with { Confidence = confidence }], new(0, 200, true));
        Assert.Equal(identified ? 1000 : 0, result.Session.TotalMinor);
        Assert.Equal(identified ? 100 : 200, result.Session.PendingWeightG);
        Assert.Equal(identified ? 1 : 0, result.Session.Items.Count);
    }

    [Theory]
    [InlineData("A", null)]
    [InlineData(null, 0.99f)]
    public async Task MissingBarcodeOrConfidenceCannotBeRescuedFromWeight(string? barcode, float? confidence)
    {
        var result = await ReportAsync([Enter(barcode) with { Confidence = confidence }], new(0, 100, true));
        Assert.Empty(result.Session.Items);
        Assert.Equal(100, result.Session.PendingWeightG);
        Assert.Equal("Unknown", result.Movement.Items[0].Outcome);
    }

    [Fact]
    public async Task CombinedWeightDoesNotIdentifyTwoWeakCandidates()
    {
        var result = await ReportAsync([Enter("A") with { Confidence = 0.5f }, Enter("B") with { Confidence = 0.5f }], new(0, 300, true));
        Assert.Empty(result.Session.Items);
        Assert.Equal(300, result.Session.PendingWeightG);
    }

    [Fact]
    public async Task PiecesMissedCompletelyByTheCameraStillCreateUnexplainedMass()
    {
        var result = await ReportAsync([Enter("A")], new(0, 300, true));
        Assert.Equal("A", Assert.Single(result.Session.Items).Barcode);
        Assert.Equal(200, result.Session.PendingWeightG);
        Assert.Single(result.Movement.Items);
    }

    [Fact]
    public async Task UnknownMassAccumulatesWhileKnownEntriesAndScansContinue()
    {
        await ReportAsync([Enter(null)], new(0, 200, true));
        await ReportAsync([Enter("B")], new(200, 400, true));
        await ReportAsync([Enter(null), Enter(null)], new(400, 700, true));
        var result = await ReportAsync([Scan("A")]);
        Assert.Equal(500, result.Session.PendingWeightG);
        Assert.Equal(3000, result.Session.TotalMinor);
        Assert.Equal(2, result.Session.Items.Count);
        Assert.Equal(BasketState.Pending, result.Session.BasketState);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task MissingOrMergedUnknownObservationsDoNotChangePendingMassOrInterruptKnownItemsAndScanPlacement(
        int unknownObservations)
    {
        var observations = new List<CartMovementItemRequest> { Scan("D"), Enter("A") };
        observations.AddRange(Enumerable.Range(0, unknownObservations).Select(_ => Enter(null)));
        var result = await ReportAsync(observations, new(0, 300, true));
        Assert.Equal(200, result.Session.PendingWeightG);
        Assert.Equal(2500, result.Session.TotalMinor);
        Assert.Equal(2, result.Session.Items.Count);
        Assert.Equal(BasketPresence.Unconfirmed, result.Session.Items.Single(item => item.Barcode == "D").BasketPresence);
        var heldId = result.Session.Items.Single(item => item.Barcode == "D").Id;

        var knownEntry = await ReportAsync([Enter("B")], new(300, 500, true));
        Assert.Equal(200, knownEntry.Session.PendingWeightG);
        var placed = await ReportAsync([Enter("D")], new(500, 600, true));
        Assert.Equal(4500, placed.Session.TotalMinor);
        Assert.Equal(200, placed.Session.PendingWeightG);
        Assert.Equal(BasketPresence.Inside, placed.Session.Items.Single(item => item.Id == heldId).BasketPresence);
        Assert.Equal(3, placed.Session.Items.Count);
        Assert.Equal(BasketState.Pending, placed.Session.BasketState);
    }

    [Fact]
    public async Task ReturningUnknownContentsThenScanningIsAnOrdinaryNewPurchase()
    {
        await ReportAsync([Enter(null)], new(0, 200, true));
        var returned = await ReportAsync([Exit(null)], new(200, 0, true));
        Assert.Equal(0, returned.Session.PendingWeightG);
        Assert.True(returned.Session.CanCheckout);
        var scan = await ReportAsync([Scan("A")]);
        var result = await ReportAsync([Enter("A")], new(0, 100, true));
        Assert.Equal(scan.Session.Items[0].Id, Assert.Single(result.Session.Items).Id);
        Assert.Equal(1000, result.Session.TotalMinor);
        await Assert.ThrowsAsync<HubException>(() => _cart.InvokeAsync("ClarifyCartItems", _session, new { }));
    }

    [Fact]
    public async Task NormalScanDoesNotConsumePendingAndPlacementDoesNotChargeTwice()
    {
        await ReportAsync([Enter(null)], new(0, 100, true));
        var scan = await ReportAsync([Scan("A")]);
        Assert.Equal(100, scan.Session.PendingWeightG);
        Assert.Equal(1000, scan.Session.TotalMinor);
        Assert.Equal(BasketPresence.Unconfirmed, Assert.Single(scan.Session.Items).BasketPresence);
        var placed = await ReportAsync([Enter("A")], new(100, 200, true));
        Assert.Equal(1000, placed.Session.TotalMinor);
        Assert.Equal(100, placed.Session.PendingWeightG);
        Assert.Equal(scan.Session.Items[0].Id, Assert.Single(placed.Session.Items).Id);
    }

    [Fact]
    public async Task VisibleExitWithoutPendingRemovesTheKnownPurchase()
    {
        await ReportAsync([Enter("A")], new(0, 100, true));
        var result = await ReportAsync([Exit("A")], new(100, 0, true));
        Assert.Empty(result.Session.Items);
        Assert.True(result.Session.CanCheckout);
    }

    [Fact]
    public async Task BlindExitWithOneCompleteFinancialCombinationIsAppliedAutomatically()
    {
        await ReportAsync([Enter("A")], new(0, 100, true));
        var result = await ReportAsync([Exit(null)], new(100, 0, true));
        Assert.Empty(result.Session.Items);
        Assert.Equal("UNIQUE_WEIGHT_MATCH", result.Movement.ReasonCode);
        Assert.True(result.Session.CanCheckout);
    }

    [Fact]
    public async Task EmptyInvoiceSelectionIsNotAWeightMatchForABlindExitWithoutPending()
    {
        await ReportAsync([Enter("A")], new(0, 100, true));
        var result = await ReportAsync([Exit(null)], new(100, 95, true));
        Assert.Equal(1000, result.Session.TotalMinor);
        Assert.Equal("NO_EXIT_MATCH", result.Movement.ReasonCode);
        Assert.Equal(BasketState.NeedsStaff, result.Session.BasketState);
        Assert.False(result.Session.CanCheckout);
    }

    [Fact]
    public async Task BlindExitWithCompetingInvoiceCombinationsRequiresAChoice()
    {
        var original = await ReportAsync([Enter("A"), Enter("D")], new(0, 200, true));
        var exit = await ReportAsync([Exit(null)], new(200, 100, true));
        Assert.Equal(2500, exit.Session.TotalMinor);
        Assert.Equal(2, exit.Session.ExitChoices.Count);
        var choice = await ChoiceAsync(exit.Movement.MovementId, [original.Session.Items.Single(item => item.Barcode == "A").Id]);
        var result = await _cart.InvokeAsync<CartMovementResult>("ConfirmExitChoice", _session, choice);
        var replay = await _cart.InvokeAsync<CartMovementResult>("ConfirmExitChoice", _session, choice);
        Assert.Equal(1500, result.Session.TotalMinor);
        Assert.Equal("D", Assert.Single(result.Session.Items).Barcode);
        Assert.Equal(result.Session.Version, replay.Session.Version);
        Assert.True(result.Session.CanCheckout);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AVisibleInvoiceBarcodeDuringPendingAsksWhichPhysicalUnitLeft(bool invoiceUnitLeft)
    {
        var original = await ReportAsync([Enter("A")], new(0, 100, true));
        await ReportAsync([Enter(null)], new(100, 200, true));
        var exit = await ReportAsync([Exit("A")], new(200, 100, true));
        Assert.Equal(1000, exit.Session.TotalMinor);
        Assert.Equal(100, exit.Session.PendingWeightG);
        Assert.Equal(100, exit.Movement.UnconfirmedExitWeightG);
        var result = await _cart.InvokeAsync<CartMovementResult>("ConfirmExitChoice", _session,
            await ChoiceAsync(exit.Movement.MovementId, invoiceUnitLeft ? [original.Session.Items[0].Id] : []));
        Assert.Equal(invoiceUnitLeft ? 0 : 1000, result.Session.TotalMinor);
        Assert.Equal(invoiceUnitLeft ? 100 : 0, result.Session.PendingWeightG);
        Assert.Equal(!invoiceUnitLeft, result.Session.CanCheckout);
    }

    [Fact]
    public async Task KnownExitLargerThanPendingRemovesItsInvoiceUnitWithoutOfferingEqualWeightAlternatives()
    {
        var original = await ReportAsync([Enter("A"), Enter("B"), Enter("C")], new(0, 600, true));
        await ReportAsync([Enter(null)], new(600, 700, true));
        var request = await RequestAsync([Exit("C")], new(700, 400, true));
        var result = await SendAsync(request);
        Assert.Equal(3000, result.Session.TotalMinor);
        Assert.Equal(100, result.Session.PendingWeightG);
        Assert.DoesNotContain(result.Session.Items, item => item.Barcode == "C");
        Assert.Contains(result.Session.Items, item => item.Id == original.Session.Items.Single(unit => unit.Barcode == "A").Id);
        Assert.Contains(result.Session.Items, item => item.Id == original.Session.Items.Single(unit => unit.Barcode == "B").Id);
        Assert.Empty(result.Session.ExitChoices);
        Assert.Null(result.Session.CurrentExitMovementId);
        var retry = await SendAsync(request);
        Assert.Equal(result.Session.TotalMinor, retry.Session.TotalMinor);
        Assert.Equal(result.Session.Version, retry.Session.Version);
    }

    [Fact]
    public async Task KnownExitLargerThanPendingUsesTheOldestPurchaseWhenItsSkuHasDifferentPrices()
    {
        var original = await ReportAsync([Enter("B")], new(0, 200, true));
        await _factory.InDatabaseAsync(async db =>
        {
            (await db.ProductVariants.SingleAsync(item => item.Barcode == "B")).PriceMinor = 3000;
            await db.SaveChangesAsync();
        });
        var newer = await ReportAsync([Enter("B")], new(200, 400, true));
        await ReportAsync([Enter(null)], new(400, 450, true));
        var result = await ReportAsync([Exit("B")], new(450, 250, true));
        Assert.Equal(3000, result.Session.TotalMinor);
        Assert.DoesNotContain(result.Session.Items, item => item.Id == original.Session.Items[0].Id);
        Assert.Equal(newer.Session.Items.Single(item => item.UnitPriceMinor == 3000).Id, Assert.Single(result.Session.Items).Id);
        Assert.Equal(50, result.Session.PendingWeightG);
        Assert.Null(result.Session.CurrentExitMovementId);
    }

    [Fact]
    public async Task KnownExitWithinPendingOffersOnlyItsMatchingInvoiceUnitOrPending()
    {
        var original = await ReportAsync([Enter("A"), Enter("D"), Enter("E")], new(0, 260, true));
        await ReportAsync([Enter(null)], new(260, 460, true));
        var result = await ReportAsync([Exit("A")], new(460, 360, true));
        var a = original.Session.Items.Single(item => item.Barcode == "A").Id;
        Assert.Equal(2, result.Session.ExitChoices.Count);
        Assert.Contains(result.Session.ExitChoices, choice => choice.SessionItemIds.Count == 0 && choice.PendingWeightG == 100);
        Assert.Contains(result.Session.ExitChoices, choice => choice.SessionItemIds.SequenceEqual(new[] { a }) && choice.PendingWeightG == 0);
        var rejoined = await SnapshotAsync();
        Assert.All(rejoined.ExitChoices, choice => Assert.All(choice.SessionItemIds, id => Assert.Equal(a, id)));
        Assert.Equal(result.Movement.MovementId, rejoined.CurrentExitMovementId);
    }

    [Theory]
    [InlineData("D")]
    [InlineData("E")]
    public async Task CustomerCannotReplaceAKnownPendingExitWithAnUnrelatedWeightCompatibleItem(string barcode)
    {
        var original = await ReportAsync([Enter("A"), Enter("D"), Enter("E")], new(0, 260, true));
        await ReportAsync([Enter(null)], new(260, 460, true));
        var exit = await ReportAsync([Exit("A")], new(460, 360, true));
        var result = await _cart.InvokeAsync<CartMovementResult>("ConfirmExitChoice", _session,
            await ChoiceAsync(exit.Movement.MovementId, [original.Session.Items.Single(item => item.Barcode == barcode).Id]));
        Assert.Equal(BasketState.NeedsStaff, result.Session.BasketState);
        Assert.Equal(3100, result.Session.TotalMinor);
        Assert.Equal(200, result.Session.PendingWeightG);
        Assert.False(result.Movement.ConfirmedByCustomer);
    }

    [Fact]
    public async Task PendingUncertaintyStillRequiresAChoiceWhenItCanCoverTheKnownExit()
    {
        await _factory.InDatabaseAsync(async db =>
        {
            (await db.Products.SingleAsync()).WeightTolerancePercent = 10m;
            await db.SaveChangesAsync();
        });
        await ReportAsync([Enter("A"), Enter(null)], new(0, 190, true));
        var result = await ReportAsync([Exit("A")], new(190, 90, true));
        Assert.Equal(90, result.Session.PendingWeightG);
        Assert.Equal(10, result.Session.PendingWeightUncertaintyG);
        Assert.Equal(1000, result.Session.TotalMinor);
        Assert.Equal(2, result.Session.ExitChoices.Count);
        Assert.NotNull(result.Session.CurrentExitMovementId);
    }

    [Fact]
    public async Task MatchingKnownExitDoesNotConsumePendingForPackageVariationInsideTolerance()
    {
        await _factory.InDatabaseAsync(async db =>
        {
            (await db.Products.SingleAsync()).WeightTolerancePercent = 3m;
            await db.SaveChangesAsync();
        });
        await ReportAsync([Enter("C")], new(0, 309, true));
        await ReportAsync([Enter(null)], new(309, 409, true));
        var result = await ReportAsync([Exit("C")], new(409, 100, true));
        Assert.Equal(0, result.Session.TotalMinor);
        Assert.Equal(100, result.Session.PendingWeightG);
        Assert.Null(result.Session.CurrentExitMovementId);
    }

    [Fact]
    public async Task PartiallyRemovingUncertainPendingDoesNotHideUnknownMassStillOnTheScale()
    {
        await _factory.InDatabaseAsync(async db =>
        {
            (await db.Products.SingleAsync()).WeightTolerancePercent = 3m;
            await db.SaveChangesAsync();
        });
        var entered = await ReportAsync([Enter("C"), Enter(null)], new(0, 301, true));
        Assert.Equal(1, entered.Session.PendingWeightG);
        Assert.Equal(9, entered.Session.PendingWeightUncertaintyG);
        await ReportAsync([Exit("C")], new(301, 10, true));
        var partial = await ReportAsync([Exit(null)], new(10, 5, true));
        Assert.Equal(0, partial.Session.TotalMinor);
        Assert.Empty(partial.Session.Items);
        Assert.Equal(5, partial.Session.PendingWeightG);
        Assert.Equal(BasketState.Pending, partial.Session.BasketState);
        Assert.False(partial.Session.CanCheckout);
        var empty = await ReportAsync([Exit(null)], new(5, 0, true));
        Assert.Equal(0, empty.Session.PendingWeightG);
        Assert.True(empty.Session.CanCheckout);
    }

    [Fact]
    public async Task MultipleKnownExitsCanStillMixInvoiceAndPendingEvenWhenTheirTotalExceedsPending()
    {
        var original = await ReportAsync([Enter("A"), Enter("C")], new(0, 400, true));
        await ReportAsync([Enter(null)], new(400, 550, true));
        var result = await ReportAsync([Exit("A"), Exit("C")], new(550, 150, true));
        Assert.Equal(4000, result.Session.TotalMinor);
        Assert.Equal(2, result.Session.ExitChoices.Count);
        var c = original.Session.Items.Single(item => item.Barcode == "C").Id;
        Assert.Contains(result.Session.ExitChoices, choice => choice.SessionItemIds.SequenceEqual(new[] { c }) && choice.PendingWeightG == 100);
        var confirmed = await _cart.InvokeAsync<CartMovementResult>("ConfirmExitChoice", _session,
            await ChoiceAsync(result.Movement.MovementId, [c]));
        Assert.Equal("A", Assert.Single(confirmed.Session.Items).Barcode);
        Assert.Equal(1000, confirmed.Session.TotalMinor);
        Assert.Equal(50, confirmed.Session.PendingWeightG);
    }

    [Fact]
    public async Task KnownExitLargerThanPendingStillWaitsForTheOlderQuestionBeforeDeducting()
    {
        var original = await ReportAsync([Enter("A"), Enter("D"), Enter("C")], new(0, 500, true));
        await ReportAsync([Enter(null)], new(500, 550, true));
        var first = await ReportAsync([Exit(null)], new(550, 450, true));
        var later = await ReportAsync([Exit("C")], new(450, 150, true));
        Assert.Equal(first.Movement.MovementId, later.Session.CurrentExitMovementId);
        Assert.Equal(5500, later.Session.TotalMinor);
        var result = await _cart.InvokeAsync<CartMovementResult>("ConfirmExitChoice", _session,
            await ChoiceAsync(first.Movement.MovementId, [original.Session.Items.Single(item => item.Barcode == "A").Id]));
        Assert.Equal("D", Assert.Single(result.Session.Items).Barcode);
        Assert.Equal(1500, result.Session.TotalMinor);
        Assert.Equal(50, result.Session.PendingWeightG);
        Assert.Null(result.Session.CurrentExitMovementId);
    }

    [Fact]
    public async Task AnEarlierChoiceCannotConsumeAKnownLaterExitThatExceedsItsRemainingPending()
    {
        var original = await ReportAsync([Enter("A"), Enter("B"), Enter("C")], new(0, 600, true));
        await ReportAsync([Enter(null)], new(600, 650, true));
        var first = await ReportAsync([Exit(null)], new(650, 350, true));
        var later = await ReportAsync([Exit("C")], new(350, 50, true));
        Assert.Equal(first.Movement.MovementId, later.Session.CurrentExitMovementId);
        var c = original.Session.Items.Single(item => item.Barcode == "C").Id;
        Assert.DoesNotContain(later.Session.ExitChoices, choice => choice.SessionItemIds.Contains(c));
        var result = await _cart.InvokeAsync<CartMovementResult>("ConfirmExitChoice", _session,
            await ChoiceAsync(first.Movement.MovementId, original.Session.Items.Where(item => item.Barcode != "C").Select(item => item.Id).ToArray()));
        Assert.Equal(0, result.Session.TotalMinor);
        Assert.Equal(50, result.Session.PendingWeightG);
        Assert.Empty(result.Session.Items);
        Assert.Null(result.Session.CurrentExitMovementId);
    }

    [Fact]
    public async Task RecognizedNonInvoiceExitConsumesPendingEvenWithOtherEqualWeightProducts()
    {
        await ReportAsync([Enter("D")], new(0, 100, true));
        await ReportAsync([Enter(null)], new(100, 230, true));
        var result = await ReportAsync([Exit("A")], new(230, 100, true));
        Assert.Equal(0, result.Session.PendingWeightG);
        Assert.Equal(1500, result.Session.TotalMinor);
        Assert.Equal("D", Assert.Single(result.Session.Items).Barcode);
        Assert.True(result.Session.CanCheckout);
    }

    [Fact]
    public async Task BlindExitBelowPendingWithoutInvoiceCandidatesOnlyConsumesPending()
    {
        await ReportAsync([Enter("MILK")], new(0, 1000, true));
        await ReportAsync([Enter(null)], new(1000, 1200, true));
        var result = await ReportAsync([Exit(null)], new(1200, 1100, true));
        Assert.Equal(100, result.Session.PendingWeightG);
        Assert.Equal(5000, result.Session.TotalMinor);
        Assert.Null(result.Movement.UnconfirmedExitWeightG);
    }

    [Fact]
    public async Task PartialInvoicePlusPartialPendingIsARealChoiceEvenWithoutAWholeWeightInvoiceMatch()
    {
        var original = await ReportAsync([Enter("E")], new(0, 60, true));
        await ReportAsync([Enter(null)], new(60, 260, true));
        var exit = await ReportAsync([Exit(null)], new(260, 160, true));
        Assert.Equal(200, exit.Session.PendingWeightG);
        Assert.Contains(exit.Session.ExitChoices, choice => choice.InvoiceWeightG == 60 && choice.PendingWeightG == 40);
        var result = await _cart.InvokeAsync<CartMovementResult>("ConfirmExitChoice", _session,
            await ChoiceAsync(exit.Movement.MovementId, [original.Session.Items[0].Id]));
        Assert.Empty(result.Session.Items);
        Assert.Equal(160, result.Session.PendingWeightG);
    }

    [Fact]
    public async Task MilkAndOnlyPartOfUnknownContentsCanLeaveTogether()
    {
        var original = await ReportAsync([Enter("MILK")], new(0, 1000, true));
        await ReportAsync([Enter(null)], new(1000, 1200, true));
        var exit = await ReportAsync([Exit(null)], new(1200, 100, true));
        var result = await _cart.InvokeAsync<CartMovementResult>("ConfirmExitChoice", _session,
            await ChoiceAsync(exit.Movement.MovementId, [original.Session.Items[0].Id]));
        Assert.Equal(0, result.Session.TotalMinor);
        Assert.Equal(100, result.Session.PendingWeightG);
        Assert.False(result.Session.CanCheckout);
    }

    [Fact]
    public async Task AnExitLargerThanPendingWithoutAnyPossibleInvoicePartNeedsStaff()
    {
        await ReportAsync([Enter("MILK")], new(0, 1000, true));
        await ReportAsync([Enter(null)], new(1000, 1200, true));
        var result = await ReportAsync([Exit(null)], new(1200, 900, true));
        Assert.Equal(BasketState.NeedsStaff, result.Session.BasketState);
        Assert.Equal(200, result.Session.PendingWeightG);
        Assert.Equal(5000, result.Session.TotalMinor);
    }

    [Fact]
    public async Task ContradictoryCustomerSelectionDoesNotChangeTheInvoice()
    {
        await ReportAsync([Enter("MILK")], new(0, 1000, true));
        await ReportAsync([Enter(null)], new(1000, 1100, true));
        var exit = await ReportAsync([Exit(null)], new(1100, 100, true));
        var result = await _cart.InvokeAsync<CartMovementResult>("ConfirmExitChoice", _session,
            await ChoiceAsync(exit.Movement.MovementId, []));
        Assert.Equal(BasketState.NeedsStaff, result.Session.BasketState);
        Assert.Equal(5000, result.Session.TotalMinor);
        Assert.Equal(100, result.Session.PendingWeightG);
    }

    [Fact]
    public async Task ScanCanContinueWhileAnExitQuestionWaitsForAnAnswer()
    {
        var original = await ReportAsync([Enter("A"), Enter("D")], new(0, 200, true));
        var exit = await ReportAsync([Exit(null)], new(200, 100, true));
        await ReportAsync([Scan("B")]);
        var result = await _cart.InvokeAsync<CartMovementResult>("ConfirmExitChoice", _session,
            await ChoiceAsync(exit.Movement.MovementId, [original.Session.Items.Single(item => item.Barcode == "A").Id]));
        Assert.Equal(3500, result.Session.TotalMinor);
        Assert.Equal(2, result.Session.Items.Count);
        Assert.True(result.Session.CanCheckout);
    }

    [Fact]
    public async Task OldVersionChoiceCannotBeAppliedAfterTheSessionChanges()
    {
        var original = await ReportAsync([Enter("A"), Enter("D")], new(0, 200, true));
        var exit = await ReportAsync([Exit(null)], new(200, 100, true));
        var choice = await ChoiceAsync(exit.Movement.MovementId, [original.Session.Items[0].Id]);
        await ReportAsync([Scan("B")]);
        await Assert.ThrowsAsync<HubException>(() => _cart.InvokeAsync<CartMovementResult>("ConfirmExitChoice", _session, choice));
        Assert.Equal(4500, (await SnapshotAsync()).TotalMinor);
    }

    [Fact]
    public async Task AProblemInOneItemDoesNotSkipALaterRecognizedItem()
    {
        var result = await ReportAsync([Exit("A") with { SessionItemId = Guid.NewGuid() }, Enter("B"), Scan("D")], new(0, 200, true));
        Assert.Equal(3500, result.Session.TotalMinor);
        Assert.Equal("Accepted", result.Movement.Items[1].Outcome);
        Assert.Equal("Accepted", result.Movement.Items[2].Outcome);
        Assert.Equal(BasketState.NeedsStaff, result.Session.BasketState);
    }

    [Fact]
    public async Task SimultaneousUnknownDirectionsDoNotDiscardRecognizedItemsOrScans()
    {
        var result = await ReportAsync([Enter(null), Exit(null), Enter("B"), Scan("A")], new(0, 200, true));
        Assert.Equal(3000, result.Session.TotalMinor);
        Assert.Equal(2, result.Session.Items.Count);
        Assert.Equal(BasketState.NeedsStaff, result.Session.BasketState);
    }

    [Fact]
    public async Task AWaitingScanUsesItsOriginalSnapshotForWeakPlacement()
    {
        var scan = await ReportAsync([Scan("A")]);
        await _factory.InDatabaseAsync(async db =>
        {
            var variant = await db.ProductVariants.SingleAsync(item => item.Barcode == "A");
            variant.PriceMinor = 9999;
            variant.WeightG = 900;
            variant.IsActive = false;
            await db.SaveChangesAsync();
        });
        var result = await ReportAsync([Enter("A") with { Confidence = 0.5f }], new(0, 100, true));
        Assert.Equal(scan.Session.Items[0].Id, Assert.Single(result.Session.Items).Id);
        Assert.Equal(1000, result.Session.TotalMinor);
        Assert.Equal(100, result.Session.ExpectedBasketWeightG);
    }

    [Fact]
    public async Task AWaitingScanWithAMissingHistoricalWeightNeedsStaffInsteadOfInventingASnapshot()
    {
        var scan = await ReportAsync([Scan("A")]);
        await _factory.InDatabaseAsync(async db =>
        {
            var historical = await db.SessionItems.SingleAsync();
            historical.ExpectedWeightG = null;
            await db.SaveChangesAsync();
        });
        var result = await ReportAsync([Enter("A")], new(0, 100, true));
        Assert.Equal(scan.Session.Items[0].Id, Assert.Single(result.Session.Items).Id);
        Assert.Equal("WEIGHT_SNAPSHOT_MISSING", result.Movement.ReasonCode);
        Assert.Null(result.Session.Items[0].ExpectedWeightG);
        Assert.False(result.Session.CanCheckout);
    }

    [Fact]
    public async Task CustomerCanAskForStaffWithoutChangingTheInvoice()
    {
        await ReportAsync([Enter("A"), Enter("D")], new(0, 200, true));
        var exit = await ReportAsync([Exit(null)], new(200, 100, true));
        var request = (await ChoiceAsync(exit.Movement.MovementId, [])) with { NeedsStaff = true };
        var result = await _cart.InvokeAsync<CartMovementResult>("ConfirmExitChoice", _session, request);
        Assert.Equal(2500, result.Session.TotalMinor);
        Assert.Equal(BasketState.NeedsStaff, result.Session.BasketState);
        Assert.Equal("CUSTOMER_REQUESTED_HELP", result.Movement.ReasonCode);
        Assert.False(result.Session.CanCheckout);
    }

    [Fact]
    public async Task SmallPendingDoesNotDisappearWhenKnownGoodsMove()
    {
        await ReportAsync([Enter(null)], new(0, 5, true));
        await ReportAsync([Enter("A")], new(5, 105, true));
        var knownRemoved = await ReportAsync([Exit("A")], new(105, 5, true));
        Assert.Equal(0, knownRemoved.Session.TotalMinor);
        Assert.Null(knownRemoved.Session.CurrentExitMovementId);
        Assert.Equal(5, knownRemoved.Session.PendingWeightG);
        Assert.False(knownRemoved.Session.CanCheckout);
        var returned = await ReportAsync([Exit(null)], new(5, 0, true));
        Assert.True(returned.Session.CanCheckout);
    }

    [Fact]
    public async Task TruncatedSearchCannotClaimThatTheSolutionIsUnique()
    {
        await ReportAsync(Enumerable.Range(0, 11).Select(_ => Enter("A")).ToArray(), new(0, 1100, true));
        var result = await ReportAsync([Exit(null)], new(1100, 0, true));
        Assert.Equal(11000, result.Session.TotalMinor);
        Assert.Equal("SELECT_EXIT_ITEMS", result.Movement.ReasonCode);
        Assert.False(result.Session.CanCheckout);
    }

    [Fact]
    public async Task ClosingAndCheckoutAreBlockedByUnexplainedMassOnTheServer()
    {
        var pending = await ReportAsync([Enter(null)], new(0, 100, true));
        await Assert.ThrowsAsync<HubException>(() => _cart.InvokeAsync<CartSummaryResponse>("CloseSession", _session));
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", _factory.CreateToken());
        var checkout = await client.PostAsync($"/api/cart/{_session}/checkout", null);
        Assert.False(checkout.IsSuccessStatusCode);
        Assert.False(pending.Session.CanCheckout);
    }

    [Fact]
    public async Task StaffRecountResolvesUnknownContentsWithoutReplayingTheirOldReports()
    {
        await ReportAsync([Enter("A")], new(0, 100, true));
        var unknown = await RequestAsync([Enter(null)], new(100, 300, true));
        await SendAsync(unknown);
        using var admin = _factory.CreateClient();
        admin.DefaultRequestHeaders.Authorization = new("Bearer", _factory.CreateToken());
        async Task<CartAuditCommand> CommandAsync(Guid? auditId = null)
        {
            var current = await SnapshotAsync();
            return new(Guid.NewGuid(), Guid.NewGuid(), current.LastMovementSequence + 1, _boot, current.Version) { AuditId = auditId };
        }
        async Task<CartMovementResult> AdminAsync(string action, Guid? auditId = null)
        {
            var response = await admin.PostAsJsonAsync($"/api/v1/admin/sessions/{_session}/audit/{action}", await CommandAsync(auditId));
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<CartMovementResult>())!;
        }
        await _cart.InvokeAsync<CartMovementResult>("RequestStaff", _session, await CommandAsync());
        var audit = (await AdminAsync("start")).Session.Audit!.Id;
        await SendAsync((await RequestAsync([Scan("A")])) with { AuditId = audit });
        await SendAsync((await RequestAsync([], new(0, 100, true))) with { AuditId = audit });
        var restored = await AdminAsync("commit", audit);
        Assert.Equal(0, restored.Session.PendingWeightG);
        Assert.Equal(1000, restored.Session.TotalMinor);
        Assert.True(restored.Session.CanCheckout);
        var replay = await SendAsync(unknown);
        Assert.Equal("Cancelled", replay.Movement.Status);
        Assert.Equal(1000, replay.Session.TotalMinor);
    }

    [Theory]
    [InlineData("B", 200, 2000, false)]
    [InlineData("D", 100, 1500, true)]
    [InlineData("E", 60, 600, false)]
    public async Task KnownIngressDoesNotHideABlindExitBehindAPositiveZeroOrNegativeNetDelta(
        string barcode, int weight, int price, bool ambiguous)
    {
        var original = await ReportAsync([Enter("A")], new(0, 100, true));
        var result = await ReportAsync([Enter(barcode)], new(100, weight, true));
        if (ambiguous)
        {
            Assert.Equal(BasketState.Pending, result.Session.BasketState);
            Assert.Equal(2, result.Session.ExitChoices.Count);
            result = await _cart.InvokeAsync<CartMovementResult>("ConfirmExitChoice", _session,
                await ChoiceAsync(result.Movement.MovementId, [original.Session.Items[0].Id]));
        }
        Assert.Equal(BasketState.Verified, result.Session.BasketState);
        Assert.Equal(barcode, Assert.Single(result.Session.Items).Barcode);
        Assert.Equal(price, result.Session.TotalMinor);
        Assert.Equal(0, result.Session.PendingWeightG);
        Assert.True(result.Session.CanCheckout);
    }

    [Fact]
    public async Task KnownAndUnknownEntriesContinueWhileOnlyTheOldestExitChoicesAreShown()
    {
        var initial = await ReportAsync([Enter("A"), Enter("D")], new(0, 200, true));
        var first = await ReportAsync([Exit(null)], new(200, 100, true));
        await ReportAsync([Enter("B")], new(100, 300, true));
        var addedUnknown = await ReportAsync([Enter(null)], new(300, 700, true));
        Assert.Equal(BasketState.Pending, addedUnknown.Session.BasketState);
        Assert.Equal(first.Movement.MovementId, addedUnknown.Session.CurrentExitMovementId);
        Assert.Equal(100, addedUnknown.Session.UnconfirmedExitWeightG);
        Assert.Equal(400, addedUnknown.Session.PendingWeightG);
        Assert.All(addedUnknown.Session.ExitChoices, choice => Assert.Equal(0, choice.PendingWeightG));
        await ReportAsync([Scan("E")]);
        var result = await _cart.InvokeAsync<CartMovementResult>("ConfirmExitChoice", _session,
            await ChoiceAsync(first.Movement.MovementId, [initial.Session.Items.Single(item => item.Barcode == "A").Id]));
        Assert.Equal(4100, result.Session.TotalMinor);
        Assert.Equal(400, result.Session.PendingWeightG);
        Assert.Null(result.Session.CurrentExitMovementId);
        Assert.False(result.Session.CanCheckout);
    }

    [Fact]
    public async Task TwoBlindExitsAreFifoAndTheRemainingUniqueExitSettlesAutomatically()
    {
        var initial = await ReportAsync([Enter("A"), Enter("D")], new(0, 200, true));
        var first = await ReportAsync([Exit(null)], new(200, 100, true));
        var second = await ReportAsync([Exit(null)], new(100, 0, true));
        Assert.Equal(2500, second.Session.TotalMinor);
        Assert.Equal(2, second.Session.ExitChoices.Count);
        Assert.Equal(200, second.Session.UnconfirmedExitWeightG);
        var laterAnswer = await ChoiceAsync(second.Movement.MovementId, [initial.Session.Items.Single(item => item.Barcode == "D").Id]);
        var error = await Assert.ThrowsAsync<HubException>(() => _cart.InvokeAsync<CartMovementResult>("ConfirmExitChoice", _session, laterAnswer));
        Assert.Contains("EXIT_ORDER_REQUIRED", error.Message);
        Assert.Equal(second.Session.Version, (await SnapshotAsync()).Version);

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", _factory.CreateToken());
        var http = await client.GetFromJsonAsync<CartSummaryResponse>($"/api/cart/{_session}");
        Assert.Equal(first.Movement.MovementId, http!.CurrentExitMovementId);
        Assert.Equal(200, http.UnconfirmedExitWeightG);

        var answer = await ChoiceAsync(first.Movement.MovementId, [initial.Session.Items.Single(item => item.Barcode == "A").Id]);
        var result = await _cart.InvokeAsync<CartMovementResult>("ConfirmExitChoice", _session, answer);
        Assert.Empty(result.Session.Items);
        Assert.Empty(result.Session.PendingMovements);
        Assert.True(result.Session.CanCheckout);
        Assert.Equal(2, result.Movement.Items.Count(item => item.Effect == "Removed"));
        await _cart.InvokeAsync<CartSummaryResponse>("CloseSession", _session);
        var replay = await _cart.InvokeAsync<CartMovementResult>("ConfirmExitChoice", _session, answer);
        Assert.Equal("closed", replay.Session.Status);
        Assert.Equal(0, replay.Session.TotalMinor);
    }

    [Fact]
    public async Task LaterUnknownMassCannotAnswerAnOlderExitQuestion()
    {
        await ReportAsync([Enter("A"), Enter("D")], new(0, 200, true));
        var first = await ReportAsync([Exit(null)], new(200, 100, true));
        await ReportAsync([Enter(null)], new(100, 200, true));
        var result = await _cart.InvokeAsync<CartMovementResult>("ConfirmExitChoice", _session,
            await ChoiceAsync(first.Movement.MovementId, []));
        Assert.Equal(BasketState.NeedsStaff, result.Session.BasketState);
        Assert.Equal(2500, result.Session.TotalMinor);
        Assert.Equal(100, result.Session.PendingWeightG);
    }

    [Fact]
    public async Task AWaitingScanPlacedLaterCannotBeSelectedForAnOlderExit()
    {
        var initial = await ReportAsync([Scan("A"), Scan("A"), Enter("A"), Enter("D")], new(0, 200, true));
        var first = await ReportAsync([Exit(null)], new(200, 100, true));
        var held = initial.Session.Items.Single(item => item.Barcode == "A" && item.BasketPresence == BasketPresence.Unconfirmed);
        var later = await ReportAsync([Enter("A")], new(100, 200, true));
        Assert.DoesNotContain(later.Session.ExitChoices, choice => choice.SessionItemIds.Contains(held.Id));
        var invalid = await ChoiceAsync(first.Movement.MovementId, [held.Id]);
        var error = await Assert.ThrowsAsync<HubException>(() => _cart.InvokeAsync<CartMovementResult>("ConfirmExitChoice", _session, invalid));
        Assert.Contains("INVALID_ITEM_SELECTION", error.Message);
        var original = initial.Session.Items.Single(item => item.Barcode == "A" && item.BasketPresence == BasketPresence.Inside);
        var result = await _cart.InvokeAsync<CartMovementResult>("ConfirmExitChoice", _session,
            await ChoiceAsync(first.Movement.MovementId, [original.Id]));
        Assert.Contains(result.Session.Items, item => item.Id == held.Id);
        Assert.Equal(2500, result.Session.TotalMinor);
    }

    [Fact]
    public async Task ClearingOldPendingTurnsTheNextLargerBlindExitIntoANoPendingUniqueMatch()
    {
        await ReportAsync([Enter("A"), Enter("C")], new(0, 400, true));
        await ReportAsync([Enter(null)], new(400, 600, true));
        var first = await ReportAsync([Exit(null)], new(600, 400, true));
        var second = await ReportAsync([Exit(null)], new(400, 100, true));
        Assert.Equal(first.Movement.MovementId, second.Session.CurrentExitMovementId);
        var result = await _cart.InvokeAsync<CartMovementResult>("ConfirmExitChoice", _session,
            await ChoiceAsync(first.Movement.MovementId, []));
        Assert.Equal(0, result.Session.PendingWeightG);
        Assert.Equal("A", Assert.Single(result.Session.Items).Barcode);
        Assert.Equal(1000, result.Session.TotalMinor);
        Assert.True(result.Session.CanCheckout);
        await _factory.InDatabaseAsync(async db =>
        {
            var saved = await db.CartMovements.SingleAsync(item => item.Id == second.Movement.MovementId);
            Assert.Equal(result.Movement.MovementId, saved.ResolvedByMovementId);
            Assert.Equal("UNIQUE_WEIGHT_MATCH", saved.ReasonCode);
        });
    }

    [Fact]
    public async Task UnknownIngressBetweenQuestionsBelongsToTheSecondButNotTheFirst()
    {
        await ReportAsync([Enter("A"), Enter("D")], new(0, 200, true));
        await ReportAsync([Enter(null)], new(200, 300, true));
        var first = await ReportAsync([Exit("A")], new(300, 200, true));
        await ReportAsync([Enter(null)], new(200, 300, true));
        var second = await ReportAsync([Exit(null)], new(300, 200, true));
        var result = await _cart.InvokeAsync<CartMovementResult>("ConfirmExitChoice", _session,
            await ChoiceAsync(first.Movement.MovementId, []));
        Assert.Equal(100, result.Session.PendingWeightG);
        Assert.Equal(second.Movement.MovementId, result.Session.CurrentExitMovementId);
        Assert.Contains(result.Session.ExitChoices, choice => choice.SessionItemIds.Count == 0 && choice.PendingWeightG == 100);
        Assert.Equal(2500, result.Session.TotalMinor);
    }

    [Fact]
    public async Task UnknownIngressAfterBothQuestionsSurvivesTheirSettlement()
    {
        var initial = await ReportAsync([Enter("A"), Enter("D")], new(0, 200, true));
        var first = await ReportAsync([Exit(null)], new(200, 100, true));
        await ReportAsync([Exit(null)], new(100, 0, true));
        await ReportAsync([Enter(null)], new(0, 200, true));
        var result = await _cart.InvokeAsync<CartMovementResult>("ConfirmExitChoice", _session,
            await ChoiceAsync(first.Movement.MovementId, [initial.Session.Items.Single(item => item.Barcode == "A").Id]));
        Assert.Empty(result.Session.Items);
        Assert.Equal(200, result.Session.PendingWeightG);
        Assert.Null(result.Session.CurrentExitMovementId);
        Assert.Equal(0, result.Session.UnconfirmedExitWeightG);
        Assert.False(result.Session.CanCheckout);
    }

    [Fact]
    public async Task OneAnswerCanResolveSeveralLaterQuestionsWithoutRepeatedCharges()
    {
        var initial = await ReportAsync([Enter("A"), Enter("D"), Enter("B"), Enter("C")], new(0, 700, true));
        var first = await ReportAsync([Exit(null)], new(700, 600, true));
        await ReportAsync([Exit(null)], new(600, 400, true));
        await ReportAsync([Exit(null)], new(400, 100, true));
        var answer = await ChoiceAsync(first.Movement.MovementId, [initial.Session.Items.Single(item => item.Barcode == "A").Id]);
        var result = await _cart.InvokeAsync<CartMovementResult>("ConfirmExitChoice", _session, answer);
        Assert.Equal("D", Assert.Single(result.Session.Items).Barcode);
        Assert.Equal(1500, result.Session.TotalMinor);
        Assert.Equal(3, result.Movement.Items.Count(item => item.Effect == "Removed"));
        Assert.True(result.Session.CanCheckout);
        var replay = await _cart.InvokeAsync<CartMovementResult>("ConfirmExitChoice", _session, answer);
        Assert.Equal(result.Session.Version, replay.Session.Version);
    }

    [Fact]
    public async Task IdentifiedExitBehindAnOpenQuestionSettlesAutomaticallyInOrder()
    {
        var initial = await ReportAsync([Enter("A"), Enter("D"), Enter("B")], new(0, 400, true));
        var first = await ReportAsync([Exit(null)], new(400, 300, true));
        var later = await ReportAsync([Exit("B")], new(300, 100, true));
        Assert.Equal(4500, later.Session.TotalMinor);
        Assert.Equal(BasketState.Pending, later.Session.BasketState);
        var result = await _cart.InvokeAsync<CartMovementResult>("ConfirmExitChoice", _session,
            await ChoiceAsync(first.Movement.MovementId, [initial.Session.Items.Single(item => item.Barcode == "A").Id]));
        Assert.Equal("D", Assert.Single(result.Session.Items).Barcode);
        Assert.Equal(1500, result.Session.TotalMinor);
        Assert.True(result.Session.CanCheckout);
    }

    [Fact]
    public async Task ReenteringTheSameBarcodeCreatesANewPieceForLaterQuestionsOnly()
    {
        var initial = await ReportAsync([Enter("A"), Enter("D")], new(0, 200, true));
        var first = await ReportAsync([Exit(null)], new(200, 100, true));
        await _factory.InDatabaseAsync(async db =>
        {
            (await db.ProductVariants.SingleAsync(item => item.Barcode == "A")).PriceMinor = 9999;
            await db.SaveChangesAsync();
        });
        await ReportAsync([Enter("A")], new(100, 200, true));
        await ReportAsync([Exit("A")], new(200, 100, true));
        await _cart.DisposeAsync();
        _cart = await ConnectAsync();
        Assert.Equal(first.Movement.MovementId, (await SnapshotAsync()).CurrentExitMovementId);
        var result = await _cart.InvokeAsync<CartMovementResult>("ConfirmExitChoice", _session,
            await ChoiceAsync(first.Movement.MovementId, [initial.Session.Items.Single(item => item.Barcode == "A").Id]));
        Assert.Equal(1500, result.Session.TotalMinor);
        Assert.True(result.Session.CanCheckout);
    }

    [Fact]
    public async Task AQueuedMixedExitKeepsTheIdentifiedSkuWhileResolvingItsBlindRemainder()
    {
        var initial = await ReportAsync([Enter("A"), Enter("D"), Enter("B"), Enter("C")], new(0, 700, true));
        var first = await ReportAsync([Exit(null)], new(700, 600, true));
        await ReportAsync([Exit("B"), Exit(null)], new(600, 300, true));
        var result = await _cart.InvokeAsync<CartMovementResult>("ConfirmExitChoice", _session,
            await ChoiceAsync(first.Movement.MovementId, [initial.Session.Items.Single(item => item.Barcode == "A").Id]));
        Assert.Equal("C", Assert.Single(result.Session.Items).Barcode);
        Assert.Equal(3000, result.Session.TotalMinor);
        Assert.True(result.Session.CanCheckout);
    }

    [Fact]
    public async Task AnEqualWeightChoiceCannotOverrideTheRecognizedPartOfAQueuedMixedExit()
    {
        var initial = await ReportAsync([Enter("A"), Enter("D"), Enter("B"), Enter("C")], new(0, 700, true));
        await ReportAsync([Enter(null)], new(700, 800, true));
        var first = await ReportAsync([Exit("A")], new(800, 700, true));
        var second = await ReportAsync([Exit("B"), Exit(null)], new(700, 400, true));
        var settled = await _cart.InvokeAsync<CartMovementResult>("ConfirmExitChoice", _session,
            await ChoiceAsync(first.Movement.MovementId, []));
        Assert.Equal(second.Movement.MovementId, settled.Session.CurrentExitMovementId);
        var b = initial.Session.Items.Single(item => item.Barcode == "B").Id;
        Assert.All(settled.Session.ExitChoices, choice => Assert.Contains(b, choice.SessionItemIds));
        var result = await _cart.InvokeAsync<CartMovementResult>("ConfirmExitChoice", _session,
            await ChoiceAsync(second.Movement.MovementId, [initial.Session.Items.Single(item => item.Barcode == "C").Id]));
        Assert.Equal(BasketState.NeedsStaff, result.Session.BasketState);
        Assert.Equal(7500, result.Session.TotalMinor);
    }

    [Fact]
    public async Task RepeatedEqualWeightPiecesRemainSeparateChoicesUntilEarlierAnswersConsumeThem()
    {
        await _factory.InDatabaseAsync(async db =>
        {
            (await db.ProductVariants.SingleAsync(item => item.Barcode == "E")).WeightG = 100;
            await db.SaveChangesAsync();
        });
        var initial = await ReportAsync([Enter("A"), Enter("D"), Enter("E")], new(0, 300, true));
        var first = await ReportAsync([Exit(null)], new(300, 200, true));
        var second = await ReportAsync([Exit(null)], new(200, 100, true));
        await ReportAsync([Exit(null)], new(100, 0, true));
        var firstAnswer = await _cart.InvokeAsync<CartMovementResult>("ConfirmExitChoice", _session,
            await ChoiceAsync(first.Movement.MovementId, [initial.Session.Items.Single(item => item.Barcode == "A").Id]));
        Assert.Equal(second.Movement.MovementId, firstAnswer.Session.CurrentExitMovementId);
        Assert.Equal(2, firstAnswer.Session.ExitChoices.Count);
        var result = await _cart.InvokeAsync<CartMovementResult>("ConfirmExitChoice", _session,
            await ChoiceAsync(second.Movement.MovementId, [initial.Session.Items.Single(item => item.Barcode == "D").Id]));
        Assert.Empty(result.Session.Items);
        Assert.True(result.Session.CanCheckout);
    }

    [Fact]
    public async Task LaterRecognizedExitCanEliminateAnEarlierChoiceAndResolveBoth()
    {
        await ReportAsync([Enter("A"), Enter("D")], new(0, 200, true));
        var first = await ReportAsync([Exit(null)], new(200, 100, true));
        Assert.Equal(2, first.Session.ExitChoices.Count);
        var result = await ReportAsync([Exit("D")], new(100, 0, true));
        Assert.Empty(result.Session.Items);
        Assert.Empty(result.Session.PendingMovements);
        Assert.Equal(0, result.Session.TotalMinor);
        Assert.True(result.Session.CanCheckout);
    }

    [Fact]
    public async Task AUniqueLaterMixedInvoicePendingExitCanSettleAfterTheFirstAnswer()
    {
        var initial = await ReportAsync([Enter("A"), Enter("D"), Enter("B")], new(0, 400, true));
        await ReportAsync([Enter(null)], new(400, 500, true));
        var first = await ReportAsync([Exit(null)], new(500, 400, true));
        await ReportAsync([Exit(null)], new(400, 150, true));
        var result = await _cart.InvokeAsync<CartMovementResult>("ConfirmExitChoice", _session,
            await ChoiceAsync(first.Movement.MovementId, [initial.Session.Items.Single(item => item.Barcode == "A").Id]));
        Assert.Equal("D", Assert.Single(result.Session.Items).Barcode);
        Assert.Equal(50, result.Session.PendingWeightG);
        Assert.Equal(1500, result.Session.TotalMinor);
        Assert.Null(result.Session.CurrentExitMovementId);
        Assert.False(result.Session.CanCheckout);
    }

    [Fact]
    public async Task AQueuedRecognizedExitUsesPurchaseOrderDespiteALaterRemovalButton()
    {
        var initial = await ReportAsync([Enter("A"), Enter("D"), Enter("B")], new(0, 400, true));
        await _factory.InDatabaseAsync(async db =>
        {
            (await db.ProductVariants.SingleAsync(item => item.Barcode == "B")).PriceMinor = 3000;
            await db.SaveChangesAsync();
        });
        var added = await ReportAsync([Enter("B")], new(400, 600, true));
        var newer = added.Session.Items.Single(item => item.Barcode == "B" && item.UnitPriceMinor == 3000);
        var first = await ReportAsync([Exit(null)], new(600, 500, true));
        await ReportAsync([Exit("B")], new(500, 300, true));
        await _cart.InvokeAsync<CartSummaryResponse>("RemoveSessionItem", _session, newer.Id);
        var result = await _cart.InvokeAsync<CartMovementResult>("ConfirmExitChoice", _session,
            await ChoiceAsync(first.Movement.MovementId, [initial.Session.Items.Single(item => item.Barcode == "A").Id]));
        Assert.Contains(result.Session.Items, item => item.Id == newer.Id);
        Assert.Equal(4500, result.Session.TotalMinor);
        Assert.False(result.Session.CanCheckout);
        Assert.True((await _cart.InvokeAsync<CartSummaryResponse>("CancelItemRemoval", _session, newer.Id)).CanCheckout);
    }

    [Fact]
    public async Task LaterExplicitUnitTargetRemainsAvailableWhenAnEarlierKnownExitHasDuplicateSkus()
    {
        var initial = await ReportAsync([Enter("A"), Enter("D"), Enter("B"), Enter("B")], new(0, 600, true));
        var target = initial.Session.Items.First(item => item.Barcode == "B").Id;
        var first = await ReportAsync([Exit(null)], new(600, 500, true));
        await ReportAsync([Exit("B")], new(500, 300, true));
        await ReportAsync([Exit("B") with { SessionItemId = target }], new(300, 100, true));
        var result = await _cart.InvokeAsync<CartMovementResult>("ConfirmExitChoice", _session,
            await ChoiceAsync(first.Movement.MovementId, [initial.Session.Items.Single(item => item.Barcode == "A").Id]));
        Assert.Equal("D", Assert.Single(result.Session.Items).Barcode);
        Assert.Equal(1500, result.Session.TotalMinor);
        Assert.True(result.Session.CanCheckout);
    }

    [Fact]
    public async Task EqualPricePiecesAreNotCollapsedBeforeCheckingTheLaterExplicitTarget()
    {
        await _factory.InDatabaseAsync(async db =>
        {
            (await db.ProductVariants.SingleAsync(item => item.Barcode == "B")).WeightG = 100;
            await db.SaveChangesAsync();
        });
        var initial = await ReportAsync([Enter("A"), Enter("B"), Enter("B")], new(0, 300, true));
        var pieces = initial.Session.Items.Where(item => item.Barcode == "B").ToArray();
        var first = await ReportAsync([Exit(null)], new(300, 200, true));
        var later = await ReportAsync([Exit("B") with { SessionItemId = pieces[0].Id }], new(200, 100, true));
        Assert.Equal(first.Movement.MovementId, later.Session.CurrentExitMovementId);
        Assert.Equal(2, later.Session.ExitChoices.Count);
        Assert.Contains(later.Session.ExitChoices, choice => choice.SessionItemIds.Contains(pieces[1].Id));
        Assert.DoesNotContain(later.Session.ExitChoices, choice => choice.SessionItemIds.Contains(pieces[0].Id));
        var result = await _cart.InvokeAsync<CartMovementResult>("ConfirmExitChoice", _session,
            await ChoiceAsync(first.Movement.MovementId, [pieces[1].Id]));
        Assert.Equal("A", Assert.Single(result.Session.Items).Barcode);
        Assert.Equal(1000, result.Session.TotalMinor);
        Assert.True(result.Session.CanCheckout);
    }

    [Fact]
    public async Task AnImpossibleLaterExitRequestsStaffWithoutUndoingTheValidEarlierAnswer()
    {
        var initial = await ReportAsync([Enter("A"), Enter("D")], new(0, 200, true));
        var first = await ReportAsync([Exit(null)], new(200, 100, true));
        await ReportAsync([Exit(null)], new(100, 50, true));
        var result = await _cart.InvokeAsync<CartMovementResult>("ConfirmExitChoice", _session,
            await ChoiceAsync(first.Movement.MovementId, [initial.Session.Items.Single(item => item.Barcode == "A").Id]));
        Assert.Equal(BasketState.NeedsStaff, result.Session.BasketState);
        Assert.Equal("D", Assert.Single(result.Session.Items).Barcode);
        Assert.Equal(1500, result.Session.TotalMinor);
        Assert.False(result.Session.CanCheckout);
    }

    private async Task<CartMovementRequest> RequestAsync(IReadOnlyList<CartMovementItemRequest> items, BasketWeightDto? weight = null)
    {
        var state = await SnapshotAsync();
        var now = DateTime.UtcNow;
        return new(Guid.NewGuid(), Guid.NewGuid(), state.LastMovementSequence + 1, _boot, now, now, items, weight);
    }

    private async Task<CartMovementResult> ReportAsync(IReadOnlyList<CartMovementItemRequest> items, BasketWeightDto? weight = null)
        => await SendAsync(await RequestAsync(items, weight));

    private Task<CartMovementResult> SendAsync(CartMovementRequest request)
        => _cart.InvokeAsync<CartMovementResult>("ReportCartMovement", _session, request);

    private Task<CartSummaryResponse> SnapshotAsync()
        => _cart.InvokeAsync<CartSummaryResponse>("JoinCartGroup", CartId, Token);

    private async Task<ConfirmExitChoiceRequest> ChoiceAsync(Guid movement, IReadOnlyList<Guid> ids)
    {
        var state = await SnapshotAsync();
        return new(Guid.NewGuid(), Guid.NewGuid(), state.LastMovementSequence + 1, _boot, movement, state.Version, ids);
    }

    private async Task<HubConnection> ConnectAsync(bool admin = false)
    {
        var accessToken = admin ? _factory.CreateToken() : null;
        var connection = new HubConnectionBuilder().WithUrl("http://localhost/hubs/cart", options =>
        {
            options.Transports = HttpTransportType.LongPolling;
            options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            if (accessToken is not null) options.AccessTokenProvider = () => Task.FromResult<string?>(accessToken);
        }).Build();
        await connection.StartAsync();
        if (!admin) await connection.InvokeAsync<CartSummaryResponse?>("JoinCartGroup", CartId, Token);
        return connection;
    }

    private static CartMovementItemRequest Scan(string barcode) => new(CartItemAction.Scan, barcode, CartItemSource.Scanner);
    private static CartMovementItemRequest Enter(string? barcode) => new(CartItemAction.Enter, barcode, Confidence: 0.95f);
    private static CartMovementItemRequest Exit(string? barcode) => new(CartItemAction.Exit, barcode, Confidence: 0.95f);
}
