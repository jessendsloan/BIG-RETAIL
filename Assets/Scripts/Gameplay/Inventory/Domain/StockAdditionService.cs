using System;
using BigRetail.Merchandise.Domain;

namespace BigRetail.Inventory.Domain
{
    /// <summary>
    /// Adds newly received stock to one authoritative inventory location.
    /// Transfers remain responsible for movement between existing locations.
    /// </summary>
    public sealed class StockAdditionService
    {
        private readonly InventoryState inventory;


        public StockAdditionService(InventoryState inventory)
        {
            this.inventory =
                inventory
                ?? throw new ArgumentNullException(nameof(inventory));
        }


        public StockAdditionResult TryAdd(
            StorageLocationId locationId,
            ProductId productId,
            int quantity)
        {
            if (quantity <= 0)
            {
                return StockAdditionResult.Failed(
                    StockAdditionFailure.InvalidQuantity,
                    0);
            }

            if (!inventory.ContainsProduct(productId))
            {
                return StockAdditionResult.Failed(
                    StockAdditionFailure.UnknownProduct,
                    0);
            }

            if (!inventory.ContainsLocation(locationId))
            {
                return StockAdditionResult.Failed(
                    StockAdditionFailure.UnknownLocation,
                    0);
            }

            int currentQuantity =
                inventory.GetQuantityUnchecked(
                    locationId,
                    productId);

            if (currentQuantity > int.MaxValue - quantity)
            {
                return StockAdditionResult.Failed(
                    StockAdditionFailure.QuantityOverflow,
                    currentQuantity);
            }

            inventory.ApplyAddition(
                locationId,
                productId,
                quantity);

            return StockAdditionResult.Success(
                quantity,
                currentQuantity + quantity);
        }
    }
}
