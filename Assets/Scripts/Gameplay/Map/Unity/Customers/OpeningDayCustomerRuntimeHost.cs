using System;
using System.Collections.Generic;
using BigRetail.Characters.Rigging;
using BigRetail.Map.Domain;
using BigRetail.Map.Fixtures;
using BigRetail.Map.Unity.Fixtures;
using BigRetail.Map.Unity.Floors;
using BigRetail.Map.Unity.Sidewalks;
using BigRetail.Map.Unity.View;
using BigRetail.Map.Unity.Walls;
using BigRetail.Map.Walls;
using BigRetail.Merchandise.Domain;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;

namespace BigRetail.Map.Unity.Customers
{
    /// <summary>
    /// Runs the smallest complete opening-day customer loop: arrive from a
    /// sidewalk, take one stocked product, pay at an operational checkout,
    /// and leave. Inventory and accounting remain owned by their existing
    /// domain services.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-40)]
    public sealed class OpeningDayCustomerRuntimeHost : MonoBehaviour
    {
        [Header("Store Runtime")]

        [SerializeField]
        private GridMapHost mapHost;

        [SerializeField]
        private FloorRuntimeHost floorRuntimeHost;

        [SerializeField]
        private SidewalkRuntimeHost sidewalkRuntimeHost;

        [SerializeField]
        private FixtureRuntimeHost fixtureRuntimeHost;

        [SerializeField]
        private FixturePlanogramRuntimeHost planogramRuntimeHost;

        [SerializeField]
        private IsometricViewHost viewHost;

        [SerializeField]
        private Tilemap coordinateTilemap;

        [SerializeField]
        private WallViewSystem wallViewSystem;


        [Header("Customer")]

        [SerializeField]
        private GameObject customerPrefab;

        [SerializeField]
        private NpcPopulationDefinition customerPopulation;

        [SerializeField, Min(0f)]
        private float initialArrivalDelay = 1.5f;

        [SerializeField, Min(0f)]
        private float timeBetweenCustomers = 3f;

        [SerializeField, Min(0f)]
        private float browsingDuration = 0.6f;

        [SerializeField, Min(0f)]
        private float checkoutDuration = 0.8f;

        [SerializeField, Min(0.01f)]
        private float customerMovementSpeed = 1.2f;


        private CustomerStage stage;
        private float stageTimer;
        private float nextInitializationAttemptTime;
        private int appearanceSeed = 1000;
        private GameObject activeCustomer;
        private NpcPathFollower pathFollower;
        private SortingGroup sortingGroup;
        private ShoppingBasket basket;
        private CustomerJourney journey;
        private GridPosition currentLogicalCell;
        private GridPosition currentWalkDestination;
        private readonly HashSet<DoorAssemblyId> openedDoors =
            new HashSet<DoorAssemblyId>();
        private readonly HashSet<DoorAssemblyId> nearbyDoors =
            new HashSet<DoorAssemblyId>();
        private readonly List<DoorAssemblyId> doorsToClose =
            new List<DoorAssemblyId>();


        public bool IsInitialized { get; private set; }

        public GameObject ActiveCustomer => activeCustomer;

        public int CompletedCustomerCount { get; private set; }

        public long LastSaleRevenueCents { get; private set; }

        public string Status { get; private set; } =
            "Waiting for opening-day customer setup.";


        private void OnEnable()
        {
            SubscribeToViewRotation();
        }


        private void Start()
        {
            if (TryInitialize())
            {
                BeginWaiting(initialArrivalDelay);
            }
        }


        private void Update()
        {
            if (!IsInitialized)
            {
                if (Time.unscaledTime >= nextInitializationAttemptTime
                    && TryInitialize())
                {
                    BeginWaiting(initialArrivalDelay);
                }

                return;
            }

            UpdateCustomerSorting();
            UpdateNearbyDoors();

            switch (stage)
            {
                case CustomerStage.Waiting:
                    TickWaiting();
                    break;

                case CustomerStage.WalkingToShelf:
                    TickWalking(CustomerStage.Browsing);
                    break;

                case CustomerStage.Browsing:
                    TickBrowsing();
                    break;

                case CustomerStage.WalkingToCheckout:
                    TickWalking(CustomerStage.CheckingOut);
                    break;

                case CustomerStage.CheckingOut:
                    TickCheckingOut();
                    break;

                case CustomerStage.WalkingToExit:
                    TickWalking(CustomerStage.Departing);
                    break;

                case CustomerStage.Departing:
                    CompleteVisit();
                    break;
            }
        }


