using System.Collections.Generic;
using System.Linq;
using BigRetail.Inventory.Domain;
using BigRetail.Map.Construction;
using BigRetail.Map.Domain;
using BigRetail.Map.Fixtures;
using BigRetail.Merchandise.Domain;
using BigRetail.Purchasing.Domain;
using NUnit.Framework;

namespace BigRetail.Purchasing.Unity.Tests
{
    public sealed class SupplierCaseStockingServiceTests
    {
        private static readonly ProductId ProductId =
            new ProductId("RIDGEWAY-CHIPS");

        private static readonly FixtureDefinitionId RackDefinitionId =
            new FixtureDefinitionId("BACKSTOCK-RACK");

        private static readonly FixtureInstanceId FirstRackId =
            new FixtureInstanceId("RACK-ONE");

        private static readonly FixtureInstanceId SecondRackId =
            new FixtureInstanceId("RACK-TWO");

        private static readonly StorageLocationId InboundLocationId =
            new StorageLocationId("INBOUND");


        [Test]
        public void StockCase_PlayerAndWorkerCommandTargetsChosenRackOneCaseAtATime()
        {
            TestContext context = CreateContext();

            try
            {
                Assert.That(
                    context.Stocking.TryGetNextCase(
                        context.OrderNumber,
                        out InboundPurchasePack firstCase),
                    Is.True);

                SupplierCaseStockingResult firstResult =
                    context.Stocking.TryStockCase(
                        firstCase,
                        SecondRackId);

                Assert.That(firstResult.Succeeded, Is.True);
                Assert.That(firstResult.ReceivedUnitCount, Is.EqualTo(12));
                Assert.That(firstResult.CompletedOrderCount, Is.Zero);
                Assert.That(
                    context.Backstock.GetRackStoredUnitCount(FirstRackId),
                    Is.Zero);
                Assert.That(
                    context.Backstock.GetRackStoredUnitCount(SecondRackId),
                    Is.EqualTo(12));
                Assert.That(
                    context.Fulfillment.EnumerateReadyDeliveries()
                        .Single().PurchasePackCount,
                    Is.EqualTo(1));

                context.Stocking.TryGetNextCase(
                    context.OrderNumber,
                    out InboundPurchasePack secondCase);
                SupplierCaseStockingResult secondResult =
                    context.Stocking.TryStockCase(
                        secondCase,
                        FirstRackId);

                Assert.That(secondResult.Succeeded, Is.True);
                Assert.That(secondResult.CompletedOrderCount, Is.EqualTo(1));
                Assert.That(
                    context.Backstock.GetRackStoredUnitCount(FirstRackId),
                    Is.EqualTo(12));
                Assert.That(
                    context.Backstock.GetRackStoredUnitCount(SecondRackId),
                    Is.EqualTo(12));
                Assert.That(
                    context.Fulfillment.ReceivedOrderCount,
                    Is.EqualTo(1));
            }
            finally
            {
                context.Dispose();
            }
        }

        [Test]
        public void StockCase_CaseUnitsDoNotCreateASeparateRackLimit()
        {
            TestContext context = CreateContext(caseSlotCapacity: 1);

            try
            {
                context.Stocking.TryGetNextCase(
                    context.OrderNumber,
                    out InboundPurchasePack supplierCase);

                SupplierCaseStockingResult result =
                    context.Stocking.TryStockCase(
                        supplierCase,
                        FirstRackId);

                Assert.That(result.Succeeded, Is.True);
                Assert.That(result.ReceivedUnitCount, Is.EqualTo(12));
                Assert.That(context.Backstock.StoredUnitCount, Is.EqualTo(12));
                Assert.That(
                    context.Backstock.GetRackOccupiedCaseSlotCount(
                        FirstRackId),
                    Is.EqualTo(1));
                Assert.That(
                    context.Fulfillment.EnumerateReadyDeliveries()
                        .Single().PurchasePackCount,
                    Is.EqualTo(1));
            }
            finally
            {
                context.Dispose();
            }
        }

        [Test]
        public void StockCase_FilledPhysicalSlotsLeaveSupplierCaseOnPallet()
        {
            TestContext context = CreateContext(caseSlotCapacity: 1);

            try
            {
                context.Stocking.TryGetNextCase(
                    context.OrderNumber,
                    out InboundPurchasePack firstCase);

                Assert.That(
                    context.Stocking.TryStockCase(
                        firstCase,
                        FirstRackId).Succeeded,
                    Is.True);

                context.Stocking.TryGetNextCase(
                    context.OrderNumber,
                    out InboundPurchasePack secondCase);
                SupplierCaseStockingResult result =
                    context.Stocking.TryStockCase(
                        secondCase,
                        FirstRackId);

                Assert.That(result.Succeeded, Is.False);
                Assert.That(
                    result.Failure,
                    Is.EqualTo(
                        SupplierCaseStockingFailure
                            .NoAvailableRackCaseSlot));
                Assert.That(
                    context.Backstock.GetRackOccupiedCaseSlotCount(
                        FirstRackId),
                    Is.EqualTo(1));
                Assert.That(
                    context.Fulfillment.EnumerateReadyDeliveries()
                        .Single().PurchasePackCount,
                    Is.EqualTo(1));
            }
            finally
            {
                context.Dispose();
            }
        }


