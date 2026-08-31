using System.Collections.Generic;
using BigRetail.Economy.Domain;
using BigRetail.Inventory.Domain;
using BigRetail.Map.Construction;
using BigRetail.Map.Domain;
using BigRetail.Map.Fixtures;
using BigRetail.Merchandise.Domain;
using NUnit.Framework;

namespace BigRetail.Map.Fixtures.Tests
{
    public sealed class FixtureDisplayInventoryServiceTests
    {
        private static readonly FixtureDefinitionId ShelfDefinitionId =
            new FixtureDefinitionId("HALF-SHELF");

        private static readonly FixtureInstanceId ShelfInstanceId =
            new FixtureInstanceId("SHELF-ONE");

        private static readonly FixtureDefinitionId BackstockDefinitionId =
            new FixtureDefinitionId("BACKSTOCK-SHELF");

        private static readonly FixtureInstanceId BackstockInstanceId =
            new FixtureInstanceId("BACKSTOCK-ONE");

        private static readonly FixtureInstanceId SecondBackstockInstanceId =
            new FixtureInstanceId("BACKSTOCK-TWO");

        private static readonly FixtureDefinitionId CheckoutDefinitionId =
            new FixtureDefinitionId("BASIC-CHECKOUT-COUNTER");

        private static readonly FixtureInstanceId CheckoutInstanceId =
            new FixtureInstanceId("CHECKOUT-ONE");

        private static readonly ProductId CerealProductId =
            new ProductId("CEREAL");

        private static readonly ProductId SoupProductId =
            new ProductId("SOUP");

        private static readonly StorageLocationId BackstockLocationId =
            new StorageLocationId("BACKSTOCK");


        [Test]
        public void RestockFixture_AssignedFrontage_FillsRealDisplayInventory()
        {
            TestContext context = CreateContext(100, 100);

            try
            {
                AssignCereal(context, frontageUnitCount: 2);

                Assert.That(
                    context.DisplayInventory.TryGetSnapshot(
                        ShelfInstanceId,
                        out FixtureDisplayStockSnapshot before),
                    Is.True);
                Assert.That(before.StockedUnitCount, Is.Zero);
                Assert.That(before.CapacityUnitCount, Is.EqualTo(12));
                Assert.That(before.BackstockUnitCount, Is.EqualTo(100));

                FixtureRestockResult result =
                    context.DisplayInventory.TryRestockFixture(
                        ShelfInstanceId);

                Assert.That(result.Succeeded, Is.True);
                Assert.That(result.MovedUnitCount, Is.EqualTo(12));
                Assert.That(result.RemainingShortfall, Is.Zero);

                Assert.That(
                    context.DisplayInventory.TryGetSnapshot(
                        ShelfInstanceId,
                        out FixtureDisplayStockSnapshot after),
                    Is.True);
                Assert.That(after.StockedUnitCount, Is.EqualTo(12));
                Assert.That(after.CapacityUnitCount, Is.EqualTo(12));
                Assert.That(after.BackstockUnitCount, Is.EqualTo(88));

                Assert.That(
                    context.DisplayInventory.GetFrontageFillRatio(
                        CreateShelfRun(),
                        0),
                    Is.EqualTo(1f));
                Assert.That(
                    context.DisplayInventory.GetFrontageFillRatio(
                        CreateShelfRun(),
                        1),
                    Is.EqualTo(1f));
            }
            finally
            {
                context.Dispose();
            }
        }

        [Test]
        public void RestockFixture_LimitedBackstock_ReportsAndDisplaysPartialFill()
        {
            TestContext context = CreateContext(8, 100);

            try
            {
                AssignCereal(context, frontageUnitCount: 2);

                FixtureRestockResult result =
                    context.DisplayInventory.TryRestockFixture(
                        ShelfInstanceId);

                Assert.That(result.Succeeded, Is.True);
                Assert.That(result.MovedUnitCount, Is.EqualTo(8));
                Assert.That(result.RemainingShortfall, Is.EqualTo(4));
                Assert.That(
                    context.DisplayInventory.GetFrontageFillRatio(
                        CreateShelfRun(),
                        0),
                    Is.EqualTo(1f));
                Assert.That(
                    context.DisplayInventory.GetFrontageFillRatio(
                        CreateShelfRun(),
                        1),
                    Is.EqualTo(2f / 6f).Within(0.001f));
            }
            finally
            {
                context.Dispose();
            }
        }

        [Test]
        public void RestockFixture_CaseSizedPass_MovesOnlyRequestedUnits()
        {
            TestContext context = CreateContext(100, 100);

            try
            {
                AssignCereal(context, frontageUnitCount: 3);

                FixtureRestockResult firstPass =
                    context.DisplayInventory.TryRestockFixture(
                        ShelfInstanceId,
                        maximumUnitCount: 12);

                Assert.That(firstPass.Succeeded, Is.True);
                Assert.That(firstPass.MovedUnitCount, Is.EqualTo(12));
                Assert.That(firstPass.RemainingShortfall, Is.EqualTo(6));
                Assert.That(
                    context.DisplayInventory.GetDisplayedQuantity(
                        CerealProductId),
                    Is.EqualTo(12));
                Assert.That(
                    context.DisplayInventory.GetFrontageFillRatio(
                        CreateShelfRun(),
                        2),
                    Is.Zero);

                FixtureRestockResult secondPass =
                    context.DisplayInventory.TryRestockFixture(
                        ShelfInstanceId,
                        maximumUnitCount: 12);

                Assert.That(secondPass.MovedUnitCount, Is.EqualTo(6));
                Assert.That(secondPass.RemainingShortfall, Is.Zero);
                Assert.That(
                    context.DisplayInventory.GetDisplayedQuantity(
                        CerealProductId),
                    Is.EqualTo(18));
            }
            finally
            {
                context.Dispose();
            }
        }

