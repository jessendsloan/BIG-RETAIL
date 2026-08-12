using System;
using BigRetail.Inventory.Domain;
using BigRetail.Merchandise.Domain;

namespace BigRetail.Map.Fixtures
{
    /// <summary>
    /// Converts placed storage fixtures into physical capacity for the
    /// store's shared backstock pool.
    ///
    /// InventoryState remains authoritative for quantities. This service
    /// answers whether that inventory is physically housed and reachable.
    /// </summary>
    public sealed class FixtureBackstockService : IDisposable
    {
        private readonly FixtureState fixtureState;
        private readonly ProductCatalog productCatalog;
        private readonly InventoryState inventory;

        private bool isDisposed;


        public StorageLocationId LocationId { get; }

        public int CapacityUnitCount { get; private set; }

        public int StoredUnitCount
        {
            get
            {
                int storedUnitCount = 0;

                foreach (
                    ProductDefinition product
                    in productCatalog.EnumerateDefinitions())
                {
                    storedUnitCount +=
                        inventory.GetQuantity(
                            LocationId,
                            product.Id);
                }

                return storedUnitCount;
            }
        }

        public int AvailableCapacityUnitCount =>
            Math.Max(0, CapacityUnitCount - StoredUnitCount);

        public bool IsOperational =>
            CapacityUnitCount > 0;

        public bool IsOverCapacity =>
            StoredUnitCount > CapacityUnitCount;


        public FixtureBackstockService(
            FixtureState fixtureState,
            ProductCatalog productCatalog,
            InventoryState inventory,
            StorageLocationId locationId)
        {
            this.fixtureState =
                fixtureState
                ?? throw new ArgumentNullException(nameof(fixtureState));

            this.productCatalog =
                productCatalog
                ?? throw new ArgumentNullException(nameof(productCatalog));

            this.inventory =
                inventory
                ?? throw new ArgumentNullException(nameof(inventory));

            if (!inventory.ContainsLocation(locationId))
            {
                throw new ArgumentException(
                    "Fixture backstock requires a known inventory location.",
                    nameof(locationId));
            }

            LocationId = locationId;

            foreach (
                FixtureInstance fixture
                in fixtureState.EnumerateFixtures())
            {
                AddCapacity(fixture);
            }

            fixtureState.FixtureAdded += HandleFixtureAdded;
            fixtureState.FixtureRemoved += HandleFixtureRemoved;
        }


        public event Action CapacityChanged;


        public int GetAvailableQuantity(
            ProductId productId)
        {
            return IsOperational
                ? inventory.GetQuantity(LocationId, productId)
                : 0;
        }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            fixtureState.FixtureAdded -= HandleFixtureAdded;
            fixtureState.FixtureRemoved -= HandleFixtureRemoved;
            isDisposed = true;
        }


        private void HandleFixtureAdded(
            FixtureInstance fixture)
        {
            AddCapacity(fixture);
            CapacityChanged?.Invoke();
        }

        private void HandleFixtureRemoved(
            FixtureInstance fixture)
        {
            CapacityUnitCount -=
                fixture.Definition.StorageProfile
                    .BackstockCapacityUnits;

            if (CapacityUnitCount < 0)
            {
                throw new InvalidOperationException(
                    "Fixture backstock capacity became negative.");
            }

            CapacityChanged?.Invoke();
        }

        private void AddCapacity(
            FixtureInstance fixture)
        {
            CapacityUnitCount +=
                fixture.Definition.StorageProfile
                    .BackstockCapacityUnits;
        }
    }
}
