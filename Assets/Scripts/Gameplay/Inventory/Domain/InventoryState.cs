using System;
using System.Collections.Generic;
using BigRetail.Merchandise.Domain;

namespace BigRetail.Inventory.Domain
{
    /// <summary>
    /// Owns the authoritative stock quantities for one store simulation.
    ///
    /// Product definitions remain in ProductCatalog. Physical shelves and
    /// rooms can later point at storage locations without owning stock truth.
    /// </summary>
    public sealed class InventoryState
    {
        private readonly ProductCatalog productCatalog;

        private readonly Dictionary<
            StorageLocationId,
            StorageLocationDefinition> locations;

        private readonly Dictionary<
            InventoryKey,
            int> quantities;


        public int LocationCount =>
            locations.Count;


        public InventoryState(
            ProductCatalog productCatalog,
            IEnumerable<StorageLocationDefinition> locations,
            IEnumerable<StockBalance> initialBalances = null)
        {
            this.productCatalog =
                productCatalog
                ?? throw new ArgumentNullException(
                    nameof(productCatalog));

            if (locations == null)
            {
                throw new ArgumentNullException(
                    nameof(locations));
            }

            this.locations =
                new Dictionary<
                    StorageLocationId,
                    StorageLocationDefinition>();

            foreach (StorageLocationDefinition location in locations)
            {
                if (location == null)
                {
                    throw new ArgumentException(
                        "Inventory locations cannot contain a null definition.",
                        nameof(locations));
                }

                if (this.locations.ContainsKey(
                        location.Id))
                {
                    throw new ArgumentException(
                        $"Storage location '{location.Id}' is duplicated.",
                        nameof(locations));
                }

                this.locations.Add(
                    location.Id,
                    location);
            }

            quantities =
                new Dictionary<
                    InventoryKey,
                    int>();

            if (initialBalances != null)
            {
                LoadInitialBalances(
                    initialBalances);
            }
        }


        public bool ContainsProduct(
            ProductId productId)
        {
            return productCatalog.Contains(
                productId);
        }

        public bool ContainsLocation(
            StorageLocationId locationId)
        {
            return locations.ContainsKey(
                locationId);
        }

        public StorageLocationDefinition GetLocationRequired(
            StorageLocationId locationId)
        {
            if (locations.TryGetValue(
                    locationId,
                    out StorageLocationDefinition location))
            {
                return location;
            }

            throw new KeyNotFoundException(
                $"Storage location '{locationId}' does not exist.");
        }

        public int GetQuantity(
            StorageLocationId locationId,
            ProductId productId)
        {
            RequireKnownLocation(
                locationId);

            RequireKnownProduct(
                productId);

            return GetQuantityUnchecked(
                locationId,
                productId);
        }

        public IEnumerable<StorageLocationDefinition> EnumerateLocations()
        {
            foreach (StorageLocationDefinition location in locations.Values)
            {
                yield return location;
            }
        }

        public IEnumerable<StockBalance> EnumerateBalances()
        {
            foreach (KeyValuePair<InventoryKey, int> entry in quantities)
            {
                if (entry.Value <= 0)
                {
                    continue;
                }

                yield return
                    new StockBalance(
                        entry.Key.LocationId,
                        entry.Key.ProductId,
                        entry.Value);
            }
        }


        internal int GetQuantityUnchecked(
            StorageLocationId locationId,
            ProductId productId)
        {
            InventoryKey key =
                new InventoryKey(
                    locationId,
                    productId);

            return quantities.TryGetValue(
                    key,
                    out int quantity)
                ? quantity
                : 0;
        }

        internal void ApplyTransfer(
            StorageLocationId sourceLocationId,
            StorageLocationId destinationLocationId,
            ProductId productId,
            int quantity)
        {
            InventoryKey sourceKey =
                new InventoryKey(
                    sourceLocationId,
                    productId);

            InventoryKey destinationKey =
                new InventoryKey(
                    destinationLocationId,
                    productId);

            int sourceQuantity =
                quantities[sourceKey];

            int destinationQuantity =
                GetQuantityUnchecked(
                    destinationLocationId,
                    productId);

            int remainingSourceQuantity =
                sourceQuantity - quantity;

            if (remainingSourceQuantity == 0)
            {
                quantities.Remove(
                    sourceKey);
            }
            else
            {
                quantities[sourceKey] =
                    remainingSourceQuantity;
            }

            quantities[destinationKey] =
                destinationQuantity + quantity;
        }


        private void LoadInitialBalances(
            IEnumerable<StockBalance> initialBalances)
        {
            foreach (StockBalance balance in initialBalances)
            {
                RequireKnownLocation(
                    balance.LocationId);

                RequireKnownProduct(
                    balance.ProductId);

                InventoryKey key =
                    new InventoryKey(
                        balance.LocationId,
                        balance.ProductId);

                if (quantities.ContainsKey(key))
                {
                    throw new ArgumentException(
                        $"Initial stock for product '{balance.ProductId}' "
                        + $"at location '{balance.LocationId}' is duplicated.",
                        nameof(initialBalances));
                }

                if (balance.Quantity > 0)
                {
                    quantities.Add(
                        key,
                        balance.Quantity);
                }
            }
        }

        private void RequireKnownLocation(
            StorageLocationId locationId)
        {
            if (!locations.ContainsKey(locationId))
            {
                throw new KeyNotFoundException(
                    $"Storage location '{locationId}' does not exist.");
            }
        }

        private void RequireKnownProduct(
            ProductId productId)
        {
            if (!productCatalog.Contains(productId))
            {
                throw new KeyNotFoundException(
                    $"Product '{productId}' does not exist in the catalog.");
            }
        }
    }
}
