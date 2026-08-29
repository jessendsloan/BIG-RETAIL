using System;
using System.Collections.Generic;
using BigRetail.Inventory.Domain;
using BigRetail.Map.Fixtures;
using BigRetail.Map.Unity.Fixtures;
using BigRetail.Merchandise.Domain;
using BigRetail.Purchasing.Domain;
using BigRetail.Purchasing.Unity;
using BigRetail.Simulation.Time.Domain;
using BigRetail.Simulation.Time.Unity;

namespace BigRetail.StoreLayouts.Unity
{
    /// <summary>
    /// Applies one validated opening state after its physical layout exists.
    /// All scenario-owned runtime values are captured and rolled back as one
    /// transaction if any canonical service rejects application.
    /// </summary>
    public sealed class StoreScenarioRuntimeLoader
    {
        private readonly FixtureRuntimeHost fixtureHost;
        private readonly FixturePlanogramRuntimeHost merchandisingHost;
        private readonly SimulationTimeRuntimeHost timeHost;
        private readonly PurchasingRuntimeHost purchasingHost;
        private readonly StoreDataCanonicalizer canonicalizer =
            new StoreDataCanonicalizer();
        private readonly StoreScenarioValidator validator =
            new StoreScenarioValidator();


        public string ActiveScenarioId { get; private set; } =
            string.Empty;

        public int ActiveDeterministicSeed { get; private set; }

        public bool IsLoading { get; private set; }


        public event Action<StoreScenarioData> ScenarioLoaded;


        public StoreScenarioRuntimeLoader(
            FixtureRuntimeHost fixtureHost,
            FixturePlanogramRuntimeHost merchandisingHost,
            SimulationTimeRuntimeHost timeHost,
            PurchasingRuntimeHost purchasingHost)
        {
            this.fixtureHost = fixtureHost;
            this.merchandisingHost = merchandisingHost;
            this.timeHost = timeHost;
            this.purchasingHost = purchasingHost;
        }


        public StoreScenarioLoadResult Load(
            StoreScenarioAsset scenarioAsset,
            StoreLayoutAsset layoutAsset)
        {
            if (scenarioAsset == null)
            {
                return StoreScenarioLoadResult.Rejected(
                    StoreScenarioLoadFailure.ValidationFailed,
                    "No StoreScenarioAsset was supplied.");
            }

            if (layoutAsset == null)
            {
                return StoreScenarioLoadResult.Rejected(
                    StoreScenarioLoadFailure.ValidationFailed,
                    "No StoreLayoutAsset was supplied for scenario validation.");
            }

            try
            {
                return Load(
                    scenarioAsset.CreateRuntimeCopy(),
                    layoutAsset.CreateRuntimeCopy());
            }
            catch (Exception exception)
            {
                return StoreScenarioLoadResult.Rejected(
                    StoreScenarioLoadFailure.ValidationFailed,
                    exception.Message);
            }
        }