        public bool TryInitialize()
        {
            if (IsInitialized)
            {
                return true;
            }

            nextInitializationAttemptTime =
                Time.unscaledTime + 1f;

            if (mapHost == null
                || !mapHost.IsInitialized
                || mapHost.MapDefinition == null
                || floorRuntimeHost == null
                || !floorRuntimeHost.TryInitialize()
                || sidewalkRuntimeHost == null
                || !sidewalkRuntimeHost.TryInitialize()
                || fixtureRuntimeHost == null
                || !fixtureRuntimeHost.TryInitialize()
                || planogramRuntimeHost == null
                || !planogramRuntimeHost.TryInitialize()
                || viewHost == null
                || !viewHost.TryInitialize()
                || coordinateTilemap == null
                || wallViewSystem == null
                || customerPrefab == null
                || customerPopulation == null)
            {
                Status =
                    "Waiting for the map, fixtures, checkout, sidewalk, or customer art.";
                return false;
            }

            SubscribeToViewRotation();
            IsInitialized = true;
            Status = "Store is ready for its first customer.";
            return true;
        }


        private void TickWaiting()
        {
            stageTimer -= Time.deltaTime;

            if (stageTimer > 0f)
            {
                return;
            }

            if (!TryCreateJourney(out CustomerJourney nextJourney))
            {
                Status =
                    "Waiting for a stocked display, checkout, and walkable entrance.";
                stageTimer = 1f;
                return;
            }

            if (!TrySpawnCustomer(nextJourney))
            {
                stageTimer = 1f;
            }
        }


        private void TickWalking(CustomerStage arrivalStage)
        {
            if (activeCustomer == null || pathFollower == null)
            {
                AbortVisit("The active customer rig became unavailable.");
                return;
            }

            if (pathFollower.IsMoving)
            {
                return;
            }

            currentLogicalCell = currentWalkDestination;
            activeCustomer.transform.position =
                GetCellCenter(currentLogicalCell);
            stage = arrivalStage;

            if (arrivalStage == CustomerStage.Browsing)
            {
                stageTimer = browsingDuration;
                Status = "Customer is choosing one item.";
            }
            else if (arrivalStage == CustomerStage.CheckingOut)
            {
                stageTimer = checkoutDuration;
                Status = "Customer is paying at the checkout.";
            }
        }


        private void TickBrowsing()
        {
            stageTimer -= Time.deltaTime;

            if (stageTimer > 0f)
            {
                return;
            }

            FixtureBasketPickupResult pickup =
                planogramRuntimeHost.DisplayInventory
                    .TryMoveProductToBasket(
                        journey.DisplayFixtureId,
                        journey.ProductId,
                        1,
                        basket);

            if (!pickup.Succeeded)
            {
                Status =
                    $"Customer could not take the item: {pickup.Outcome}.";
                SendCustomerToExit();
                return;
            }

            if (!TryStartWalk(
                    journey.CheckoutCell,
                    CustomerStage.WalkingToCheckout))
            {
                AbortVisit(
                    "The route from the shelf to checkout became blocked.");
                return;
            }

            Status = "Customer is walking to the checkout.";
        }


        private void TickCheckingOut()
        {
            stageTimer -= Time.deltaTime;

            if (stageTimer > 0f)
            {
                return;
            }

            FixtureSaleResult sale =
                planogramRuntimeHost.Checkout.TryProcessBasket(
                    journey.CheckoutFixtureId,
                    basket);

            if (sale.Succeeded)
            {
                LastSaleRevenueCents = sale.RevenueCents;
                Status =
                    $"Sale complete: {sale.UnitsSold} item, "
                    + $"${sale.RevenueCents / 100f:0.00}.";

                Debug.Log(Status, this);
            }
            else
            {
                Status = $"Checkout could not complete: {sale.Outcome}.";
            }

            SendCustomerToExit();
        }


