using System;
using BigRetail.Merchandise.Domain;

namespace BigRetail.Inventory.Domain
{
    /// <summary>
    /// Internal dictionary key for one product balance at one location.
    /// </summary>
    internal readonly struct InventoryKey :
        IEquatable<InventoryKey>
    {
        public StorageLocationId LocationId { get; }
        public ProductId ProductId { get; }


        public InventoryKey(
            StorageLocationId locationId,
            ProductId productId)
        {
            LocationId = locationId;
            ProductId = productId;
        }


        public bool Equals(
            InventoryKey other)
        {
            return LocationId.Equals(other.LocationId)
                && ProductId.Equals(other.ProductId);
        }

        public override bool Equals(
            object obj)
        {
            return obj is InventoryKey other
                && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;

                hash =
                    (hash * 31)
                    + LocationId.GetHashCode();

                hash =
                    (hash * 31)
                    + ProductId.GetHashCode();

                return hash;
            }
        }
    }
}