        private static TestContext CreateContext(
            int caseSlotCapacity = 4)
        {
            HashSet<GridPosition> cells = new HashSet<GridPosition>();

            for (int x = 0; x < 5; x++)
            {
                for (int y = 0; y < 5; y++)
                {
                    cells.Add(new GridPosition(x, y));
                }
            }

            GridMapDefinition map = new GridMapDefinition(
                "supplier-case-stocking.test",
                cells);
            FixtureDefinition rackDefinition =
                new FixtureDefinition(
                    RackDefinitionId,
                    "Backstock Rack",
                    1,
                    1,
                    new FixtureAccessProfile(
                        FixtureAccessMode.None,
                        FixtureAccessMode.None,
                        FixtureAccessMode.EmployeeStock,
                        FixtureAccessMode.None),
                    storageProfile:
                        new FixtureStorageProfile(
                            caseSlotCapacity));
            FixtureState fixtureState = new FixtureState();
            FixturePlacementService placement =
                new FixturePlacementService(
                    map,
                    new ConstructionAreaDefinition(map, cells),
                    new FixtureDefinitionCatalog(
                        new[] { rackDefinition }),
                    fixtureState,
                    new TestSurfaceQuery(cells));

            Assert.That(
                placement.TryPlaceFixture(
                    FirstRackId,
                    RackDefinitionId,
                    new GridPosition(1, 3),
                    FixtureOrientation.North).Succeeded,
                Is.True);
            Assert.That(
                placement.TryPlaceFixture(
                    SecondRackId,
                    RackDefinitionId,
                    new GridPosition(3, 3),
                    FixtureOrientation.North).Succeeded,
                Is.True);

            ProductDefinition product =
                new ProductDefinition(
                    ProductId,
                    "Ridgeway Chips",
                    BrandId.Unbranded,
                    "Ridgeway",
                    new ProductCategoryId("SNACKS"),
                    MarketPosition.Standard,
                    "Bag",
                    StockUnit.Each);
            ProductCatalog products =
                new ProductCatalog(new[] { product });
            InventoryState inventory =
                new InventoryState(
                    products,
                    new[]
                    {
                        new StorageLocationDefinition(
                            InboundLocationId,
                            "Inbound",
                            StorageRole.Backroom)
                    });
            FixtureBackstockService backstock =
                new FixtureBackstockService(
                    fixtureState,
                    products,
                    inventory,
                    InboundLocationId);

            SupplierDefinition supplier =
                new SupplierDefinition(
                    new SupplierId("BIG"),
                    "BIG Wholesale",
                    "Broadline",
                    minimumOrderCents: 0,
                    SupplierDeliveryRule.SameDay(1));
            SupplierOfferDefinition offer =
                new SupplierOfferDefinition(
                    new SupplierOfferId("BIG-RIDGEWAY"),
                    supplier.Id,
                    product.Id,
                    purchasePackQuantity: 12,
                    packPriceCents: 1200,
                    isAvailable: true);
            CommercialCatalog commercialCatalog =
                new CommercialCatalog(
                    new BrandCatalog(
                        new[]
                        {
                            new BrandDefinition(
                                BrandId.Unbranded,
                                "Unbranded")
                        }),
                    products,
                    new SupplierCatalog(new[] { supplier }),
                    new SupplierOfferCatalog(new[] { offer }));
            PurchasingService purchasing =
                new PurchasingService(commercialCatalog);
            purchasing.SetPurchasePackCount(offer.Id, 2);
            IReadOnlyList<PlacedPurchaseOrder> orders =
                purchasing.PlaceDrafts(
                    new CommercialTime(0, 8, 0));
            PurchaseOrderFulfillmentService fulfillment =
                new PurchaseOrderFulfillmentService(
                    new FixtureBackstockPurchaseOrderReceiver(
                        backstock));
            fulfillment.Schedule(orders);
            fulfillment.AdvanceTo(new CommercialTime(0, 9, 0));
            SupplierCaseStockingService stocking =
                new SupplierCaseStockingService(
                    fulfillment,
                    backstock);

            return new TestContext(
                orders[0].OrderNumber,
                fulfillment,
                backstock,
                stocking);
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


        private sealed class TestContext : System.IDisposable
        {
            public TestContext(
                long orderNumber,
                PurchaseOrderFulfillmentService fulfillment,
                FixtureBackstockService backstock,
                SupplierCaseStockingService stocking)
            {
                OrderNumber = orderNumber;
                Fulfillment = fulfillment;
                Backstock = backstock;
                Stocking = stocking;
            }


            public long OrderNumber { get; }

            public PurchaseOrderFulfillmentService Fulfillment { get; }

            public FixtureBackstockService Backstock { get; }

            public SupplierCaseStockingService Stocking { get; }


            public void Dispose()
            {
                Backstock.Dispose();
            }
        }
    }
}
