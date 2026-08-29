using System;
using System.Collections.Generic;
using BigRetail.Merchandise.Domain;

namespace BigRetail.Purchasing.Domain
{
    public enum PurchaseOrderDeliveryStatus
    {
        Scheduled = 0,
        ReadyToReceive = 1,
        Received = 2
    }


    /// <summary>
    /// Advances placed purchase orders from their supplier schedule into the
    /// store receiving boundary. Inventory changes only when the player
    /// receives an arrived delivery.
    /// </summary>
    public sealed class PurchaseOrderFulfillmentService
    {
        private readonly IPurchaseOrderReceiver receiver;
        private readonly Dictionary<long, FulfillmentRecord> records =
            new Dictionary<long, FulfillmentRecord>();
        private readonly List<FulfillmentRecord> recordOrder =
            new List<FulfillmentRecord>();


        public int ScheduledOrderCount =>
            CountOrders(PurchaseOrderDeliveryStatus.Scheduled);

        public int ReadyToReceiveOrderCount =>
            CountOrders(PurchaseOrderDeliveryStatus.ReadyToReceive);

        public int ReceivedOrderCount =>
            CountOrders(PurchaseOrderDeliveryStatus.Received);

        public bool HasAvailableDeliveries =>
            ReadyToReceiveOrderCount > 0;

        public int ReadyToReceiveUnitCount
        {
            get
            {
                long unitCount = 0;

                for (int index = 0; index < recordOrder.Count; index++)
                {
                    FulfillmentRecord record = recordOrder[index];

                    if (record.Status
                        != PurchaseOrderDeliveryStatus.ReadyToReceive)
                    {
                        continue;
                    }

                    unitCount += record.RemainingUnitCount;

                    if (unitCount >= int.MaxValue)
                    {
                        return int.MaxValue;
                    }
                }

                return (int)unitCount;
            }
        }


        public PurchaseOrderFulfillmentService(
            IPurchaseOrderReceiver receiver)
        {
            this.receiver = receiver
                ?? throw new ArgumentNullException(nameof(receiver));
        }


        public event Action DeliveriesChanged;


        public void Schedule(
            IReadOnlyList<PlacedPurchaseOrder> orders)
        {
            if (orders == null)
            {
                throw new ArgumentNullException(nameof(orders));
            }

            for (int index = 0; index < orders.Count; index++)
            {
                PlacedPurchaseOrder order = orders[index]
                    ?? throw new ArgumentException(
                        "A scheduled order collection cannot contain null.",
                        nameof(orders));

                if (records.ContainsKey(order.OrderNumber))
                {
                    throw new ArgumentException(
                        $"Purchase order '{order.OrderNumber}' is already scheduled.",
                        nameof(orders));
                }
            }

            if (orders.Count == 0)
            {
                return;
            }

            for (int index = 0; index < orders.Count; index++)
            {
                FulfillmentRecord record =
                    new FulfillmentRecord(orders[index]);
                records.Add(record.Order.OrderNumber, record);
                recordOrder.Add(record);
            }

            DeliveriesChanged?.Invoke();
        }

        /// <summary>
        /// Restores one fulfillment record at an authored status without
        /// replaying delivery time or inventory side effects.
        /// </summary>
        public void Restore(
            PlacedPurchaseOrder order,
            PurchaseOrderDeliveryStatus status)
        {
            if (order == null)
            {
                throw new ArgumentNullException(nameof(order));
            }

            if (!Enum.IsDefined(
                    typeof(PurchaseOrderDeliveryStatus),
                    status))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(status),
                    status,
                    "The restored fulfillment status is unsupported.");
            }

            if (records.ContainsKey(order.OrderNumber))
            {
                throw new ArgumentException(
                    $"Purchase order '{order.OrderNumber}' is already scheduled.",
                    nameof(order));
            }

