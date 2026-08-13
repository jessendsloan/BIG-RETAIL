using BigRetail.Merchandise.Domain;
using NUnit.Framework;

namespace BigRetail.Inventory.Domain.Tests
{
    public sealed class StockAdditionServiceTests
    {
        private ProductId productId;
        private StorageLocationId inboundId;
        private InventoryState inventory;
        private StockAdditionService additions;


        [SetUp]
        public void SetUp()
        {
            productId = new ProductId("CEREAL");
            inboundId = new StorageLocationId("INBOUND");

            ProductCatalog products =
                new ProductCatalog(
                    new[]
                    {
                        new ProductDefinition(
                            productId,
                            "Cereal",
                            new ProductCategoryId("GROCERY"),
                            StockUnit.Each)
                    });

            inventory =
                new InventoryState(
                    products,
                    new[]
                    {
                        new StorageLocationDefinition(
                            inboundId,
                            "Inbound",
                            StorageRole.Backroom)
                    });

            additions = new StockAdditionService(inventory);
        }


        [Test]
        public void Add_ValidDelivery_IncreasesInventory()
        {
            StockAdditionResult result =
                additions.TryAdd(
                    inboundId,
                    productId,
                    24);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.QuantityAdded, Is.EqualTo(24));
            Assert.That(result.QuantityAfter, Is.EqualTo(24));
            Assert.That(
                inventory.GetQuantity(inboundId, productId),
                Is.EqualTo(24));
        }

        [Test]
        public void Add_NonPositiveQuantity_FailsWithoutChangingInventory()
        {
            StockAdditionResult result =
                additions.TryAdd(
                    inboundId,
                    productId,
                    0);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.Failure,
                Is.EqualTo(StockAdditionFailure.InvalidQuantity));
            Assert.That(
                inventory.GetQuantity(inboundId, productId),
                Is.Zero);
        }
    }
}