        [Test]
        public void ProductSpecificFrontageCapacity_MapsEachBagToShelfArtLevel()
        {
            TestContext context = CreateContext(
                cerealBackstock: 48,
                soupBackstock: 0,
                cerealDisplayUnitsPerFrontageUnit: 3);

            try
            {
                AssignCereal(context, frontageUnitCount: 2);

                for (int unitCount = 1; unitCount <= 3; unitCount++)
                {
                    FixtureRestockResult result =
                        context.DisplayInventory.TryRestockFixture(
                            ShelfInstanceId,
                            maximumUnitCount: 1);

                    Assert.That(result.MovedUnitCount, Is.EqualTo(1));
                    Assert.That(
                        context.DisplayInventory.GetFrontageFillRatio(
                            CreateShelfRun(),
                            0),
                        Is.EqualTo(unitCount / 3f).Within(0.001f));
                    Assert.That(
                        context.DisplayInventory.GetFrontageFillRatio(
                            CreateShelfRun(),
                            1),
                        Is.Zero);
                }

                Assert.That(
                    context.DisplayInventory.TryGetSnapshot(
                        ShelfInstanceId,
                        out FixtureDisplayStockSnapshot snapshot),
                    Is.True);
                Assert.That(snapshot.CapacityUnitCount, Is.EqualTo(6));
            }
            finally
            {
                context.Dispose();
            }
        }

        [Test]
        public void ReturnFixtureStock_OneBagAtATime_RestoresBackstock()
        {
            TestContext context = CreateContext(
                cerealBackstock: 48,
                soupBackstock: 0,
                cerealDisplayUnitsPerFrontageUnit: 3);

            try
            {
                AssignCereal(context, frontageUnitCount: 1);
                context.DisplayInventory.TryRestockFixture(
                    ShelfInstanceId,
                    maximumUnitCount: 3);

                FixtureUnstockResult result =
                    context.DisplayInventory
                        .TryReturnFixtureStockToBackstock(
                            ShelfInstanceId,
                            maximumUnitCount: 1);

                Assert.That(result.Succeeded, Is.True);
                Assert.That(result.ReturnedUnitCount, Is.EqualTo(1));
                Assert.That(
                    context.DisplayInventory.GetFrontageFillRatio(
                        CreateShelfRun(),
                        0),
                    Is.EqualTo(2f / 3f).Within(0.001f));
                Assert.That(
                    context.Inventory.GetQuantity(
                        BackstockLocationId,
                        CerealProductId),
                    Is.EqualTo(46));
            }
            finally
            {
                context.Dispose();
            }
        }

        [Test]
        public void ShrinkPlanogram_ExcessDisplayStock_ReturnsToBackstock()
        {
            TestContext context = CreateContext(100, 100);

            try
            {
                AssignCereal(context, frontageUnitCount: 2);
                context.DisplayInventory.TryRestockFixture(ShelfInstanceId);

                Assert.That(
                    context.Planograms.TryResizeFacing(
                        CreateShelfRun(),
                        frontageUnitIndex: 0,
                        newFrontageUnitCount: 1,
                        out FixturePlanogramFailure failure),
                    Is.True,
                    failure.ToString());

                Assert.That(
                    context.DisplayInventory.TryGetSnapshot(
                        ShelfInstanceId,
                        out FixtureDisplayStockSnapshot snapshot),
                    Is.True);
                Assert.That(snapshot.StockedUnitCount, Is.EqualTo(6));
                Assert.That(snapshot.CapacityUnitCount, Is.EqualTo(6));
                Assert.That(snapshot.BackstockUnitCount, Is.EqualTo(94));
            }
            finally
            {
                context.Dispose();
            }
        }

        [Test]
        public void ConsumeFixtureStock_AfterRestock_CreatesVisibleShortfall()
        {
            TestContext context = CreateContext(100, 100);

            try
            {
                AssignCereal(context, frontageUnitCount: 2);
                context.DisplayInventory.TryRestockFixture(ShelfInstanceId);

                FixtureStockConsumptionResult result =
                    context.DisplayInventory.TryConsumeFixtureStock(
                        ShelfInstanceId,
                        requestedUnitCount: 1);

                Assert.That(result.Succeeded, Is.True);
                Assert.That(result.ConsumedUnitCount, Is.EqualTo(1));
                Assert.That(result.UnfulfilledUnitCount, Is.Zero);

                Assert.That(
                    context.DisplayInventory.TryGetSnapshot(
                        ShelfInstanceId,
                        out FixtureDisplayStockSnapshot snapshot),
                    Is.True);
                Assert.That(snapshot.StockedUnitCount, Is.EqualTo(11));
                Assert.That(snapshot.CapacityUnitCount, Is.EqualTo(12));
                Assert.That(snapshot.BackstockUnitCount, Is.EqualTo(88));
                Assert.That(snapshot.CanRestock, Is.True);

                Assert.That(
                    context.DisplayInventory.GetFrontageFillRatio(
                        CreateShelfRun(),
                        1),
                    Is.EqualTo(5f / 6f).Within(0.001f));

                FixtureRestockResult restock =
                    context.DisplayInventory.TryRestockFixture(
                        ShelfInstanceId);

                Assert.That(restock.MovedUnitCount, Is.EqualTo(1));
            }
            finally
            {
                context.Dispose();
            }
        }

        [Test]
        public void ConsumeFixtureStock_EmptyDisplay_DoesNotChangeBackstock()
        {
            TestContext context = CreateContext(100, 100);

            try
            {
                AssignCereal(context, frontageUnitCount: 1);

                FixtureStockConsumptionResult result =
                    context.DisplayInventory.TryConsumeFixtureStock(
                        ShelfInstanceId,
                        requestedUnitCount: 1);

                Assert.That(result.Succeeded, Is.False);
                Assert.That(
                    result.Outcome,
                    Is.EqualTo(FixtureStockConsumptionOutcome.DisplayEmpty));
                Assert.That(
                    context.Inventory.GetQuantity(
                        BackstockLocationId,
                        CerealProductId),
                    Is.EqualTo(100));
            }
            finally
            {
                context.Dispose();
            }
        }

