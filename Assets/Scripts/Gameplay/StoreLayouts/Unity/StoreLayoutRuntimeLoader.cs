using System;
using System.Collections.Generic;
using BigRetail.Departments;
using BigRetail.Departments.Unity;
using BigRetail.Map.Domain;
using BigRetail.Map.Floors;
using BigRetail.Map.Fixtures;
using BigRetail.Map.Foundations;
using BigRetail.Map.Sidewalks;
using BigRetail.Map.Unity;
using BigRetail.Map.Unity.Fixtures;
using BigRetail.Map.Unity.Floors;
using BigRetail.Map.Unity.Foundations;
using BigRetail.Map.Unity.Sidewalks;
using BigRetail.Map.Walls;
using BigRetail.Receiving.Domain;
using BigRetail.Receiving.Unity;

namespace BigRetail.StoreLayouts.Unity
{
    /// <summary>
    /// Applies one preflighted physical-store template through the permanent
    /// construction services. Loading bypasses tools, prices, objectives, and
    /// undo history while retaining the same authoritative runtime states.
    /// </summary>
    public sealed class StoreLayoutRuntimeLoader
    {
        private readonly GridMapHost mapHost;
        private readonly FoundationRuntimeHost foundationHost;
        private readonly SidewalkRuntimeHost sidewalkHost;
        private readonly FloorRuntimeHost floorHost;
        private readonly FixtureRuntimeHost fixtureHost;
        private readonly FixtureEquipmentPlanState fixturePlanState;
        private readonly DepartmentRuntimeHost departmentHost;
        private readonly ReceivingAreaRuntimeHost receivingHost;
        private FixtureEquipmentPlanningService fixturePlanning;
        private readonly StoreDataCanonicalizer canonicalizer =
            new StoreDataCanonicalizer();
        private readonly StoreLayoutValidator validator =
            new StoreLayoutValidator();


        public string ActiveLayoutId { get; private set; } =
            string.Empty;

        public bool IsLoading { get; private set; }


        public event Action<StoreLayoutData> LayoutLoaded;


        public StoreLayoutRuntimeLoader(
            GridMapHost mapHost,
            FoundationRuntimeHost foundationHost,
            SidewalkRuntimeHost sidewalkHost,
            FloorRuntimeHost floorHost,
            FixtureRuntimeHost fixtureHost,
            FixtureEquipmentPlanState fixturePlanState,
            DepartmentRuntimeHost departmentHost,
            ReceivingAreaRuntimeHost receivingHost)
        {
            this.mapHost = mapHost;
            this.foundationHost = foundationHost;
            this.sidewalkHost = sidewalkHost;
            this.floorHost = floorHost;
            this.fixtureHost = fixtureHost;
            this.fixturePlanState = fixturePlanState;
            this.departmentHost = departmentHost;
            this.receivingHost = receivingHost;
        }


        public StoreDataValidationResult Validate(
            StoreLayoutData layout)
        {
            if (!TryPrepareRuntime(out string error))
            {
                throw new InvalidOperationException(error);
            }

            return validator.Validate(
                layout,
                CreateValidationContext());
        }


        public StoreLayoutLoadResult Load(
            StoreLayoutAsset asset)
        {
            if (asset == null)
            {
                return StoreLayoutLoadResult.Rejected(
                    StoreLayoutLoadFailure.ValidationFailed,
                    "No StoreLayoutAsset was supplied.");
            }

            try
            {
                return Load(asset.CreateRuntimeCopy());
            }
            catch (Exception exception)
            {
                return StoreLayoutLoadResult.Rejected(
                    StoreLayoutLoadFailure.ValidationFailed,
                    exception.Message);
            }
        }


