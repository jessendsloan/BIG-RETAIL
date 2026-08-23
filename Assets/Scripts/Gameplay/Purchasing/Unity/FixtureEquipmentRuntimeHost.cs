using System;
using System.Collections.Generic;
using BigRetail.Economy.Domain;
using BigRetail.Map.Fixtures;
using BigRetail.Map.Unity.Fixtures;
using BigRetail.Receiving.Domain;
using BigRetail.Receiving.Unity;
using BigRetail.Simulation.Time.Domain;
using BigRetail.Simulation.Time.Unity;
using UnityEngine;

namespace BigRetail.Purchasing.Unity
{
    /// <summary>
    /// Owns the fixture plan/order/deliver/own/install loop for one store.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-48)]
    public sealed class FixtureEquipmentRuntimeHost : MonoBehaviour
    {
        private const string ReceivingLoadSource =
            "big-wholesale-equipment-orders";

        [SerializeField]
        private FixtureEquipmentCatalogAsset equipmentCatalogAsset;

        [SerializeField]
        private FixtureRuntimeHost fixtureRuntimeHost;

        [SerializeField]
        private FixturePlanogramRuntimeHost planogramRuntimeHost;

        [SerializeField]
        private SimulationTimeRuntimeHost timeRuntimeHost;

        [SerializeField]
        private ReceivingAreaRuntimeHost receivingAreaRuntimeHost;

        private SimulationClock subscribedClock;


        public bool IsInitialized { get; private set; }

        public bool IsPlanMode { get; private set; }

        public string InitializationError { get; private set; } =
            string.Empty;

        public FixtureEquipmentCatalog Catalog { get; private set; }

        public FixtureEquipmentCatalogAsset CatalogAsset =>
            equipmentCatalogAsset;

        public StoreCashState Cash =>
            planogramRuntimeHost?.Cash;

        public SimulationDateTime CurrentTime =>
            timeRuntimeHost?.Clock?.CurrentTime
            ?? SimulationDateTime.FromTotalGameSeconds(0);

        public FixtureEquipmentInventory Inventory { get; private set; }

        public FixtureEquipmentOrderService Orders { get; private set; }

        public FixtureEquipmentPlanState Plans { get; private set; }

        public FixtureEquipmentPlanningService Planning { get; private set; }

        public FixtureEquipmentInstallationService Installation
        {
            get;
            private set;
        }

        public int ReadyToReceiveCount
        {
            get
            {
                int count = 0;

                if (Orders == null)
                {
                    return count;
                }

                foreach (FixtureEquipmentOrder order
                         in Orders.EnumerateReadyOrders())
                {
                    count++;
                }

                return count;
            }
        }

        public int StagedReadyCount
        {
            get
            {
                int count = 0;

                if (Orders == null
                    || receivingAreaRuntimeHost?.State == null)
                {
                    return count;
                }

                foreach (FixtureEquipmentOrder order
                         in Orders.EnumerateReadyOrders())
                {
                    if (receivingAreaRuntimeHost.State.TryGetReservation(
                            ReceivingLoadId.EquipmentOrder(
                                order.OrderNumber),
                            out _))
                    {
                        count++;
                    }
                }

                return count;
            }
        }


        public event Action<FixtureEquipmentRuntimeHost> Initialized;

        public event Action StateChanged;

        public event Action<bool> PlanModeChanged;


        private void OnEnable()
        {
            if (fixtureRuntimeHost != null)
            {
                fixtureRuntimeHost.Initialized +=
                    HandleFixtureRuntimeInitialized;
            }

            if (planogramRuntimeHost != null)
            {
                planogramRuntimeHost.Initialized +=
                    HandlePlanogramRuntimeInitialized;
            }

            if (timeRuntimeHost != null)
            {
                timeRuntimeHost.Initialized += HandleTimeInitialized;
            }

            if (receivingAreaRuntimeHost != null)
            {
                receivingAreaRuntimeHost.Initialized +=
                    HandleReceivingInitialized;
            }

            TryInitialize();
        }

