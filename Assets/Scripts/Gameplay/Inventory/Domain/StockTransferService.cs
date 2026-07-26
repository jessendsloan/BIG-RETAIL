using System;
using BigRetail.Merchandise.Domain;

namespace BigRetail.Inventory.Domain
{
    /// <summary>
    /// Moves exact stock quantities between known logical locations.
    ///
    /// Validation completes before InventoryState changes, so failed transfers
    /// are atomic and never partially move stock.
    /// </summary>
    public sealed class StockTransferService
    {
        private readonly InventoryState inventory;


        public StockTransferService(
            InventoryState inventory)
        {
            this.inventory =
                inventory
                ?? throw new ArgumentNullException(
                    nameof(inventory));
        }


        public StockTransferResult TryTransfer(
            StorageLocationId sourceLocationId,
            StorageLocationId destinationLocationId,
            ProductId productId,
            int quantity)
        {
            if (quantity <= 0)
            {
                return StockTransferResult.Failed(
                    StockTransferFailure.InvalidQuantity,
                    0,
                    0);
            }

            if (!inventory.ContainsProduct(productId))
            {
                return StockTransferResult.Failed(
                    StockTransferFailure.UnknownProduct,
                    0,
                    0);
            }

            if (!inventory.ContainsLocation(sourceLocationId))
            {
                return StockTransferResult.Failed(
                    StockTransferFailure.UnknownSourceLocation,
                    0,
                    0);
            }

            if (!inventory.ContainsLocation(destinationLocationId))
            {
                return StockTransferResult.Failed(
                    StockTransferFailure.UnknownDestinationLocation,
                    0,
                    0);
            }

            int sourceQuantity =
                inventory.GetQuantityUnchecked(
                    sourceLocationId,
                    productId);

            int destinationQuantity =
                inventory.GetQuantityUnchecked(
                    destinationLocationId,
                    productId);

            if (sourceLocationId == destinationLocationId)
            {
                return StockTransferResult.Failed(
                    StockTransferFailure.SameLocation,
                    sourceQuantity,
                    destinationQuantity);
            }

            if (sourceQuantity < quantity)
            {
                return StockTransferResult.Failed(
                    StockTransferFailure.InsufficientSourceStock,
                    sourceQuantity,
                    destinationQuantity);
            }

            if (destinationQuantity > int.MaxValue - quantity)
            {
                return StockTransferResult.Failed(
                    StockTransferFailure.DestinationQuantityOverflow,
                    sourceQuantity,
                    destinationQuantity);
            }

            inventory.ApplyTransfer(
                sourceLocationId,
                destinationLocationId,
                productId,
                quantity);

            return StockTransferResult.Success(
                quantity,
                sourceQuantity - quantity,
                destinationQuantity + quantity);
        }
    }
}