        [Test]
        public void MoveProductToBasket_StockedFixture_TransfersOwnershipBeforeCheckout()
        {
            TestContext context = CreateContext(100, 100);

            try
            {
                AssignCereal(context, frontageUnitCount: 1);
                context.DisplayInventory.TryRestockFixture(ShelfInstanceId);

                ShoppingBasket basket = new ShoppingBasket();

                FixtureBasketPickupResult result =
                    context.DisplayInventory.TryMoveProductToBasket(
                        ShelfInstanceId,
                        CerealProductId,
                        requestedUnitCount: 1,
                        basket);

                Assert.That(result.Succeeded, Is.True);
                Assert.That(result.ProductId, Is.EqualTo(CerealProductId));
                Assert.That(result.PickedUpUnitCount, Is.EqualTo(1));
                Assert.That(basket.TotalUnitCount, Is.EqualTo(1));
                Assert.That(
                    basket.GetQuantity(
                        ShelfInstanceId,
                        CerealProductId),
                    Is.EqualTo(1));

                Assert.That(
                    context.DisplayInventory.TryGetSnapshot(
                        ShelfInstanceId,
                        out FixtureDisplayStockSnapshot snapshot),
                    Is.True);
                Assert.That(snapshot.StockedUnitCount, Is.EqualTo(5));
                Assert.That(snapshot.CapacityUnitCount, Is.EqualTo(6));
                Assert.That(snapshot.CanRestock, Is.True);
            }
            finally
            {
                context.Dispose();
            }
        }

        [Test]
        public void CompleteBasketSale_OwnedStock_CreditsCashAndClearsBasket()
        {
            TestContext context = CreateContext(100, 100);

            try
            {
                AssignCereal(context, frontageUnitCount: 1);
                context.DisplayInventory.TryRestockFixture(ShelfInstanceId);

                ShoppingBasket basket = new ShoppingBasket();
                context.DisplayInventory.TryMoveProductToBasket(
                    ShelfInstanceId,
                    CerealProductId,
                    requestedUnitCount: 1,
                    basket);

                StoreCashState cash = new StoreCashState(10000);
                FixtureSalesService sales =
                    new FixtureSalesService(
                        context.Products,
                        cash);

                FixtureInstanceId changedFixtureId = default;
                sales.SalesChanged +=
                    fixtureId => changedFixtureId = fixtureId;

                FixtureSaleResult result =
                    sales.TryCompleteBasketSale(basket);

                Assert.That(result.Succeeded, Is.True);
                Assert.That(result.UnitsSold, Is.EqualTo(1));
                Assert.That(result.RevenueCents, Is.EqualTo(349));
                Assert.That(basket.IsEmpty, Is.True);
                Assert.That(cash.BalanceCents, Is.EqualTo(10349));
                Assert.That(sales.SalesTodayCents, Is.EqualTo(349));
                Assert.That(sales.UnitsSoldToday, Is.EqualTo(1));
                Assert.That(
                    sales.GetFixtureSalesTodayCents(ShelfInstanceId),
                    Is.EqualTo(349));
                Assert.That(
                    sales.GetFixtureUnitsSoldToday(ShelfInstanceId),
                    Is.EqualTo(1));
                Assert.That(changedFixtureId, Is.EqualTo(ShelfInstanceId));
            }
            finally
            {
                context.Dispose();
            }
        }

        [Test]
        public void CompleteBasketSale_EmptyBasket_LeavesAccountingUnchanged()
        {
            TestContext context = CreateContext(100, 100);

            try
            {
                StoreCashState cash = new StoreCashState(10000);
                FixtureSalesService sales =
                    new FixtureSalesService(
                        context.Products,
                        cash);

                bool salesChanged = false;
                sales.SalesChanged += _ => salesChanged = true;

                FixtureSaleResult result =
                    sales.TryCompleteBasketSale(
                        new ShoppingBasket());

                Assert.That(result.Succeeded, Is.False);
                Assert.That(
                    result.Outcome,
                    Is.EqualTo(FixtureSaleOutcome.BasketEmpty));
                Assert.That(cash.BalanceCents, Is.EqualTo(10000));
                Assert.That(sales.SalesTodayCents, Is.Zero);
                Assert.That(sales.UnitsSoldToday, Is.Zero);
                Assert.That(salesChanged, Is.False);
            }
            finally
            {
                context.Dispose();
            }
        }

        [Test]
        public void ProcessBasket_NoPlacedCheckout_LeavesBasketAndCashUnchanged()
        {
            TestContext context = CreateContext(100, 100);

            try
            {
                AssignCereal(context, frontageUnitCount: 1);
                context.DisplayInventory.TryRestockFixture(ShelfInstanceId);

                ShoppingBasket basket = new ShoppingBasket();
                context.DisplayInventory.TryMoveProductToBasket(
                    ShelfInstanceId,
                    CerealProductId,
                    requestedUnitCount: 1,
                    basket);

                StoreCashState cash = new StoreCashState(10000);
                FixtureSalesService sales =
                    new FixtureSalesService(
                        context.Products,
                        cash);

                using FixtureCheckoutService checkout =
                    new FixtureCheckoutService(
                        context.FixtureState,
                        sales);

                FixtureSaleResult result =
                    checkout.TryProcessBasket(
                        CheckoutInstanceId,
                        basket);

                Assert.That(checkout.HasOperationalCheckout, Is.False);
                Assert.That(result.Succeeded, Is.False);
                Assert.That(
                    result.Outcome,
                    Is.EqualTo(FixtureSaleOutcome.CheckoutUnavailable));
                Assert.That(cash.BalanceCents, Is.EqualTo(10000));
                Assert.That(sales.SalesTodayCents, Is.Zero);
                Assert.That(basket.TotalUnitCount, Is.EqualTo(1));
                Assert.That(
                    context.DisplayInventory.TryGetSnapshot(
                        ShelfInstanceId,
                        out FixtureDisplayStockSnapshot snapshot),
                    Is.True);
                Assert.That(snapshot.StockedUnitCount, Is.EqualTo(5));
            }
            finally
            {
                context.Dispose();
            }
        }

