using System;
using BigRetail.Merchandise.Domain;

namespace BigRetail.Inventory.Domain
{
    /// <summary>
    /// Removes exact quantities that have left store inventory through a
    /// sale, spoilage, or another explicit simulation transaction.
    /// </summary>
    public sealed class StockRemovalService
    {
        private readonly InventoryState inventory;


        public StockRemovalService(InventoryState inventory)
        {
            this.inventory =
                inventory
                ?? throw new ArgumentNullException(nameof(inventory));
        }


        public StockRemovalResult TryRemove(
            StorageLocationId locationId,
            ProductId productId,
            int quantity)
        {
            if (quantity <= 0)
            {
                return StockRemovalResult.Failed(
                    StockRemovalFailure.InvalidQuantity,
                    0);
            }

            if (!inventory.ContainsProduct(productId))
            {
                return StockRemovalResult.Failed(
                    StockRemovalFailure.UnknownProduct,
                    0);
            }

            if (!inventory.ContainsLocation(locationId))
            {
                return StockRemovalResult.Failed(
                    StockRemovalFailure.UnknownLocation,
                    0);
            }

            int currentQuantity =
                inventory.GetQuantityUnchecked(
                    locationId,
                    productId);

            if (currentQuantity < quantity)
            {
                return StockRemovalResult.Failed(
                    StockRemovalFailure.InsufficientStock,
                    currentQuantity);
            }

            inventory.ApplyRemoval(
                locationId,
                productId,
                quantity);

            return StockRemovalResult.Success(
                quantity,
                currentQuantity - quantity);
        }
    }
}