        private void Start()
        {
            if (!TryInitialize())
            {
                Debug.LogError(InitializationError, this);
            }
        }

        public bool TryInitialize()
        {
            if (IsInitialized)
            {
                return true;
            }

            if (equipmentCatalogAsset == null
                || fixtureRuntimeHost == null
                || !fixtureRuntimeHost.TryInitialize()
                || fixtureRuntimeHost.Definitions == null
                || fixtureRuntimeHost.FixturePlacement == null
                || planogramRuntimeHost == null
                || !planogramRuntimeHost.TryInitialize()
                || planogramRuntimeHost.Cash == null
                || timeRuntimeHost == null
                || !timeRuntimeHost.IsInitialized)
            {
                InitializationError =
                    "Fixture equipment is waiting for its catalog, fixture "
                    + "runtime, store cash, and campaign clock.";
                return false;
            }

            try
            {
                Catalog = equipmentCatalogAsset.CreateDomainCatalog(
                    fixtureRuntimeHost.Definitions);
                Inventory = new FixtureEquipmentInventory(Catalog);
                Orders = new FixtureEquipmentOrderService(
                    Catalog,
                    Inventory,
                    planogramRuntimeHost.Cash);
                Plans = new FixtureEquipmentPlanState();
                Planning = new FixtureEquipmentPlanningService(
                    fixtureRuntimeHost.FixturePlacement,
                    Plans);
                Installation = new FixtureEquipmentInstallationService(
                    fixtureRuntimeHost.FixturePlacement,
                    Inventory,
                    Plans);
            }
            catch (Exception exception)
            {
                InitializationError = exception.Message;
                Debug.LogException(exception, this);
                return false;
            }

            Inventory.QuantityChanged += HandleEquipmentQuantityChanged;
            Orders.OrdersChanged += HandleOrdersChanged;
            Plans.PlansChanged += HandlePlansChanged;
            planogramRuntimeHost.Cash.BalanceChanged +=
                HandleCashBalanceChanged;
            AttachClock(timeRuntimeHost.Clock);
            Orders.AdvanceTo(
                timeRuntimeHost.Clock.CurrentTime.TotalGameSeconds);

            IsInitialized = true;
            InitializationError = string.Empty;
            RefreshReceivingReservations();
            Initialized?.Invoke(this);
            StateChanged?.Invoke();
            return true;
        }

        public void SetPlanMode(bool isPlanMode)
        {
            if (IsPlanMode == isPlanMode)
            {
                return;
            }

            IsPlanMode = isPlanMode;
            PlanModeChanged?.Invoke(isPlanMode);
            StateChanged?.Invoke();
        }

        public FixtureEquipmentOrderResult TryOrderRequiredEquipment()
        {
            if (!TryInitialize())
            {
                return FixtureEquipmentOrderResult.Rejected(
                    FixtureEquipmentOrderFailure.EmptyOrder);
            }

            Dictionary<FixtureDefinitionId, int> required =
                new Dictionary<FixtureDefinitionId, int>();

            foreach (FixtureEquipmentDefinition definition
                     in Catalog.EnumerateDefinitions())
            {
                FixtureDefinitionId id =
                    definition.FixtureDefinitionId;
                int needed = Plans.CountFor(id)
                    - Inventory.GetQuantity(id)
                    - Orders.GetOutstandingQuantity(id);

                if (needed > 0)
                {
                    required.Add(id, needed);
                }
            }

            return Orders.TryPlaceOrders(
                required,
                timeRuntimeHost.Clock.CurrentTime.TotalGameSeconds);
        }

        public int ReceiveStagedEquipment()
        {
            if (!TryInitialize()
                || receivingAreaRuntimeHost?.State == null)
            {
                return 0;
            }

            List<long> stagedOrders = new List<long>();

            foreach (FixtureEquipmentOrder order
                     in Orders.EnumerateReadyOrders())
            {
                if (receivingAreaRuntimeHost.State.TryGetReservation(
                        ReceivingLoadId.EquipmentOrder(order.OrderNumber),
                        out _))
                {
                    stagedOrders.Add(order.OrderNumber);
                }
            }

            int received = 0;

            for (int index = 0; index < stagedOrders.Count; index++)
            {
                FixtureEquipmentOrderResult result =
                    Orders.Receive(stagedOrders[index]);

                if (result.Succeeded)
                {
                    received += result.Orders[0].Quantity;
                }
            }

            return received;
        }