        [Test]
        public void ProcessBasket_PlacedCheckout_ClearsBasketAndCreditsCash()
        {
            TestContext context = CreateContext(100, 100);

            try
            {
                AssignCereal(context, frontageUnitCount: 1);
                context.DisplayInventory.TryRestockFixture(ShelfInstanceId);

                ShoppingBasket basket = new ShoppingBasket();
                context.DisplayInventory.TryMoveProductToBasket(
                    ShelfInstanceId,
                    CerealProductId,
                    requestedUnitCount: 1,
                    basket);

                StoreCashState cash = new StoreCashState(10000);
                FixtureSalesService sales =
                    new FixtureSalesService(
                        context.Products,
                        cash);

                using FixtureCheckoutService checkout =
                    new FixtureCheckoutService(
                        context.FixtureState,
                        sales);

                FixturePlacementResult placement =
                    context.Placement.TryPlaceFixture(
                        CheckoutInstanceId,
                        CheckoutDefinitionId,
                        new GridPosition(1, 3),
                        FixtureOrientation.North);

                Assert.That(
                    placement.Succeeded,
                    Is.True,
                    placement.Failure.ToString());
                Assert.That(checkout.OperationalCheckoutCount, Is.EqualTo(1));

                FixtureSaleResult result =
                    checkout.TryProcessBasket(
                        CheckoutInstanceId,
                        basket);

                Assert.That(result.Succeeded, Is.True);
                Assert.That(result.UnitsSold, Is.EqualTo(1));
                Assert.That(cash.BalanceCents, Is.EqualTo(10349));
                Assert.That(basket.IsEmpty, Is.True);
                Assert.That(
                    context.DisplayInventory.TryGetSnapshot(
                        ShelfInstanceId,
                        out FixtureDisplayStockSnapshot snapshot),
                    Is.True);
                Assert.That(snapshot.StockedUnitCount, Is.EqualTo(5));

                FixturePlacementResult removal =
                    context.Placement.TryRemoveFixture(CheckoutInstanceId);

                Assert.That(removal.Succeeded, Is.True);
                Assert.That(checkout.HasOperationalCheckout, Is.False);
            }
            finally
            {
                context.Dispose();
            }
        }

        [Test]
        public void RemoveFixture_AllDisplayStock_ReturnsAndLocationIsRemoved()
        {
            TestContext context = CreateContext(100, 100);

            try
            {
                AssignCereal(context, frontageUnitCount: 2);
                context.DisplayInventory.TryRestockFixture(ShelfInstanceId);

                FixturePlacementResult removal =
                    context.Placement.TryRemoveFixture(ShelfInstanceId);

                Assert.That(removal.Succeeded, Is.True);
                Assert.That(
                    context.Inventory.ContainsLocation(
                        FixtureDisplayInventoryService
                            .GetDisplayLocationId(ShelfInstanceId)),
                    Is.False);
                Assert.That(
                    context.Inventory.GetQuantity(
                        BackstockLocationId,
                        CerealProductId),
                    Is.EqualTo(100));
            }
            finally
            {
                context.Dispose();
            }
        }

        [Test]
        public void RestockFixture_PhysicalBackstockRequiresPlacedStorage()
        {
            TestContext context =
                CreateContext(
                    100,
                    100,
                    usePhysicalBackstock: true);

            try
            {
                AssignCereal(context, frontageUnitCount: 2);

                Assert.That(context.Backstock.IsOperational, Is.False);
                Assert.That(context.Backstock.StoredUnitCount, Is.Zero);
                Assert.That(
                    context.Backstock.UnallocatedUnitCount,
                    Is.EqualTo(200));
                Assert.That(context.Backstock.CaseSlotCapacity, Is.Zero);

                FixtureRestockResult unavailable =
                    context.DisplayInventory.TryRestockFixture(
                        ShelfInstanceId);

                Assert.That(
                    unavailable.Outcome,
                    Is.EqualTo(
                        FixtureRestockOutcome.BackstockUnavailable));

                FixturePlacementResult placement =
                    context.Placement.TryPlaceFixture(
                        BackstockInstanceId,
                        BackstockDefinitionId,
                        new GridPosition(1, 3),
                        FixtureOrientation.North);

                Assert.That(
                    placement.Succeeded,
                    Is.True,
                    placement.Failure.ToString());
                Assert.That(context.Backstock.IsOperational, Is.True);
                Assert.That(
                    context.Backstock.CaseSlotCapacity,
                    Is.EqualTo(12));
                Assert.That(
                    context.Backstock.OccupiedCaseSlotCount,
                    Is.EqualTo(2));
                Assert.That(
                    context.Backstock.AvailableCaseSlotCount,
                    Is.EqualTo(10));
                Assert.That(
                    context.Backstock.UnallocatedUnitCount,
                    Is.Zero);
                Assert.That(
                    context.Backstock.GetRackStoredUnitCount(
                        BackstockInstanceId),
                    Is.EqualTo(200));
                Assert.That(
                    context.Inventory.GetQuantity(
                        FixtureBackstockService.GetRackLocationId(
                            BackstockInstanceId),
                        CerealProductId),
                    Is.EqualTo(100));

                FixtureRestockResult restocked =
                    context.DisplayInventory.TryRestockFixture(
                        ShelfInstanceId);

                Assert.That(restocked.Succeeded, Is.True);
                Assert.That(restocked.MovedUnitCount, Is.EqualTo(12));
                Assert.That(
                    context.Backstock.GetRackStoredUnitCount(
                        BackstockInstanceId),
                    Is.EqualTo(188));
                Assert.That(
                    context.Inventory.GetQuantity(
                        FixtureBackstockService.GetRackLocationId(
                            BackstockInstanceId),
                        CerealProductId),
                    Is.EqualTo(88));

                FixturePlacementResult removal =
                    context.Placement.TryRemoveFixture(
                        BackstockInstanceId);

                Assert.That(removal.Succeeded, Is.True);
                Assert.That(context.Backstock.StoredUnitCount, Is.Zero);
                Assert.That(
                    context.Backstock.UnallocatedUnitCount,
                    Is.EqualTo(188));
                Assert.That(
                    context.Inventory.ContainsLocation(
                        FixtureBackstockService.GetRackLocationId(
                            BackstockInstanceId)),
                    Is.False);
            }
            finally
            {
                context.Dispose();
            }
        }

