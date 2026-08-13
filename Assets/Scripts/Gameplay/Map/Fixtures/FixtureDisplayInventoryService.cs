using System;
using System.Collections.Generic;
using BigRetail.Inventory.Domain;
using BigRetail.Merchandise.Domain;

namespace BigRetail.Map.Fixtures
{
    /// <summary>
    /// Connects fixture planograms to physical display inventory.
    ///
    /// A planogram declares capacity. Inventory remains authoritative for how
    /// many units are physically on the fixture or in backstock.
    /// </summary>
    public sealed class FixtureDisplayInventoryService : IDisposable
    {
        public const int UnitsPerFrontageUnit = 6;

        private readonly FixtureState fixtureState;
        private readonly FixturePlanogramState planogramState;
        private readonly ProductCatalog productCatalog;
        private readonly InventoryState inventory;
        private readonly StockTransferService transfers;
        private readonly StockRemovalService removals;
        private readonly StorageLocationId backstockLocationId;
        private readonly FixtureBackstockService backstockService;

        private bool isDisposed;


        public FixtureDisplayInventoryService(
            FixtureState fixtureState,
            FixturePlanogramState planogramState,
            ProductCatalog productCatalog,
            InventoryState inventory,
            StorageLocationId backstockLocationId)
            : this(
                fixtureState,
                planogramState,
                productCatalog,
                inventory,
                backstockLocationId,
                null)
        {
        }

        public FixtureDisplayInventoryService(
            FixtureState fixtureState,
            FixturePlanogramState planogramState,
            ProductCatalog productCatalog,
            InventoryState inventory,
            FixtureBackstockService backstockService)
            : this(
                fixtureState,
                planogramState,
                productCatalog,
                inventory,
                RequireBackstockService(backstockService).LocationId,
                backstockService)
        {
        }

        private FixtureDisplayInventoryService(
            FixtureState fixtureState,
            FixturePlanogramState planogramState,
            ProductCatalog productCatalog,
            InventoryState inventory,
            StorageLocationId backstockLocationId,
            FixtureBackstockService backstockService)
        {
            this.fixtureState =
                fixtureState
                ?? throw new ArgumentNullException(nameof(fixtureState));

            this.planogramState =
                planogramState
                ?? throw new ArgumentNullException(nameof(planogramState));

            this.productCatalog =
                productCatalog
                ?? throw new ArgumentNullException(nameof(productCatalog));

            this.inventory =
                inventory
                ?? throw new ArgumentNullException(nameof(inventory));

            if (!inventory.ContainsLocation(backstockLocationId))
            {
                throw new ArgumentException(
                    "Display inventory requires a known backstock location.",
                    nameof(backstockLocationId));
            }

            this.backstockLocationId = backstockLocationId;
            this.backstockService = backstockService;
            transfers = new StockTransferService(inventory);
            removals = new StockRemovalService(inventory);

            foreach (FixtureInstance fixture in fixtureState.EnumerateFixtures())
            {
                RegisterFixtureLocation(fixture);
            }

            fixtureState.FixtureAdded += HandleFixtureAdded;
            fixtureState.FixtureRemoved += HandleFixtureRemoved;
            planogramState.ShelfRunChanged += HandleShelfRunChanged;
        }


        public event Action<FixtureInstanceId> FixtureStockChanged;


        public bool TryGetSnapshot(
            FixtureInstanceId fixtureId,
            out FixtureDisplayStockSnapshot snapshot)
        {
            if (!fixtureState.TryGetFixture(
                    fixtureId,
                    out FixtureInstance fixture))
            {
                snapshot = default;
                return false;
            }

            StorageLocationId displayLocationId =
                GetDisplayLocationId(fixtureId);

            if (!inventory.ContainsLocation(displayLocationId))
            {
                snapshot = default;
                return false;
            }

            Dictionary<ProductId, int> capacityByProduct =
                GetCapacityByProduct(fixture);

            int stockedUnitCount = 0;
            int capacityUnitCount = 0;
            int relevantBackstockUnitCount = 0;

            foreach (
                KeyValuePair<ProductId, int> entry
                in capacityByProduct)
            {
                capacityUnitCount += entry.Value;
                stockedUnitCount +=
                    Math.Min(
                        inventory.GetQuantity(
                            displayLocationId,
                            entry.Key),
                        entry.Value);
                relevantBackstockUnitCount +=
                    GetAvailableBackstockQuantity(entry.Key);
            }

            snapshot =
                new FixtureDisplayStockSnapshot(
                    stockedUnitCount,
                    capacityUnitCount,
                    relevantBackstockUnitCount);

            return true;
        }

