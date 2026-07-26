using BigRetail.Merchandise.Domain;
using NUnit.Framework;

namespace BigRetail.Inventory.Domain.Tests
{
    public sealed class StockTransferServiceTests
    {
        private ProductId productId;

        private StorageLocationId backroomId;
        private StorageLocationId salesFloorId;

        private InventoryState inventory;
        private StockTransferService transfers;


        [SetUp]
        public void SetUp()
        {
            productId =
                new ProductId("choco-bar");

            backroomId =
                new StorageLocationId("backroom-a");

            salesFloorId =
                new StorageLocationId("sales-floor-a");

            ProductCatalog catalog =
                new ProductCatalog(
                    new[]
                    {
                        new ProductDefinition(
                            productId,
                            "Chocolate Bar",
                            new ProductCategoryId("candy"),
                            StockUnit.Each)
                    });

            inventory =
                new InventoryState(
                    catalog,
                    new[]
                    {
                        new StorageLocationDefinition(
                            backroomId,
                            "Backroom A",
                            StorageRole.Backroom),

                        new StorageLocationDefinition(
                            salesFloorId,
                            "Sales Floor A",
                            StorageRole.SalesFloor)
                    },
                    new[]
                    {
                        new StockBalance(
                            backroomId,
                            productId,
                            96),

                        new StockBalance(
                            salesFloorId,
                            productId,
                            12)
                    });

            transfers =
                new StockTransferService(
                    inventory);
        }


        [Test]
        public void Transfer_MovesExactQuantityBetweenLocations()
        {
            StockTransferResult result =
                transfers.TryTransfer(
                    backroomId,
                    salesFloorId,
                    productId,
                    24);

            Assert.That(
                result.Succeeded,
                Is.True);

            Assert.That(
                result.QuantityMoved,
                Is.EqualTo(24));

            Assert.That(
                inventory.GetQuantity(
                    backroomId,
                    productId),
                Is.EqualTo(72));

            Assert.That(
                inventory.GetQuantity(
                    salesFloorId,
                    productId),
                Is.EqualTo(36));
        }

        [Test]
        public void Transfer_AllSourceStock_RemovesSourceBalance()
        {
            StockTransferResult result =
                transfers.TryTransfer(
                    backroomId,
                    salesFloorId,
                    productId,
                    96);

            Assert.That(
                result.SourceQuantityAfter,
                Is.Zero);

            Assert.That(
                inventory.GetQuantity(
                    backroomId,
                    productId),
                Is.Zero);

            Assert.That(
                inventory.GetQuantity(
                    salesFloorId,
                    productId),
                Is.EqualTo(108));
        }

        [Test]
        public void InsufficientStock_FailsWithoutChangingEitherLocation()
        {
            StockTransferResult result =
                transfers.TryTransfer(
                    backroomId,
                    salesFloorId,
                    productId,
                    97);

            Assert.That(
                result.Failure,
                Is.EqualTo(
                    StockTransferFailure.InsufficientSourceStock));

            AssertBalances(
                backroom: 96,
                salesFloor: 12);
        }

        [Test]
        public void SameLocation_FailsWithoutChangingStock()
        {
            StockTransferResult result =
                transfers.TryTransfer(
                    backroomId,
                    backroomId,
                    productId,
                    10);

            Assert.That(
                result.Failure,
                Is.EqualTo(
                    StockTransferFailure.SameLocation));

            AssertBalances(
                backroom: 96,
                salesFloor: 12);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void NonPositiveQuantity_Fails(
            int quantity)
        {
            StockTransferResult result =
                transfers.TryTransfer(
                    backroomId,
                    salesFloorId,
                    productId,
                    quantity);

            Assert.That(
                result.Failure,
                Is.EqualTo(
                    StockTransferFailure.InvalidQuantity));

            AssertBalances(
                backroom: 96,
                salesFloor: 12);
        }

        [Test]
        public void UnknownProduct_Fails()
        {
            StockTransferResult result =
                transfers.TryTransfer(
                    backroomId,
                    salesFloorId,
                    new ProductId("unknown"),
                    1);

            Assert.That(
                result.Failure,
                Is.EqualTo(
                    StockTransferFailure.UnknownProduct));

            AssertBalances(
                backroom: 96,
                salesFloor: 12);
        }

        [Test]
        public void UnknownSource_Fails()
        {
            StockTransferResult result =
                transfers.TryTransfer(
                    new StorageLocationId("missing"),
                    salesFloorId,
                    productId,
                    1);

            Assert.That(
                result.Failure,
                Is.EqualTo(
                    StockTransferFailure.UnknownSourceLocation));

            AssertBalances(
                backroom: 96,
                salesFloor: 12);
        }

        [Test]
        public void UnknownDestination_Fails()
        {
            StockTransferResult result =
                transfers.TryTransfer(
                    backroomId,
                    new StorageLocationId("missing"),
                    productId,
                    1);

            Assert.That(
                result.Failure,
                Is.EqualTo(
                    StockTransferFailure.UnknownDestinationLocation));

            AssertBalances(
                backroom: 96,
                salesFloor: 12);
        }

        [Test]
        public void DestinationOverflow_FailsWithoutChangingStock()
        {
            ProductCatalog catalog =
                new ProductCatalog(
                    new[]
                    {
                        new ProductDefinition(
                            productId,
                            "Chocolate Bar",
                            new ProductCategoryId("candy"),
                            StockUnit.Each)
                    });

            inventory =
                new InventoryState(
                    catalog,
                    new[]
                    {
                        new StorageLocationDefinition(
                            backroomId,
                            "Backroom A",
                            StorageRole.Backroom),

                        new StorageLocationDefinition(
                            salesFloorId,
                            "Sales Floor A",
                            StorageRole.SalesFloor)
                    },
                    new[]
                    {
                        new StockBalance(
                            backroomId,
                            productId,
                            10),

                        new StockBalance(
                            salesFloorId,
                            productId,
                            int.MaxValue)
                    });

            transfers =
                new StockTransferService(
                    inventory);

            StockTransferResult result =
                transfers.TryTransfer(
                    backroomId,
                    salesFloorId,
                    productId,
                    1);

            Assert.That(
                result.Failure,
                Is.EqualTo(
                    StockTransferFailure.DestinationQuantityOverflow));

            AssertBalances(
                backroom: 10,
                salesFloor: int.MaxValue);
        }


        private void AssertBalances(
            int backroom,
            int salesFloor)
        {
            Assert.That(
                inventory.GetQuantity(
                    backroomId,
                    productId),
                Is.EqualTo(backroom));

            Assert.That(
                inventory.GetQuantity(
                    salesFloorId,
                    productId),
                Is.EqualTo(salesFloor));
        }
    }
}
