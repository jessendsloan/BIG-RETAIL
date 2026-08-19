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
                            receivedUnitCount + line.RemainingUnitCount);
                        line.MarkReceived();
                    }
                    else
                    {
                        failedUnitCount = checked(
                            failedUnitCount + line.RemainingUnitCount);
                    }
                }

                if (record.RemainingUnitCount == 0)
                {
                    record.MarkReceived();
                    completedOrderCount++;
                }
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


            public void MarkReadyToReceive()
            {
                Status = PurchaseOrderDeliveryStatus.ReadyToReceive;
            }

            public void MarkReceived()
            {
                Status = PurchaseOrderDeliveryStatus.Received;
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
                RemainingUnitCount = line.TotalUnits;
            }


            public ProductId ProductId { get; }

            public int RemainingUnitCount { get; private set; }


            public void MarkReceived()
            {
                RemainingUnitCount = 0;
            }
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