        public float GetFrontageFillRatio(
            FixtureShelfRunKey shelfRun,
            int frontageUnitIndex)
        {
            if (!planogramState.TryGetProductAt(
                    shelfRun,
                    frontageUnitIndex,
                    out ProductId productId)
                || !fixtureState.TryGetFixture(
                    shelfRun.FixtureId,
                    out FixtureInstance fixture))
            {
                return 0f;
            }

            StorageLocationId displayLocationId =
                GetDisplayLocationId(shelfRun.FixtureId);

            if (!inventory.ContainsLocation(displayLocationId))
            {
                return 0f;
            }

            int stockedUnits =
                inventory.GetQuantity(
                    displayLocationId,
                    productId);

            int matchingUnitsBefore = 0;
            FixtureMerchandisingProfile profile =
                fixture.Definition.MerchandisingProfile;

            for (int faceIndex = 0;
                 faceIndex < profile.DisplayFaceCount;
                 faceIndex++)
            {
                FixtureDisplayFaceDefinition displayFace =
                    profile.GetDisplayFace(faceIndex);

                for (int shelfRunIndex = 0;
                     shelfRunIndex < displayFace.ShelfRunCount;
                     shelfRunIndex++)
                {
                    FixtureShelfRunKey candidateRun =
                        new FixtureShelfRunKey(
                            fixture.Id,
                            displayFace.LocalSide,
                            shelfRunIndex);

                    for (int unitIndex = 0;
                         unitIndex < displayFace.FrontageUnitsPerRun;
                         unitIndex++)
                    {
                        if (!planogramState.TryGetProductAt(
                                candidateRun,
                                unitIndex,
                                out ProductId candidateProductId)
                            || candidateProductId != productId)
                        {
                            continue;
                        }

                        if (candidateRun == shelfRun
                            && unitIndex == frontageUnitIndex)
                        {
                            int stockedOnUnit =
                                Math.Max(
                                    0,
                                    Math.Min(
                                        UnitsPerFrontageUnit,
                                        stockedUnits
                                        - matchingUnitsBefore
                                        * UnitsPerFrontageUnit));

                            return stockedOnUnit
                                / (float)UnitsPerFrontageUnit;
                        }

                        matchingUnitsBefore++;
                    }
                }
            }

            return 0f;
        }

        public FixtureRestockResult TryRestockFixture(
            FixtureInstanceId fixtureId)
        {
            if (!fixtureState.TryGetFixture(
                    fixtureId,
                    out FixtureInstance fixture))
            {
                return FixtureRestockResult.Failed(
                    FixtureRestockOutcome.UnknownFixture);
            }

            StorageLocationId displayLocationId =
                GetDisplayLocationId(fixtureId);

            if (!inventory.ContainsLocation(displayLocationId))
            {
                return FixtureRestockResult.Failed(
                    FixtureRestockOutcome.UnknownFixture);
            }

            ReconcileDisplayCapacity(fixture);

            Dictionary<ProductId, int> capacityByProduct =
                GetCapacityByProduct(fixture);

            if (capacityByProduct.Count == 0)
            {
                return FixtureRestockResult.Failed(
                    FixtureRestockOutcome.NothingAssigned);
            }

            int movedUnitCount = 0;
            int remainingShortfall = 0;

            foreach (
                KeyValuePair<ProductId, int> entry
                in capacityByProduct)
            {
                int currentQuantity =
                    inventory.GetQuantity(
                        displayLocationId,
                        entry.Key);

                int shortfall =
                    Math.Max(0, entry.Value - currentQuantity);

                if (shortfall == 0)
                {
                    continue;
                }

                int availableBackstock =
                    GetAvailableBackstockQuantity(entry.Key);

                int transferQuantity =
                    Math.Min(shortfall, availableBackstock);

                if (transferQuantity > 0)
                {
                    int transferredUnitCount =
                        backstockService != null
                            ? backstockService.TransferToLocation(
                                displayLocationId,
                                entry.Key,
                                transferQuantity)
                            : TransferFromSharedBackstock(
                                displayLocationId,
                                entry.Key,
                                transferQuantity);

                    if (transferredUnitCount != transferQuantity)
                    {
                        throw new InvalidOperationException(
                            "Calculated fixture restock did not move the expected stock quantity.");
                    }

                    movedUnitCount += transferredUnitCount;
                }

                remainingShortfall += shortfall - transferQuantity;
            }

            if (movedUnitCount > 0)
            {
                FixtureStockChanged?.Invoke(fixtureId);

                return FixtureRestockResult.Restocked(
                    movedUnitCount,
                    remainingShortfall);
            }

            return FixtureRestockResult.Failed(
                remainingShortfall > 0
                    ? FixtureRestockOutcome.BackstockUnavailable
                    : FixtureRestockOutcome.AlreadyFull);
        }

