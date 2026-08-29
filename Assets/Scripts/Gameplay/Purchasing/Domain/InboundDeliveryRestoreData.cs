using System;
using System.Collections.Generic;
using BigRetail.Merchandise.Domain;

namespace BigRetail.Purchasing.Domain
{
    /// <summary>
    /// Engine-free input for restoring one authored or saved inbound order
    /// through the normal Purchasing fulfillment pipeline.
    /// </summary>
    public sealed class InboundDeliveryRestoreData
    {
        private readonly IReadOnlyList<InboundDeliveryRestoreLine> lines;


        public long OrderNumber { get; }

        public SupplierId SupplierId { get; }

        public CommercialTime ArrivalTime { get; }

        public PurchaseOrderDeliveryStatus Status { get; }

        public IReadOnlyList<InboundDeliveryRestoreLine> Lines =>
            lines;


        public InboundDeliveryRestoreData(
            long orderNumber,
            SupplierId supplierId,
            CommercialTime arrivalTime,
            PurchaseOrderDeliveryStatus status,
            IReadOnlyList<InboundDeliveryRestoreLine> lines)
        {
            if (orderNumber <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(orderNumber),
                    orderNumber,
                    "A restored order number must be positive.");
            }

            if (!supplierId.IsValid)
            {
                throw new ArgumentException(
                    "A restored delivery requires a supplier.",
                    nameof(supplierId));
            }

            if (!Enum.IsDefined(
                    typeof(PurchaseOrderDeliveryStatus),
                    status))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(status),
                    status,
                    "The restored delivery status is unsupported.");
            }

            if (lines == null || lines.Count == 0)
            {
                throw new ArgumentException(
                    "A restored delivery requires at least one line.",
                    nameof(lines));
            }

            InboundDeliveryRestoreLine[] snapshot =
                new InboundDeliveryRestoreLine[lines.Count];

            for (int index = 0; index < lines.Count; index++)
            {
                snapshot[index] = lines[index];
            }

            OrderNumber = orderNumber;
            SupplierId = supplierId;
            ArrivalTime = arrivalTime;
            Status = status;
            this.lines = Array.AsReadOnly(snapshot);
        }
    }


    public readonly struct InboundDeliveryRestoreLine
    {
        public ProductId ProductId { get; }

        public int UnitCount { get; }


        public InboundDeliveryRestoreLine(
            ProductId productId,
            int unitCount)
        {
            if (!productId.IsValid)
            {
                throw new ArgumentException(
                    "A restored delivery line requires a product.",
                    nameof(productId));
            }

            if (unitCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(unitCount),
                    unitCount,
                    "A restored delivery line requires positive units.");
            }

            ProductId = productId;
            UnitCount = unitCount;
        }
    }
}