        [Test]
        public void Purchasing_OrderThenReceive_DistributesDeliveryToRack()
        {
            TestContext context =
                CreateContext(
                    0,
                    0,
                    usePhysicalBackstock: true);

            try
            {
                FixturePlacementResult placement =
                    context.Placement.TryPlaceFixture(
                        BackstockInstanceId,
                        BackstockDefinitionId,
                        new GridPosition(1, 3),
                        FixtureOrientation.North);

                Assert.That(placement.Succeeded, Is.True);

                StoreCashState cash = new StoreCashState(10000);
                FixturePurchasingService purchasing =
                    new FixturePurchasingService(
                        context.Products,
                        context.Backstock,
                        cash,
                        caseUnitCount: 24);

                Assert.That(
                    purchasing.TryPlaceCaseOrder(CerealProductId),
                    Is.True);
                Assert.That(cash.BalanceCents, Is.EqualTo(7500));
                Assert.That(purchasing.PendingUnitCount, Is.EqualTo(24));
                Assert.That(context.Backstock.StoredUnitCount, Is.Zero);

                FixtureDeliveryReceipt receipt =
                    purchasing.ReceivePendingDelivery();

                Assert.That(receipt.Succeeded, Is.True);
                Assert.That(receipt.ReceivedUnitCount, Is.EqualTo(24));
                Assert.That(purchasing.PendingUnitCount, Is.Zero);
                Assert.That(
                    context.Backstock.GetRackStoredUnitCount(
                        BackstockInstanceId),
                    Is.EqualTo(24));
                Assert.That(context.Backstock.UnallocatedUnitCount, Is.Zero);
                Assert.That(cash.BalanceCents, Is.EqualTo(7500));
            }
            finally
            {
                context.Dispose();
            }
        }

        [Test]
        public void TargetedReceiving_StoresCaseOnlyInTheChosenRack()
        {
            TestContext context =
                CreateContext(
                    0,
                    0,
                    usePhysicalBackstock: true);

            try
            {
                Assert.That(
                    context.Placement.TryPlaceFixture(
                        BackstockInstanceId,
                        BackstockDefinitionId,
                        new GridPosition(0, 3),
                        FixtureOrientation.North).Succeeded,
                    Is.True);
                Assert.That(
                    context.Placement.TryPlaceFixture(
                        SecondBackstockInstanceId,
                        BackstockDefinitionId,
                        new GridPosition(3, 3),
                        FixtureOrientation.North).Succeeded,
                    Is.True);

                FixtureBackstockReceiptResult result =
                    context.Backstock.TryReceiveInboundAtRack(
                        SecondBackstockInstanceId,
                        CerealProductId,
                        unitCount: 12);

                Assert.That(result.Succeeded, Is.True);
                Assert.That(result.ReceivedUnitCount, Is.EqualTo(12));
                Assert.That(
                    context.Backstock.GetRackStoredUnitCount(
                        BackstockInstanceId),
                    Is.Zero);
                Assert.That(
                    context.Backstock.GetRackStoredUnitCount(
                        SecondBackstockInstanceId),
                    Is.EqualTo(12));
                Assert.That(context.Backstock.UnallocatedUnitCount, Is.Zero);
            }
            finally
            {
                context.Dispose();
            }
        }

        [Test]
        public void TargetedReceiving_TwoReceiptsRemainTwoPhysicalCases()
        {
            TestContext context =
                CreateContext(
                    0,
                    0,
                    usePhysicalBackstock: true);

            try
            {
                Assert.That(
                    context.Placement.TryPlaceFixture(
                        BackstockInstanceId,
                        BackstockDefinitionId,
                        new GridPosition(1, 3),
                        FixtureOrientation.North).Succeeded,
                    Is.True);

                Assert.That(
                    context.Backstock.TryReceiveInboundAtRack(
                        BackstockInstanceId,
                        CerealProductId,
                        unitCount: 12).Succeeded,
                    Is.True);
                Assert.That(
                    context.Backstock.TryReceiveInboundAtRack(
                        BackstockInstanceId,
                        CerealProductId,
                        unitCount: 12).Succeeded,
                    Is.True);

                List<FixtureBackstockCaseSnapshot> storedCases =
                    new List<FixtureBackstockCaseSnapshot>(
                        context.Backstock.EnumerateRackCases(
                            BackstockInstanceId));

                Assert.That(storedCases, Has.Count.EqualTo(2));
                Assert.That(
                    storedCases[0].ProductId,
                    Is.EqualTo(CerealProductId));
                Assert.That(storedCases[0].RemainingUnitCount, Is.EqualTo(12));
                Assert.That(storedCases[1].RemainingUnitCount, Is.EqualTo(12));
                Assert.That(
                    context.Backstock.GetRackStoredUnitCount(
                        BackstockInstanceId),
                    Is.EqualTo(24));
            }
            finally
            {
                context.Dispose();
            }
        }

        [Test]
        public void TargetedReceiving_FilledPhysicalCaseSlotsRejectAnotherCase()
        {
            TestContext context =
                CreateContext(
                    0,
                    0,
                    usePhysicalBackstock: true,
                    backstockCaseSlotCapacity: 2);

            try
            {
                Assert.That(
                    context.Placement.TryPlaceFixture(
                        BackstockInstanceId,
                        BackstockDefinitionId,
                        new GridPosition(1, 3),
                        FixtureOrientation.North).Succeeded,
                    Is.True);

                Assert.That(
                    context.Backstock.TryReceiveInboundAtRack(
                        BackstockInstanceId,
                        CerealProductId,
                        unitCount: 12).Succeeded,
                    Is.True);
                Assert.That(
                    context.Backstock.TryReceiveInboundAtRack(
                        BackstockInstanceId,
                        CerealProductId,
                        unitCount: 12).Succeeded,
                    Is.True);

                FixtureBackstockReceiptResult rejected =
                    context.Backstock.TryReceiveInboundAtRack(
                        BackstockInstanceId,
                        CerealProductId,
                        unitCount: 12);

                Assert.That(rejected.Succeeded, Is.False);
                Assert.That(
                    rejected.Failure,
                    Is.EqualTo(
                        FixtureBackstockReceiptFailure
                            .NoAvailableCaseSlot));
                Assert.That(
                    context.Backstock.GetRackCaseSlotCapacity(
                        BackstockInstanceId),
                    Is.EqualTo(2));
                Assert.That(
                    context.Backstock.GetRackOccupiedCaseSlotCount(
                        BackstockInstanceId),
                    Is.EqualTo(2));
                Assert.That(
                    context.Backstock.GetRackStoredUnitCount(
                        BackstockInstanceId),
                    Is.EqualTo(24));
            }
            finally
            {
                context.Dispose();
            }
        }