        /// <summary>
        /// Removes stocked units from one fixture as a temporary stand-in for
        /// customer purchases. The real sales loop can call the same inventory
        /// boundary later without changing planogram or restock behavior.
        /// </summary>
        public FixtureStockConsumptionResult TryConsumeFixtureStock(
            FixtureInstanceId fixtureId,
            int requestedUnitCount)
        {
            if (requestedUnitCount <= 0)
            {
                return FixtureStockConsumptionResult.Failed(
                    FixtureStockConsumptionOutcome.InvalidQuantity);
            }

            if (!fixtureState.TryGetFixture(
                    fixtureId,
                    out FixtureInstance fixture))
            {
                return FixtureStockConsumptionResult.Failed(
                    FixtureStockConsumptionOutcome.UnknownFixture);
            }

            StorageLocationId displayLocationId =
                GetDisplayLocationId(fixtureId);

            if (!inventory.ContainsLocation(displayLocationId))
            {
                return FixtureStockConsumptionResult.Failed(
                    FixtureStockConsumptionOutcome.UnknownFixture);
            }

            ReconcileDisplayCapacity(fixture);

            Dictionary<ProductId, int> capacityByProduct =
                GetCapacityByProduct(fixture);

            int remainingRequest = requestedUnitCount;
            int removedUnitCount = 0;

            foreach (
                ProductDefinition product
                in productCatalog.EnumerateDefinitions())
            {
                if (remainingRequest == 0
                    || !capacityByProduct.ContainsKey(product.Id))
                {
                    continue;
                }

                int stockedUnitCount =
                    inventory.GetQuantity(
                        displayLocationId,
                        product.Id);

                int removalQuantity =
                    Math.Min(
                        stockedUnitCount,
                        remainingRequest);

                if (removalQuantity == 0)
                {
                    continue;
                }

                StockRemovalResult removal =
                    removals.TryRemove(
                        displayLocationId,
                        product.Id,
                        removalQuantity);

                if (!removal.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Calculated fixture stock consumption failed: {removal.Failure}.");
                }

                removedUnitCount += removal.QuantityRemoved;
                remainingRequest -= removal.QuantityRemoved;
            }

            if (removedUnitCount == 0)
            {
                return FixtureStockConsumptionResult.Failed(
                    FixtureStockConsumptionOutcome.DisplayEmpty);
            }

            FixtureStockChanged?.Invoke(fixtureId);

            return FixtureStockConsumptionResult.Consumed(
                removedUnitCount,
                remainingRequest);
        }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            fixtureState.FixtureAdded -= HandleFixtureAdded;
            fixtureState.FixtureRemoved -= HandleFixtureRemoved;
            planogramState.ShelfRunChanged -= HandleShelfRunChanged;
            isDisposed = true;
        }


        public static StorageLocationId GetDisplayLocationId(
            FixtureInstanceId fixtureId)
        {
            if (!fixtureId.IsValid)
            {
                throw new ArgumentException(
                    "A display location requires a valid fixture ID.",
                    nameof(fixtureId));
            }

            return new StorageLocationId(
                $"FIXTURE-DISPLAY-{fixtureId.Value}");
        }


        private void HandleFixtureAdded(FixtureInstance fixture)
        {
            RegisterFixtureLocation(fixture);
            FixtureStockChanged?.Invoke(fixture.Id);
        }

        private void HandleFixtureRemoved(FixtureInstance fixture)
        {
            StorageLocationId displayLocationId =
                GetDisplayLocationId(fixture.Id);

            if (!inventory.ContainsLocation(displayLocationId))
            {
                return;
            }

            ReturnAllStockToBackstock(displayLocationId);

            if (!inventory.TryRemoveLocation(displayLocationId))
            {
                throw new InvalidOperationException(
                    $"Fixture display location '{displayLocationId}' still contains stock.");
            }

            FixtureStockChanged?.Invoke(fixture.Id);
        }

