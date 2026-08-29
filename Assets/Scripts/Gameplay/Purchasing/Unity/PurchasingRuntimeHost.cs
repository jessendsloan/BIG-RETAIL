using System;
using System.Collections.Generic;
using BigRetail.Economy.Domain;
using BigRetail.Map.Unity.Fixtures;
using BigRetail.Merchandise.Domain;
using BigRetail.Purchasing.Domain;
using BigRetail.Receiving.Unity;
using BigRetail.Simulation.Time.Domain;
using BigRetail.Simulation.Time.Unity;
using UnityEngine;

namespace BigRetail.Purchasing.Unity
{
    /// <summary>
    /// Owns Purchasing for one live store session and connects its commercial
    /// records to the authoritative campaign clock, cash, and receiving path.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-50)]
    public sealed class PurchasingRuntimeHost : MonoBehaviour
    {
        [SerializeField]
        private CommercialCatalogAsset commercialCatalog;

        [SerializeField]
        private SimulationTimeRuntimeHost timeHost;

        [SerializeField]
        private FixturePlanogramRuntimeHost planogramRuntimeHost;

        [SerializeField]
        private ReceivingAreaRuntimeHost receivingAreaRuntimeHost;

        private SimulationClock subscribedClock;


        public bool IsInitialized { get; private set; }

        public string InitializationError { get; private set; } =
            string.Empty;

        public CommercialCatalogAsset CatalogAsset =>
            commercialCatalog;

        public CommercialCatalog Catalog { get; private set; }

        public PurchasingService Purchasing { get; private set; }

        public PurchaseOrderFulfillmentService Fulfillment { get; private set; }

        public SupplierCaseStockingService CaseStocking
        {
            get;
            private set;
        }

        public StoreCashState Cash =>
            planogramRuntimeHost?.Cash;

        public CommercialTime CurrentTime { get; private set; }