        [Test]
        public void RestockingFromTargetedCase_PreservesThenRemovesItsHandlingUnit()
        {
            TestContext context =
                CreateContext(
                    0,
                    0,
                    usePhysicalBackstock: true);

            try
            {
                Assert.That(
                    context.Placement.TryPlaceFixture(
                        BackstockInstanceId,
                        BackstockDefinitionId,
                        new GridPosition(1, 3),
                        FixtureOrientation.North).Succeeded,
                    Is.True);
                AssignCereal(context, frontageUnitCount: 1);

                Assert.That(
                    context.Backstock.TryReceiveInboundAtRack(
                        BackstockInstanceId,
                        CerealProductId,
                        unitCount: 12).Succeeded,
                    Is.True);

                FixtureRestockResult firstRestock =
                    context.DisplayInventory.TryRestockFixture(
                        ShelfInstanceId);
                List<FixtureBackstockCaseSnapshot> afterFirstRestock =
                    new List<FixtureBackstockCaseSnapshot>(
                        context.Backstock.EnumerateRackCases(
                            BackstockInstanceId));

                Assert.That(firstRestock.MovedUnitCount, Is.EqualTo(6));
                Assert.That(afterFirstRestock, Has.Count.EqualTo(1));
                Assert.That(
                    afterFirstRestock[0].RemainingUnitCount,
                    Is.EqualTo(6));
                Assert.That(
                    afterFirstRestock[0].CapacityUnitCount,
                    Is.EqualTo(12));
                Assert.That(
                    afterFirstRestock[0].AvailableUnitCount,
                    Is.EqualTo(6));

                Assert.That(
                    context.DisplayInventory.TryConsumeFixtureStock(
                        ShelfInstanceId,
                        requestedUnitCount: 6).ConsumedUnitCount,
                    Is.EqualTo(6));
                Assert.That(
                    context.DisplayInventory.TryRestockFixture(
                        ShelfInstanceId).MovedUnitCount,
                    Is.EqualTo(6));
                Assert.That(
                    new List<FixtureBackstockCaseSnapshot>(
                        context.Backstock.EnumerateRackCases(
                            BackstockInstanceId)),
                    Is.Empty);
            }
            finally
            {
                context.Dispose();
            }
        }

        [Test]
        public void ReturningStock_RefillsTheOpenedCaseUpToItsOwnLimit()
        {
            TestContext context =
                CreateContext(
                    0,
                    0,
                    usePhysicalBackstock: true);

            try
            {
                Assert.That(
                    context.Placement.TryPlaceFixture(
                        BackstockInstanceId,
                        BackstockDefinitionId,
                        new GridPosition(1, 3),
                        FixtureOrientation.North).Succeeded,
                    Is.True);
                AssignCereal(context, frontageUnitCount: 1);

                Assert.That(
                    context.Backstock.TryReceiveInboundAtRack(
                        BackstockInstanceId,
                        CerealProductId,
                        unitCount: 12).Succeeded,
                    Is.True);
                Assert.That(
                    context.DisplayInventory.TryRestockFixture(
                        ShelfInstanceId).MovedUnitCount,
                    Is.EqualTo(6));

                int returnedUnitCount =
                    context.Backstock.StoreFromLocation(
                        FixtureDisplayInventoryService
                            .GetDisplayLocationId(ShelfInstanceId),
                        CerealProductId,
                        requestedUnitCount: 6);
                List<FixtureBackstockCaseSnapshot> storedCases =
                    new List<FixtureBackstockCaseSnapshot>(
                        context.Backstock.EnumerateRackCases(
                            BackstockInstanceId));

                Assert.That(returnedUnitCount, Is.EqualTo(6));
                Assert.That(storedCases, Has.Count.EqualTo(1));
                Assert.That(storedCases[0].RemainingUnitCount, Is.EqualTo(12));
                Assert.That(storedCases[0].CapacityUnitCount, Is.EqualTo(12));
                Assert.That(storedCases[0].AvailableUnitCount, Is.Zero);
                Assert.That(
                    context.Backstock.GetRackStoredUnitCount(
                        BackstockInstanceId),
                    Is.EqualTo(12));
            }
            finally
            {
                context.Dispose();
            }
        }

