using System;
using System.Collections.Generic;
using BigRetail.Characters.Rigging;
using BigRetail.Inventory.Domain;
using BigRetail.Map.Domain;
using BigRetail.Map.Fixtures;
using BigRetail.Map.Navigation;
using BigRetail.Map.Unity;
using BigRetail.Map.Unity.Fixtures;
using BigRetail.Map.Unity.View;
using BigRetail.Merchandise.Domain;
using BigRetail.Merchandise.Unity;
using BigRetail.Work.Domain;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace BigRetail.Work.Unity
{
    /// <summary>
    /// Executes one employee-compatible fixture stocking job through the
    /// Founder. Inventory transactions happen only at visible pickup and
    /// placement beats; the character rig remains presentation-only.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(120)]
    public sealed class FounderStockTaskController :
        MonoBehaviour,
        IGridRouteSurfaceQuery
    {
        private const string FounderObjectName = "Person";
        private const string FounderDisplayName = "Founder Frank";
        private const string FounderWorkStartMarkerId =
            "bigretail.marker.frank.store_footprint_center";
        private const string SouthShelfGrabState =
            "Person_ShelfGrab_SouthFacing";
        private const string NorthShelfGrabState =
            "Person_ShelfGrab_NorthFacing";
        private const string IdleState = "Idle";
        private const float CarriedCaseWorldWidth = 0.42f;
        private const int CarriedCaseSortingOrder = 30;

        private static readonly StorageLocationId FounderCarryLocationId =
            new StorageLocationId("WORKER-CARRY-FOUNDER-FRANK");

        [Header("Runtime Map")]

        [SerializeField]
        private GridMapHost mapHost;

        [SerializeField]
        private IsometricViewHost viewHost;

        [SerializeField]
        private Tilemap coordinateTilemap;

        [SerializeField]
        private FixtureRuntimeHost fixtureRuntimeHost;

        [SerializeField]
        private FixturePlanogramRuntimeHost planogramRuntimeHost;

        [SerializeField]
        private LocationMarkerHost locationMarkerHost;

        [Header("Founder")]

        [SerializeField]
        private NpcPathFollower founderPathFollower;

        [Header("Task Timing")]

        [SerializeField]
        [Min(0.05f)]
        private float pickupDurationSeconds = 0.65f;

        [SerializeField]
        [Min(0.05f)]
        private float stockUnitIntervalSeconds = 0.16f;

        [SerializeField]
        [Min(0.05f)]
        private float returnDurationSeconds = 0.55f;


        private GridRoutePlanner routePlanner;
        private StockFixtureWorkOrder activeWork;
        private FixtureBackstockCaseSnapshot carriedCase;
        private FixtureInstanceId destinationFixtureId;
        private Animator founderAnimator;
        private NpcCutoutRig founderRig;
        private IsometricDepthSortingGroup founderDepthSorting;
        private GameObject carriedCaseObject;
        private SpriteRenderer carriedCaseRenderer;
        private float actionTimeRemaining;
        private bool returnActionStarted;
        private bool isInitialized;
        private bool inventoryLocationRegistered;


        public bool IsInitialized => isInitialized;

        public bool IsBusy =>
            activeWork != null && !activeWork.IsTerminal;

        public StockFixtureWorkOrder ActiveWork => activeWork;

        public string StatusMessage =>
            activeWork?.StatusMessage
            ?? "Founder is ready for work";


        public event Action StatusChanged;


        private void Awake()
        {
            ResolveReferences();
        }


        private void OnEnable()
        {
            ResolveReferences();

            if (planogramRuntimeHost != null)
            {
                planogramRuntimeHost.Initialized +=
                    HandlePlanogramInitialized;
            }

            TryInitialize();
        }


        private void Start()
        {
            if (!TryInitialize())
            {
                Debug.LogWarning(
                    "Founder stock tasks are waiting for the map, inventory, or Founder rig.",
                    this);
            }
        }


        private void Update()
        {
            if (!IsBusy || !TryInitialize())
            {
                return;
            }

            switch (activeWork.Phase)
            {
                case StockFixtureWorkPhase.TravelingToBackstock:
                    if (!founderPathFollower.IsMoving)
                    {
                        BeginPickup();
                    }
                    break;

                case StockFixtureWorkPhase.PickingUpCase:
                    actionTimeRemaining -= Time.deltaTime;

                    if (actionTimeRemaining <= 0f)
                    {
                        CompletePickup();
                    }
                    break;

                case StockFixtureWorkPhase.TravelingToFixture:
                    if (!founderPathFollower.IsMoving)
                    {
                        BeginStocking();
                    }
                    break;

                case StockFixtureWorkPhase.StockingFixture:
                    actionTimeRemaining -= Time.deltaTime;

                    if (actionTimeRemaining <= 0f)
                    {
                        StockOneUnit();
                    }
                    break;

                case StockFixtureWorkPhase.ReturningRemainder:
                    if (founderPathFollower.IsMoving)
                    {
                        break;
                    }

                    if (!returnActionStarted)
                    {
                        FaceFixture(destinationFixtureId);
                        PlayShelfGrab();
                        actionTimeRemaining = returnDurationSeconds;
                        returnActionStarted = true;
                        break;
                    }

                    actionTimeRemaining -= Time.deltaTime;

                    if (actionTimeRemaining <= 0f)
                    {
                        CompleteReturn();
                    }
                    break;
            }
        }


        private void OnDisable()
        {
            if (planogramRuntimeHost != null)
            {
                planogramRuntimeHost.Initialized -=
                    HandlePlanogramInitialized;
            }

            founderPathFollower?.Stop();
        }


        public bool TryAssignStockFixture(
            FixtureInstanceId fixtureId,
            out string status)
        {
            if (!TryInitialize())
            {
                status = "Founder is not ready yet";
                return false;
            }

            if (IsBusy)
            {
                status = activeWork.StatusMessage;
                return false;
            }

            if (!EnsureFounderOnNavigableCell(out status))
            {
                return false;
            }

            if (!planogramRuntimeHost.DisplayInventory
                    .TryGetNextRestockProduct(
                        fixtureId,
                        out ProductId productId,
                        out _))
            {
                status = DescribeNoAvailableWork(fixtureId);
                return false;
            }

            activeWork = new StockFixtureWorkOrder(
                fixtureId,
                productId);

            if (!TryBeginNextCaseTrip(out status))
            {
                activeWork.Block(status);
                PublishStatus();
                return false;
            }

            PublishStatus();
            status = activeWork.StatusMessage;
            return true;
        }


        public bool CanStandAt(GridPosition cell)
        {
            return mapHost != null
                && mapHost.MapDefinition != null
                && mapHost.MapDefinition.ContainsCell(cell)
                && mapHost.FoundationState != null
                && mapHost.FoundationState.HasFoundation(cell)
                && fixtureRuntimeHost != null
                && fixtureRuntimeHost.FixtureState != null
                && !fixtureRuntimeHost.FixtureState.IsOccupied(cell);
        }


        public bool CanTraverse(CellEdge edge)
        {
            if (mapHost?.WallState == null
                || !mapHost.WallState.HasWall(edge))
            {
                return true;
            }

            return mapHost.DoorAssemblies != null
                && mapHost.DoorAssemblies.TryGetAssemblyAtEdge(
                    edge,
                    out BigRetail.Map.Walls.DoorAssembly door)
                && door.IsPassageEdge(edge);
        }


        private void HandlePlanogramInitialized(
            FixturePlanogramRuntimeHost _)
        {
            TryInitialize();
        }


        private bool TryInitialize()
        {
            if (isInitialized)
            {
                return true;
            }

            ResolveReferences();

            if (mapHost == null
                || !mapHost.IsInitialized
                || viewHost == null
                || !viewHost.TryInitialize()
                || coordinateTilemap == null
                || fixtureRuntimeHost == null
                || !fixtureRuntimeHost.TryInitialize()
                || fixtureRuntimeHost.FixtureAccess == null
                || planogramRuntimeHost == null
                || !planogramRuntimeHost.TryInitialize()
                || planogramRuntimeHost.Inventory == null
                || planogramRuntimeHost.Backstock == null
                || planogramRuntimeHost.DisplayInventory == null
                || founderPathFollower == null)
            {
                return false;
            }

            if (!planogramRuntimeHost.Inventory.ContainsLocation(
                    FounderCarryLocationId))
            {
                inventoryLocationRegistered =
                    planogramRuntimeHost.Inventory.TryRegisterLocation(
                        new StorageLocationDefinition(
                            FounderCarryLocationId,
                            "Founder carried case",
                            StorageRole.Backroom));
            }
            else
            {
                inventoryLocationRegistered = true;
            }

            if (!inventoryLocationRegistered)
            {
                return false;
            }

            routePlanner = new GridRoutePlanner(this);
            founderAnimator =
                founderPathFollower.GetComponent<Animator>();
            founderRig =
                founderPathFollower.GetComponent<NpcCutoutRig>();
            founderDepthSorting =
                founderPathFollower.GetComponent<
                    IsometricDepthSortingGroup>();

            if (founderDepthSorting == null)
            {
                founderDepthSorting =
                    founderPathFollower.gameObject.AddComponent<
                        IsometricDepthSortingGroup>();
            }

            founderDepthSorting.Configure(
                viewHost,
                coordinateTilemap,
                founderPathFollower.transform);
            founderPathFollower.gameObject.name = FounderDisplayName;
            isInitialized = true;
            return true;
        }


        private void ResolveReferences()
        {
            mapHost ??= FindAnyObjectByType<GridMapHost>(
                FindObjectsInactive.Include);
            viewHost ??= FindAnyObjectByType<IsometricViewHost>(
                FindObjectsInactive.Include);
            fixtureRuntimeHost ??=
                FindAnyObjectByType<FixtureRuntimeHost>(
                    FindObjectsInactive.Include);
            planogramRuntimeHost ??=
                FindAnyObjectByType<FixturePlanogramRuntimeHost>(
                    FindObjectsInactive.Include);
            locationMarkerHost ??=
                FindAnyObjectByType<LocationMarkerHost>(
                    FindObjectsInactive.Include);

            if (coordinateTilemap == null)
            {
                Tilemap[] tilemaps =
                    FindObjectsByType<Tilemap>(
                        FindObjectsInactive.Include);

                for (int index = 0;
                     index < tilemaps.Length;
                     index++)
                {
                    if (tilemaps[index].name == "MapVisuals")
                    {
                        coordinateTilemap = tilemaps[index];
                        break;
                    }
                }
            }

            if (founderPathFollower != null)
            {
                return;
            }

            NpcPathFollower[] people =
                FindObjectsByType<NpcPathFollower>(
                    FindObjectsInactive.Include);

            for (int index = 0;
                 index < people.Length;
                 index++)
            {
                if (people[index].gameObject.name == FounderObjectName
                    && people[index].transform.parent == null)
                {
                    founderPathFollower = people[index];
                    return;
                }
            }
        }


        private bool EnsureFounderOnNavigableCell(
            out string failureReason)
        {
            GridPosition currentCell = GetFounderCell();

            if (CanStandAt(currentCell))
            {
                failureReason = string.Empty;
                return true;
            }

            if (locationMarkerHost == null
                || !locationMarkerHost.TryGetMarker(
                    FounderWorkStartMarkerId,
                    out LocationMarkerAuthoring marker))
            {
                failureReason =
                    "Founder has no valid report-for-work location";
                return false;
            }

            Vector3Int markerCell = marker.LogicalCell;
            GridPosition workStartCell = new GridPosition(
                markerCell.x,
                markerCell.y,
                markerCell.z);

            if (!CanStandAt(workStartCell))
            {
                failureReason =
                    "Founder's report-for-work location is obstructed";
                return false;
            }

            founderPathFollower.transform.position =
                viewHost.GetLogicalCellCenterWorld(
                    workStartCell,
                    coordinateTilemap);
            founderPathFollower.Stop();

            Debug.Log(
                $"Founder reported for work at {workStartCell} because "
                + $"the authored character position {currentCell} was "
                + "outside the navigable store.",
                this);
            failureReason = string.Empty;
            return true;
        }


        private bool TryBeginNextCaseTrip(out string failureReason)
        {
            FixtureBackstockService backstock =
                planogramRuntimeHost.Backstock;

            if (!backstock.TryFindRackCase(
                    activeWork.ProductId,
                    out FixtureInstanceId rackFixtureId,
                    out _))
            {
                failureReason = "No matching case remains in storage";
                return false;
            }

            GridPosition origin = GetFounderCell();

            if (!TryPlanRouteToFixture(
                    rackFixtureId,
                    origin,
                    out FixtureAccessPoint accessPoint,
                    out IReadOnlyList<GridPosition> route))
            {
                failureReason = "Founder cannot reach the storage rack";
                return false;
            }

            activeWork.BeginBackstockTrip(rackFixtureId);
            BeginTravel(rackFixtureId, accessPoint, route);
            failureReason = string.Empty;
            return true;
        }


        private void BeginPickup()
        {
            activeWork.BeginPickup();
            FaceFixture(activeWork.SourceRackId);
            PlayShelfGrab();
            actionTimeRemaining = pickupDurationSeconds;
            PublishStatus();
        }


        private void CompletePickup()
        {
            FixtureBackstockCasePickupResult result =
                planogramRuntimeHost.Backstock.TryTakeCase(
                    activeWork.SourceRackId,
                    activeWork.ProductId,
                    FounderCarryLocationId);

            if (!result.Succeeded)
            {
                BlockWork(
                    "The case was no longer available in storage");
                return;
            }

            carriedCase = result.Case;
            activeWork.RecordCasePickedUp(
                carriedCase.RemainingUnitCount);
            ShowCarriedCase(activeWork.ProductId);
            PlayIdle();

            GridPosition origin = GetFounderCell();

            if (!TryPlanRouteToFixture(
                    activeWork.TargetFixtureId,
                    origin,
                    out FixtureAccessPoint accessPoint,
                    out IReadOnlyList<GridPosition> route))
            {
                BeginEmergencyReturn(
                    "Founder cannot reach the merchandise fixture");
                return;
            }

            BeginTravel(
                activeWork.TargetFixtureId,
                accessPoint,
                route);
            PublishStatus();
        }


        private void BeginStocking()
        {
            activeWork.BeginStocking();
            FaceFixture(activeWork.TargetFixtureId);
            PlayShelfGrab();
            actionTimeRemaining = stockUnitIntervalSeconds;
            PublishStatus();
        }


        private void StockOneUnit()
        {
            FixtureRestockResult result =
                planogramRuntimeHost.DisplayInventory
                    .TryRestockFixtureFromLocation(
                        activeWork.TargetFixtureId,
                        FounderCarryLocationId,
                        maximumUnitCount: 1);

            if (result.Succeeded)
            {
                activeWork.RecordUnitsStocked(
                    result.MovedUnitCount);

                if (activeWork.StockedUnitCount % 3 == 0)
                {
                    PlayShelfGrab();
                }

                PublishStatus();
            }

            if (activeWork.CarriedUnitCount <= 0)
            {
                carriedCase = default;
                HideCarriedCase();
                PlayIdle();

                if (TargetStillNeedsProduct())
                {
                    if (!TryBeginNextCaseTrip(
                            out string failureReason))
                    {
                        BlockWork(failureReason);
                        return;
                    }

                    PublishStatus();
                    return;
                }

                if (planogramRuntimeHost.DisplayInventory.TryGetSnapshot(
                        activeWork.TargetFixtureId,
                        out FixtureDisplayStockSnapshot snapshot)
                    && snapshot.MissingUnitCount == 0)
                {
                    CompleteWork();
                }
                else
                {
                    BlockWork("No matching case remains in storage");
                }
                return;
            }

            if (!result.Succeeded)
            {
                if (result.Outcome
                    == FixtureRestockOutcome.AlreadyFull)
                {
                    BeginRemainderReturn();
                    return;
                }

                BeginEmergencyReturn(
                    "Founder could not place the carried stock");
                return;
            }

            actionTimeRemaining = stockUnitIntervalSeconds;
        }


        private void BeginRemainderReturn()
        {
            activeWork.BeginReturn();
            PlayIdle();

            GridPosition origin = GetFounderCell();

            if (!TryPlanRouteToFixture(
                    activeWork.SourceRackId,
                    origin,
                    out FixtureAccessPoint accessPoint,
                    out IReadOnlyList<GridPosition> route))
            {
                BeginEmergencyReturn(
                    "Founder cannot return the open case to storage");
                return;
            }

            actionTimeRemaining = 0f;
            returnActionStarted = false;
            BeginTravel(
                activeWork.SourceRackId,
                accessPoint,
                route);
            PublishStatus();
        }


        private void CompleteReturn()
        {
            FixtureBackstockCaseReturnResult result =
                planogramRuntimeHost.Backstock.TryReturnCase(
                    activeWork.SourceRackId,
                    FounderCarryLocationId,
                    new FixtureBackstockCaseSnapshot(
                        activeWork.ProductId,
                        activeWork.CarriedUnitCount,
                        carriedCase.CapacityUnitCount));

            if (!result.Succeeded)
            {
                BlockWork("Founder could not return the open case");
                return;
            }

            activeWork.RecordRemainderReturned();
            carriedCase = default;
            returnActionStarted = false;
            HideCarriedCase();
            PlayIdle();
            CompleteWork();
        }


        private void BeginEmergencyReturn(string reason)
        {
            if (activeWork.CarriedUnitCount <= 0)
            {
                BlockWork(reason);
                return;
            }

            FixtureBackstockCaseReturnResult result =
                planogramRuntimeHost.Backstock.TryReturnCase(
                    activeWork.SourceRackId,
                    FounderCarryLocationId,
                    new FixtureBackstockCaseSnapshot(
                        activeWork.ProductId,
                        activeWork.CarriedUnitCount,
                        carriedCase.CapacityUnitCount));

            if (result.Succeeded)
            {
                activeWork.BeginReturn();
                activeWork.RecordRemainderReturned();
            }

            carriedCase = default;
            HideCarriedCase();
            BlockWork(reason);
        }


        private bool TargetStillNeedsProduct()
        {
            return planogramRuntimeHost.DisplayInventory
                .TryGetNextRestockProduct(
                    activeWork.TargetFixtureId,
                    out ProductId productId,
                    out _)
                && productId == activeWork.ProductId;
        }


        private string DescribeNoAvailableWork(
            FixtureInstanceId fixtureId)
        {
            if (!planogramRuntimeHost.DisplayInventory.TryGetSnapshot(
                    fixtureId,
                    out FixtureDisplayStockSnapshot snapshot))
            {
                return "That fixture is unavailable";
            }

            if (snapshot.CapacityUnitCount == 0)
            {
                return "Assign products before giving a stock task";
            }

            if (snapshot.MissingUnitCount == 0)
            {
                return "That fixture is already full";
            }

            return "No matching case is available in storage";
        }


        private bool TryPlanRouteToFixture(
            FixtureInstanceId fixtureId,
            GridPosition origin,
            out FixtureAccessPoint selectedAccessPoint,
            out IReadOnlyList<GridPosition> selectedRoute)
        {
            selectedAccessPoint = default;
            selectedRoute = Array.Empty<GridPosition>();

            IReadOnlyList<FixtureAccessPoint> accessPoints =
                fixtureRuntimeHost.FixtureAccess
                    .GetAvailableAccessPoints(
                        fixtureId,
                        FixtureAccessMode.EmployeeStock);

            int shortestCellCount = int.MaxValue;

            for (int index = 0;
                 index < accessPoints.Count;
                 index++)
            {
                FixtureAccessPoint candidate = accessPoints[index];

                if (!routePlanner.TryFindRoute(
                        origin,
                        candidate.Cell,
                        out IReadOnlyList<GridPosition> route)
                    || route.Count >= shortestCellCount)
                {
                    continue;
                }

                selectedAccessPoint = candidate;
                selectedRoute = route;
                shortestCellCount = route.Count;
            }

            if (shortestCellCount < int.MaxValue)
            {
                return true;
            }

            List<string> accessDiagnostics = new List<string>();

            for (int index = 0;
                 index < accessPoints.Count;
                 index++)
            {
                FixtureAccessPoint accessPoint = accessPoints[index];
                accessDiagnostics.Add(
                    $"{accessPoint.Cell}:stand={CanStandAt(accessPoint.Cell)}");
            }

            bool originInMap =
                mapHost.MapDefinition.ContainsCell(origin);
            bool originHasFoundation =
                mapHost.FoundationState.HasFoundation(origin);
            bool originOccupied =
                fixtureRuntimeHost.FixtureState.IsOccupied(origin);

            Debug.LogWarning(
                $"Founder route failed from {origin} "
                + $"(map={originInMap}, "
                + $"foundation={originHasFoundation}, "
                + $"occupied={originOccupied}) to fixture "
                + $"'{fixtureId}' across {accessPoints.Count} access "
                + $"point(s): {string.Join(", ", accessDiagnostics)}.",
                this);
            return false;
        }


        private void BeginTravel(
            FixtureInstanceId fixtureId,
            FixtureAccessPoint accessPoint,
            IReadOnlyList<GridPosition> route)
        {
            destinationFixtureId = fixtureId;

            int firstWaypointIndex = route.Count > 1 ? 1 : 0;
            Vector3[] worldWaypoints =
                new Vector3[route.Count - firstWaypointIndex];

            for (int index = firstWaypointIndex;
                 index < route.Count;
                 index++)
            {
                worldWaypoints[index - firstWaypointIndex] =
                    viewHost.GetLogicalCellCenterWorld(
                        route[index],
                        coordinateTilemap);
            }

            if (worldWaypoints.Length == 0)
            {
                founderPathFollower.SetPath(
                    new[]
                    {
                        viewHost.GetLogicalCellCenterWorld(
                            accessPoint.Cell,
                            coordinateTilemap)
                    });
                return;
            }

            founderPathFollower.SetPath(worldWaypoints);
        }


        private GridPosition GetFounderCell()
        {
            return viewHost.WorldToLogicalCell(
                founderPathFollower.transform.position,
                coordinateTilemap);
        }


        private void FaceFixture(FixtureInstanceId fixtureId)
        {
            if (founderRig == null
                || !fixtureRuntimeHost.FixtureState.TryGetFixture(
                    fixtureId,
                    out FixtureInstance fixture))
            {
                return;
            }

            Vector3 fixtureWorld =
                viewHost.GetLogicalCellCenterWorld(
                    fixture.AnchorCell,
                    coordinateTilemap);
            Vector3 direction =
                fixtureWorld - founderPathFollower.transform.position;

            bool east = direction.x >= 0f;
            bool south = direction.y <= 0f;
            founderRig.SetFacing(
                east
                    ? (south
                        ? NpcFacing.SouthEast
                        : NpcFacing.NorthEast)
                    : (south
                        ? NpcFacing.SouthWest
                        : NpcFacing.NorthWest));
        }


        private void PlayShelfGrab()
        {
            if (founderAnimator == null)
            {
                return;
            }

            bool usesNorthAnimation =
                founderRig != null
                && NpcFacingUtility.UsesNorthFacingAnimation(
                    founderRig.Facing);

            founderAnimator.Play(
                usesNorthAnimation
                    ? NorthShelfGrabState
                    : SouthShelfGrabState,
                layer: 0,
                normalizedTime: 0f);
        }


        private void PlayIdle()
        {
            founderAnimator?.Play(
                IdleState,
                layer: 0,
                normalizedTime: 0f);
        }


        private void ShowCarriedCase(ProductId productId)
        {
            EnsureCarriedCaseRenderer();

            if (carriedCaseRenderer == null)
            {
                return;
            }

            Sprite sprite = null;

            if (planogramRuntimeHost.TryGetProductAsset(
                    productId,
                    out ProductDefinitionAsset productAsset))
            {
                sprite = productAsset.GetCaseImage(
                    risingLeft: true);
            }

            carriedCaseRenderer.sprite = sprite;
            carriedCaseObject.SetActive(sprite != null);

            if (sprite == null)
            {
                return;
            }

            float width = Mathf.Max(
                0.001f,
                sprite.bounds.size.x);
            float scale = CarriedCaseWorldWidth / width;
            carriedCaseObject.transform.localScale =
                new Vector3(scale, scale, 1f);
        }


        private void EnsureCarriedCaseRenderer()
        {
            if (carriedCaseRenderer != null
                || founderPathFollower == null)
            {
                return;
            }

            carriedCaseObject =
                new GameObject("Carried Supplier Case");
            carriedCaseObject.transform.SetParent(
                founderPathFollower.transform,
                worldPositionStays: false);
            carriedCaseObject.transform.localPosition =
                new Vector3(0f, 0.18f, -0.01f);
            carriedCaseRenderer =
                carriedCaseObject.AddComponent<SpriteRenderer>();
            carriedCaseRenderer.sortingOrder =
                CarriedCaseSortingOrder;
            carriedCaseObject.SetActive(false);
        }


        private void HideCarriedCase()
        {
            carriedCaseObject?.SetActive(false);
        }


        private void CompleteWork()
        {
            activeWork.Complete();
            PlayIdle();
            PublishStatus();
        }


        private void BlockWork(string reason)
        {
            founderPathFollower?.Stop();
            PlayIdle();
            activeWork?.Block(reason);
            PublishStatus();
        }


        private void PublishStatus()
        {
            StatusChanged?.Invoke();
        }


        private void OnValidate()
        {
            pickupDurationSeconds =
                Mathf.Max(0.05f, pickupDurationSeconds);
            stockUnitIntervalSeconds =
                Mathf.Max(0.05f, stockUnitIntervalSeconds);
            returnDurationSeconds =
                Mathf.Max(0.05f, returnDurationSeconds);
        }
    }
}