        public int StagedReadyOrderCount
        {
            get
            {
                int count = 0;

                if (Fulfillment == null
                    || receivingAreaRuntimeHost?.State == null)
                {
                    return count;
                }

                foreach (
                    InboundDeliveryLoad load
                    in Fulfillment.EnumerateReadyDeliveries())
                {
                    if (receivingAreaRuntimeHost.State.TryGetReservation(
                            load.OrderNumber,
                            out _))
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public int StagedReadyUnitCount
        {
            get
            {
                long unitCount = 0;

                if (Fulfillment == null
                    || receivingAreaRuntimeHost?.State == null)
                {
                    return 0;
                }

                foreach (
                    InboundDeliveryLoad load
                    in Fulfillment.EnumerateReadyDeliveries())
                {
                    if (!receivingAreaRuntimeHost.State.TryGetReservation(
                            load.OrderNumber,
                            out _))
                    {
                        continue;
                    }

                    unitCount += load.RemainingUnitCount;

                    if (unitCount >= int.MaxValue)
                    {
                        return int.MaxValue;
                    }
                }

                return (int)unitCount;
            }
        }

        public int WaitingForReceivingSpaceOrderCount =>
            Fulfillment == null
                ? 0
                : Math.Max(
                    0,
                    Fulfillment.ReadyToReceiveOrderCount
                    - StagedReadyOrderCount);

        public bool HasStagedDeliveries =>
            StagedReadyOrderCount > 0;


        public event Action<PurchasingRuntimeHost> Initialized;

        public event Action<CommercialTime> CommercialTimeChanged;

        public event Action DeliveriesChanged;


        private void OnEnable()
        {
            if (timeHost != null)
            {
                timeHost.Initialized += HandleTimeHostInitialized;
            }

            if (planogramRuntimeHost != null)
            {
                planogramRuntimeHost.Initialized +=
                    HandlePlanogramRuntimeInitialized;
            }

            TryInitialize();
        }

        private void Start()
        {
            if (!TryInitialize())
            {
                Debug.LogError(
                    string.IsNullOrEmpty(InitializationError)
                        ? "Purchasing runtime could not initialize."
                        : InitializationError,
                    this);
            }
        }

        private void OnDisable()
        {
            if (timeHost != null)
            {
                timeHost.Initialized -= HandleTimeHostInitialized;
            }

            if (planogramRuntimeHost != null)
            {
                planogramRuntimeHost.Initialized -=
                    HandlePlanogramRuntimeInitialized;
            }

            DetachClock();

            if (Fulfillment != null)
            {
                Fulfillment.DeliveriesChanged -=
                    HandleDeliveriesChanged;
            }

            CaseStocking = null;
            Fulfillment = null;
            Purchasing = null;
            Catalog = null;
            IsInitialized = false;
        }


        public bool TryInitialize()
        {
            if (IsInitialized)
            {
                return true;
            }

            if (commercialCatalog == null
                || timeHost == null
                || planogramRuntimeHost == null)
            {
                InitializationError =
                    "Purchasing runtime requires the commercial catalog, "
                    + "simulation clock, and fixture planogram runtime.";
                return false;
            }

            if (!timeHost.IsInitialized
                || !planogramRuntimeHost.IsInitialized
                || planogramRuntimeHost.Backstock == null
                || planogramRuntimeHost.Cash == null
                || planogramRuntimeHost.Products == null)
            {
                InitializationError =
                    "Purchasing runtime is waiting for campaign time, cash, "
                    + "and receiving inventory.";
                return false;
            }

            if (!commercialCatalog.TryCreateCatalog(
                    out CommercialCatalog catalog,
                    out string error))
            {
                InitializationError = error;
                return false;
            }

            if (!CatalogsMatch(
                    catalog.Products,
                    planogramRuntimeHost.Products))
            {
                InitializationError =
                    "Purchasing and fixture inventory must use the same Product catalog.";
                return false;
            }

            Catalog = catalog;
            Purchasing = new PurchasingService(Catalog);
            Fulfillment =
                new PurchaseOrderFulfillmentService(
                    new FixtureBackstockPurchaseOrderReceiver(
                        planogramRuntimeHost.Backstock));
            CaseStocking =
                new SupplierCaseStockingService(
                    Fulfillment,
                    planogramRuntimeHost.Backstock);
            Fulfillment.DeliveriesChanged += HandleDeliveriesChanged;

            CurrentTime = ToCommercialTime(timeHost.Clock.CurrentTime);
            AttachClock(timeHost.Clock);
            Fulfillment.AdvanceTo(CurrentTime);

            InitializationError = string.Empty;
            IsInitialized = true;
            Initialized?.Invoke(this);
            return true;
        }

        public bool TryPlaceDrafts(
            out IReadOnlyList<PlacedPurchaseOrder> placedOrders,
            out string error)
        {
            if (!TryInitialize())
            {
                placedOrders = null;
                error = InitializationError;
                return false;
            }

            try
            {
                placedOrders =
                    Purchasing.PlaceDrafts(
                        CurrentTime,
                        Cash.TrySpend);
                Fulfillment.Schedule(placedOrders);
                Fulfillment.AdvanceTo(CurrentTime);
                error = string.Empty;
                return true;
            }
            catch (InvalidOperationException exception)
            {
                placedOrders = null;
                error = exception.Message;
                return false;
            }
            catch (OverflowException exception)
            {
                placedOrders = null;
                error = exception.Message;
                return false;
            }
        }

        public bool TryValidateDeliveryRestore(
            IReadOnlyList<InboundDeliveryRestoreData> deliveries,
            out string error)
        {
            if (!TryInitialize())
            {
                error = InitializationError;
                return false;
            }

            return TryCreateRestoredState(
                deliveries,
                out _,
                out _,
                out error);
        }

        /// <summary>
        /// Atomically replaces the live commercial order and fulfillment
        /// records with authored or saved deliveries. Inventory is unchanged
        /// until the normal receiving action accepts a ready load.
        /// </summary>
        public bool TryReplaceDeliveries(
            IReadOnlyList<InboundDeliveryRestoreData> deliveries,
            out string error)
        {
            if (!TryInitialize())
            {
                error = InitializationError;
                return false;
            }

            if (!TryCreateRestoredState(
                    deliveries,
                    out PurchasingService nextPurchasing,
                    out PurchaseOrderFulfillmentService nextFulfillment,
                    out error))
            {
                return false;
            }

            if (Fulfillment != null)
            {
                Fulfillment.DeliveriesChanged -=
                    HandleDeliveriesChanged;
            }

            Purchasing = nextPurchasing;
            Fulfillment = nextFulfillment;
            CaseStocking =
                new SupplierCaseStockingService(
                    Fulfillment,
                    planogramRuntimeHost.Backstock);
            Fulfillment.DeliveriesChanged += HandleDeliveriesChanged;
            DeliveriesChanged?.Invoke();
            return true;
        }

        public PurchaseOrderReceivingResult ReceiveAvailableDeliveries()
        {
            if (!TryInitialize())
            {
                return default;
            }

            if (receivingAreaRuntimeHost?.State == null)
            {
                return default;
            }

            List<long> stagedOrderNumbers = new List<long>();

            foreach (
                InboundDeliveryLoad load
                in Fulfillment.EnumerateReadyDeliveries())
            {
                if (receivingAreaRuntimeHost.State.TryGetReservation(
                        load.OrderNumber,
                        out _))
                {
                    stagedOrderNumbers.Add(load.OrderNumber);
                }
            }

            int receivedUnitCount = 0;
            int failedUnitCount = 0;
            int completedOrderCount = 0;

            for (int index = 0;
                 index < stagedOrderNumbers.Count;
                 index++)
            {
                PurchaseOrderReceivingResult orderResult =
                    Fulfillment.ReceiveDelivery(
                        stagedOrderNumbers[index]);
                receivedUnitCount = checked(
                    receivedUnitCount
                    + orderResult.ReceivedUnitCount);
                failedUnitCount = checked(
                    failedUnitCount
                    + orderResult.FailedUnitCount);
                completedOrderCount = checked(
                    completedOrderCount
                    + orderResult.CompletedOrderCount);
            }

            return new PurchaseOrderReceivingResult(
                receivedUnitCount,
                failedUnitCount,
                completedOrderCount);
        }

        public PurchaseOrderReceivingResult ReceiveDelivery(
            long orderNumber)
        {
            if (!TryInitialize())
            {
                return default;
            }

            if (receivingAreaRuntimeHost?.State == null
                || !receivingAreaRuntimeHost.State.TryGetReservation(
                    orderNumber,
                    out _))
            {
                return default;
            }

            return Fulfillment.ReceiveDelivery(orderNumber);
        }

        public static CommercialTime ToCommercialTime(
            SimulationDateTime simulationTime)
        {
            return new CommercialTime(
                simulationTime.DayNumber - 1,
                simulationTime.Hour,
                simulationTime.Minute);
        }


        private void HandleTimeHostInitialized()
        {
            TryInitialize();
        }

        private void HandlePlanogramRuntimeInitialized(
            FixturePlanogramRuntimeHost initializedHost)
        {
            TryInitialize();
        }

        private void HandleClockTimeChanged(
            SimulationDateTime simulationTime)
        {
            CommercialTime nextTime = ToCommercialTime(simulationTime);

            if (nextTime == CurrentTime)
            {
                return;
            }

            CurrentTime = nextTime;
            Fulfillment?.AdvanceTo(CurrentTime);
            CommercialTimeChanged?.Invoke(CurrentTime);
        }

        private void HandleDeliveriesChanged()
        {
            DeliveriesChanged?.Invoke();
        }

        private void AttachClock(
            SimulationClock clock)
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

        private static bool CatalogsMatch(
            ProductCatalog commercialProducts,
            ProductCatalog inventoryProducts)
        {
            if (commercialProducts == null
                || inventoryProducts == null
                || commercialProducts.Count != inventoryProducts.Count)
            {
                return false;
            }

            foreach (
                ProductDefinition product
                in commercialProducts.EnumerateDefinitions())
            {
                if (!inventoryProducts.Contains(product.Id))
                {
                    return false;
                }
            }

            return true;
        }

        private bool TryCreateRestoredState(
            IReadOnlyList<InboundDeliveryRestoreData> deliveries,
            out PurchasingService restoredPurchasing,
            out PurchaseOrderFulfillmentService restoredFulfillment,
            out string error)
        {
            restoredPurchasing = null;
            restoredFulfillment = null;

            if (deliveries == null)
            {
                error = "A delivery restore requires a collection.";
                return false;
            }

            try
            {
                PurchasingService candidatePurchasing =
                    new PurchasingService(Catalog);
                PurchaseOrderFulfillmentService candidateFulfillment =
                    new PurchaseOrderFulfillmentService(
                        new FixtureBackstockPurchaseOrderReceiver(
                            planogramRuntimeHost.Backstock));

                for (int deliveryIndex = 0;
                     deliveryIndex < deliveries.Count;
                     deliveryIndex++)
                {
                    InboundDeliveryRestoreData delivery =
                        deliveries[deliveryIndex]
                        ?? throw new ArgumentException(
                            "A delivery restore cannot contain null.",
                            nameof(deliveries));
                    DraftPurchaseOrder draft =
                        new DraftPurchaseOrder(delivery.SupplierId);
                    HashSet<ProductId> restoredProducts =
                        new HashSet<ProductId>();

                    for (int lineIndex = 0;
                         lineIndex < delivery.Lines.Count;
                         lineIndex++)
                    {
                        InboundDeliveryRestoreLine line =
                            delivery.Lines[lineIndex];

                        if (!restoredProducts.Add(line.ProductId))
                        {
                            throw new ArgumentException(
                                $"Restored order '{delivery.OrderNumber}' "
                                + $"duplicates product '{line.ProductId}'.",
                                nameof(deliveries));
                        }

                        SupplierOfferDefinition offer =
                            ResolveRestoredOffer(
                                delivery.SupplierId,
                                line.ProductId);

                        if (line.UnitCount
                            % offer.PurchasePackQuantity != 0)
                        {
                            throw new ArgumentException(
                                $"Restored order '{delivery.OrderNumber}' "
                                + $"contains {line.UnitCount} units of "
                                + $"'{line.ProductId}', which is not a whole "
                                + $"number of {offer.PurchasePackQuantity}-unit "
                                + "purchase cases.",
                                nameof(deliveries));
                        }

                        draft.SetPurchasePackCount(
                            offer,
                            line.UnitCount
                                / offer.PurchasePackQuantity);
                    }

                    PlacedPurchaseOrder order =
                        candidatePurchasing.RestorePlacedOrder(
                            delivery.OrderNumber,
                            draft,
                            delivery.ArrivalTime,
                            SupplierDeliveryEstimate.Exact(
                                delivery.ArrivalTime));
                    candidateFulfillment.Restore(
                        order,
                        delivery.Status);
                }

                restoredPurchasing = candidatePurchasing;
                restoredFulfillment = candidateFulfillment;
                error = string.Empty;
                return true;
            }
            catch (Exception exception)
                when (exception is ArgumentException
                      || exception is InvalidOperationException
                      || exception is KeyNotFoundException
                      || exception is OverflowException)
            {
                error = exception.Message;
                return false;
            }
        }

        private SupplierOfferDefinition ResolveRestoredOffer(
            SupplierId supplierId,
            ProductId productId)
        {
            SupplierOfferDefinition match = null;

            foreach (
                SupplierOfferDefinition offer
                in Catalog.Offers.EnumerateForSupplier(
                    supplierId,
                    availableOnly: false))
            {
                if (offer.ProductId != productId)
                {
                    continue;
                }

                if (match != null)
                {
                    throw new InvalidOperationException(
                        $"Supplier '{supplierId}' has more than one offer "
                        + $"for restored product '{productId}'.");
                }

                match = offer;
            }

            return match
                ?? throw new InvalidOperationException(
                    $"Supplier '{supplierId}' has no offer for restored "
                    + $"product '{productId}'.");
        }
    }
}