        [Test]
        public void WorkerCaseTrip_PreservesCaseBoundaryAndInventoryAtEachBeat()
        {
            TestContext context =
                CreateContext(
                    0,
                    0,
                    usePhysicalBackstock: true,
                    cerealDisplayUnitsPerFrontageUnit: 3);
            StorageLocationId workerCarryLocationId =
                new StorageLocationId("WORKER-CARRY-TEST");

            try
            {
                Assert.That(
                    context.Inventory.TryRegisterLocation(
                        new StorageLocationDefinition(
                            workerCarryLocationId,
                            "Worker carried case",
                            StorageRole.Backroom)),
                    Is.True);
                Assert.That(
                    context.Placement.TryPlaceFixture(
                        BackstockInstanceId,
                        BackstockDefinitionId,
                        new GridPosition(1, 3),
                        FixtureOrientation.North).Succeeded,
                    Is.True);
                AssignCereal(context, frontageUnitCount: 1);
                Assert.That(
                    context.Backstock.TryReceiveInboundAtRack(
                        BackstockInstanceId,
                        CerealProductId,
                        unitCount: 12).Succeeded,
                    Is.True);

                Assert.That(
                    context.Backstock.TryFindRackCase(
                        CerealProductId,
                        out FixtureInstanceId sourceRackId,
                        out FixtureBackstockCaseSnapshot foundCase),
                    Is.True);
                Assert.That(sourceRackId, Is.EqualTo(BackstockInstanceId));
                Assert.That(foundCase.RemainingUnitCount, Is.EqualTo(12));

                FixtureBackstockCasePickupResult pickup =
                    context.Backstock.TryTakeCase(
                        sourceRackId,
                        CerealProductId,
                        workerCarryLocationId);

                Assert.That(pickup.Succeeded, Is.True);
                Assert.That(
                    context.Backstock.GetRackOccupiedCaseSlotCount(
                        BackstockInstanceId),
                    Is.Zero);
                Assert.That(
                    context.Inventory.GetQuantity(
                        workerCarryLocationId,
                        CerealProductId),
                    Is.EqualTo(12));

                FixtureRestockResult stocked =
                    context.DisplayInventory.TryRestockFixtureFromLocation(
                        ShelfInstanceId,
                        workerCarryLocationId,
                        maximumUnitCount: 3);

                Assert.That(stocked.Succeeded, Is.True);
                Assert.That(stocked.MovedUnitCount, Is.EqualTo(3));
                Assert.That(
                    context.Inventory.GetQuantity(
                        workerCarryLocationId,
                        CerealProductId),
                    Is.EqualTo(9));
                Assert.That(
                    context.DisplayInventory.GetDisplayedQuantity(
                        CerealProductId),
                    Is.EqualTo(3));

                FixtureBackstockCaseReturnResult returned =
                    context.Backstock.TryReturnCase(
                        sourceRackId,
                        workerCarryLocationId,
                        new FixtureBackstockCaseSnapshot(
                            CerealProductId,
                            remainingUnitCount: 9,
                            capacityUnitCount: 12));
                List<FixtureBackstockCaseSnapshot> storedCases =
                    new List<FixtureBackstockCaseSnapshot>(
                        context.Backstock.EnumerateRackCases(
                            BackstockInstanceId));

                Assert.That(returned.Succeeded, Is.True);
                Assert.That(returned.WasStoredOnRack, Is.True);
                Assert.That(storedCases, Has.Count.EqualTo(1));
                Assert.That(storedCases[0].RemainingUnitCount, Is.EqualTo(9));
                Assert.That(storedCases[0].CapacityUnitCount, Is.EqualTo(12));
                Assert.That(
                    context.Inventory.GetQuantity(
                        workerCarryLocationId,
                        CerealProductId),
                    Is.Zero);
            }
            finally
            {
                context.Dispose();
            }
        }

        [Test]
        public void TargetedReceiving_LargeCaseUsesOneSlotWithoutUnitCeiling()
        {
            TestContext context =
                CreateContext(
                    0,
                    0,
                    usePhysicalBackstock: true);

            try
            {
                Assert.That(
                    context.Placement.TryPlaceFixture(
                        BackstockInstanceId,
                        BackstockDefinitionId,
                        new GridPosition(1, 3),
                        FixtureOrientation.North).Succeeded,
                    Is.True);

                FixtureBackstockReceiptResult result =
                    context.Backstock.TryReceiveInboundAtRack(
                        BackstockInstanceId,
                        CerealProductId,
                        unitCount: 481);

                Assert.That(result.Succeeded, Is.True);
                Assert.That(result.ReceivedUnitCount, Is.EqualTo(481));
                Assert.That(context.Backstock.StoredUnitCount, Is.EqualTo(481));
                Assert.That(
                    context.Backstock.GetRackOccupiedCaseSlotCount(
                        BackstockInstanceId),
                    Is.EqualTo(1));
                Assert.That(context.Backstock.UnallocatedUnitCount, Is.Zero);
            }
            finally
            {
                context.Dispose();
            }
        }

        [Test]
        public void Purchasing_OrderCostsMoreThanAvailableCash_IsRejected()
        {
            TestContext context =
                CreateContext(
                    0,
                    0,
                    usePhysicalBackstock: true);

            try
            {
                StoreCashState cash = new StoreCashState(2499);
                FixturePurchasingService purchasing =
                    new FixturePurchasingService(
                        context.Products,
                        context.Backstock,
                        cash,
                        caseUnitCount: 24);

                bool succeeded =
                    purchasing.TryPlaceCaseOrder(
                        CerealProductId,
                        out FixturePurchaseFailure failure);

                Assert.That(succeeded, Is.False);
                Assert.That(
                    failure,
                    Is.EqualTo(FixturePurchaseFailure.InsufficientFunds));
                Assert.That(cash.BalanceCents, Is.EqualTo(2499));
                Assert.That(purchasing.PendingUnitCount, Is.Zero);
            }
            finally
            {
                context.Dispose();
            }
        }

        [Test]
        public void Purchasing_ReceiveWithoutRack_LeavesDeliveryInbound()
        {
            TestContext context =
                CreateContext(
                    0,
                    0,
                    usePhysicalBackstock: true);

            try
            {
                FixturePurchasingService purchasing =
                    new FixturePurchasingService(
                        context.Products,
                        context.Backstock,
                        new StoreCashState(10000),
                        caseUnitCount: 24);

                purchasing.TryPlaceCaseOrder(SoupProductId);

                FixtureDeliveryReceipt receipt =
                    purchasing.ReceivePendingDelivery();

                Assert.That(receipt.Succeeded, Is.True);
                Assert.That(context.Backstock.StoredUnitCount, Is.Zero);
                Assert.That(
                    context.Backstock.UnallocatedUnitCount,
                    Is.EqualTo(24));
            }
            finally
            {
                context.Dispose();
            }
        }


        private static void AssignCereal(
            TestContext context,
            int frontageUnitCount)
        {
            Assert.That(
                context.Planograms.TryAssignFrontage(
                    CreateShelfRun(),
                    startFrontageUnit: 0,
                    frontageUnitCount: frontageUnitCount,
                    productId: CerealProductId,
                    out FixturePlanogramFailure failure),
                Is.True,
                failure.ToString());
        }

        private static FixtureShelfRunKey CreateShelfRun()
        {
            return new FixtureShelfRunKey(
                ShelfInstanceId,
                FixtureSide.South,
                shelfRunIndex: 0);
        }