        private void HandleShelfRunChanged(
            FixtureShelfRunKey shelfRun)
        {
            if (!fixtureState.TryGetFixture(
                    shelfRun.FixtureId,
                    out FixtureInstance fixture))
            {
                return;
            }

            ReconcileDisplayCapacity(fixture);
            FixtureStockChanged?.Invoke(fixture.Id);
        }

        private void RegisterFixtureLocation(FixtureInstance fixture)
        {
            StorageLocationId locationId =
                GetDisplayLocationId(fixture.Id);

            if (!inventory.TryRegisterLocation(
                    new StorageLocationDefinition(
                        locationId,
                        $"{fixture.Definition.DisplayName} Display",
                        StorageRole.SalesFloor)))
            {
                throw new InvalidOperationException(
                    $"Fixture display location '{locationId}' already exists.");
            }
        }

        private void ReconcileDisplayCapacity(FixtureInstance fixture)
        {
            StorageLocationId displayLocationId =
                GetDisplayLocationId(fixture.Id);

            Dictionary<ProductId, int> capacityByProduct =
                GetCapacityByProduct(fixture);

            foreach (
                ProductDefinition product
                in productCatalog.EnumerateDefinitions())
            {
                capacityByProduct.TryGetValue(
                    product.Id,
                    out int capacity);

                int currentQuantity =
                    inventory.GetQuantity(
                        displayLocationId,
                        product.Id);

                int excessQuantity =
                    Math.Max(0, currentQuantity - capacity);

                if (excessQuantity == 0)
                {
                    continue;
                }

                int returnedUnitCount =
                    ReturnToBackstock(
                        displayLocationId,
                        product.Id,
                        excessQuantity);

                if (returnedUnitCount != excessQuantity)
                {
                    throw new InvalidOperationException(
                        "Calculated display reconciliation did not return all excess stock.");
                }
            }
        }

        private void ReturnAllStockToBackstock(
            StorageLocationId displayLocationId)
        {
            foreach (
                ProductDefinition product
                in productCatalog.EnumerateDefinitions())
            {
                int quantity =
                    inventory.GetQuantity(
                        displayLocationId,
                        product.Id);

                if (quantity == 0)
                {
                    continue;
                }

                int returnedUnitCount =
                    ReturnToBackstock(
                        displayLocationId,
                        product.Id,
                        quantity);

                if (returnedUnitCount != quantity)
                {
                    throw new InvalidOperationException(
                        "Fixture stock return did not move all display stock.");
                }
            }
        }

        private int TransferFromSharedBackstock(
            StorageLocationId destinationLocationId,
            ProductId productId,
            int quantity)
        {
            StockTransferResult transfer =
                transfers.TryTransfer(
                    backstockLocationId,
                    destinationLocationId,
                    productId,
                    quantity);

            if (!transfer.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Calculated fixture restock failed: {transfer.Failure}.");
            }

            return transfer.QuantityMoved;
        }

        private int ReturnToBackstock(
            StorageLocationId sourceLocationId,
            ProductId productId,
            int quantity)
        {
            if (backstockService != null)
            {
                return backstockService.StoreFromLocation(
                    sourceLocationId,
                    productId,
                    quantity);
            }

            StockTransferResult transfer =
                transfers.TryTransfer(
                    sourceLocationId,
                    backstockLocationId,
                    productId,
                    quantity);

            if (!transfer.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Fixture stock return failed: {transfer.Failure}.");
            }

            return transfer.QuantityMoved;
        }

        private Dictionary<ProductId, int> GetCapacityByProduct(
            FixtureInstance fixture)
        {
            Dictionary<ProductId, int> result =
                new Dictionary<ProductId, int>();

            FixtureMerchandisingProfile profile =
                fixture.Definition.MerchandisingProfile;

            for (int faceIndex = 0;
                 faceIndex < profile.DisplayFaceCount;
                 faceIndex++)
            {
                FixtureDisplayFaceDefinition displayFace =
                    profile.GetDisplayFace(faceIndex);

                for (int shelfRunIndex = 0;
                     shelfRunIndex < displayFace.ShelfRunCount;
                     shelfRunIndex++)
                {
                    FixtureShelfRunKey shelfRun =
                        new FixtureShelfRunKey(
                            fixture.Id,
                            displayFace.LocalSide,
                            shelfRunIndex);

                    for (int frontageUnitIndex = 0;
                         frontageUnitIndex < displayFace.FrontageUnitsPerRun;
                         frontageUnitIndex++)
                    {
                        if (!planogramState.TryGetProductAt(
                                shelfRun,
                                frontageUnitIndex,
                                out ProductId productId))
                        {
                            continue;
                        }

                        result.TryGetValue(
                            productId,
                            out int productCapacity);

                        result[productId] =
                            productCapacity + UnitsPerFrontageUnit;
                    }
                }
            }

            return result;
        }