        public StoreLayoutLoadResult Load(
            StoreLayoutData layout)
        {
            if (IsLoading)
            {
                return StoreLayoutLoadResult.Rejected(
                    StoreLayoutLoadFailure.RuntimeUnavailable,
                    "A store layout transaction is already running.");
            }

            if (!TryPrepareRuntime(out string preparationError))
            {
                return StoreLayoutLoadResult.Rejected(
                    StoreLayoutLoadFailure.RuntimeUnavailable,
                    preparationError);
            }

            StoreLayoutData canonical;

            try
            {
                canonical =
                    canonicalizer.CreateCanonicalCopy(layout);
            }
            catch (Exception exception)
            {
                return StoreLayoutLoadResult.Rejected(
                    StoreLayoutLoadFailure.ValidationFailed,
                    exception.Message);
            }

            StoreDataValidationResult validation =
                validator.Validate(
                    canonical,
                    CreateValidationContext());

            if (!validation.IsValid)
            {
                return StoreLayoutLoadResult.Rejected(
                    StoreLayoutLoadFailure.ValidationFailed,
                    "The store layout failed preflight validation.",
                    validation);
            }

            if (receivingHost.State.ReservationCount > 0)
            {
                return StoreLayoutLoadResult.Rejected(
                    StoreLayoutLoadFailure.ActiveDeliveries,
                    "A layout cannot be replaced while a delivery occupies "
                    + "the Receiving Area.");
            }

            if (!HasMatchingLandEntitlement(canonical))
            {
                return StoreLayoutLoadResult.Rejected(
                    StoreLayoutLoadFailure.ValidationFailed,
                    "The selected layout does not match the location's "
                    + "current land entitlement.");
            }

            StoreLayoutData previous =
                CaptureCurrent(
                    "bigretail.layout.runtime.rollback",
                    "Runtime rollback snapshot");
            string previousLayoutId = ActiveLayoutId;
            StoreLayoutLoadResult result;

            IsLoading = true;

            try
            {
                ClearCurrent();
                Apply(canonical);
                ActiveLayoutId = canonical.LayoutId;
                result = StoreLayoutLoadResult.Success(
                    canonical.LayoutId);
            }
            catch (Exception applyException)
            {
                try
                {
                    ClearCurrent();
                    Apply(previous);
                    ActiveLayoutId = previousLayoutId;

                    result = StoreLayoutLoadResult.Rejected(
                        StoreLayoutLoadFailure.ApplyFailed,
                        $"Layout application failed: "
                        + $"{applyException.Message}",
                        previousStateRestored: true);
                }
                catch (Exception rollbackException)
                {
                    ActiveLayoutId = string.Empty;
                    result = StoreLayoutLoadResult.Rejected(
                        StoreLayoutLoadFailure.RollbackFailed,
                        $"Layout application failed: "
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
                PublishLayoutLoaded(canonical);
            }

            return result;
        }


        public StoreLayoutData CaptureCurrent(
            string layoutId,
            string displayName)
        {
            if (!TryPrepareRuntime(out string error))
            {
                throw new InvalidOperationException(error);
            }

            StoreLayoutData snapshot =
                new StoreLayoutData
                {
                    LayoutId = layoutId,
                    DisplayName = displayName,
                    MapId = mapHost.MapDefinition.MapId,
                    MapFingerprint = mapHost.MapFingerprint
                };

            foreach (string regionId in
                     mapHost.LandPolicy.EnumerateOwnedLandRegionIds())
            {
                snapshot.OwnedLandRegionIds.Add(regionId);
            }

            foreach (GridPosition cell in
                     foundationHost.FoundationState
                         .EnumerateFoundations())
            {
                snapshot.Foundations.Add(
                    StoreLayoutRuntimeConversions.ToStoreCell(cell));
            }

            foreach (GridPosition cell in
                     sidewalkHost.SidewalkState
                         .EnumerateSidewalks())
            {
                snapshot.Sidewalks.Add(
                    StoreLayoutRuntimeConversions.ToStoreCell(cell));
            }

            foreach (GridPosition cell in
                     floorHost.FloorState.EnumerateFloors())
            {
                snapshot.Floors.Add(
                    new StoreFloorData(
                        StoreLayoutRuntimeConversions.ToStoreCell(cell),
                        floorHost.FloorFinishes
                            .GetEffectiveFinish(cell)
                            .Value));
            }

            foreach (CellEdge edge in
                     mapHost.WallState.EnumerateWalls())
            {
                snapshot.Walls.Add(
                    new StoreWallData(
                        StoreLayoutRuntimeConversions.ToStoreEdge(edge),
                        mapHost.WallFinishes.GetEffectiveFinish(
                            edge,
                            edge.FirstCell).Value,
                        mapHost.WallFinishes.GetEffectiveFinish(
                            edge,
                            edge.SecondCell).Value));
            }

            foreach (DoorAssembly assembly in
                     mapHost.DoorAssemblies.EnumerateAssemblies())
            {
                StoreOpeningData opening =
                    new StoreOpeningData
                    {
                        InstanceId = assembly.Id.Value,
                        DefinitionId = assembly.DefinitionId.Value
                    };

                for (int edgeIndex = 0;
                     edgeIndex < assembly.Edges.Count;
                     edgeIndex++)
                {
                    opening.Edges.Add(
                        StoreLayoutRuntimeConversions.ToStoreEdge(
                            assembly.Edges[edgeIndex]));
                }

                snapshot.Openings.Add(opening);
            }

            foreach (FixtureInstance fixture in
                     fixtureHost.FixtureState.EnumerateFixtures())
            {
                StoreFixtureData fixtureData =
                    new StoreFixtureData
                    {
                        InstanceId = fixture.Id.Value,
                        DefinitionId = fixture.DefinitionId.Value,
                        AnchorCell =
                            StoreLayoutRuntimeConversions.ToStoreCell(
                                fixture.AnchorCell),
                        Orientation =
                            StoreLayoutRuntimeConversions
                                .ToStoreOrientation(
                                    fixture.Orientation)
                    };

                for (int cellIndex = 0;
                     cellIndex < fixture.OccupiedCellCount;
                     cellIndex++)
                {
                    fixtureData.OccupiedCells.Add(
                        StoreLayoutRuntimeConversions.ToStoreCell(
                            fixture.GetOccupiedCell(cellIndex)));
                }

                snapshot.Fixtures.Add(fixtureData);
            }

            foreach (FixtureEquipmentPlan plan in
                     fixturePlanState.EnumeratePlans())
            {
                StoreFixturePlanData planData =
                    new StoreFixturePlanData
                    {
                        InstanceId = plan.Id.Value,
                        DefinitionId =
                            plan.FixtureDefinitionId.Value,
                        AnchorCell =
                            StoreLayoutRuntimeConversions.ToStoreCell(
                                plan.AnchorCell),
                        Orientation =
                            StoreLayoutRuntimeConversions
                                .ToStoreOrientation(plan.Orientation)
                    };

                for (int cellIndex = 0;
                     cellIndex < plan.Footprint.CellCount;
                     cellIndex++)
                {
                    planData.OccupiedCells.Add(
                        StoreLayoutRuntimeConversions.ToStoreCell(
                            plan.Footprint.GetCell(cellIndex)));
                }

                snapshot.FixturePlans.Add(planData);
            }

            foreach (DepartmentPlan plan in
                     departmentHost.PlanningState.EnumeratePlans())
            {
                StoreDepartmentData department =
                    new StoreDepartmentData
                    {
                        InstanceId = plan.Id.Value,
                        DefinitionId = plan.DefinitionId.Value
                    };

                foreach (GridPosition cell in
                         plan.EnumerateCells())
                {
                    department.Cells.Add(
                        StoreLayoutRuntimeConversions.ToStoreCell(cell));
                }

                snapshot.Departments.Add(department);
            }

            foreach (GridPosition cell in
                     receivingHost.State.EnumerateCells())
            {
                snapshot.ReceivingCells.Add(
                    StoreLayoutRuntimeConversions.ToStoreCell(cell));
            }

            return canonicalizer.CreateCanonicalCopy(snapshot);
        }


        private bool TryPrepareRuntime(
            out string error)
        {
            if (mapHost == null
                || foundationHost == null
                || sidewalkHost == null
                || floorHost == null
                || fixtureHost == null
                || fixturePlanState == null
                || departmentHost == null
                || receivingHost == null)
            {
                error =
                    "The layout loader is missing one or more runtime hosts.";
                return false;
            }

            mapHost.Initialize();

            if (!mapHost.IsInitialized
                || !foundationHost.TryInitialize()
                || !sidewalkHost.TryInitialize()
                || !floorHost.TryInitialize()
                || !fixtureHost.TryInitialize()
                || !departmentHost.TryInitialize()
                || !receivingHost.TryInitialize())
            {
                error =
                    "The location runtime could not initialize every "
                    + "layout dependency.";
                return false;
            }

            if (fixturePlanning == null)
            {
                fixturePlanning =
                    new FixtureEquipmentPlanningService(
                        fixtureHost.FixturePlacement,
                        fixturePlanState);
            }

            error = string.Empty;
            return true;
        }


        private void PublishLayoutLoaded(
            StoreLayoutData loadedLayout)
        {
            Action<StoreLayoutData> handlers = LayoutLoaded;

            if (handlers == null)
            {
                return;
            }

            Delegate[] invocationList = handlers.GetInvocationList();

            for (int index = 0;
                 index < invocationList.Length;
                 index++)
            {
                try
                {
                    ((Action<StoreLayoutData>)invocationList[index])
                        .Invoke(loadedLayout);
                }
                catch (Exception exception)
                {
                    UnityEngine.Debug.LogException(exception);
                }
            }
        }


        private StoreLocationValidationContext CreateValidationContext()
        {
            List<StoreCellData> validCells =
                new List<StoreCellData>();

            foreach (GridPosition cell in
                     mapHost.MapDefinition.EnumerateValidCells())
            {
                validCells.Add(
                    StoreLayoutRuntimeConversions.ToStoreCell(cell));
            }

            return new StoreLocationValidationContext(
                mapHost.MapDefinition.MapId,
                mapHost.MapFingerprint,
                validCells,
                mapHost.LandPolicy.EnumerateDefinedLandRegionIds(),
                new StoreRuntimeDefinitionCatalog(
                    mapHost,
                    floorHost,
                    fixtureHost,
                    departmentHost));
        }


        private bool HasMatchingLandEntitlement(
            StoreLayoutData layout)
        {
            HashSet<string> expected =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);

            for (int index = 0;
                 index < layout.OwnedLandRegionIds.Count;
                 index++)
            {
                expected.Add(
                    layout.OwnedLandRegionIds[index].Trim());
            }

            HashSet<string> current =
                new HashSet<string>(
                    mapHost.LandPolicy
                        .EnumerateOwnedLandRegionIds(),
                    StringComparer.OrdinalIgnoreCase);

            return expected.SetEquals(current);
        }


        private void ClearCurrent()
        {
            ClearReceiving();
            ClearDepartments();
            ClearFixturePlans();
            ClearFixtures();
            ClearOpenings();
            ClearWalls();
            ClearFloors();
            ClearSidewalks();
            ClearFoundations();
        }


        private void ClearFixturePlans()
        {
            List<FixtureInstanceId> planIds =
                new List<FixtureInstanceId>();

            foreach (FixtureEquipmentPlan plan in
                     fixturePlanState.EnumeratePlans())
            {
                planIds.Add(plan.Id);
            }

            for (int index = 0;
                 index < planIds.Count;
                 index++)
            {
                FixtureEquipmentPlanResult result =
                    fixturePlanning.TryRemovePlan(planIds[index]);

                Require(
                    result.Succeeded,
                    $"Could not clear fixture plan {planIds[index]}: "
                    + $"{result.Failure}.");
            }
        }


        private void ClearReceiving()
        {
            List<GridPosition> cells =
                new List<GridPosition>(
                    receivingHost.State.EnumerateCells());

            if (cells.Count == 0)
            {
                return;
            }

            ReceivingAreaChangeResult result =
                receivingHost.Designations.TryRemoveArea(cells);

            Require(
                result.Succeeded,
                $"Could not clear Receiving Area: {result.Failure}.");
        }


        private void ClearDepartments()
        {
            List<DepartmentPlanId> planIds =
                new List<DepartmentPlanId>();

            foreach (DepartmentPlan plan in
                     departmentHost.PlanningState.EnumeratePlans())
            {
                planIds.Add(plan.Id);
            }

            for (int index = 0;
                 index < planIds.Count;
                 index++)
            {
                DepartmentPlanChangeResult result =
                    departmentHost.Planning.TryRemovePlan(
                        planIds[index]);

                Require(
                    result.Succeeded,
                    $"Could not clear department {planIds[index]}: "
                    + $"{result.Failure}.");
            }
        }


        private void ClearFixtures()
        {
            List<FixtureInstanceId> fixtureIds =
                new List<FixtureInstanceId>();

            foreach (FixtureInstance fixture in
                     fixtureHost.FixtureState.EnumerateFixtures())
            {
                fixtureIds.Add(fixture.Id);
            }

            for (int index = 0;
                 index < fixtureIds.Count;
                 index++)
            {
                FixturePlacementResult result =
                    fixtureHost.FixturePlacement.TryRemoveFixture(
                        fixtureIds[index]);

                Require(
                    result.Succeeded,
                    $"Could not clear fixture {fixtureIds[index]}: "
                    + $"{result.Failure}.");
            }
        }


        private void ClearOpenings()
        {
            List<DoorAssemblyId> openingIds =
                new List<DoorAssemblyId>();

            foreach (DoorAssembly assembly in
                     mapHost.DoorAssemblies.EnumerateAssemblies())
            {
                openingIds.Add(assembly.Id);
            }

            for (int index = 0;
                 index < openingIds.Count;
                 index++)
            {
                DoorAssemblyChangeResult result =
                    mapHost.DoorConstruction.TryRemoveAssembly(
                        openingIds[index]);

                Require(
                    result.Succeeded,
                    $"Could not clear opening {openingIds[index]}: "
                    + $"{result.Failure}.");
            }
        }


        private void ClearWalls()
        {
            List<CellEdge> edges =
                new List<CellEdge>(
                    mapHost.WallState.EnumerateWalls());

            if (edges.Count == 0)
            {
                return;
            }

            WallClearResult result =
                mapHost.WallConstruction.TryClearWalls(edges);

            Require(
                result.Succeeded,
                $"Could not clear walls: {result.Failure}.");
        }


        private void ClearFloors()
        {
            List<GridPosition> cells =
                new List<GridPosition>(
                    floorHost.FloorState.EnumerateFloors());

            if (cells.Count == 0)
            {
                return;
            }

            FloorClearResult result =
                floorHost.FloorConstruction.TryClearFloors(cells);

            Require(
                result.Succeeded,
                $"Could not clear floors: {result.Failure}.");
        }


        private void ClearSidewalks()
        {
            List<GridPosition> cells =
                new List<GridPosition>(
                    sidewalkHost.SidewalkState.EnumerateSidewalks());

            if (cells.Count == 0)
            {
                return;
            }

            SidewalkClearResult result =
                sidewalkHost.SidewalkConstruction
                    .TryClearSidewalks(cells);

            Require(
                result.Succeeded,
                $"Could not clear sidewalks: {result.Failure}.");
        }


        private void ClearFoundations()
        {
            List<GridPosition> cells =
                new List<GridPosition>(
                    foundationHost.FoundationState
                        .EnumerateFoundations());

            if (cells.Count == 0)
            {
                return;
            }

            FoundationClearResult result =
                foundationHost.FoundationConstruction
                    .TryClearFoundations(cells);

            Require(
                result.Succeeded,
                $"Could not clear foundations: {result.Failure}.");
        }


        private void Apply(
            StoreLayoutData layout)
        {
            ApplyFoundations(layout.Foundations);
            ApplySidewalks(layout.Sidewalks);
            ApplyFloors(layout.Floors);
            ApplyWalls(layout.Walls);
            ApplyOpenings(layout.Openings);
            ApplyFixtures(layout.Fixtures);
            ApplyFixturePlans(layout.FixturePlans);
            ApplyDepartments(layout.Departments);
            ApplyReceiving(layout.ReceivingCells);
        }


        private void ApplyFoundations(
            IReadOnlyList<StoreCellData> foundations)
        {
            List<GridPosition> cells = ToGridPositions(foundations);

            if (cells.Count == 0)
            {
                return;
            }

            FoundationEnsureResult result =
                foundationHost.FoundationConstruction
                    .TryEnsureFoundations(cells);

            Require(
                result.Succeeded
                && result.SkippedCount == 0
                && result.SatisfiedCount == cells.Count,
                $"Could not restore foundations: {result}.");
        }


        private void ApplySidewalks(
            IReadOnlyList<StoreCellData> sidewalks)
        {
            List<GridPosition> cells = ToGridPositions(sidewalks);

            if (cells.Count == 0)
            {
                return;
            }

            SidewalkEnsureResult result =
                sidewalkHost.SidewalkConstruction
                    .TryEnsureSidewalks(cells);

            Require(
                result.Succeeded
                && result.SkippedCount == 0
                && result.SatisfiedCount == cells.Count,
                $"Could not restore sidewalks: {result.Failure}.");
        }


        private void ApplyFloors(
            IReadOnlyList<StoreFloorData> floors)
        {
            List<GridPosition> cells =
                new List<GridPosition>(floors.Count);

            for (int index = 0;
                 index < floors.Count;
                 index++)
            {
                cells.Add(
                    StoreLayoutRuntimeConversions.ToGridPosition(
                        floors[index].Cell));
            }

            if (cells.Count == 0)
            {
                return;
            }

            FloorEnsureResult result =
                floorHost.FloorConstruction.TryEnsureFloors(cells);

            Require(
                result.Succeeded
                && result.SkippedCount == 0
                && result.SatisfiedCount == cells.Count,
                $"Could not restore floors: {result}.");

            for (int index = 0;
                 index < floors.Count;
                 index++)
            {
                FloorFinishChangeResult finish =
                    floorHost.FloorFinishes.TrySetFinish(
                        cells[index],
                        new FloorFinishId(floors[index].FinishId));

                Require(
                    finish.Succeeded,
                    $"Could not restore floor finish at "
                    + $"{floors[index].Cell}: {finish.Failure}.");
            }
        }


        private void ApplyWalls(
            IReadOnlyList<StoreWallData> walls)
        {
            List<CellEdge> edges =
                new List<CellEdge>(walls.Count);

            for (int index = 0;
                 index < walls.Count;
                 index++)
            {
                edges.Add(
                    StoreLayoutRuntimeConversions.ToCellEdge(
                        walls[index].Edge));
            }

            if (edges.Count == 0)
            {
                return;
            }

            WallEnsureResult result =
                mapHost.WallConstruction.TryEnsureWalls(edges);

            Require(
                result.Succeeded
                && result.SkippedCount == 0
                && result.SatisfiedCount == edges.Count,
                $"Could not restore walls: {result}.");

            for (int index = 0;
                 index < walls.Count;
                 index++)
            {
                CellEdge edge = edges[index];

                WallFinishChangeResult firstFinish =
                    mapHost.WallFinishes.TrySetFinish(
                        edge,
                        edge.FirstCell,
                        new WallFinishId(
                            walls[index].FirstCellFinishId));

                Require(
                    firstFinish.Succeeded,
                    $"Could not restore the first wall finish at "
                    + $"{walls[index].Edge}: "
                    + $"{firstFinish.Failure}.");

                WallFinishChangeResult secondFinish =
                    mapHost.WallFinishes.TrySetFinish(
                        edge,
                        edge.SecondCell,
                        new WallFinishId(
                            walls[index].SecondCellFinishId));

                Require(
                    secondFinish.Succeeded,
                    $"Could not restore the second wall finish at "
                    + $"{walls[index].Edge}: "
                    + $"{secondFinish.Failure}.");
            }
        }


        private void ApplyOpenings(
            IReadOnlyList<StoreOpeningData> openings)
        {
            for (int index = 0;
                 index < openings.Count;
                 index++)
            {
                List<CellEdge> edges =
                    new List<CellEdge>(
                        openings[index].Edges.Count);

                for (int edgeIndex = 0;
                     edgeIndex < openings[index].Edges.Count;
                     edgeIndex++)
                {
                    edges.Add(
                        StoreLayoutRuntimeConversions.ToCellEdge(
                            openings[index].Edges[edgeIndex]));
                }

                DoorAssemblyChangeResult result =
                    mapHost.DoorConstruction.TryPlaceAssembly(
                        new DoorAssemblyId(openings[index].InstanceId),
                        new DoorDefinitionId(
                            openings[index].DefinitionId),
                        edges);

                Require(
                    result.Succeeded,
                    $"Could not restore opening "
                    + $"'{openings[index].InstanceId}': "
                    + $"{result.Failure}.");
            }
        }


        private void ApplyFixtures(
            IReadOnlyList<StoreFixtureData> fixtures)
        {
            for (int index = 0;
                 index < fixtures.Count;
                 index++)
            {
                FixtureInstanceId instanceId =
                    new FixtureInstanceId(
                        fixtures[index].InstanceId);
                FixtureDefinitionId definitionId =
                    new FixtureDefinitionId(
                        fixtures[index].DefinitionId);
                GridPosition anchor =
                    StoreLayoutRuntimeConversions.ToGridPosition(
                        fixtures[index].AnchorCell);
                FixtureOrientation orientation =
                    StoreLayoutRuntimeConversions.ToFixtureOrientation(
                        fixtures[index].Orientation);

                FixturePlacementResult evaluation =
                    fixtureHost.FixturePlacement.EvaluatePlacement(
                        instanceId,
                        definitionId,
                        anchor,
                        orientation);

                Require(
                    evaluation.Succeeded,
                    $"Could not validate fixture "
                    + $"'{fixtures[index].InstanceId}': "
                    + $"{evaluation.Failure}.");

                Require(
                    FootprintMatches(
                        evaluation.Footprint,
                        fixtures[index].OccupiedCells),
                    $"Fixture '{fixtures[index].InstanceId}' no longer "
                    + "matches its authored footprint.");

                FixturePlacementResult result =
                    fixtureHost.FixturePlacement.TryPlaceFixture(
                        instanceId,
                        definitionId,
                        anchor,
                        orientation);

                Require(
                    result.Succeeded,
                    $"Could not restore fixture "
                    + $"'{fixtures[index].InstanceId}': "
                    + $"{result.Failure}.");
            }
        }


        private void ApplyDepartments(
            IReadOnlyList<StoreDepartmentData> departments)
        {
            for (int index = 0;
                 index < departments.Count;
                 index++)
            {
                DepartmentPlanChangeResult result =
                    departmentHost.Planning.TryCreatePlan(
                        new DepartmentPlanId(
                            departments[index].InstanceId),
                        new DepartmentDefinitionId(
                            departments[index].DefinitionId),
                        ToGridPositions(departments[index].Cells));

                Require(
                    result.Succeeded,
                    $"Could not restore department "
                    + $"'{departments[index].InstanceId}': "
                    + $"{result.Failure}.");
            }
        }


        private void ApplyFixturePlans(
            IReadOnlyList<StoreFixturePlanData> fixturePlans)
        {
            for (int index = 0;
                 index < fixturePlans.Count;
                 index++)
            {
                FixtureInstanceId instanceId =
                    new FixtureInstanceId(
                        fixturePlans[index].InstanceId);
                FixtureDefinitionId definitionId =
                    new FixtureDefinitionId(
                        fixturePlans[index].DefinitionId);
                GridPosition anchor =
                    StoreLayoutRuntimeConversions.ToGridPosition(
                        fixturePlans[index].AnchorCell);
                FixtureOrientation orientation =
                    StoreLayoutRuntimeConversions.ToFixtureOrientation(
                        fixturePlans[index].Orientation);

                FixturePlacementResult evaluation =
                    fixtureHost.FixturePlacement.EvaluatePlacement(
                        instanceId,
                        definitionId,
                        anchor,
                        orientation);

                Require(
                    evaluation.Succeeded,
                    $"Could not validate fixture plan "
                    + $"'{fixturePlans[index].InstanceId}': "
                    + $"{evaluation.Failure}.");

                Require(
                    FootprintMatches(
                        evaluation.Footprint,
                        fixturePlans[index].OccupiedCells),
                    $"Fixture plan '{fixturePlans[index].InstanceId}' no "
                    + "longer matches its authored footprint.");

                FixtureEquipmentPlanResult result =
                    fixturePlanning.TryCreatePlan(
                        instanceId,
                        definitionId,
                        anchor,
                        orientation);

                Require(
                    result.Succeeded,
                    $"Could not restore fixture plan "
                    + $"'{fixturePlans[index].InstanceId}': "
                    + $"{result.Failure}.");
            }
        }


        private void ApplyReceiving(
            IReadOnlyList<StoreCellData> receivingCells)
        {
            List<GridPosition> cells =
                ToGridPositions(receivingCells);

            if (cells.Count == 0)
            {
                return;
            }

            ReceivingAreaChangeResult result =
                receivingHost.Designations.TryAddArea(cells);

            Require(
                result.Succeeded,
                $"Could not restore Receiving Area: {result.Failure}.");
        }


        private static List<GridPosition> ToGridPositions(
            IReadOnlyList<StoreCellData> cells)
        {
            List<GridPosition> positions =
                new List<GridPosition>(cells.Count);

            for (int index = 0;
                 index < cells.Count;
                 index++)
            {
                positions.Add(
                    StoreLayoutRuntimeConversions.ToGridPosition(
                        cells[index]));
            }

            return positions;
        }


        private static bool FootprintMatches(
            FixtureFootprint footprint,
            IReadOnlyList<StoreCellData> authoredCells)
        {
            if (footprint == null
                || authoredCells == null
                || footprint.CellCount != authoredCells.Count)
            {
                return false;
            }

            HashSet<GridPosition> expected =
                new HashSet<GridPosition>();

            for (int index = 0;
                 index < authoredCells.Count;
                 index++)
            {
                expected.Add(
                    StoreLayoutRuntimeConversions.ToGridPosition(
                        authoredCells[index]));
            }

            for (int index = 0;
                 index < footprint.CellCount;
                 index++)
            {
                if (!expected.Remove(
                        footprint.GetCell(index)))
                {
                    return false;
                }
            }

            return expected.Count == 0;
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
    }
}