        public void RefreshReceivingReservations()
        {
            if (!IsInitialized
                || receivingAreaRuntimeHost == null)
            {
                return;
            }

            List<ReceivingLoadId> readyLoads =
                new List<ReceivingLoadId>();

            foreach (FixtureEquipmentOrder order
                     in Orders.EnumerateReadyOrders())
            {
                readyLoads.Add(
                    ReceivingLoadId.EquipmentOrder(order.OrderNumber));
            }

            receivingAreaRuntimeHost.SetReadyLoads(
                ReceivingLoadSource,
                readyLoads);
        }

        private void HandleClockTimeChanged(SimulationDateTime currentTime)
        {
            Orders?.AdvanceTo(currentTime.TotalGameSeconds);
        }

        private void HandleOrdersChanged()
        {
            RefreshReceivingReservations();
            StateChanged?.Invoke();
        }

        private void HandlePlansChanged()
        {
            StateChanged?.Invoke();
        }

        private void HandleEquipmentQuantityChanged(
            FixtureDefinitionId fixtureDefinitionId)
        {
            StateChanged?.Invoke();
        }

        private void HandleCashBalanceChanged()
        {
            StateChanged?.Invoke();
        }

        private void HandleFixtureRuntimeInitialized(
            FixtureRuntimeHost initializedHost)
        {
            TryInitialize();
        }

        private void HandlePlanogramRuntimeInitialized(
            FixturePlanogramRuntimeHost initializedHost)
        {
            TryInitialize();
        }

        private void HandleTimeInitialized()
        {
            TryInitialize();
        }

        private void HandleReceivingInitialized(
            ReceivingAreaRuntimeHost initializedHost)
        {
            RefreshReceivingReservations();
        }

        private void AttachClock(SimulationClock clock)
        {
            if (subscribedClock == clock)
            {
                return;
            }

            DetachClock();
            subscribedClock = clock;

            if (subscribedClock != null)
            {
                subscribedClock.TimeChanged += HandleClockTimeChanged;
            }
        }

        private void DetachClock()
        {
            if (subscribedClock == null)
            {
                return;
            }

            subscribedClock.TimeChanged -= HandleClockTimeChanged;
            subscribedClock = null;
        }

        private void OnDisable()
        {
            if (fixtureRuntimeHost != null)
            {
                fixtureRuntimeHost.Initialized -=
                    HandleFixtureRuntimeInitialized;
            }

            if (planogramRuntimeHost != null)
            {
                planogramRuntimeHost.Initialized -=
                    HandlePlanogramRuntimeInitialized;
            }

            if (timeRuntimeHost != null)
            {
                timeRuntimeHost.Initialized -= HandleTimeInitialized;
            }

            if (receivingAreaRuntimeHost != null)
            {
                receivingAreaRuntimeHost.Initialized -=
                    HandleReceivingInitialized;
                receivingAreaRuntimeHost.ClearReadyLoads(
                    ReceivingLoadSource);
            }

            DetachClock();

            if (Inventory != null)
            {
                Inventory.QuantityChanged -=
                    HandleEquipmentQuantityChanged;
            }

            if (Orders != null)
            {
                Orders.OrdersChanged -= HandleOrdersChanged;
            }

            if (Plans != null)
            {
                Plans.PlansChanged -= HandlePlansChanged;
            }

            if (planogramRuntimeHost?.Cash != null)
            {
                planogramRuntimeHost.Cash.BalanceChanged -=
                    HandleCashBalanceChanged;
            }

            Installation = null;
            Planning = null;
            Plans = null;
            Orders = null;
            Inventory = null;
            Catalog = null;
            IsInitialized = false;
        }
    }
}
