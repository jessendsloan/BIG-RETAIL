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


        public int GetDisplayedQuantity(
            ProductId productId)
        {
            long displayedUnitCount = 0;

            foreach (FixtureInstance fixture in fixtureState.EnumerateFixtures())
            {
                StorageLocationId displayLocationId =
                    GetDisplayLocationId(fixture.Id);

                if (!inventory.ContainsLocation(displayLocationId))
                {
                    continue;
                }

                displayedUnitCount +=
                    inventory.GetQuantity(
                        displayLocationId,
                        productId);

                if (displayedUnitCount >= int.MaxValue)
                {
                    return int.MaxValue;
                }
            }

            return (int)displayedUnitCount;
        }


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
            int unitsPerFrontageUnit =
                GetUnitsPerFrontageUnit(productId);

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
                                        unitsPerFrontageUnit,
                                        stockedUnits
                                        - matchingUnitsBefore
                                        * unitsPerFrontageUnit));

                            return stockedOnUnit
                                / (float)unitsPerFrontageUnit;
                        }

                        matchingUnitsBefore++;
                    }
                }
            }

            return 0f;
        }

        /// <summary>
        /// Finds the first assigned product that is physically stocked on a
        /// fixture. Catalog order keeps the graybox sale choice deterministic.
        /// A future customer choice can bypass this helper and request a
        /// specific product through TryConsumeProductStock.
        /// </summary>
        public bool TryGetFirstStockedProduct(
            FixtureInstanceId fixtureId,
            out ProductId productId)
        {
            productId = default;

            if (!fixtureState.TryGetFixture(
                    fixtureId,
                    out FixtureInstance fixture))
            {
                return false;
            }

            StorageLocationId displayLocationId =
                GetDisplayLocationId(fixtureId);

            if (!inventory.ContainsLocation(displayLocationId))
            {
                return false;
            }

            ReconcileDisplayCapacity(fixture);

            Dictionary<ProductId, int> capacityByProduct =
                GetCapacityByProduct(fixture);

            foreach (
                ProductDefinition product
                in productCatalog.EnumerateDefinitions())
            {
                if (!capacityByProduct.ContainsKey(product.Id)
                    || inventory.GetQuantity(
                        displayLocationId,
                        product.Id) <= 0)
                {
                    continue;
                }

                productId = product.Id;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Removes a specific assigned product from one display. This is the
        /// inventory half of a sale transaction; pricing and cash belong to
        /// FixtureSalesService.
        /// </summary>
        public FixtureStockConsumptionResult TryConsumeProductStock(
            FixtureInstanceId fixtureId,
            ProductId productId,
            int requestedUnitCount)
        {
            if (requestedUnitCount <= 0)
            {
                return FixtureStockConsumptionResult.Failed(
                    FixtureStockConsumptionOutcome.InvalidQuantity);
            }

            if (!productCatalog.Contains(productId))
            {
                return FixtureStockConsumptionResult.Failed(
                    FixtureStockConsumptionOutcome.UnknownProduct);
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

            if (!GetCapacityByProduct(fixture).ContainsKey(productId))
            {
                return FixtureStockConsumptionResult.Failed(
                    FixtureStockConsumptionOutcome.ProductNotAssigned);
            }

            int stockedUnitCount =
                inventory.GetQuantity(
                    displayLocationId,
                    productId);

            int removalQuantity =
                Math.Min(
                    stockedUnitCount,
                    requestedUnitCount);

            if (removalQuantity == 0)
            {
                return FixtureStockConsumptionResult.Failed(
                    FixtureStockConsumptionOutcome.DisplayEmpty);
            }

            StockRemovalResult removal =
                removals.TryRemove(
                    displayLocationId,
                    productId,
                    removalQuantity);

            if (!removal.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Calculated fixture stock consumption failed: {removal.Failure}.");
            }

            FixtureStockChanged?.Invoke(fixtureId);

            return FixtureStockConsumptionResult.Consumed(
                removal.QuantityRemoved,
                requestedUnitCount - removal.QuantityRemoved);
        }

        /// <summary>
        /// Transfers displayed merchandise into shopper-owned inventory. The
        /// display is depleted now, while pricing and payment remain the
        /// responsibility of the checkout transaction.
        /// </summary>
        public FixtureBasketPickupResult TryMoveProductToBasket(
            FixtureInstanceId fixtureId,
            ProductId productId,
            int requestedUnitCount,
            ShoppingBasket basket)
        {
            if (basket == null)
            {
                return FixtureBasketPickupResult.Failed(
                    FixtureBasketPickupOutcome.BasketUnavailable);
            }

            if (!basket.CanAccept(
                    fixtureId,
                    productId,
                    requestedUnitCount))
            {
                return FixtureBasketPickupResult.Failed(
                    requestedUnitCount <= 0
                        ? FixtureBasketPickupOutcome.InvalidQuantity
                        : FixtureBasketPickupOutcome.BasketLimitReached);
            }

            FixtureStockConsumptionResult consumption =
                TryConsumeProductStock(
                    fixtureId,
                    productId,
                    requestedUnitCount);

            if (!consumption.Succeeded)
            {
                return FixtureBasketPickupResult.Failed(
                    MapPickupOutcome(consumption.Outcome));
            }

            basket.Add(
                fixtureId,
                productId,
                consumption.ConsumedUnitCount);

            return FixtureBasketPickupResult.PickedUp(
                productId,
                consumption.ConsumedUnitCount,
                consumption.UnfulfilledUnitCount);
        }

        public FixtureRestockResult TryRestockFixture(
            FixtureInstanceId fixtureId)
        {
            return TryRestockFixture(
                fixtureId,
                int.MaxValue);
        }


        public FixtureRestockResult TryRestockFixture(
            FixtureInstanceId fixtureId,
            int maximumUnitCount)
        {
            if (maximumUnitCount <= 0)
            {
                return FixtureRestockResult.Failed(
                    FixtureRestockOutcome.InvalidQuantity);
            }

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
            int remainingTransferUnitCount = maximumUnitCount;

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
                    Math.Min(
                        shortfall,
                        Math.Min(
                            availableBackstock,
                            remainingTransferUnitCount));

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
                    remainingTransferUnitCount -= transferredUnitCount;
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
        /// Finds the next assigned product that is both short on the target
        /// fixture and available in physical backstock. Worker schedulers use
        /// this before choosing a concrete case and pickup rack.
        /// </summary>
        public bool TryGetNextRestockProduct(
            FixtureInstanceId fixtureId,
            out ProductId productId,
            out int missingUnitCount)
        {
            productId = default;
            missingUnitCount = 0;

            if (!fixtureState.TryGetFixture(
                    fixtureId,
                    out FixtureInstance fixture))
            {
                return false;
            }

            ReconcileDisplayCapacity(fixture);

            StorageLocationId displayLocationId =
                GetDisplayLocationId(fixtureId);
            Dictionary<ProductId, int> capacityByProduct =
                GetCapacityByProduct(fixture);

            foreach (
                KeyValuePair<ProductId, int> entry
                in capacityByProduct)
            {
                int currentQuantity = inventory.GetQuantity(
                    displayLocationId,
                    entry.Key);
                int shortfall = Math.Max(
                    0,
                    entry.Value - currentQuantity);

                if (shortfall <= 0
                    || GetAvailableBackstockQuantity(entry.Key) <= 0)
                {
                    continue;
                }

                productId = entry.Key;
                missingUnitCount = shortfall;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Stocks from a specific inventory location, such as a case carried
        /// by a worker. Unlike the ordinary backstock operation, this never
        /// reaches into a rack on the caller's behalf.
        /// </summary>
        public FixtureRestockResult TryRestockFixtureFromLocation(
            FixtureInstanceId fixtureId,
            StorageLocationId sourceLocationId,
            int maximumUnitCount)
        {
            if (maximumUnitCount <= 0)
            {
                return FixtureRestockResult.Failed(
                    FixtureRestockOutcome.InvalidQuantity);
            }

            if (!inventory.ContainsLocation(sourceLocationId))
            {
                return FixtureRestockResult.Failed(
                    FixtureRestockOutcome.SourceUnavailable);
            }

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
            int remainingTransferUnitCount = maximumUnitCount;

            foreach (
                KeyValuePair<ProductId, int> entry
                in capacityByProduct)
            {
                int currentQuantity = inventory.GetQuantity(
                    displayLocationId,
                    entry.Key);
                int shortfall = Math.Max(
                    0,
                    entry.Value - currentQuantity);

                if (shortfall == 0)
                {
                    continue;
                }

                int availableSourceQuantity = inventory.GetQuantity(
                    sourceLocationId,
                    entry.Key);
                int transferQuantity = Math.Min(
                    shortfall,
                    Math.Min(
                        availableSourceQuantity,
                        remainingTransferUnitCount));

                if (transferQuantity > 0)
                {
                    StockTransferResult transfer = transfers.TryTransfer(
                        sourceLocationId,
                        displayLocationId,
                        entry.Key,
                        transferQuantity);

                    if (!transfer.Succeeded)
                    {
                        throw new InvalidOperationException(
                            "Calculated carried-stock transfer failed: "
                            + transfer.Failure
                            + ".");
                    }

                    movedUnitCount += transferQuantity;
                    remainingTransferUnitCount -= transferQuantity;
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
                    ? FixtureRestockOutcome.SourceUnavailable
                    : FixtureRestockOutcome.AlreadyFull);
        }

        /// <summary>
        /// Moves physical display stock back into storage. This is distinct
        /// from customer consumption: no inventory is destroyed, and a
        /// one-unit request represents removing one handled package.
        /// </summary>
        public FixtureUnstockResult TryReturnFixtureStockToBackstock(
            FixtureInstanceId fixtureId,
            int maximumUnitCount)
        {
            if (maximumUnitCount <= 0)
            {
                return FixtureUnstockResult.Failed(
                    FixtureUnstockOutcome.InvalidQuantity);
            }

            if (!fixtureState.TryGetFixture(
                    fixtureId,
                    out FixtureInstance fixture))
            {
                return FixtureUnstockResult.Failed(
                    FixtureUnstockOutcome.UnknownFixture);
            }

            StorageLocationId displayLocationId =
                GetDisplayLocationId(fixtureId);

            if (!inventory.ContainsLocation(displayLocationId))
            {
                return FixtureUnstockResult.Failed(
                    FixtureUnstockOutcome.UnknownFixture);
            }

            ReconcileDisplayCapacity(fixture);

            Dictionary<ProductId, int> capacityByProduct =
                GetCapacityByProduct(fixture);

            if (capacityByProduct.Count == 0)
            {
                return FixtureUnstockResult.Failed(
                    FixtureUnstockOutcome.NothingAssigned);
            }

            int remainingRequest = maximumUnitCount;
            int returnedUnitCount = 0;

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
                int returnQuantity =
                    Math.Min(
                        stockedUnitCount,
                        remainingRequest);

                if (returnQuantity == 0)
                {
                    continue;
                }

                int movedUnitCount =
                    ReturnToBackstock(
                        displayLocationId,
                        product.Id,
                        returnQuantity);

                returnedUnitCount += movedUnitCount;
                remainingRequest -= movedUnitCount;
            }

            if (returnedUnitCount == 0)
            {
                return FixtureUnstockResult.Failed(
                    FixtureUnstockOutcome.DisplayEmpty);
            }

            FixtureStockChanged?.Invoke(fixtureId);

            return FixtureUnstockResult.Returned(
                returnedUnitCount,
                remainingRequest);
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

        /// <summary>
        /// Reconciles restored inventory with current planogram capacity and
        /// republishes fixture stock so every subscribed view refreshes.
        /// </summary>
        public void SynchronizeAfterInventoryRestore()
        {
            foreach (
                FixtureInstance fixture
                in fixtureState.EnumerateFixtures())
            {
                ReconcileDisplayCapacity(fixture);
                FixtureStockChanged?.Invoke(fixture.Id);
            }
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
                            productCapacity
                            + GetUnitsPerFrontageUnit(productId);
                    }
                }
            }

            return result;
        }

        private int GetUnitsPerFrontageUnit(
            ProductId productId)
        {
            return productCatalog.GetRequired(productId)
                .DisplayUnitsPerFrontageUnit;
        }

        private static FixtureBasketPickupOutcome MapPickupOutcome(
            FixtureStockConsumptionOutcome outcome)
        {
            return outcome switch
            {
                FixtureStockConsumptionOutcome.DisplayEmpty =>
                    FixtureBasketPickupOutcome.DisplayEmpty,
                FixtureStockConsumptionOutcome.InvalidQuantity =>
                    FixtureBasketPickupOutcome.InvalidQuantity,
                FixtureStockConsumptionOutcome.UnknownFixture =>
                    FixtureBasketPickupOutcome.UnknownFixture,
                FixtureStockConsumptionOutcome.UnknownProduct =>
                    FixtureBasketPickupOutcome.UnknownProduct,
                FixtureStockConsumptionOutcome.ProductNotAssigned =>
                    FixtureBasketPickupOutcome.ProductNotAssigned,
                _ => FixtureBasketPickupOutcome.InventoryUnavailable
            };
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
        UnknownFixture = 5,
        InvalidQuantity = 6,
        SourceUnavailable = 7
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


    public enum FixtureUnstockOutcome
    {
        None = 0,
        ReturnedToBackstock = 1,
        NothingAssigned = 2,
        DisplayEmpty = 3,
        UnknownFixture = 4,
        InvalidQuantity = 5
    }


    public readonly struct FixtureUnstockResult
    {
        public FixtureUnstockOutcome Outcome { get; }

        public int ReturnedUnitCount { get; }

        public int UnfulfilledUnitCount { get; }

        public bool Succeeded =>
            Outcome == FixtureUnstockOutcome.ReturnedToBackstock;


        private FixtureUnstockResult(
            FixtureUnstockOutcome outcome,
            int returnedUnitCount,
            int unfulfilledUnitCount)
        {
            Outcome = outcome;
            ReturnedUnitCount = returnedUnitCount;
            UnfulfilledUnitCount = unfulfilledUnitCount;
        }


        internal static FixtureUnstockResult Returned(
            int returnedUnitCount,
            int unfulfilledUnitCount)
        {
            return new FixtureUnstockResult(
                FixtureUnstockOutcome.ReturnedToBackstock,
                returnedUnitCount,
                unfulfilledUnitCount);
        }

        internal static FixtureUnstockResult Failed(
            FixtureUnstockOutcome outcome)
        {
            return new FixtureUnstockResult(
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
        UnknownFixture = 4,
        UnknownProduct = 5,
        ProductNotAssigned = 6
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


    public enum FixtureBasketPickupOutcome
    {
        None = 0,
        PickedUp = 1,
        DisplayEmpty = 2,
        InvalidQuantity = 3,
        UnknownFixture = 4,
        UnknownProduct = 5,
        ProductNotAssigned = 6,
        BasketUnavailable = 7,
        BasketLimitReached = 8,
        InventoryUnavailable = 9
    }


    public readonly struct FixtureBasketPickupResult
    {
        public FixtureBasketPickupOutcome Outcome { get; }

        public ProductId ProductId { get; }

        public int PickedUpUnitCount { get; }

        public int UnfulfilledUnitCount { get; }

        public bool Succeeded =>
            Outcome == FixtureBasketPickupOutcome.PickedUp;


        private FixtureBasketPickupResult(
            FixtureBasketPickupOutcome outcome,
            ProductId productId,
            int pickedUpUnitCount,
            int unfulfilledUnitCount)
        {
            Outcome = outcome;
            ProductId = productId;
            PickedUpUnitCount = pickedUpUnitCount;
            UnfulfilledUnitCount = unfulfilledUnitCount;
        }


        internal static FixtureBasketPickupResult PickedUp(
            ProductId productId,
            int pickedUpUnitCount,
            int unfulfilledUnitCount)
        {
            return new FixtureBasketPickupResult(
                FixtureBasketPickupOutcome.PickedUp,
                productId,
                pickedUpUnitCount,
                unfulfilledUnitCount);
        }

        internal static FixtureBasketPickupResult Failed(
            FixtureBasketPickupOutcome outcome)
        {
            return new FixtureBasketPickupResult(
                outcome,
                default,
                0,
                0);
        }
    }
}