        private void SendCustomerToExit()
        {
            if (!TryStartWalk(
                    journey.ExitCell,
                    CustomerStage.WalkingToExit))
            {
                AbortVisit("The customer exit route became blocked.");
                return;
            }

            Status = "Customer is leaving the store.";
        }


        private bool TrySpawnCustomer(CustomerJourney nextJourney)
        {
            GameObject instance = Instantiate(customerPrefab);
            instance.name = $"Opening Day Customer {appearanceSeed}";
            instance.transform.SetPositionAndRotation(
                GetCellCenter(nextJourney.EntranceCell),
                Quaternion.identity);

            NpcPersonIdentity identity =
                instance.GetComponentInChildren<NpcPersonIdentity>(true);

            NpcPathFollower follower =
                instance.GetComponentInChildren<NpcPathFollower>(true);

            if (identity == null || follower == null)
            {
                Destroy(instance);
                Status =
                    "The customer prefab needs identity and path-follower components.";
                return false;
            }

            if (!identity.TryInitialize(
                    customerPopulation,
                    appearanceSeed++,
                    string.Empty,
                    out string failureReason))
            {
                Destroy(instance);
                Status =
                    $"Customer appearance could not initialize: {failureReason}";
                return false;
            }

            activeCustomer = instance;
            pathFollower = follower;
            sortingGroup =
                instance.GetComponentInChildren<SortingGroup>(true);
            basket = new ShoppingBasket();
            journey = nextJourney;
            currentLogicalCell = nextJourney.EntranceCell;

            pathFollower.Configure(
                customerMovementSpeed,
                pathFollower.ArrivalDistance,
                pathFollower.WalkAnimationMetersPerSecond);

            if (!TryStartWalk(
                    journey.DisplayCell,
                    CustomerStage.WalkingToShelf))
            {
                AbortVisit("The planned entrance route became blocked.");
                return false;
            }

            Status = "Customer entered and is walking to a stocked shelf.";
            return true;
        }


