using System;
using BigRetail.Merchandise.Domain;

namespace BigRetail.Inventory.Domain
{
    /// <summary>
    /// Represents an exact quantity of one product at one logical location.
    /// </summary>
    public readonly struct StockBalance
    {
        public StorageLocationId LocationId { get; }
        public ProductId ProductId { get; }
        public int Quantity { get; }


        public StockBalance(
            StorageLocationId locationId,
            ProductId productId,
            int quantity)
        {
            if (!locationId.IsValid)
            {
                throw new ArgumentException(
                    "A stock balance requires a valid location identifier.",
                    nameof(locationId));
            }

            if (!productId.IsValid)
            {
                throw new ArgumentException(
                    "A stock balance requires a valid product identifier.",
                    nameof(productId));
            }

            if (quantity < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(quantity),
                    quantity,
                    "A stock balance cannot be negative.");
            }

            LocationId = locationId;
            ProductId = productId;
            Quantity = quantity;
        }
    }
}
