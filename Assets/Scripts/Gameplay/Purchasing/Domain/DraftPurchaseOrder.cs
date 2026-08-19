using System;
using System.Collections.Generic;

namespace BigRetail.Purchasing.Domain
{
    /// <summary>
    /// Mutable runtime draft for exactly one supplier. It is not an authored
    /// ScriptableObject and does not represent a placed order.
    /// </summary>
    public sealed class DraftPurchaseOrder
    {
        private readonly Dictionary<SupplierOfferId, PurchaseOrderLine> lines =
            new Dictionary<SupplierOfferId, PurchaseOrderLine>();
        private readonly List<SupplierOfferId> lineOrder =
            new List<SupplierOfferId>();


        public SupplierId SupplierId { get; }

        public int LineCount =>
            lines.Count;

        public bool IsEmpty =>
            lines.Count == 0;

        public long TotalCents
        {
            get
            {
                long totalCents = 0;

                for (int index = 0; index < lineOrder.Count; index++)
                {
                    totalCents = checked(
                        totalCents + lines[lineOrder[index]].LineTotalCents);
                }

                return totalCents;
            }
        }


        public DraftPurchaseOrder(SupplierId supplierId)
        {
            if (!supplierId.IsValid)
            {
                throw new ArgumentException(
                    "A draft purchase order requires a valid supplier.",
                    nameof(supplierId));
            }

            SupplierId = supplierId;
        }


        public int GetPurchasePackCount(SupplierOfferId offerId)
        {
            return lines.TryGetValue(offerId, out PurchaseOrderLine line)
                ? line.PurchasePackCount
                : 0;
        }

        public void SetPurchasePackCount(
            SupplierOfferDefinition offer,
            int purchasePackCount)
        {
            if (offer == null)
            {
                throw new ArgumentNullException(nameof(offer));
            }

            if (offer.SupplierId != SupplierId)
            {
                throw new ArgumentException(
                    $"Offer '{offer.Id}' belongs to supplier "
                    + $"'{offer.SupplierId}', not '{SupplierId}'.",
                    nameof(offer));
            }

            if (purchasePackCount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(purchasePackCount),
                    purchasePackCount,
                    "A draft quantity cannot be negative.");
            }

            if (purchasePackCount == 0)
            {
                if (lines.Remove(offer.Id))
                {
                    lineOrder.Remove(offer.Id);
                }

                return;
            }

            if (lines.TryGetValue(offer.Id, out PurchaseOrderLine line))
            {
                line.SetPurchasePackCount(purchasePackCount);
                return;
            }

            lines.Add(
                offer.Id,
                new PurchaseOrderLine(offer, purchasePackCount));
            lineOrder.Add(offer.Id);
        }

        public long GetAmountRemainingForMinimum(
            SupplierDefinition supplier)
        {
            if (supplier == null)
            {
                throw new ArgumentNullException(nameof(supplier));
            }

            if (supplier.Id != SupplierId)
            {
                throw new ArgumentException(
                    "The supplier does not own this draft order.",
                    nameof(supplier));
            }

            return Math.Max(0, supplier.MinimumOrderCents - TotalCents);
        }

        public IEnumerable<PurchaseOrderLine> EnumerateLines()
        {
            for (int index = 0; index < lineOrder.Count; index++)
            {
                yield return lines[lineOrder[index]];
            }
        }
    }
}
