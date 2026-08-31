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
        private readonly List<RackCaseRecord> inboundCases =
            new List<RackCaseRecord>();

        private bool isDisposed;


        /// <summary>
        /// Temporary inbound/overflow inventory. Stock at this location is
        /// not available to sales-floor restocking until a rack houses it.
        /// </summary>
        public StorageLocationId LocationId { get; }

        public int CaseSlotCapacity { get; private set; }

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

        public int OccupiedCaseSlotCount
        {
            get
            {
                int occupiedCaseSlotCount = 0;

                for (int index = 0; index < rackOrder.Count; index++)
                {
                    occupiedCaseSlotCount +=
                        racks[rackOrder[index]].Cases.Count;
                }

                return occupiedCaseSlotCount;
            }
        }

        public int AvailableCaseSlotCount =>
            Math.Max(0, CaseSlotCapacity - OccupiedCaseSlotCount);

        public bool IsOperational =>
            CaseSlotCapacity > 0;

        public bool HasStockAwaitingStorage =>
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

        public int GetRackCaseSlotCapacity(
            FixtureInstanceId fixtureId)
        {
            return racks.TryGetValue(fixtureId, out RackRecord rack)
                ? rack.CaseSlotCapacity
                : 0;
        }

        public int GetRackOccupiedCaseSlotCount(
            FixtureInstanceId fixtureId)
        {
            return racks.TryGetValue(fixtureId, out RackRecord rack)
                ? rack.Cases.Count
                : 0;
        }

        public int GetRackAvailableCaseSlotCount(
            FixtureInstanceId fixtureId)
        {
            return racks.TryGetValue(fixtureId, out RackRecord rack)
                ? Math.Max(
                    0,
                    rack.CaseSlotCapacity - rack.Cases.Count)
                : 0;
        }

        /// <summary>
        /// Receives one physical handling case through inbound, then places
        /// it in the first available rack slot. The unit count becomes that
        /// case's own capacity; racks prescribe slots, never unit totals.
        /// A case remains in inbound when no slot is available.
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

            inboundCases.Add(
                new RackCaseRecord(
                    productId,
                    unitCount,
                    unitCount));
            DistributeUnallocatedStock();
            ContentsChanged?.Invoke();
            return result;
        }

        /// <summary>
        /// Receives one physical supplier case directly into the rack chosen
        /// by the acting worker. Unlike bulk receiving, this never selects a
        /// rack or overflow location on the worker's behalf.
        /// </summary>
        public FixtureBackstockReceiptResult TryReceiveInboundAtRack(
            FixtureInstanceId fixtureId,
            ProductId productId,
            int unitCount)
        {
            if (unitCount <= 0)
            {
                return FixtureBackstockReceiptResult.Failed(
                    FixtureBackstockReceiptFailure.InvalidQuantity);
            }

            if (!racks.TryGetValue(fixtureId, out RackRecord rack))
            {
                return FixtureBackstockReceiptResult.Failed(
                    FixtureBackstockReceiptFailure.UnknownRack);
            }

            int availableCaseSlotCount = Math.Max(
                0,
                rack.CaseSlotCapacity - rack.Cases.Count);

            if (availableCaseSlotCount == 0)
            {
                return FixtureBackstockReceiptResult.Failed(
                    FixtureBackstockReceiptFailure.NoAvailableCaseSlot,
                    remainingRackCaseSlotCount: 0);
            }

            StockAdditionResult result = additions.TryAdd(
                rack.LocationId,
                productId,
                unitCount);

            if (!result.Succeeded)
            {
                return FixtureBackstockReceiptResult.Failed(
                    FixtureBackstockReceiptFailure.InventoryRejected,
                    availableCaseSlotCount);
            }

            rack.Cases.Add(
                new RackCaseRecord(
                    productId,
                    unitCount,
                    unitCount));

            ContentsChanged?.Invoke();
            return FixtureBackstockReceiptResult.Success(
                unitCount,
                availableCaseSlotCount - 1);
        }

        /// <summary>
        /// Enumerates the physical supplier cases currently housed by one
        /// rack. Aggregate inventory remains authoritative for unit counts;
        /// these handling-unit records preserve the case boundaries that
        /// workers and presentation systems need to interact with.
        /// </summary>
        public IEnumerable<FixtureBackstockCaseSnapshot>
            EnumerateRackCases(FixtureInstanceId fixtureId)
        {
            if (!racks.TryGetValue(fixtureId, out RackRecord rack))
            {
                yield break;
            }

            for (int index = 0; index < rack.Cases.Count; index++)
            {
                RackCaseRecord storedCase = rack.Cases[index];

                yield return new FixtureBackstockCaseSnapshot(
                    storedCase.ProductId,
                    storedCase.RemainingUnitCount,
                    storedCase.CapacityUnitCount);
            }
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
        /// Finds the first physical rack, in stable rack order, containing a
        /// case of the requested product. Worker schedulers can use this to
        /// choose a concrete pickup source before route planning.
        /// </summary>
        public bool TryFindRackCase(
            ProductId productId,
            out FixtureInstanceId fixtureId,
            out FixtureBackstockCaseSnapshot storedCase)
        {
            for (int rackIndex = 0;
                 rackIndex < rackOrder.Count;
                 rackIndex++)
            {
                FixtureInstanceId candidateId = rackOrder[rackIndex];
                RackRecord rack = racks[candidateId];

                for (int caseIndex = 0;
                     caseIndex < rack.Cases.Count;
                     caseIndex++)
                {
                    RackCaseRecord candidate = rack.Cases[caseIndex];

                    if (candidate.ProductId != productId
                        || candidate.RemainingUnitCount <= 0)
                    {
                        continue;
                    }

                    fixtureId = candidateId;
                    storedCase = new FixtureBackstockCaseSnapshot(
                        candidate.ProductId,
                        candidate.RemainingUnitCount,
                        candidate.CapacityUnitCount);
                    return true;
                }
            }

            fixtureId = default;
            storedCase = default;
            return false;
        }

        /// <summary>
        /// Removes one complete handling case from a concrete rack and moves
        /// its remaining units into a worker-owned inventory location.
        /// </summary>
        public FixtureBackstockCasePickupResult TryTakeCase(
            FixtureInstanceId fixtureId,
            ProductId productId,
            StorageLocationId destinationLocationId)
        {
            if (!racks.TryGetValue(fixtureId, out RackRecord rack))
            {
                return FixtureBackstockCasePickupResult.Failed(
                    FixtureBackstockCasePickupFailure.UnknownRack);
            }

            if (!inventory.ContainsLocation(destinationLocationId))
            {
                return FixtureBackstockCasePickupResult.Failed(
                    FixtureBackstockCasePickupFailure.UnknownDestination);
            }

            if (GetLocationStoredUnitCount(destinationLocationId) > 0)
            {
                return FixtureBackstockCasePickupResult.Failed(
                    FixtureBackstockCasePickupFailure.DestinationOccupied);
            }

            for (int caseIndex = 0;
                 caseIndex < rack.Cases.Count;
                 caseIndex++)
            {
                RackCaseRecord storedCase = rack.Cases[caseIndex];

                if (storedCase.ProductId != productId
                    || storedCase.RemainingUnitCount <= 0)
                {
                    continue;
                }

                FixtureBackstockCaseSnapshot snapshot =
                    new FixtureBackstockCaseSnapshot(
                        storedCase.ProductId,
                        storedCase.RemainingUnitCount,
                        storedCase.CapacityUnitCount);

                TransferRequired(
                    rack.LocationId,
                    destinationLocationId,
                    productId,
                    storedCase.RemainingUnitCount);
                rack.Cases.RemoveAt(caseIndex);
                ContentsChanged?.Invoke();

                return FixtureBackstockCasePickupResult.PickedUp(
                    fixtureId,
                    snapshot);
            }

            return FixtureBackstockCasePickupResult.Failed(
                FixtureBackstockCasePickupFailure.NoMatchingCase);
        }

        /// <summary>
        /// Returns a partially used handling case to its preferred rack. If
        /// that rack filled while the worker was away, another rack or inbound
        /// safely receives the same case without merging its boundary away.
        /// </summary>
        public FixtureBackstockCaseReturnResult TryReturnCase(
            FixtureInstanceId preferredFixtureId,
            StorageLocationId sourceLocationId,
            FixtureBackstockCaseSnapshot returnedCase)
        {
            if (!inventory.ContainsLocation(sourceLocationId))
            {
                return FixtureBackstockCaseReturnResult.Failed(
                    FixtureBackstockCaseReturnFailure.UnknownSource);
            }

            if (!returnedCase.ProductId.IsValid
                || returnedCase.RemainingUnitCount <= 0
                || returnedCase.CapacityUnitCount
                    < returnedCase.RemainingUnitCount)
            {
                return FixtureBackstockCaseReturnResult.Failed(
                    FixtureBackstockCaseReturnFailure.InvalidCase);
            }

            if (inventory.GetQuantity(
                    sourceLocationId,
                    returnedCase.ProductId)
                < returnedCase.RemainingUnitCount)
            {
                return FixtureBackstockCaseReturnResult.Failed(
                    FixtureBackstockCaseReturnFailure.InsufficientSourceStock);
            }

            FixtureInstanceId destinationFixtureId = default;
            RackRecord destinationRack = null;

            if (racks.TryGetValue(
                    preferredFixtureId,
                    out RackRecord preferredRack)
                && preferredRack.Cases.Count
                    < preferredRack.CaseSlotCapacity)
            {
                destinationFixtureId = preferredFixtureId;
                destinationRack = preferredRack;
            }
            else
            {
                for (int index = 0;
                     index < rackOrder.Count;
                     index++)
                {
                    FixtureInstanceId candidateId = rackOrder[index];
                    RackRecord candidate = racks[candidateId];

                    if (candidate.Cases.Count
                        >= candidate.CaseSlotCapacity)
                    {
                        continue;
                    }

                    destinationFixtureId = candidateId;
                    destinationRack = candidate;
                    break;
                }
            }

            RackCaseRecord caseRecord = new RackCaseRecord(
                returnedCase.ProductId,
                returnedCase.CapacityUnitCount,
                returnedCase.RemainingUnitCount);

            if (destinationRack != null)
            {
                TransferRequired(
                    sourceLocationId,
                    destinationRack.LocationId,
                    returnedCase.ProductId,
                    returnedCase.RemainingUnitCount);
                destinationRack.Cases.Add(caseRecord);
                ContentsChanged?.Invoke();

                return FixtureBackstockCaseReturnResult.ReturnedToRack(
                    destinationFixtureId,
                    returnedCase.RemainingUnitCount);
            }

            TransferRequired(
                sourceLocationId,
                LocationId,
                returnedCase.ProductId,
                returnedCase.RemainingUnitCount);
            inboundCases.Add(caseRecord);
            ContentsChanged?.Invoke();

            return FixtureBackstockCaseReturnResult.ReturnedToInbound(
                returnedCase.RemainingUnitCount);
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

                ConsumeTrackedCases(
                    rack,
                    productId,
                    rackQuantity,
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
        /// Returns product to partially emptied cases of the same product,
        /// respecting each case's own unit limit. Remainders without a case
        /// are moved to inbound so fixture edits never destroy inventory.
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

                if (sourceLocationId == rack.LocationId)
                {
                    continue;
                }

                for (int caseIndex = 0;
                     caseIndex < rack.Cases.Count
                        && remainingUnitCount > 0;
                     caseIndex++)
                {
                    RackCaseRecord storedCase = rack.Cases[caseIndex];

                    if (storedCase.ProductId != productId)
                    {
                        continue;
                    }

                    int availableCaseCapacity = Math.Max(
                        0,
                        storedCase.CapacityUnitCount
                        - storedCase.RemainingUnitCount);
                    int transferUnitCount = Math.Min(
                        remainingUnitCount,
                        availableCaseCapacity);

                    if (transferUnitCount == 0)
                    {
                        continue;
                    }

                    TransferRequired(
                        sourceLocationId,
                        rack.LocationId,
                        productId,
                        transferUnitCount);

                    storedCase.RemainingUnitCount += transferUnitCount;
                    remainingUnitCount -= transferUnitCount;
                    movedUnitCount += transferUnitCount;
                }
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

        /// <summary>
        /// Rebuilds physical case observations from restored aggregate rack
        /// balances, then republishes the restored inventory.
        /// </summary>
        public void SynchronizeAfterInventoryRestore()
        {
            inboundCases.Clear();

            for (int rackIndex = 0;
                 rackIndex < rackOrder.Count;
                 rackIndex++)
            {
                RackRecord rack = racks[rackOrder[rackIndex]];
                rack.Cases.Clear();

                foreach (
                    ProductDefinition product
                    in productCatalog.EnumerateDefinitions())
                {
                    int quantity = inventory.GetQuantity(
                        rack.LocationId,
                        product.Id);

                    if (quantity > 0)
                    {
                        rack.Cases.Add(
                            new RackCaseRecord(
                                product.Id,
                                quantity,
                                quantity));
                    }
                }
            }

            ContentsChanged?.Invoke();
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
            CaseSlotCapacity -= rack.CaseSlotCapacity;

            if (CaseSlotCapacity < 0)
            {
                throw new InvalidOperationException(
                    "Fixture backstock case-slot capacity became negative.");
            }

            DistributeUnallocatedStock();
            CapacityChanged?.Invoke();
            ContentsChanged?.Invoke();
        }

        private bool RegisterRack(
            FixtureInstance fixture)
        {
            int caseSlotCapacity =
                fixture.Definition.StorageProfile
                    .BackstockCaseSlotCapacity;

            if (caseSlotCapacity <= 0)
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
                    caseSlotCapacity));
            rackOrder.Add(fixture.Id);
            CaseSlotCapacity += caseSlotCapacity;
            return true;
        }

        private void DistributeUnallocatedStock()
        {
            for (int caseIndex = 0;
                 caseIndex < inboundCases.Count;)
            {
                RackRecord rack = FindFirstRackWithAvailableCaseSlot();

                if (rack == null)
                {
                    return;
                }

                RackCaseRecord inboundCase = inboundCases[caseIndex];
                TransferRequired(
                    LocationId,
                    rack.LocationId,
                    inboundCase.ProductId,
                    inboundCase.RemainingUnitCount);
                rack.Cases.Add(inboundCase.Clone());
                inboundCases.RemoveAt(caseIndex);
            }

            foreach (
                ProductDefinition product
                in productCatalog.EnumerateDefinitions())
            {
                int unallocatedUnitCount =
                    inventory.GetQuantity(
                        LocationId,
                        product.Id)
                    - GetTrackedInboundUnitCount(product.Id);

                if (unallocatedUnitCount <= 0)
                {
                    continue;
                }

                RackRecord rack = FindFirstRackWithAvailableCaseSlot();

                if (rack == null)
                {
                    return;
                }

                TransferRequired(
                    LocationId,
                    rack.LocationId,
                    product.Id,
                    unallocatedUnitCount);
                rack.Cases.Add(
                    new RackCaseRecord(
                        product.Id,
                        unallocatedUnitCount,
                        unallocatedUnitCount));
            }
        }

        private RackRecord FindFirstRackWithAvailableCaseSlot()
        {
            for (int index = 0; index < rackOrder.Count; index++)
            {
                RackRecord rack = racks[rackOrder[index]];

                if (rack.Cases.Count < rack.CaseSlotCapacity)
                {
                    return rack;
                }
            }

            return null;
        }

        private int GetTrackedInboundUnitCount(ProductId productId)
        {
            int trackedUnitCount = 0;

            for (int index = 0; index < inboundCases.Count; index++)
            {
                RackCaseRecord inboundCase = inboundCases[index];

                if (inboundCase.ProductId == productId)
                {
                    trackedUnitCount = checked(
                        trackedUnitCount
                        + inboundCase.RemainingUnitCount);
                }
            }

            return trackedUnitCount;
        }

        private void MoveRackContentsToInbound(
            RackRecord rack)
        {
            for (int index = 0; index < rack.Cases.Count; index++)
            {
                inboundCases.Add(rack.Cases[index].Clone());
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
                    TransferRequired(
                        rack.LocationId,
                        LocationId,
                        product.Id,
                        quantity);
                }
            }

            rack.Cases.Clear();
        }

        private static void ConsumeTrackedCases(
            RackRecord rack,
            ProductId productId,
            int rackQuantityBeforeTransfer,
            int transferUnitCount)
        {
            int trackedUnitCount = 0;

            for (int index = 0; index < rack.Cases.Count; index++)
            {
                RackCaseRecord storedCase = rack.Cases[index];

                if (storedCase.ProductId == productId)
                {
                    trackedUnitCount += storedCase.RemainingUnitCount;
                }
            }

            int untrackedUnitCount = Math.Max(
                0,
                rackQuantityBeforeTransfer - trackedUnitCount);
            int trackedTransferUnitCount = Math.Max(
                0,
                transferUnitCount - untrackedUnitCount);

            for (int index = 0;
                 index < rack.Cases.Count
                     && trackedTransferUnitCount > 0;)
            {
                RackCaseRecord storedCase = rack.Cases[index];

                if (storedCase.ProductId != productId)
                {
                    index++;
                    continue;
                }

                int consumedUnitCount = Math.Min(
                    trackedTransferUnitCount,
                    storedCase.RemainingUnitCount);
                storedCase.RemainingUnitCount -= consumedUnitCount;
                trackedTransferUnitCount -= consumedUnitCount;

                if (storedCase.RemainingUnitCount == 0)
                {
                    rack.Cases.RemoveAt(index);
                    continue;
                }

                index++;
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
                int caseSlotCapacity)
            {
                LocationId = locationId;
                CaseSlotCapacity = caseSlotCapacity;
            }


            public StorageLocationId LocationId { get; }

            public int CaseSlotCapacity { get; }

            public List<RackCaseRecord> Cases { get; } =
                new List<RackCaseRecord>();
        }


        private sealed class RackCaseRecord
        {
            public RackCaseRecord(
                ProductId productId,
                int capacityUnitCount,
                int remainingUnitCount)
            {
                ProductId = productId;
                CapacityUnitCount = capacityUnitCount;
                RemainingUnitCount = remainingUnitCount;
            }


            public ProductId ProductId { get; }

            public int CapacityUnitCount { get; }

            public int RemainingUnitCount { get; set; }

            public RackCaseRecord Clone()
            {
                return new RackCaseRecord(
                    ProductId,
                    CapacityUnitCount,
                    RemainingUnitCount);
            }
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


    /// <summary>
    /// Read-only description of one physical supplier case on a rack.
    /// Remaining units may be lower than the original pack size when a worker
    /// has opened the case to restock a sales fixture.
    /// </summary>
    public readonly struct FixtureBackstockCaseSnapshot
    {
        public FixtureBackstockCaseSnapshot(
            ProductId productId,
            int remainingUnitCount,
            int capacityUnitCount)
        {
            ProductId = productId;
            RemainingUnitCount = remainingUnitCount;
            CapacityUnitCount = capacityUnitCount;
        }


        public ProductId ProductId { get; }

        public int RemainingUnitCount { get; }

        public int CapacityUnitCount { get; }

        public int AvailableUnitCount =>
            Math.Max(0, CapacityUnitCount - RemainingUnitCount);
    }


    public enum FixtureBackstockCasePickupFailure
    {
        None = 0,
        UnknownRack = 1,
        UnknownDestination = 2,
        DestinationOccupied = 3,
        NoMatchingCase = 4
    }


    public readonly struct FixtureBackstockCasePickupResult
    {
        public FixtureInstanceId RackFixtureId { get; }

        public FixtureBackstockCaseSnapshot Case { get; }

        public FixtureBackstockCasePickupFailure Failure { get; }

        public bool Succeeded =>
            Failure == FixtureBackstockCasePickupFailure.None
            && Case.RemainingUnitCount > 0;


        private FixtureBackstockCasePickupResult(
            FixtureInstanceId rackFixtureId,
            FixtureBackstockCaseSnapshot storedCase,
            FixtureBackstockCasePickupFailure failure)
        {
            RackFixtureId = rackFixtureId;
            Case = storedCase;
            Failure = failure;
        }


        internal static FixtureBackstockCasePickupResult PickedUp(
            FixtureInstanceId rackFixtureId,
            FixtureBackstockCaseSnapshot storedCase)
        {
            return new FixtureBackstockCasePickupResult(
                rackFixtureId,
                storedCase,
                FixtureBackstockCasePickupFailure.None);
        }


        internal static FixtureBackstockCasePickupResult Failed(
            FixtureBackstockCasePickupFailure failure)
        {
            return new FixtureBackstockCasePickupResult(
                default,
                default,
                failure);
        }
    }


    public enum FixtureBackstockCaseReturnFailure
    {
        None = 0,
        UnknownSource = 1,
        InvalidCase = 2,
        InsufficientSourceStock = 3
    }


    public readonly struct FixtureBackstockCaseReturnResult
    {
        public FixtureInstanceId RackFixtureId { get; }

        public int ReturnedUnitCount { get; }

        public bool WasStoredOnRack { get; }

        public FixtureBackstockCaseReturnFailure Failure { get; }

        public bool Succeeded =>
            Failure == FixtureBackstockCaseReturnFailure.None
            && ReturnedUnitCount > 0;


        private FixtureBackstockCaseReturnResult(
            FixtureInstanceId rackFixtureId,
            int returnedUnitCount,
            bool returnedToRack,
            FixtureBackstockCaseReturnFailure failure)
        {
            RackFixtureId = rackFixtureId;
            ReturnedUnitCount = returnedUnitCount;
            WasStoredOnRack = returnedToRack;
            Failure = failure;
        }


        internal static FixtureBackstockCaseReturnResult ReturnedToRack(
            FixtureInstanceId rackFixtureId,
            int returnedUnitCount)
        {
            return new FixtureBackstockCaseReturnResult(
                rackFixtureId,
                returnedUnitCount,
                returnedToRack: true,
                FixtureBackstockCaseReturnFailure.None);
        }


        internal static FixtureBackstockCaseReturnResult ReturnedToInbound(
            int returnedUnitCount)
        {
            return new FixtureBackstockCaseReturnResult(
                default,
                returnedUnitCount,
                returnedToRack: false,
                FixtureBackstockCaseReturnFailure.None);
        }


        internal static FixtureBackstockCaseReturnResult Failed(
            FixtureBackstockCaseReturnFailure failure)
        {
            return new FixtureBackstockCaseReturnResult(
                default,
                0,
                returnedToRack: false,
                failure);
        }
    }


    public enum FixtureBackstockReceiptFailure
    {
        None = 0,
        InvalidQuantity = 1,
        UnknownRack = 2,
        NoAvailableCaseSlot = 3,
        InventoryRejected = 4
    }


    public readonly struct FixtureBackstockReceiptResult
    {
        public int ReceivedUnitCount { get; }

        public int RemainingRackCaseSlotCount { get; }

        public FixtureBackstockReceiptFailure Failure { get; }

        public bool Succeeded =>
            Failure == FixtureBackstockReceiptFailure.None
            && ReceivedUnitCount > 0;


        private FixtureBackstockReceiptResult(
            int receivedUnitCount,
            int remainingRackCaseSlotCount,
            FixtureBackstockReceiptFailure failure)
        {
            ReceivedUnitCount = receivedUnitCount;
            RemainingRackCaseSlotCount =
                remainingRackCaseSlotCount;
            Failure = failure;
        }


        internal static FixtureBackstockReceiptResult Success(
            int receivedUnitCount,
            int remainingRackCaseSlotCount)
        {
            return new FixtureBackstockReceiptResult(
                receivedUnitCount,
                remainingRackCaseSlotCount,
                FixtureBackstockReceiptFailure.None);
        }

        internal static FixtureBackstockReceiptResult Failed(
            FixtureBackstockReceiptFailure failure,
            int remainingRackCaseSlotCount = 0)
        {
            return new FixtureBackstockReceiptResult(
                receivedUnitCount: 0,
                remainingRackCaseSlotCount,
                failure);
        }
    }
}