            FulfillmentRecord record =
                new FulfillmentRecord(order);
            record.RestoreStatus(status);
            records.Add(order.OrderNumber, record);
            recordOrder.Add(record);
        }

        public void AdvanceTo(
            CommercialTime currentTime)
        {
            bool changed = false;

            for (int index = 0; index < recordOrder.Count; index++)
            {
                FulfillmentRecord record = recordOrder[index];

                if (record.Status != PurchaseOrderDeliveryStatus.Scheduled
                    || currentTime.CompareTo(
                        record.Order.DeliveryEstimate.EarliestArrival) < 0)
                {
                    continue;
                }

                record.MarkReadyToReceive();
                changed = true;
            }

            if (changed)
            {
                DeliveriesChanged?.Invoke();
            }
        }

        public IEnumerable<InboundDeliveryLoad>
            EnumerateReadyDeliveries()
        {
            for (int index = 0; index < recordOrder.Count; index++)
            {
                FulfillmentRecord record = recordOrder[index];

                if (record.Status
                    == PurchaseOrderDeliveryStatus.ReadyToReceive)
                {
                    yield return new InboundDeliveryLoad(
                        record.Order,
                        record.RemainingUnitCount,
                        record.RemainingPurchasePackCount);
                }
            }
        }

        /// <summary>
        /// Resolves the next physical supplier case that can be worked from
        /// one staged delivery. Player and worker-AI adapters use this same
        /// query before committing a stocking action.
        /// </summary>
        public bool TryGetNextPurchasePack(
            long orderNumber,
            out InboundPurchasePack purchasePack)
        {
            if (!records.TryGetValue(
                    orderNumber,
                    out FulfillmentRecord record)
                || record.Status
                    != PurchaseOrderDeliveryStatus.ReadyToReceive)
            {
                purchasePack = default;
                return false;
            }

            for (int lineIndex = 0;
                 lineIndex < record.Lines.Count;
                 lineIndex++)
            {
                FulfillmentLine line = record.Lines[lineIndex];

                if (line.RemainingPurchasePackCount <= 0)
                {
                    continue;
                }

                purchasePack = new InboundPurchasePack(
                    orderNumber,
                    line.ProductId,
                    line.UnitsPerPurchasePack);
                return true;
            }

            purchasePack = default;
            return false;
        }

        /// <summary>
        /// Receives exactly one selected supplier case through a caller-owned
        /// destination. This is the atomic command shared by direct player
        /// interaction and future worker agents.
        /// </summary>
        public PurchaseOrderReceivingResult ReceivePurchasePack(
            InboundPurchasePack purchasePack,
            IPurchaseOrderReceiver purchasePackReceiver)
        {
            if (!purchasePack.IsValid)
            {
                throw new ArgumentException(
                    "A received supplier case requires a valid case reference.",
                    nameof(purchasePack));
            }

            if (purchasePackReceiver == null)
            {
                throw new ArgumentNullException(
                    nameof(purchasePackReceiver));
            }

            if (!records.TryGetValue(
                    purchasePack.OrderNumber,
                    out FulfillmentRecord record))
            {
                throw new KeyNotFoundException(
                    $"Purchase order '{purchasePack.OrderNumber}' is not scheduled.");
            }

            if (record.Status
                    != PurchaseOrderDeliveryStatus.ReadyToReceive
                || !TryGetNextPurchasePack(
                    purchasePack.OrderNumber,
                    out InboundPurchasePack nextPurchasePack)
                || nextPurchasePack != purchasePack)
            {
                return default;
            }

            FulfillmentLine selectedLine = null;

            for (int lineIndex = 0;
                 lineIndex < record.Lines.Count;
                 lineIndex++)
            {
                FulfillmentLine line = record.Lines[lineIndex];

                if (line.RemainingPurchasePackCount > 0
                    && line.ProductId == purchasePack.ProductId
                    && line.UnitsPerPurchasePack
                        == purchasePack.UnitCount)
                {
                    selectedLine = line;
                    break;
                }
            }

            if (selectedLine == null)
            {
                return default;
            }

            if (!purchasePackReceiver.TryReceive(
                    purchasePack.ProductId,
                    purchasePack.UnitCount))
            {
                return new PurchaseOrderReceivingResult(
                    receivedUnitCount: 0,
                    failedUnitCount: purchasePack.UnitCount,
                    completedOrderCount: 0);
            }

            selectedLine.MarkOnePurchasePackReceived();
            int completedOrderCount = 0;

            if (record.RemainingUnitCount == 0)
            {
                record.MarkReceived();
                completedOrderCount = 1;
            }

            DeliveriesChanged?.Invoke();
            return new PurchaseOrderReceivingResult(
                purchasePack.UnitCount,
                failedUnitCount: 0,
                completedOrderCount);
        }

        public PurchaseOrderReceivingResult ReceiveDelivery(
            long orderNumber)
        {
            if (!records.TryGetValue(
                    orderNumber,
                    out FulfillmentRecord record))
            {
                throw new KeyNotFoundException(
                    $"Purchase order '{orderNumber}' is not scheduled.");
            }

            if (record.Status
                != PurchaseOrderDeliveryStatus.ReadyToReceive)
            {
                return default;
            }

            PurchaseOrderReceivingResult result =
                ReceiveRecord(record);

            if (result.ReceivedUnitCount > 0
                || result.CompletedOrderCount > 0)
            {
                DeliveriesChanged?.Invoke();
            }

            return result;
        }

        public PurchaseOrderReceivingResult ReceiveAvailableDeliveries()
        {
            int receivedUnitCount = 0;
            int failedUnitCount = 0;
            int completedOrderCount = 0;

            for (int recordIndex = 0;
                 recordIndex < recordOrder.Count;
                 recordIndex++)
            {
                FulfillmentRecord record = recordOrder[recordIndex];

                if (record.Status
                    != PurchaseOrderDeliveryStatus.ReadyToReceive)
                {
                    continue;
                }

                PurchaseOrderReceivingResult recordResult =
                    ReceiveRecord(record);
                receivedUnitCount = checked(
                    receivedUnitCount
                    + recordResult.ReceivedUnitCount);
                failedUnitCount = checked(
                    failedUnitCount
                    + recordResult.FailedUnitCount);
                completedOrderCount = checked(
                    completedOrderCount
                    + recordResult.CompletedOrderCount);
            }

            if (receivedUnitCount > 0 || completedOrderCount > 0)
            {
                DeliveriesChanged?.Invoke();
            }

            return new PurchaseOrderReceivingResult(
                receivedUnitCount,
                failedUnitCount,
                completedOrderCount);
        }

        public PurchaseOrderDeliveryStatus GetStatus(
            long orderNumber)
        {
            if (records.TryGetValue(
                    orderNumber,
                    out FulfillmentRecord record))
            {
                return record.Status;
            }

            throw new KeyNotFoundException(
                $"Purchase order '{orderNumber}' is not scheduled.");
        }


        private int CountOrders(
            PurchaseOrderDeliveryStatus status)
        {
            int count = 0;

            for (int index = 0; index < recordOrder.Count; index++)
            {
                if (recordOrder[index].Status == status)
                {
                    count++;
                }
            }

            return count;
        }

        private PurchaseOrderReceivingResult ReceiveRecord(
            FulfillmentRecord record)
        {
            int receivedUnitCount = 0;
            int failedUnitCount = 0;

            for (int lineIndex = 0;
                 lineIndex < record.Lines.Count;
                 lineIndex++)
            {
                FulfillmentLine line = record.Lines[lineIndex];

                if (line.RemainingUnitCount <= 0)
                {
                    continue;
                }

                if (receiver.TryReceive(
                        line.ProductId,
                        line.RemainingUnitCount))
                {
                    receivedUnitCount = checked(
                        receivedUnitCount
                        + line.RemainingUnitCount);
                    line.MarkReceived();
                }
                else
                {
                    failedUnitCount = checked(
                        failedUnitCount
                        + line.RemainingUnitCount);
                }
            }

            int completedOrderCount = 0;

            if (record.RemainingUnitCount == 0)
            {
                record.MarkReceived();
                completedOrderCount = 1;
            }

            return new PurchaseOrderReceivingResult(
                receivedUnitCount,
                failedUnitCount,
                completedOrderCount);
        }


        private sealed class FulfillmentRecord
        {
            public FulfillmentRecord(
                PlacedPurchaseOrder order)
            {
                Order = order
                    ?? throw new ArgumentNullException(nameof(order));
                Lines = new List<FulfillmentLine>(order.Lines.Count);

                for (int index = 0; index < order.Lines.Count; index++)
                {
                    Lines.Add(new FulfillmentLine(order.Lines[index]));
                }
            }


            public PlacedPurchaseOrder Order { get; }

            public List<FulfillmentLine> Lines { get; }

            public PurchaseOrderDeliveryStatus Status { get; private set; }

            public int RemainingUnitCount
            {
                get
                {
                    int total = 0;

                    for (int index = 0; index < Lines.Count; index++)
                    {
                        total = checked(
                            total + Lines[index].RemainingUnitCount);
                    }

                    return total;
                }
            }

            public int RemainingPurchasePackCount
            {
                get
                {
                    int total = 0;

                    for (int index = 0; index < Lines.Count; index++)
                    {
                        total = checked(
                            total
                            + Lines[index]
                                .RemainingPurchasePackCount);
                    }

                    return total;
                }
            }


            public void MarkReadyToReceive()
            {
                Status = PurchaseOrderDeliveryStatus.ReadyToReceive;
            }

            public void MarkReceived()
            {
                Status = PurchaseOrderDeliveryStatus.Received;
            }

            public void RestoreStatus(
                PurchaseOrderDeliveryStatus status)
            {
                Status = status;

                if (status != PurchaseOrderDeliveryStatus.Received)
                {
                    return;
                }

                for (int index = 0;
                     index < Lines.Count;
                     index++)
                {
                    Lines[index].MarkReceived();
                }
            }
        }


        private sealed class FulfillmentLine
        {
            public FulfillmentLine(
                PlacedPurchaseOrderLine line)
            {
                if (line == null)
                {
                    throw new ArgumentNullException(nameof(line));
                }

                ProductId = line.ProductId;
                UnitsPerPurchasePack = line.UnitsPerPurchasePack;
                RemainingPurchasePackCount = line.PurchasePackCount;
            }


            public ProductId ProductId { get; }

            public int UnitsPerPurchasePack { get; }

            public int RemainingPurchasePackCount { get; private set; }

            public int RemainingUnitCount =>
                checked(
                    UnitsPerPurchasePack
                    * RemainingPurchasePackCount);


            public void MarkOnePurchasePackReceived()
            {
                if (RemainingPurchasePackCount <= 0)
                {
                    throw new InvalidOperationException(
                        "A fulfilled supplier line has no case left to receive.");
                }

                RemainingPurchasePackCount--;
            }


            public void MarkReceived()
            {
                RemainingPurchasePackCount = 0;
            }
        }
    }


    /// <summary>
    /// Stable description of one physical purchase pack awaiting stocking.
    /// It is intentionally independent of input so workers and the player can
    /// execute the same receiving command.
    /// </summary>
    public readonly struct InboundPurchasePack :
        IEquatable<InboundPurchasePack>
    {
        public long OrderNumber { get; }

        public ProductId ProductId { get; }

        public int UnitCount { get; }

        public bool IsValid =>
            OrderNumber > 0
            && ProductId.IsValid
            && UnitCount > 0;


        internal InboundPurchasePack(
            long orderNumber,
            ProductId productId,
            int unitCount)
        {
            OrderNumber = orderNumber;
            ProductId = productId;
            UnitCount = unitCount;
        }


        public bool Equals(InboundPurchasePack other)
        {
            return OrderNumber == other.OrderNumber
                && ProductId == other.ProductId
                && UnitCount == other.UnitCount;
        }

        public override bool Equals(object obj)
        {
            return obj is InboundPurchasePack other
                && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;

                hash = (hash * 31) + OrderNumber.GetHashCode();
                hash = (hash * 31) + ProductId.GetHashCode();
                hash = (hash * 31) + UnitCount;
                return hash;
            }
        }

        public static bool operator ==(
            InboundPurchasePack left,
            InboundPurchasePack right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            InboundPurchasePack left,
            InboundPurchasePack right)
        {
            return !left.Equals(right);
        }
    }


    public readonly struct PurchaseOrderReceivingResult
    {
        public PurchaseOrderReceivingResult(
            int receivedUnitCount,
            int failedUnitCount,
            int completedOrderCount)
        {
            ReceivedUnitCount = receivedUnitCount;
            FailedUnitCount = failedUnitCount;
            CompletedOrderCount = completedOrderCount;
        }


        public int ReceivedUnitCount { get; }

        public int FailedUnitCount { get; }

        public int CompletedOrderCount { get; }

        public bool Succeeded =>
            ReceivedUnitCount > 0
            && FailedUnitCount == 0;
    }
}
