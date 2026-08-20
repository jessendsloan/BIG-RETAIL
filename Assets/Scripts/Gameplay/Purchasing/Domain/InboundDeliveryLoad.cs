using System;
using System.Collections.Generic;

namespace BigRetail.Purchasing.Domain
{
    /// <summary>
    /// Read-only physical receiving projection of one supplier purchase
    /// order. The purchase order remains the exact commercial manifest while
    /// VisibleBoxCount compresses its case volume into one of four readable
    /// supplier-load art tiers.
    /// </summary>
    public sealed class InboundDeliveryLoad
    {
        private const int SmallLoadMaximumCaseCount = 3;
        private const int MediumLoadMaximumCaseCount = 7;
        private const int LargeLoadMaximumCaseCount = 11;


        public PlacedPurchaseOrder PurchaseOrder { get; }

        public long OrderNumber =>
            PurchaseOrder.OrderNumber;

        public SupplierId SupplierId =>
            PurchaseOrder.SupplierId;

        public IReadOnlyList<PlacedPurchaseOrderLine> Lines =>
            PurchaseOrder.Lines;

        public int PurchasePackCount { get; }

        public int RemainingUnitCount { get; }

        public int VisibleBoxCount =>
            ResolveVisibleBoxCount(PurchasePackCount);


        internal InboundDeliveryLoad(
            PlacedPurchaseOrder purchaseOrder,
            int remainingUnitCount)
        {
            PurchaseOrder = purchaseOrder
                ?? throw new ArgumentNullException(nameof(purchaseOrder));

            if (remainingUnitCount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(remainingUnitCount),
                    remainingUnitCount,
                    "A delivery cannot have negative remaining inventory.");
            }

            int purchasePackCount = 0;

            for (int index = 0; index < purchaseOrder.Lines.Count; index++)
            {
                purchasePackCount = checked(
                    purchasePackCount
                    + purchaseOrder.Lines[index].PurchasePackCount);
            }

            if (purchasePackCount <= 0)
            {
                throw new ArgumentException(
                    "An inbound delivery requires at least one purchase pack.",
                    nameof(purchaseOrder));
            }

            PurchasePackCount = purchasePackCount;
            RemainingUnitCount = remainingUnitCount;
        }


        public static int ResolveVisibleBoxCount(int purchasePackCount)
        {
            if (purchasePackCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(purchasePackCount),
                    purchasePackCount,
                    "A visible supplier load requires at least one case.");
            }

            if (purchasePackCount <= SmallLoadMaximumCaseCount)
            {
                return 1;
            }

            if (purchasePackCount <= MediumLoadMaximumCaseCount)
            {
                return 2;
            }

            return purchasePackCount <= LargeLoadMaximumCaseCount
                ? 3
                : 4;
        }
    }
}