        private bool TryCreateJourney(out CustomerJourney nextJourney)
        {
            nextJourney = default;

            List<EntrancePair> entrances = FindEntrances();

            if (entrances.Count == 0)
            {
                return false;
            }

            List<FixtureInstance> fixtures = GetOrderedFixtures();

            for (int entranceIndex = 0;
                 entranceIndex < entrances.Count;
                 entranceIndex++)
            {
                EntrancePair entrance = entrances[entranceIndex];

                for (int displayIndex = 0;
                     displayIndex < fixtures.Count;
                     displayIndex++)
                {
                    FixtureInstance display = fixtures[displayIndex];

                    if (!planogramRuntimeHost.DisplayInventory
                        .TryGetFirstStockedProduct(
                            display.Id,
                            out ProductId productId))
                    {
                        continue;
                    }

                    IReadOnlyList<FixtureAccessPoint> browsePoints =
                        fixtureRuntimeHost.FixtureAccess
                            .GetAvailableAccessPoints(
                                display.Id,
                                FixtureAccessMode.CustomerBrowse);

                    for (int browseIndex = 0;
                         browseIndex < browsePoints.Count;
                         browseIndex++)
                    {
                        GridPosition browseCell =
                            browsePoints[browseIndex].Cell;

                        if (!CanRoute(
                                entrance.OutsideCell,
                                browseCell))
                        {
                            continue;
                        }

                        for (int checkoutIndex = 0;
                             checkoutIndex < fixtures.Count;
                             checkoutIndex++)
                        {
                            FixtureInstance checkout =
                                fixtures[checkoutIndex];

                            if (!planogramRuntimeHost.Checkout
                                .IsOperationalCheckout(checkout.Id))
                            {
                                continue;
                            }

                            IReadOnlyList<FixtureAccessPoint>
                                checkoutPoints =
                                    fixtureRuntimeHost.FixtureAccess
                                        .GetAvailableAccessPoints(
                                            checkout.Id,
                                            FixtureAccessMode
                                                .CustomerCheckout);

                            for (int pointIndex = 0;
                                 pointIndex < checkoutPoints.Count;
                                 pointIndex++)
                            {
                                GridPosition checkoutCell =
                                    checkoutPoints[pointIndex].Cell;

                                if (!CanRoute(browseCell, checkoutCell)
                                    || !CanRoute(
                                        checkoutCell,
                                        entrance.OutsideCell))
                                {
                                    continue;
                                }

                                nextJourney =
                                    new CustomerJourney(
                                        entrance.OutsideCell,
                                        browseCell,
                                        display.Id,
                                        productId,
                                        checkoutCell,
                                        checkout.Id);
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }


        private List<EntrancePair> FindEntrances()
        {
            List<EntrancePair> entrances =
                new List<EntrancePair>();

            if (sidewalkRuntimeHost.SidewalkState == null)
            {
                return entrances;
            }

            foreach (
                GridPosition sidewalk
                in sidewalkRuntimeHost.SidewalkState.EnumerateSidewalks())
            {
                AddEntranceIfValid(
                    entrances,
                    sidewalk,
                    sidewalk.Offset(1, 0));
                AddEntranceIfValid(
                    entrances,
                    sidewalk,
                    sidewalk.Offset(-1, 0));
                AddEntranceIfValid(
                    entrances,
                    sidewalk,
                    sidewalk.Offset(0, 1));
                AddEntranceIfValid(
                    entrances,
                    sidewalk,
                    sidewalk.Offset(0, -1));
            }

            entrances.Sort(
                (left, right) =>
                {
                    int xComparison =
                        left.OutsideCell.X.CompareTo(
                            right.OutsideCell.X);

                    return xComparison != 0
                        ? xComparison
                        : left.OutsideCell.Y.CompareTo(
                            right.OutsideCell.Y);
                });

            return entrances;
        }


        private void AddEntranceIfValid(
            ICollection<EntrancePair> entrances,
            GridPosition sidewalk,
            GridPosition interior)
        {
            if (!floorRuntimeHost.FloorState.HasFloor(interior)
                || !IsWalkable(sidewalk)
                || !IsWalkable(interior)
                || !CanCross(
                    GridRoutePlanner.CreateSharedEdge(
                        sidewalk,
                        interior)))
            {
                return;
            }

            EntrancePair pair =
                new EntrancePair(sidewalk, interior);

            if (!entrances.Contains(pair))
            {
                entrances.Add(pair);
            }
        }


        private List<FixtureInstance> GetOrderedFixtures()
        {
            List<FixtureInstance> fixtures =
                new List<FixtureInstance>(
                    fixtureRuntimeHost.FixtureState
                        .EnumerateFixtures());

            fixtures.Sort(
                (left, right) => string.CompareOrdinal(
                    left.Id.Value,
                    right.Id.Value));

            return fixtures;
        }


        private bool TryStartWalk(
            GridPosition destination,
            CustomerStage walkingStage)
        {
            if (activeCustomer == null
                || pathFollower == null
                || !TryFindRoute(
                    currentLogicalCell,
                    destination,
                    out IReadOnlyList<GridPosition> route))
            {
                return false;
            }

            currentWalkDestination = destination;
            stage = walkingStage;
            pathFollower.SetPath(
                CreateWorldPath(route),
                startImmediately: true);
            return true;
        }


        private bool CanRoute(
            GridPosition start,
            GridPosition destination)
        {
            return TryFindRoute(
                start,
                destination,
                out _);
        }


        private bool TryFindRoute(
            GridPosition start,
            GridPosition destination,
            out IReadOnlyList<GridPosition> route)
        {
            return GridRoutePlanner.TryFindRoute(
                start,
                destination,
                mapHost.MapDefinition.ValidCellCount,
                IsWalkable,
                CanCross,
                out route);
        }


        private bool IsWalkable(GridPosition cell)
        {
            return mapHost.MapDefinition.ContainsCell(cell)
                && (floorRuntimeHost.FloorState.HasFloor(cell)
                    || sidewalkRuntimeHost.IsSidewalkWalkable(cell))
                && !fixtureRuntimeHost.FixtureState.IsOccupied(cell);
        }


        private bool CanCross(CellEdge edge)
        {
            if (mapHost.WallState == null
                || !mapHost.WallState.HasWall(edge))
            {
                return true;
            }

            return mapHost.DoorAssemblies != null
                && mapHost.DoorAssemblies.TryGetAssemblyAtEdge(
                    edge,
                    out DoorAssembly door)
                && door.IsPassageEdge(edge);
        }


        private Vector3[] CreateWorldPath(
            IReadOnlyList<GridPosition> route)
        {
            Vector3[] worldPath = new Vector3[route.Count];

            for (int index = 0; index < route.Count; index++)
            {
                worldPath[index] = GetCellCenter(route[index]);
            }

            return worldPath;
        }


        private Vector3 GetCellCenter(GridPosition cell)
        {
            return viewHost.GetLogicalCellCenterWorld(
                cell,
                coordinateTilemap);
        }


        private void UpdateCustomerSorting()
        {
            if (activeCustomer == null || sortingGroup == null)
            {
                return;
            }

            GridPosition logicalCell =
                viewHost.WorldToLogicalCell(
                    activeCustomer.transform.position,
                    coordinateTilemap);

            GridPosition displayCell =
                viewHost.Projection.ToDisplayCell(logicalCell);

            sortingGroup.sortingOrder =
                WallRenderOrderResolver.ResolveCell(displayCell) + 1;
        }


        private void UpdateNearbyDoors()
        {
            if (activeCustomer == null
                || wallViewSystem == null
                || mapHost.DoorAssemblies == null)
            {
                CloseOpenedDoors();
                return;
            }

            GridPosition customerCell =
                viewHost.WorldToLogicalCell(
                    activeCustomer.transform.position,
                    coordinateTilemap);

            nearbyDoors.Clear();

            foreach (
                DoorAssembly door
                in mapHost.DoorAssemblies.EnumerateAssemblies())
            {
                if (!IsNearPassage(door, customerCell))
                {
                    continue;
                }

                nearbyDoors.Add(door.Id);

                if (!openedDoors.Contains(door.Id)
                    && wallViewSystem.TrySetDoorOpen(
                        door.Id,
                        shouldOpen: true))
                {
                    openedDoors.Add(door.Id);
                }
            }

            if (openedDoors.Count == 0)
            {
                return;
            }

            doorsToClose.Clear();

            foreach (DoorAssemblyId doorId in openedDoors)
            {
                if (!nearbyDoors.Contains(doorId))
                {
                    doorsToClose.Add(doorId);
                }
            }

            for (int index = 0; index < doorsToClose.Count; index++)
            {
                DoorAssemblyId doorId = doorsToClose[index];
                wallViewSystem.TrySetDoorOpen(
                    doorId,
                    shouldOpen: false);
                openedDoors.Remove(doorId);
            }
        }


        private static bool IsNearPassage(
            DoorAssembly door,
            GridPosition customerCell)
        {
            for (int index = 0; index < door.SegmentCount; index++)
            {
                CellEdge edge = door.GetEdge(index);

                if (!door.IsPassageEdge(edge))
                {
                    continue;
                }

                if (ManhattanDistance(
                        edge.FirstCell,
                        customerCell) <= 2
                    || ManhattanDistance(
                        edge.SecondCell,
                        customerCell) <= 2)
                {
                    return true;
                }
            }

            return false;
        }


        private static int ManhattanDistance(
            GridPosition first,
            GridPosition second)
        {
            return Math.Abs(first.X - second.X)
                + Math.Abs(first.Y - second.Y);
        }


        private void BeginWaiting(float duration)
        {
            stage = CustomerStage.Waiting;
            stageTimer = Mathf.Max(0f, duration);
        }


        private void CompleteVisit()
        {
            CompletedCustomerCount++;
            DestroyActiveCustomer();
            Status = "Waiting for the next customer.";
            BeginWaiting(timeBetweenCustomers);
        }


        private void AbortVisit(string reason)
        {
            Status = reason;
            DestroyActiveCustomer();
            BeginWaiting(timeBetweenCustomers);
        }


        private void DestroyActiveCustomer()
        {
            CloseOpenedDoors();

            if (activeCustomer != null)
            {
                Destroy(activeCustomer);
            }

            activeCustomer = null;
            pathFollower = null;
            sortingGroup = null;
            basket = null;
            journey = default;
        }


        private void CloseOpenedDoors()
        {
            if (openedDoors.Count == 0)
            {
                return;
            }

            if (wallViewSystem != null)
            {
                foreach (DoorAssemblyId doorId in openedDoors)
                {
                    wallViewSystem.TrySetDoorOpen(
                        doorId,
                        shouldOpen: false);
                }
            }

            openedDoors.Clear();
        }


        private void SubscribeToViewRotation()
        {
            if (viewHost == null)
            {
                return;
            }

            viewHost.OrientationChanging -= HandleOrientationChanging;
            viewHost.OrientationChanged -= HandleOrientationChanged;
            viewHost.OrientationChanging += HandleOrientationChanging;
            viewHost.OrientationChanged += HandleOrientationChanged;
        }


        private void HandleOrientationChanging(
            BigRetail.Map.View.IsometricViewOrientation previous,
            BigRetail.Map.View.IsometricViewOrientation next)
        {
            if (activeCustomer == null || coordinateTilemap == null)
            {
                return;
            }

            currentLogicalCell =
                viewHost.WorldToLogicalCell(
                    activeCustomer.transform.position,
                    coordinateTilemap);

            pathFollower?.Stop();
        }


        private void HandleOrientationChanged(
            BigRetail.Map.View.IsometricViewOrientation previous,
            BigRetail.Map.View.IsometricViewOrientation next)
        {
            if (activeCustomer == null || coordinateTilemap == null)
            {
                return;
            }

            activeCustomer.transform.position =
                GetCellCenter(currentLogicalCell);
            UpdateCustomerSorting();

            if (stage == CustomerStage.WalkingToShelf
                || stage == CustomerStage.WalkingToCheckout
                || stage == CustomerStage.WalkingToExit)
            {
                CustomerStage walkingStage = stage;

                if (!TryStartWalk(
                        currentWalkDestination,
                        walkingStage))
                {
                    AbortVisit(
                        "Camera rotation revealed a blocked customer route.");
                }
            }
        }


        private void OnDisable()
        {
            if (viewHost != null)
            {
                viewHost.OrientationChanging -=
                    HandleOrientationChanging;
                viewHost.OrientationChanged -=
                    HandleOrientationChanged;
            }

            DestroyActiveCustomer();
        }


        private enum CustomerStage
        {
            None = 0,
            Waiting = 1,
            WalkingToShelf = 2,
            Browsing = 3,
            WalkingToCheckout = 4,
            CheckingOut = 5,
            WalkingToExit = 6,
            Departing = 7
        }


        private readonly struct EntrancePair : IEquatable<EntrancePair>
        {
            public EntrancePair(
                GridPosition outsideCell,
                GridPosition insideCell)
            {
                OutsideCell = outsideCell;
                InsideCell = insideCell;
            }

            public GridPosition OutsideCell { get; }

            public GridPosition InsideCell { get; }


            public bool Equals(EntrancePair other)
            {
                return OutsideCell == other.OutsideCell
                    && InsideCell == other.InsideCell;
            }
        }


        private readonly struct CustomerJourney
        {
            public CustomerJourney(
                GridPosition entranceCell,
                GridPosition displayCell,
                FixtureInstanceId displayFixtureId,
                ProductId productId,
                GridPosition checkoutCell,
                FixtureInstanceId checkoutFixtureId)
            {
                EntranceCell = entranceCell;
                DisplayCell = displayCell;
                DisplayFixtureId = displayFixtureId;
                ProductId = productId;
                CheckoutCell = checkoutCell;
                CheckoutFixtureId = checkoutFixtureId;
            }

            public GridPosition EntranceCell { get; }

            public GridPosition ExitCell => EntranceCell;

            public GridPosition DisplayCell { get; }

            public FixtureInstanceId DisplayFixtureId { get; }

            public ProductId ProductId { get; }

            public GridPosition CheckoutCell { get; }

            public FixtureInstanceId CheckoutFixtureId { get; }
        }
    }
}
