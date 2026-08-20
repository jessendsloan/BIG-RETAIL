using System;
using System.Collections.Generic;
using System.Linq;
using BigRetail.Merchandise.Domain;
using NUnit.Framework;

namespace BigRetail.Purchasing.Domain.Tests
{
    public sealed class PurchaseOrderFulfillmentServiceTests
    {
        [Test]
        public void ScheduledOrder_BecomesReceivableOnlyAtArrival()
        {
            TestContext context = CreateContext();
            TestReceiver receiver = new TestReceiver();
            PurchaseOrderFulfillmentService fulfillment =
                new PurchaseOrderFulfillmentService(receiver);
            fulfillment.Schedule(context.Orders);

            fulfillment.AdvanceTo(new CommercialTime(0, 11, 59));

            Assert.That(fulfillment.ScheduledOrderCount, Is.EqualTo(1));
            Assert.That(fulfillment.HasAvailableDeliveries, Is.False);

            fulfillment.AdvanceTo(new CommercialTime(0, 12, 0));

            Assert.That(fulfillment.ScheduledOrderCount, Is.Zero);
            Assert.That(fulfillment.ReadyToReceiveOrderCount, Is.EqualTo(1));
            Assert.That(fulfillment.ReadyToReceiveUnitCount, Is.EqualTo(24));
        }

        [Test]
        public void ReceiveAvailableDelivery_ForwardsUnitsAndCompletesOrder()
        {
            TestContext context = CreateContext();
            TestReceiver receiver = new TestReceiver();
            PurchaseOrderFulfillmentService fulfillment =
                new PurchaseOrderFulfillmentService(receiver);
            fulfillment.Schedule(context.Orders);
            fulfillment.AdvanceTo(new CommercialTime(0, 12, 0));

            PurchaseOrderReceivingResult result =
                fulfillment.ReceiveAvailableDeliveries();

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.ReceivedUnitCount, Is.EqualTo(24));
            Assert.That(result.CompletedOrderCount, Is.EqualTo(1));
            Assert.That(receiver.ReceivedUnits[context.ProductId], Is.EqualTo(24));
            Assert.That(fulfillment.ReadyToReceiveOrderCount, Is.Zero);
            Assert.That(fulfillment.ReceivedOrderCount, Is.EqualTo(1));
        }

        [Test]
        public void FailedReceivingLine_RemainsAvailableWithoutDuplicatingSuccess()
        {
            TestContext context = CreateContext();
            TestReceiver receiver = new TestReceiver
            {
                RejectNextReceipt = true
            };
            PurchaseOrderFulfillmentService fulfillment =
                new PurchaseOrderFulfillmentService(receiver);
            fulfillment.Schedule(context.Orders);
            fulfillment.AdvanceTo(new CommercialTime(0, 12, 0));

            PurchaseOrderReceivingResult failed =
                fulfillment.ReceiveAvailableDeliveries();
            PurchaseOrderReceivingResult recovered =
                fulfillment.ReceiveAvailableDeliveries();

            Assert.That(failed.ReceivedUnitCount, Is.Zero);
            Assert.That(failed.FailedUnitCount, Is.EqualTo(24));
            Assert.That(recovered.ReceivedUnitCount, Is.EqualTo(24));
            Assert.That(receiver.ReceivedUnits[context.ProductId], Is.EqualTo(24));
        }

        [Test]
        public void ReadyDelivery_ExposesExactManifestAndCompressedBoxTier()
        {
            TestContext context = CreateContext();
            PurchaseOrderFulfillmentService fulfillment =
                new PurchaseOrderFulfillmentService(new TestReceiver());
            fulfillment.Schedule(context.Orders);
            fulfillment.AdvanceTo(new CommercialTime(0, 12, 0));

            InboundDeliveryLoad load =
                fulfillment.EnumerateReadyDeliveries().Single();

            Assert.That(load.OrderNumber, Is.EqualTo(1));
            Assert.That(load.SupplierId, Is.EqualTo(new SupplierId("BIG")));
            Assert.That(load.Lines, Has.Count.EqualTo(1));
            Assert.That(load.PurchasePackCount, Is.EqualTo(2));
            Assert.That(load.RemainingUnitCount, Is.EqualTo(24));
            Assert.That(load.VisibleBoxCount, Is.EqualTo(1));
        }

        [TestCase(1, 1)]
        [TestCase(3, 1)]
        [TestCase(4, 2)]
        [TestCase(7, 2)]
        [TestCase(8, 3)]
        [TestCase(11, 3)]
        [TestCase(12, 4)]
        [TestCase(40, 4)]
        public void VisibleBoxCount_UsesFourOrderSizeTiers(
            int caseCount,
            int expectedBoxCount)
        {
            Assert.That(
                InboundDeliveryLoad.ResolveVisibleBoxCount(caseCount),
                Is.EqualTo(expectedBoxCount));
        }