        public StoreScenarioLoadResult Load(
            StoreScenarioData scenario,
            StoreLayoutData layout)
        {
            if (IsLoading)
            {
                return StoreScenarioLoadResult.Rejected(
                    StoreScenarioLoadFailure.RuntimeUnavailable,
                    "A store scenario transaction is already running.");
            }

            if (!TryPrepareRuntime(out string preparationError))
            {
                return StoreScenarioLoadResult.Rejected(
                    StoreScenarioLoadFailure.RuntimeUnavailable,
                    preparationError);
            }

            StoreScenarioData canonical;

            try
            {
                canonical = canonicalizer.CreateCanonicalCopy(scenario);
            }
            catch (Exception exception)
            {
                return StoreScenarioLoadResult.Rejected(
                    StoreScenarioLoadFailure.ValidationFailed,
                    exception.Message);
            }

            StoreDataValidationResult validation =
                validator.Validate(
                    canonical,
                    layout,
                    new ScenarioDefinitionCatalog(
                        merchandisingHost.Products,
                        purchasingHost.Catalog.Suppliers));

            if (!validation.IsValid)
            {
                return StoreScenarioLoadResult.Rejected(
                    StoreScenarioLoadFailure.ValidationFailed,
                    "The store scenario failed preflight validation.",
                    validation);
            }

            if (canonical.Spawns.Count > 0
                || canonical.StoryFlags.Count > 0)
            {
                return StoreScenarioLoadResult.Rejected(
                    StoreScenarioLoadFailure.UnsupportedContent,
                    "This scenario runtime does not yet support "
                    + "character-spawn or story-flag records.");
            }

            if (!TryValidateRuntimeTargets(
                    canonical,
                    out string runtimeValidationError))
            {
                return StoreScenarioLoadResult.Rejected(
                    StoreScenarioLoadFailure.ValidationFailed,
                    runtimeValidationError);
            }

            RuntimeSnapshot previous = CaptureCurrent();
            string previousScenarioId = ActiveScenarioId;
            int previousSeed = ActiveDeterministicSeed;
            StoreScenarioLoadResult result;

            IsLoading = true;

            try
            {
                Apply(canonical);
                ActiveScenarioId = canonical.ScenarioId;
                ActiveDeterministicSeed = canonical.DeterministicSeed;
                result = StoreScenarioLoadResult.Success(
                    canonical.ScenarioId);
            }
            catch (Exception applyException)
            {
                try
                {
                    Restore(previous);
                    ActiveScenarioId = previousScenarioId;
                    ActiveDeterministicSeed = previousSeed;

                    result = StoreScenarioLoadResult.Rejected(
                        StoreScenarioLoadFailure.ApplyFailed,
                        $"Scenario application failed: "
                        + $"{applyException.Message}",
                        previousStateRestored: true);
                }
                catch (Exception rollbackException)
                {
                    ActiveScenarioId = string.Empty;
                    ActiveDeterministicSeed = 0;

                    result = StoreScenarioLoadResult.Rejected(
                        StoreScenarioLoadFailure.RollbackFailed,
                        $"Scenario application failed: "
                        + $"{applyException.Message} Rollback also failed: "
                        + $"{rollbackException.Message}",
                        previousStateRestored: false);
                }
            }
            finally
            {
                IsLoading = false;
            }

            if (result.Succeeded)
            {
                ScenarioLoaded?.Invoke(canonical);
            }

            return result;
        }


