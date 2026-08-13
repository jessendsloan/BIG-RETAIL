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
                Assert.That(context.Backstock.CapacityUnitCount, Is.Zero);

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
                    context.Backstock.CapacityUnitCount,
                    Is.EqualTo(480));
                Assert.That(
                    context.Backstock.AvailableCapacityUnitCount,
                    Is.EqualTo(280));
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
            bool usePhysicalBackstock = false)
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
                        new FixtureStorageProfile(480));

            FixtureState fixtureState = new FixtureState();

            FixturePlacementService placement =
                new FixturePlacementService(
                    map,
                    new ConstructionAreaDefinition(map, cells),
                    new FixtureDefinitionCatalog(
                        new[]
                        {
                            definition,
                            backstockDefinition
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
                        CreateProduct(CerealProductId, "Cereal"),
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
                products,
                planograms,
                inventory,
                backstock,
                displayInventory);
        }

        private static ProductDefinition CreateProduct(
            ProductId productId,
            string displayName)
        {
            return new ProductDefinition(
                productId,
                displayName,
                new ProductCategoryId("GROCERY"),
                StockUnit.Each,
                wholesaleCaseCostCents: 2500);
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
                ProductCatalog products,
                FixturePlanogramService planograms,
                InventoryState inventory,
                FixtureBackstockService backstock,
                FixtureDisplayInventoryService displayInventory)
            {
                Placement = placement;
                Products = products;
                Planograms = planograms;
                Inventory = inventory;
                Backstock = backstock;
                DisplayInventory = displayInventory;
            }


            public FixturePlacementService Placement { get; }

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
