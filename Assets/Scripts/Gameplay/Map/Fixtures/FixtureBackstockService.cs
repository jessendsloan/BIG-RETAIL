using System;
using System.Collections.Generic;
using BigRetail.Inventory.Domain;
using BigRetail.Merchandise.Domain;

namespace BigRetail.Map.Fixtures
{
    /// <summary>
    /// Assigns backstock to the individual storage fixtures that physically
    /// hold it. The original shared location is retained as an inbound and
    /// overflow area so stock is never destroyed when storage changes.
    /// </summary>
    public sealed class FixtureBackstockService : IDisposable
    {
        private const string RackLocationPrefix = "BACKSTOCK-RACK-";

        private readonly FixtureState fixtureState;
        private readonly ProductCatalog productCatalog;
        private readonly InventoryState inventory;
        private readonly StockAdditionService additions;
        private readonly StockTransferService transfers;
        private readonly Dictionary<FixtureInstanceId, RackRecord> racks =
            new Dictionary<FixtureInstanceId, RackRecord>();
        private readonly List<FixtureInstanceId> rackOrder =
            new List<FixtureInstanceId>();

        private bool isDisposed;


        /// <summary>
        /// Temporary inbound/overflow inventory. Stock at this location is
        /// not available to sales-floor restocking until a rack houses it.
        /// </summary>
        public StorageLocationId LocationId { get; }

        public int CapacityUnitCount { get; private set; }

        public int StoredUnitCount
        {
            get
            {
                int storedUnitCount = 0;

                for (int index = 0; index < rackOrder.Count; index++)
                {
                    storedUnitCount +=
                        GetRackStoredUnitCount(rackOrder[index]);
                }

                return storedUnitCount;
            }
        }

        public int UnallocatedUnitCount =>
            GetLocationStoredUnitCount(LocationId);

        public int AvailableCapacityUnitCount =>
            Math.Max(0, CapacityUnitCount - StoredUnitCount);

        public bool IsOperational =>
            CapacityUnitCount > 0;