        private bool TryPrepareRuntime(
            out string error)
        {
            if (fixtureHost == null
                || merchandisingHost == null
                || timeHost == null
                || purchasingHost == null)
            {
                error =
                    "The scenario loader is missing one or more runtime hosts.";
                return false;
            }

            timeHost.Initialize();

            if (!fixtureHost.TryInitialize()
                || !merchandisingHost.TryInitialize()
                || merchandisingHost.Inventory == null
                || merchandisingHost.Planograms == null
                || merchandisingHost.DisplayInventory == null
                || merchandisingHost.Backstock == null
                || merchandisingHost.Checkout == null
                || merchandisingHost.Cash == null
                || merchandisingHost.Products == null
                || !purchasingHost.TryInitialize()
                || purchasingHost.Catalog == null)
            {
                error =
                    "The location runtime could not initialize every "
                    + "scenario dependency.";
                return false;
            }

            if (merchandisingHost.Cash.IsUnlimited)
            {
                error =
                    "A permanent scenario cannot replace Map Workshop cash.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        private bool TryValidateRuntimeTargets(
            StoreScenarioData scenario,
            out string error)
        {
            Dictionary<string, int> capacityByFixtureProduct =
                new Dictionary<string, int>(StringComparer.Ordinal);

            for (int index = 0;
                 index < scenario.PlanogramAssignments.Count;
                 index++)
            {
                StorePlanogramAssignmentData assignment =
                    scenario.PlanogramAssignments[index];
                FixtureInstanceId fixtureId =
                    new FixtureInstanceId(
                        assignment.FixtureInstanceId);

                if (!fixtureHost.FixtureState.TryGetFixture(
                        fixtureId,
                        out FixtureInstance fixture))
                {
                    error =
                        $"Scenario fixture '{fixtureId}' is not present in "
                        + "the loaded runtime layout.";
                    return false;
                }

                FixtureMerchandisingProfile profile =
                    fixture.Definition.MerchandisingProfile;

                if (assignment.DisplayFaceIndex < 0
                    || assignment.DisplayFaceIndex
                        >= profile.DisplayFaceCount)
                {
                    error =
                        $"Scenario fixture '{fixtureId}' has no display face "
                        + $"{assignment.DisplayFaceIndex}.";
                    return false;
                }

                FixtureDisplayFaceDefinition face =
                    profile.GetDisplayFace(
                        assignment.DisplayFaceIndex);

                if (assignment.ShelfRunIndex < 0
                    || assignment.ShelfRunIndex >= face.ShelfRunCount
                    || assignment.FrontageUnitIndex < 0
                    || assignment.FrontageUnitIndex
                        >= face.FrontageUnitsPerRun)
                {
                    error =
                        $"Scenario planogram target on fixture '{fixtureId}' "
                        + "is outside its merchandising profile.";
                    return false;
                }

                ProductId productId =
                    new ProductId(assignment.ProductId);
                string key = CreateFixtureProductKey(
                    fixtureId,
                    productId);

                capacityByFixtureProduct.TryGetValue(
                    key,
                    out int existingCapacity);
                capacityByFixtureProduct[key] =
                    existingCapacity
                    + FixtureDisplayInventoryService.UnitsPerFrontageUnit;
            }

            for (int index = 0;
                 index < scenario.DisplayInventory.Count;
                 index++)
            {
                StoreDisplayInventoryData line =
                    scenario.DisplayInventory[index];
                FixtureInstanceId fixtureId =
                    new FixtureInstanceId(line.FixtureInstanceId);
                ProductId productId =
                    new ProductId(line.ProductId);
                string key = CreateFixtureProductKey(
                    fixtureId,
                    productId);

                if (!capacityByFixtureProduct.TryGetValue(
                        key,
                        out int capacity)
                    || line.Quantity > capacity)
                {
                    error =
                        $"Scenario display stock for '{productId}' on "
                        + $"fixture '{fixtureId}' exceeds its assigned "
                        + "planogram capacity.";
                    return false;
                }
            }

            for (int index = 0;
                 index < scenario.Checkouts.Count;
                 index++)
            {
                FixtureInstanceId fixtureId =
                    new FixtureInstanceId(
                        scenario.Checkouts[index].FixtureInstanceId);

                if (!merchandisingHost.Checkout
                        .IsCheckoutStation(fixtureId))
                {
                    error =
                        $"Scenario checkout '{fixtureId}' is not an "
                        + "operational checkout fixture.";
                    return false;
                }
            }

            List<InboundDeliveryRestoreData> deliveries;

            try
            {
                deliveries =
                    CreateDeliveryRestoreData(
                        scenario.Deliveries);
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }

            if (!purchasingHost.TryValidateDeliveryRestore(
                    deliveries,
                    out error))
            {
                return false;
            }

            error = string.Empty;
            return true;
        }

        private RuntimeSnapshot CaptureCurrent()
        {
            RuntimeSnapshot snapshot =
                new RuntimeSnapshot
                {
                    ClockState = timeHost.CaptureState(),
                    CashBalanceCents =
                        merchandisingHost.Cash.BalanceCents
                };

            foreach (
                StockBalance balance
                in merchandisingHost.Inventory.EnumerateBalances())
            {
                snapshot.Inventory.Add(balance);
            }

            foreach (
                FixtureInstance fixture
                in fixtureHost.FixtureState.EnumerateFixtures())
            {
                FixtureMerchandisingProfile profile =
                    fixture.Definition.MerchandisingProfile;

                for (int faceIndex = 0;
                     faceIndex < profile.DisplayFaceCount;
                     faceIndex++)
                {
                    FixtureDisplayFaceDefinition face =
                        profile.GetDisplayFace(faceIndex);

                    for (int shelfRunIndex = 0;
                         shelfRunIndex < face.ShelfRunCount;
                         shelfRunIndex++)
                    {
                        FixtureShelfRunKey shelfRun =
                            new FixtureShelfRunKey(
                                fixture.Id,
                                face.LocalSide,
                                shelfRunIndex);

                        for (int frontageIndex = 0;
                             frontageIndex
                                < face.FrontageUnitsPerRun;
                             frontageIndex++)
                        {
                            if (merchandisingHost.PlanogramState
                                    .TryGetProductAt(
                                        shelfRun,
                                        frontageIndex,
                                        out ProductId productId))
                            {
                                snapshot.Planograms.Add(
                                    new RuntimePlanogramAssignment(
                                        shelfRun,
                                        frontageIndex,
                                        productId));
                            }
                        }
                    }
                }

                if (merchandisingHost.Checkout
                        .IsCheckoutStation(fixture.Id))
                {
                    snapshot.Checkouts.Add(
                        new RuntimeCheckoutState(
                            fixture.Id,
                            merchandisingHost.Checkout
                                .IsOpen(fixture.Id)));
                }
            }

            return snapshot;
        }

        private void Apply(
            StoreScenarioData scenario)
        {
            merchandisingHost.Inventory.RestoreBalances(
                Array.Empty<StockBalance>());
            ClearPlanograms();
            CloseEveryCheckout();

            for (int index = 0;
                 index < scenario.PlanogramAssignments.Count;
                 index++)
            {
                StorePlanogramAssignmentData assignment =
                    scenario.PlanogramAssignments[index];
                FixtureInstanceId fixtureId =
                    new FixtureInstanceId(
                        assignment.FixtureInstanceId);

                Require(
                    fixtureHost.FixtureState.TryGetFixture(
                        fixtureId,
                        out FixtureInstance fixture),
                    $"Fixture '{fixtureId}' disappeared during scenario load.");

                FixtureDisplayFaceDefinition face =
                    fixture.Definition.MerchandisingProfile
                        .GetDisplayFace(
                            assignment.DisplayFaceIndex);
                FixtureShelfRunKey shelfRun =
                    new FixtureShelfRunKey(
                        fixtureId,
                        face.LocalSide,
                        assignment.ShelfRunIndex);

                Require(
                    merchandisingHost.Planograms.TryAssignFrontage(
                        shelfRun,
                        assignment.FrontageUnitIndex,
                        1,
                        new ProductId(assignment.ProductId),
                        out FixturePlanogramFailure failure),
                    $"Could not restore planogram on fixture "
                    + $"'{fixtureId}': {failure}.");
            }

            List<StockBalance> displayBalances =
                new List<StockBalance>(
                    scenario.DisplayInventory.Count);

            for (int index = 0;
                 index < scenario.DisplayInventory.Count;
                 index++)
            {
                StoreDisplayInventoryData line =
                    scenario.DisplayInventory[index];

                if (line.Quantity <= 0)
                {
                    continue;
                }

                FixtureInstanceId fixtureId =
                    new FixtureInstanceId(line.FixtureInstanceId);

                displayBalances.Add(
                    new StockBalance(
                        FixtureDisplayInventoryService
                            .GetDisplayLocationId(fixtureId),
                        new ProductId(line.ProductId),
                        line.Quantity));
            }

            merchandisingHost.Inventory.RestoreBalances(
                displayBalances);
            merchandisingHost.DisplayInventory
                .SynchronizeAfterInventoryRestore();

            for (int index = 0;
                 index < scenario.BackstockInventory.Count;
                 index++)
            {
                StoreInventoryLineData line =
                    scenario.BackstockInventory[index];

                if (line.Quantity <= 0)
                {
                    continue;
                }

                StockAdditionResult result =
                    merchandisingHost.Backstock.ReceiveInbound(
                        new ProductId(line.ProductId),
                        line.Quantity);

                Require(
                    result.Succeeded
                    && result.QuantityAdded == line.Quantity,
                    $"Could not restore backstock for "
                    + $"'{line.ProductId}': {result.Failure}.");
            }

            merchandisingHost.Backstock
                .SynchronizeAfterInventoryRestore();

            for (int index = 0;
                 index < scenario.Checkouts.Count;
                 index++)
            {
                StoreCheckoutData checkout =
                    scenario.Checkouts[index];

                Require(
                    merchandisingHost.Checkout.TrySetOpen(
                        new FixtureInstanceId(
                            checkout.FixtureInstanceId),
                        checkout.IsOpen),
                    $"Could not restore checkout "
                    + $"'{checkout.FixtureInstanceId}'.");
            }

            merchandisingHost.Cash.RestoreBalance(
                scenario.StartingStoreCashCents);
            timeHost.RestoreState(
                new SimulationClockState(
                    scenario.StartingGameSeconds,
                    0d,
                    (SimulationSpeed)
                        scenario.StartingSimulationSpeed));

            Require(
                purchasingHost.TryReplaceDeliveries(
                    CreateDeliveryRestoreData(
                        scenario.Deliveries),
                    out string deliveryError),
                $"Could not restore inbound deliveries: "
                + deliveryError);
        }

        private void Restore(
            RuntimeSnapshot snapshot)
        {
            merchandisingHost.Inventory.RestoreBalances(
                Array.Empty<StockBalance>());
            ClearPlanograms();

            for (int index = 0;
                 index < snapshot.Planograms.Count;
                 index++)
            {
                RuntimePlanogramAssignment assignment =
                    snapshot.Planograms[index];

                Require(
                    merchandisingHost.Planograms.TryAssignFrontage(
                        assignment.ShelfRun,
                        assignment.FrontageUnitIndex,
                        1,
                        assignment.ProductId,
                        out FixturePlanogramFailure failure),
                    $"Could not roll back a planogram: {failure}.");
            }

            merchandisingHost.Inventory.RestoreBalances(
                snapshot.Inventory);
            merchandisingHost.DisplayInventory
                .SynchronizeAfterInventoryRestore();
            merchandisingHost.Backstock
                .SynchronizeAfterInventoryRestore();

            CloseEveryCheckout();

            for (int index = 0;
                 index < snapshot.Checkouts.Count;
                 index++)
            {
                RuntimeCheckoutState checkout =
                    snapshot.Checkouts[index];

                Require(
                    merchandisingHost.Checkout.TrySetOpen(
                        checkout.FixtureId,
                        checkout.IsOpen),
                    $"Could not roll back checkout "
                    + $"'{checkout.FixtureId}'.");
            }

            merchandisingHost.Cash.RestoreBalance(
                snapshot.CashBalanceCents);
            timeHost.RestoreState(snapshot.ClockState);
        }

        private void ClearPlanograms()
        {
            foreach (
                FixtureInstance fixture
                in fixtureHost.FixtureState.EnumerateFixtures())
            {
                FixtureMerchandisingProfile profile =
                    fixture.Definition.MerchandisingProfile;

                for (int faceIndex = 0;
                     faceIndex < profile.DisplayFaceCount;
                     faceIndex++)
                {
                    FixtureDisplayFaceDefinition face =
                        profile.GetDisplayFace(faceIndex);

                    for (int shelfRunIndex = 0;
                         shelfRunIndex < face.ShelfRunCount;
                         shelfRunIndex++)
                    {
                        Require(
                            merchandisingHost.Planograms
                                .TryClearShelfRun(
                                    new FixtureShelfRunKey(
                                        fixture.Id,
                                        face.LocalSide,
                                        shelfRunIndex),
                                    out FixturePlanogramFailure failure),
                            $"Could not clear a planogram on fixture "
                            + $"'{fixture.Id}': {failure}.");
                    }
                }
            }
        }

        private void CloseEveryCheckout()
        {
            foreach (
                FixtureInstance fixture
                in fixtureHost.FixtureState.EnumerateFixtures())
            {
                if (merchandisingHost.Checkout
                        .IsCheckoutStation(fixture.Id))
                {
                    Require(
                        merchandisingHost.Checkout.TrySetOpen(
                            fixture.Id,
                            false),
                        $"Could not close checkout '{fixture.Id}'.");
                }
            }
        }

        private static string CreateFixtureProductKey(
            FixtureInstanceId fixtureId,
            ProductId productId)
        {
            return fixtureId.Value + "|" + productId.Value;
        }

        private static List<InboundDeliveryRestoreData>
            CreateDeliveryRestoreData(
                IReadOnlyList<StoreDeliveryData> deliveries)
        {
            List<InboundDeliveryRestoreData> restored =
                new List<InboundDeliveryRestoreData>(
                    deliveries.Count);

            for (int deliveryIndex = 0;
                 deliveryIndex < deliveries.Count;
                 deliveryIndex++)
            {
                StoreDeliveryData delivery =
                    deliveries[deliveryIndex];
                List<InboundDeliveryRestoreLine> lines =
                    new List<InboundDeliveryRestoreLine>(
                        delivery.Lines.Count);

                for (int lineIndex = 0;
                     lineIndex < delivery.Lines.Count;
                     lineIndex++)
                {
                    StoreInventoryLineData line =
                        delivery.Lines[lineIndex];

                    lines.Add(
                        new InboundDeliveryRestoreLine(
                            new ProductId(line.ProductId),
                            line.Quantity));
                }

                CommercialTime arrivalTime =
                    PurchasingRuntimeHost.ToCommercialTime(
                        SimulationDateTime.FromTotalGameSeconds(
                            delivery.ArrivalGameSeconds));

                restored.Add(
                    new InboundDeliveryRestoreData(
                        deliveryIndex + 1L,
                        new SupplierId(delivery.SupplierId),
                        arrivalTime,
                        ResolveDeliveryStatus(delivery.Status),
                        lines));
            }

            return restored;
        }

        private static PurchaseOrderDeliveryStatus
            ResolveDeliveryStatus(
                StoreDeliveryStatus status)
        {
            return status switch
            {
                StoreDeliveryStatus.Scheduled =>
                    PurchaseOrderDeliveryStatus.Scheduled,
                StoreDeliveryStatus.ReadyToReceive =>
                    PurchaseOrderDeliveryStatus.ReadyToReceive,
                StoreDeliveryStatus.Received =>
                    PurchaseOrderDeliveryStatus.Received,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(status),
                    status,
                    "The scenario delivery status is unsupported.")
            };
        }

