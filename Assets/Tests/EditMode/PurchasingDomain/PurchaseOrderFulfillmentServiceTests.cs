using System.Collections.Generic;
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