        [Test]
        public void ReceiveDelivery_CompletesOnlyTheRequestedPallet()
        {
            IReadOnlyList<PlacedPurchaseOrder> orders =
                CreateTwoSupplierOrders();
            TestReceiver receiver = new TestReceiver();
            PurchaseOrderFulfillmentService fulfillment =
                new PurchaseOrderFulfillmentService(receiver);
            fulfillment.Schedule(orders);
            fulfillment.AdvanceTo(new CommercialTime(0, 12, 0));

            PurchaseOrderReceivingResult result =
                fulfillment.ReceiveDelivery(
                    orders[0].OrderNumber);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.CompletedOrderCount, Is.EqualTo(1));
            Assert.That(fulfillment.ReceivedOrderCount, Is.EqualTo(1));
            Assert.That(fulfillment.ReadyToReceiveOrderCount, Is.EqualTo(1));
            Assert.That(
                fulfillment.EnumerateReadyDeliveries().Single().OrderNumber,
                Is.EqualTo(orders[1].OrderNumber));
        }

        [Test]
        public void VisibleBoxCount_RejectsAnEmptyLoad()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => InboundDeliveryLoad.ResolveVisibleBoxCount(0));
        }


        private static TestContext CreateContext()
        {
            ProductDefinition cola = CommercialCatalogTests.CreateCola();
            SupplierDefinition big =
                new SupplierDefinition(
                    new SupplierId("BIG"),
                    "BIG Wholesale",
                    "Broadline",
                    0,
                    SupplierDeliveryRule.SameDay(3));
            SupplierOfferDefinition offer =
                CommercialCatalogTests.CreateOffer(
                    "BIG-COLA",
                    big.Id,
                    cola.Id,
                    12,
                    1200);
            CommercialCatalog catalog =
                new CommercialCatalog(
                    new BrandCatalog(
                        new[]
                        {
                            new BrandDefinition(
                                new BrandId("BRIGHT"),
                                "Bright Beverage Co.")
                        }),
                    new ProductCatalog(new[] { cola }),
                    new SupplierCatalog(new[] { big }),
                    new SupplierOfferCatalog(new[] { offer }));
            PurchasingService purchasing = new PurchasingService(catalog);
            purchasing.SetPurchasePackCount(offer.Id, 2);
            IReadOnlyList<PlacedPurchaseOrder> orders =
                purchasing.PlaceDrafts(new CommercialTime(0, 9, 0));
            return new TestContext(cola.Id, orders);
        }

        private static IReadOnlyList<PlacedPurchaseOrder>
            CreateTwoSupplierOrders()
        {
            ProductDefinition cola = CommercialCatalogTests.CreateCola();
            SupplierDefinition big =
                new SupplierDefinition(
                    new SupplierId("BIG"),
                    "BIG Wholesale",
                    "Broadline",
                    0,
                    SupplierDeliveryRule.SameDay(3));
            SupplierDefinition central =
                new SupplierDefinition(
                    new SupplierId("CENTRAL"),
                    "Central Grocery Supply",
                    "Grocery",
                    0,
                    SupplierDeliveryRule.SameDay(3));
            SupplierOfferDefinition bigOffer =
                CommercialCatalogTests.CreateOffer(
                    "BIG-COLA",
                    big.Id,
                    cola.Id,
                    12,
                    1200);
            SupplierOfferDefinition centralOffer =
                CommercialCatalogTests.CreateOffer(
                    "CENTRAL-COLA",
                    central.Id,
                    cola.Id,
                    24,
                    2100);
            CommercialCatalog catalog =
                new CommercialCatalog(
                    new BrandCatalog(
                        new[]
                        {
                            new BrandDefinition(
                                new BrandId("BRIGHT"),
                                "Bright Beverage Co.")
                        }),
                    new ProductCatalog(new[] { cola }),
                    new SupplierCatalog(new[] { big, central }),
                    new SupplierOfferCatalog(
                        new[] { bigOffer, centralOffer }));
            PurchasingService purchasing =
                new PurchasingService(catalog);
            purchasing.SetPurchasePackCount(bigOffer.Id, 1);
            purchasing.SetPurchasePackCount(centralOffer.Id, 1);
            return purchasing.PlaceDrafts(
                new CommercialTime(0, 9, 0));
        }


        private sealed class TestReceiver : IPurchaseOrderReceiver
        {
            public Dictionary<ProductId, int> ReceivedUnits { get; } =
                new Dictionary<ProductId, int>();

            public bool RejectNextReceipt { get; set; }


            public bool TryReceive(
                ProductId productId,
                int unitCount)
            {
                if (RejectNextReceipt)
                {
                    RejectNextReceipt = false;
                    return false;
                }

                ReceivedUnits.TryGetValue(productId, out int current);
                ReceivedUnits[productId] = current + unitCount;
                return true;
            }
        }


        private readonly struct TestContext
        {
            public TestContext(
                ProductId productId,
                IReadOnlyList<PlacedPurchaseOrder> orders)
            {
                ProductId = productId;
                Orders = orders;
            }


            public ProductId ProductId { get; }

            public IReadOnlyList<PlacedPurchaseOrder> Orders { get; }
        }
    }
}