        private static void Require(
            bool condition,
            string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }


        private sealed class ScenarioDefinitionCatalog :
            IStoreDefinitionCatalog
        {
            private readonly ProductCatalog products;
            private readonly SupplierCatalog suppliers;


            public ScenarioDefinitionCatalog(
                ProductCatalog products,
                SupplierCatalog suppliers)
            {
                this.products = products;
                this.suppliers = suppliers;
            }


            public bool Contains(
                StoreDefinitionKind kind,
                string definitionId)
            {
                if (string.IsNullOrWhiteSpace(definitionId))
                {
                    return false;
                }

                try
                {
                    return kind switch
                    {
                        StoreDefinitionKind.Product =>
                            products.Contains(
                                new ProductId(definitionId)),
                        StoreDefinitionKind.Supplier =>
                            suppliers.Contains(
                                new SupplierId(definitionId)),
                        _ => false
                    };
                }
                catch (ArgumentException)
                {
                    return false;
                }
            }
        }


        private sealed class RuntimeSnapshot
        {
            public SimulationClockState ClockState;
            public long CashBalanceCents;
            public readonly List<StockBalance> Inventory =
                new List<StockBalance>();
            public readonly List<RuntimePlanogramAssignment> Planograms =
                new List<RuntimePlanogramAssignment>();
            public readonly List<RuntimeCheckoutState> Checkouts =
                new List<RuntimeCheckoutState>();
        }


        private readonly struct RuntimePlanogramAssignment
        {
            public RuntimePlanogramAssignment(
                FixtureShelfRunKey shelfRun,
                int frontageUnitIndex,
                ProductId productId)
            {
                ShelfRun = shelfRun;
                FrontageUnitIndex = frontageUnitIndex;
                ProductId = productId;
            }


            public FixtureShelfRunKey ShelfRun { get; }

            public int FrontageUnitIndex { get; }

            public ProductId ProductId { get; }
        }


        private readonly struct RuntimeCheckoutState
        {
            public RuntimeCheckoutState(
                FixtureInstanceId fixtureId,
                bool isOpen)
            {
                FixtureId = fixtureId;
                IsOpen = isOpen;
            }


            public FixtureInstanceId FixtureId { get; }

            public bool IsOpen { get; }
        }
    }
}