        private int GetAvailableBackstockQuantity(
            ProductId productId)
        {
            return backstockService != null
                ? backstockService.GetAvailableQuantity(productId)
                : inventory.GetQuantity(
                    backstockLocationId,
                    productId);
        }

        private static FixtureBackstockService RequireBackstockService(
            FixtureBackstockService backstockService)
        {
            return backstockService
                ?? throw new ArgumentNullException(
                    nameof(backstockService));
        }
    }


    public readonly struct FixtureDisplayStockSnapshot
    {
        public int StockedUnitCount { get; }

        public int CapacityUnitCount { get; }

        public int BackstockUnitCount { get; }

        public int MissingUnitCount =>
            Math.Max(0, CapacityUnitCount - StockedUnitCount);

        public bool CanRestock =>
            MissingUnitCount > 0
            && BackstockUnitCount > 0;


        internal FixtureDisplayStockSnapshot(
            int stockedUnitCount,
            int capacityUnitCount,
            int backstockUnitCount)
        {
            StockedUnitCount = stockedUnitCount;
            CapacityUnitCount = capacityUnitCount;
            BackstockUnitCount = backstockUnitCount;
        }
    }


    public enum FixtureRestockOutcome
    {
        None = 0,
        Restocked = 1,
        NothingAssigned = 2,
        AlreadyFull = 3,
        BackstockUnavailable = 4,
        UnknownFixture = 5
    }


    public readonly struct FixtureRestockResult
    {
        public FixtureRestockOutcome Outcome { get; }

        public int MovedUnitCount { get; }

        public int RemainingShortfall { get; }

        public bool Succeeded =>
            Outcome == FixtureRestockOutcome.Restocked;


        private FixtureRestockResult(
            FixtureRestockOutcome outcome,
            int movedUnitCount,
            int remainingShortfall)
        {
            Outcome = outcome;
            MovedUnitCount = movedUnitCount;
            RemainingShortfall = remainingShortfall;
        }


        internal static FixtureRestockResult Restocked(
            int movedUnitCount,
            int remainingShortfall)
        {
            return new FixtureRestockResult(
                FixtureRestockOutcome.Restocked,
                movedUnitCount,
                remainingShortfall);
        }

        internal static FixtureRestockResult Failed(
            FixtureRestockOutcome outcome)
        {
            return new FixtureRestockResult(
                outcome,
                0,
                0);
        }
    }


    public enum FixtureStockConsumptionOutcome
    {
        None = 0,
        Consumed = 1,
        DisplayEmpty = 2,
        InvalidQuantity = 3,
        UnknownFixture = 4
    }


    public readonly struct FixtureStockConsumptionResult
    {
        public FixtureStockConsumptionOutcome Outcome { get; }

        public int ConsumedUnitCount { get; }

        public int UnfulfilledUnitCount { get; }

        public bool Succeeded =>
            Outcome == FixtureStockConsumptionOutcome.Consumed;


        private FixtureStockConsumptionResult(
            FixtureStockConsumptionOutcome outcome,
            int consumedUnitCount,
            int unfulfilledUnitCount)
        {
            Outcome = outcome;
            ConsumedUnitCount = consumedUnitCount;
            UnfulfilledUnitCount = unfulfilledUnitCount;
        }


        internal static FixtureStockConsumptionResult Consumed(
            int consumedUnitCount,
            int unfulfilledUnitCount)
        {
            return new FixtureStockConsumptionResult(
                FixtureStockConsumptionOutcome.Consumed,
                consumedUnitCount,
                unfulfilledUnitCount);
        }

        internal static FixtureStockConsumptionResult Failed(
            FixtureStockConsumptionOutcome outcome)
        {
            return new FixtureStockConsumptionResult(
                outcome,
                0,
                0);
        }
    }
}