        private static TestContext CreateContext(
            int cerealBackstock,
            int soupBackstock,
            bool usePhysicalBackstock = false,
            int backstockCaseSlotCapacity = 12,
            int cerealDisplayUnitsPerFrontageUnit =
                ProductDefinition.DefaultDisplayUnitsPerFrontageUnit)
        {
            HashSet<GridPosition> cells =
                new HashSet<GridPosition>();

            for (int x = 0; x < 5; x++)
            {
                for (int y = 0; y < 5; y++)
                {
                    cells.Add(new GridPosition(x, y));
                }
            }

            GridMapDefinition map =
                new GridMapDefinition(
                    "fixture-display-inventory-test",
                    cells);

            FixtureDefinition definition =
                new FixtureDefinition(
                    ShelfDefinitionId,
                    "Half Shelf",
                    2,
                    1,
                    new FixtureAccessProfile(
                        FixtureAccessMode.None,
                        FixtureAccessMode.None,
                        FixtureAccessMode.CustomerBrowse,
                        FixtureAccessMode.None));

            FixtureDefinition backstockDefinition =
                new FixtureDefinition(
                    BackstockDefinitionId,
                    "Backstock Shelf",
                    2,
                    1,
                    new FixtureAccessProfile(
                        FixtureAccessMode.None,
                        FixtureAccessMode.None,
                        FixtureAccessMode.EmployeeStock,
                        FixtureAccessMode.None),
                    storageProfile:
                        new FixtureStorageProfile(
                            backstockCaseSlotCapacity));

            FixtureDefinition checkoutDefinition =
                new FixtureDefinition(
                    CheckoutDefinitionId,
                    "Basic Checkout Counter",
                    2,
                    1,
                    new FixtureAccessProfile(
                        FixtureAccessMode.EmployeeCheckout,
                        FixtureAccessMode.None,
                        FixtureAccessMode.CustomerCheckout,
                        FixtureAccessMode.None));

            FixtureState fixtureState = new FixtureState();

            FixturePlacementService placement =
                new FixturePlacementService(
                    map,
                    new ConstructionAreaDefinition(map, cells),
                    new FixtureDefinitionCatalog(
                        new[]
                        {
                            definition,
                            backstockDefinition,
                            checkoutDefinition
                        }),
                    fixtureState,
                    new TestSurfaceQuery(cells));

            FixturePlacementResult placementResult =
                placement.TryPlaceFixture(
                    ShelfInstanceId,
                    ShelfDefinitionId,
                    new GridPosition(1, 1),
                    FixtureOrientation.North);

            Assert.That(
                placementResult.Succeeded,
                Is.True,
                placementResult.Failure.ToString());

            ProductCatalog products =
                new ProductCatalog(
                    new[]
                    {
                        CreateProduct(
                            CerealProductId,
                            "Cereal",
                            cerealDisplayUnitsPerFrontageUnit),
                        CreateProduct(SoupProductId, "Soup")
                    });

            FixturePlanogramService planograms =
                new FixturePlanogramService(
                    fixtureState,
                    products);

            InventoryState inventory =
                new InventoryState(
                    products,
                    new[]
                    {
                        new StorageLocationDefinition(
                            BackstockLocationId,
                            "Backstock",
                            StorageRole.Backroom)
                    },
                    new[]
                    {
                        new StockBalance(
                            BackstockLocationId,
                            CerealProductId,
                            cerealBackstock),
                        new StockBalance(
                            BackstockLocationId,
                            SoupProductId,
                            soupBackstock)
                    });

            FixtureBackstockService backstock =
                usePhysicalBackstock
                    ? new FixtureBackstockService(
                        fixtureState,
                        products,
                        inventory,
                        BackstockLocationId)
                    : null;

            FixtureDisplayInventoryService displayInventory =
                usePhysicalBackstock
                    ? new FixtureDisplayInventoryService(
                        fixtureState,
                        planograms.State,
                        products,
                        inventory,
                        backstock)
                    : new FixtureDisplayInventoryService(
                        fixtureState,
                        planograms.State,
                        products,
                        inventory,
                        BackstockLocationId);

            return new TestContext(
                placement,
                fixtureState,
                products,
                planograms,
                inventory,
                backstock,
                displayInventory);
        }

        private static ProductDefinition CreateProduct(
            ProductId productId,
            string displayName,
            int displayUnitsPerFrontageUnit =
                ProductDefinition.DefaultDisplayUnitsPerFrontageUnit)
        {
            return new ProductDefinition(
                productId,
                displayName,
                new ProductCategoryId("GROCERY"),
                StockUnit.Each,
                wholesaleCaseCostCents: 2500,
                retailUnitPriceCents: 349,
                displayUnitsPerFrontageUnit:
                    displayUnitsPerFrontageUnit);
        }


        private sealed class TestSurfaceQuery :
            IFixturePlacementSurfaceQuery
        {
            private readonly HashSet<GridPosition> floorCells;


            public TestSurfaceQuery(
                IEnumerable<GridPosition> floorCells)
            {
                this.floorCells =
                    new HashSet<GridPosition>(floorCells);
            }


            public bool HasFloor(GridPosition cell)
            {
                return floorCells.Contains(cell);
            }

            public bool HasWall(CellEdge edge)
            {
                return false;
            }

            public bool IsReservedForDoorPassage(GridPosition cell)
            {
                return false;
            }
        }


        private sealed class TestContext
        {
            public TestContext(
                FixturePlacementService placement,
                FixtureState fixtureState,
                ProductCatalog products,
                FixturePlanogramService planograms,
                InventoryState inventory,
                FixtureBackstockService backstock,
                FixtureDisplayInventoryService displayInventory)
            {
                Placement = placement;
                FixtureState = fixtureState;
                Products = products;
                Planograms = planograms;
                Inventory = inventory;
                Backstock = backstock;
                DisplayInventory = displayInventory;
            }


            public FixturePlacementService Placement { get; }

            public FixtureState FixtureState { get; }

            public ProductCatalog Products { get; }

            public FixturePlanogramService Planograms { get; }

            public InventoryState Inventory { get; }

            public FixtureBackstockService Backstock { get; }

            public FixtureDisplayInventoryService DisplayInventory { get; }


            public void Dispose()
            {
                DisplayInventory.Dispose();
                Backstock?.Dispose();
                Planograms.Dispose();
            }
        }
    }
}
