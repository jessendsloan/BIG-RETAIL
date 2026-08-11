using BigRetail.Merchandise.Domain;
using NUnit.Framework;

namespace BigRetail.Inventory.Domain.Tests
{
    public sealed class StockRemovalServiceTests
    {
        private ProductId productId;
        private StorageLocationId displayId;
        private InventoryState inventory;
        private StockRemovalService removals;


        [SetUp]
        public void SetUp()
        {
            productId = new ProductId("CEREAL");
            displayId = new StorageLocationId("DISPLAY-A");

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
                            displayId,
                            "Display A",
                            StorageRole.SalesFloor)
                    },
                    new[]
                    {
                        new StockBalance(
                            displayId,
                            productId,
                            6)
                    });

            removals = new StockRemovalService(inventory);
        }


        [Test]
        public void Remove_ExactQuantity_LeavesRemainingStock()
        {
            StockRemovalResult result =
                removals.TryRemove(
                    displayId,
                    productId,
                    2);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.QuantityRemoved, Is.EqualTo(2));
            Assert.That(result.QuantityAfter, Is.EqualTo(4));
            Assert.That(
                inventory.GetQuantity(displayId, productId),
                Is.EqualTo(4));
        }

        [Test]
        public void Remove_MoreThanAvailable_FailsWithoutChangingStock()
        {
            StockRemovalResult result =
                removals.TryRemove(
                    displayId,
                    productId,
                    7);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.Failure,
                Is.EqualTo(StockRemovalFailure.InsufficientStock));
            Assert.That(
                inventory.GetQuantity(displayId, productId),
                Is.EqualTo(6));
        }
    }
}
