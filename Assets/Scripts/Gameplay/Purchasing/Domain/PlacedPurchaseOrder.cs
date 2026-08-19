using System;
using System.Collections.Generic;

namespace BigRetail.Purchasing.Domain
{
    /// <summary>
    /// Immutable supplier purchase order committed by Purchasing. Delivery,
    /// receiving, inventory ownership, and payment remain downstream concerns.
    /// </summary>
    public sealed class PlacedPurchaseOrder
    {
        private readonly IReadOnlyList<PlacedPurchaseOrderLine> lines;


        public long OrderNumber { get; }

        public SupplierId SupplierId { get; }

        public CommercialTime PlacedAt { get; }

        public SupplierDeliveryEstimate DeliveryEstimate { get; }

        public IReadOnlyList<PlacedPurchaseOrderLine> Lines =>
            lines;

        public long TotalCents { get; }


        internal PlacedPurchaseOrder(
            long orderNumber,
            DraftPurchaseOrder draft,
            CommercialTime placedAt,
            SupplierDeliveryEstimate deliveryEstimate)
        {
            if (orderNumber <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(orderNumber),
                    orderNumber,
                    "A purchase order number must be positive.");
            }

            if (draft == null)
            {
                throw new ArgumentNullException(nameof(draft));
            }

            if (draft.IsEmpty)
            {
                throw new ArgumentException(
                    "A placed purchase order requires at least one line.",
                    nameof(draft));
            }

            List<PlacedPurchaseOrderLine> snapshots =
                new List<PlacedPurchaseOrderLine>(draft.LineCount);
            long totalCents = 0;

            foreach (PurchaseOrderLine line in draft.EnumerateLines())
            {
                PlacedPurchaseOrderLine snapshot =
                    new PlacedPurchaseOrderLine(line);
                snapshots.Add(snapshot);
                totalCents = checked(totalCents + snapshot.LineTotalCents);
            }

            OrderNumber = orderNumber;
            SupplierId = draft.SupplierId;
            PlacedAt = placedAt;
            DeliveryEstimate = deliveryEstimate;
            lines = snapshots.AsReadOnly();
            TotalCents = totalCents;
        }
    }
}