        public bool IsOverCapacity =>
            UnallocatedUnitCount > 0;


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
                    "Fixture backstock requires a known inbound inventory location.",
                    nameof(locationId));
            }

            LocationId = locationId;
            additions = new StockAdditionService(inventory);
            transfers = new StockTransferService(inventory);

            foreach (
                FixtureInstance fixture
                in fixtureState.EnumerateFixtures())
            {
                RegisterRack(fixture);
            }

            DistributeUnallocatedStock();

            fixtureState.FixtureAdded += HandleFixtureAdded;
            fixtureState.FixtureRemoved += HandleFixtureRemoved;
        }


        public event Action CapacityChanged;

        public event Action ContentsChanged;


        public int GetAvailableQuantity(
            ProductId productId)
        {
            int availableQuantity = 0;

            for (int index = 0; index < rackOrder.Count; index++)
            {
                RackRecord rack = racks[rackOrder[index]];
                availableQuantity +=
                    inventory.GetQuantity(
                        rack.LocationId,
                        productId);
            }

            return availableQuantity;
        }

        public int GetRackStoredUnitCount(
            FixtureInstanceId fixtureId)
        {
            return racks.TryGetValue(fixtureId, out RackRecord rack)
                ? GetLocationStoredUnitCount(rack.LocationId)
                : 0;
        }

        public int GetRackCapacityUnitCount(
            FixtureInstanceId fixtureId)
        {
            return racks.TryGetValue(fixtureId, out RackRecord rack)
                ? rack.CapacityUnitCount
                : 0;
        }

        /// <summary>
        /// Receives new store-owned stock through inbound, then distributes
        /// as much as possible across the currently placed physical racks.
        /// Any excess remains visible in inbound/overflow.
        /// </summary>
        public StockAdditionResult ReceiveInbound(
            ProductId productId,
            int unitCount)
        {
            StockAdditionResult result =
                additions.TryAdd(
                    LocationId,
                    productId,
                    unitCount);

            if (!result.Succeeded)
            {
                return result;
            }

            DistributeUnallocatedStock();
            ContentsChanged?.Invoke();
            return result;
        }

        public IEnumerable<FixtureBackstockProductSnapshot>
            EnumerateRackContents(FixtureInstanceId fixtureId)
        {
            if (!racks.TryGetValue(fixtureId, out RackRecord rack))
            {
                yield break;
            }

            foreach (
                ProductDefinition product
                in productCatalog.EnumerateDefinitions())
            {
                int quantity =
                    inventory.GetQuantity(
                        rack.LocationId,
                        product.Id);

                if (quantity > 0)
                {
                    yield return
                        new FixtureBackstockProductSnapshot(
                            product.Id,
                            quantity);
                }
            }
        }

        /// <summary>
        /// Pulls product from one or more physical racks into a known
        /// destination such as a sales-floor display.
        /// </summary>
        public int TransferToLocation(
            StorageLocationId destinationLocationId,
            ProductId productId,
            int requestedUnitCount)
        {
            if (requestedUnitCount <= 0)
            {
                return 0;
            }

            int remainingUnitCount = requestedUnitCount;
            int movedUnitCount = 0;

            for (int index = 0;
                 index < rackOrder.Count && remainingUnitCount > 0;
                 index++)
            {
                RackRecord rack = racks[rackOrder[index]];
                int rackQuantity =
                    inventory.GetQuantity(
                        rack.LocationId,
                        productId);
                int transferUnitCount =
                    Math.Min(remainingUnitCount, rackQuantity);

                if (transferUnitCount == 0)
                {
                    continue;
                }

                TransferRequired(
                    rack.LocationId,
                    destinationLocationId,
                    productId,
                    transferUnitCount);

                remainingUnitCount -= transferUnitCount;
                movedUnitCount += transferUnitCount;
            }

            if (movedUnitCount > 0)
            {
                ContentsChanged?.Invoke();
            }

            return movedUnitCount;
        }

        /// <summary>
        /// Returns product to available rack space. Any remainder is moved to
        /// inbound/overflow so fixture edits and demolition never lose stock.
        /// </summary>
        public int StoreFromLocation(
            StorageLocationId sourceLocationId,
            ProductId productId,
            int requestedUnitCount)
        {
            if (requestedUnitCount <= 0)
            {
                return 0;
            }

            int availableSourceQuantity =
                inventory.GetQuantity(
                    sourceLocationId,
                    productId);
            int remainingUnitCount =
                Math.Min(requestedUnitCount, availableSourceQuantity);
            int movedUnitCount = 0;

            for (int index = 0;
                 index < rackOrder.Count && remainingUnitCount > 0;
                 index++)
            {
                RackRecord rack = racks[rackOrder[index]];
                int freeRackCapacity =
                    Math.Max(
                        0,
                        rack.CapacityUnitCount
                        - GetLocationStoredUnitCount(rack.LocationId));
                int transferUnitCount =
                    Math.Min(remainingUnitCount, freeRackCapacity);

                if (transferUnitCount == 0
                    || sourceLocationId == rack.LocationId)
                {
                    continue;
                }

                TransferRequired(
                    sourceLocationId,
                    rack.LocationId,
                    productId,
                    transferUnitCount);

                remainingUnitCount -= transferUnitCount;
                movedUnitCount += transferUnitCount;
            }

            if (remainingUnitCount > 0
                && sourceLocationId != LocationId)
            {
                TransferRequired(
                    sourceLocationId,
                    LocationId,
                    productId,
                    remainingUnitCount);
                movedUnitCount += remainingUnitCount;
            }

            if (movedUnitCount > 0)
            {
                ContentsChanged?.Invoke();
            }

            return movedUnitCount;
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


        public static StorageLocationId GetRackLocationId(
            FixtureInstanceId fixtureId)
        {
            if (!fixtureId.IsValid)
            {
                throw new ArgumentException(
                    "A rack inventory location requires a valid fixture ID.",
                    nameof(fixtureId));
            }

            return new StorageLocationId(
                RackLocationPrefix + fixtureId.Value);
        }


        private void HandleFixtureAdded(
            FixtureInstance fixture)
        {
            if (!RegisterRack(fixture))
            {
                return;
            }

            DistributeUnallocatedStock();
            CapacityChanged?.Invoke();
            ContentsChanged?.Invoke();
        }

        private void HandleFixtureRemoved(
            FixtureInstance fixture)
        {
            if (!racks.TryGetValue(fixture.Id, out RackRecord rack))
            {
                return;
            }

            MoveRackContentsToInbound(rack);

            if (!inventory.TryRemoveLocation(rack.LocationId))
            {
                throw new InvalidOperationException(
                    $"Backstock rack location '{rack.LocationId}' still contains stock.");
            }

            racks.Remove(fixture.Id);
            rackOrder.Remove(fixture.Id);
            CapacityUnitCount -= rack.CapacityUnitCount;

            if (CapacityUnitCount < 0)
            {
                throw new InvalidOperationException(
                    "Fixture backstock capacity became negative.");
            }

            DistributeUnallocatedStock();
            CapacityChanged?.Invoke();
            ContentsChanged?.Invoke();
        }

        private bool RegisterRack(
            FixtureInstance fixture)
        {
            int capacityUnitCount =
                fixture.Definition.StorageProfile
                    .BackstockCapacityUnits;

            if (capacityUnitCount <= 0)
            {
                return false;
            }

            StorageLocationId rackLocationId =
                GetRackLocationId(fixture.Id);

            if (!inventory.TryRegisterLocation(
                    new StorageLocationDefinition(
                        rackLocationId,
                        $"{fixture.Definition.DisplayName} {fixture.Id.Value}",
                        StorageRole.Backroom)))
            {
                throw new InvalidOperationException(
                    $"Backstock rack location '{rackLocationId}' already exists.");
            }

            racks.Add(
                fixture.Id,
                new RackRecord(
                    rackLocationId,
                    capacityUnitCount));
            rackOrder.Add(fixture.Id);
            CapacityUnitCount += capacityUnitCount;
            return true;
        }

        private void DistributeUnallocatedStock()
        {
            foreach (
                ProductDefinition product
                in productCatalog.EnumerateDefinitions())
            {
                int remainingUnitCount =
                    inventory.GetQuantity(
                        LocationId,
                        product.Id);

                for (int index = 0;
                     index < rackOrder.Count && remainingUnitCount > 0;
                     index++)
                {
                    RackRecord rack = racks[rackOrder[index]];
                    int freeRackCapacity =
                        Math.Max(
                            0,
                            rack.CapacityUnitCount
                            - GetLocationStoredUnitCount(rack.LocationId));
                    int transferUnitCount =
                        Math.Min(remainingUnitCount, freeRackCapacity);

                    if (transferUnitCount == 0)
                    {
                        continue;
                    }

                    TransferRequired(
                        LocationId,
                        rack.LocationId,
                        product.Id,
                        transferUnitCount);
                    remainingUnitCount -= transferUnitCount;
                }
            }
        }

        private void MoveRackContentsToInbound(
            RackRecord rack)
        {
            foreach (
                ProductDefinition product
                in productCatalog.EnumerateDefinitions())
            {
                int quantity =
                    inventory.GetQuantity(
                        rack.LocationId,
                        product.Id);

                if (quantity > 0)
                {
                    TransferRequired(
                        rack.LocationId,
                        LocationId,
                        product.Id,
                        quantity);
                }
            }
        }

        private int GetLocationStoredUnitCount(
            StorageLocationId locationId)
        {
            int storedUnitCount = 0;

            foreach (
                ProductDefinition product
                in productCatalog.EnumerateDefinitions())
            {
                storedUnitCount +=
                    inventory.GetQuantity(
                        locationId,
                        product.Id);
            }

            return storedUnitCount;
        }

        private void TransferRequired(
            StorageLocationId sourceLocationId,
            StorageLocationId destinationLocationId,
            ProductId productId,
            int quantity)
        {
            StockTransferResult result =
                transfers.TryTransfer(
                    sourceLocationId,
                    destinationLocationId,
                    productId,
                    quantity);

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Calculated backstock transfer failed: {result.Failure}.");
            }
        }


        private sealed class RackRecord
        {
            public RackRecord(
                StorageLocationId locationId,
                int capacityUnitCount)
            {
                LocationId = locationId;
                CapacityUnitCount = capacityUnitCount;
            }


            public StorageLocationId LocationId { get; }

            public int CapacityUnitCount { get; }
        }
    }


    public readonly struct FixtureBackstockProductSnapshot
    {
        public FixtureBackstockProductSnapshot(
            ProductId productId,
            int quantity)
        {
            ProductId = productId;
            Quantity = quantity;
        }


        public ProductId ProductId { get; }

        public int Quantity { get; }
    }
}
