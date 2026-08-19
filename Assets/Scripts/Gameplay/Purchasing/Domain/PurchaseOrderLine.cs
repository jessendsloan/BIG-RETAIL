using System;

namespace BigRetail.Purchasing.Domain
{
    /// <summary>
    /// Runtime draft quantity for one exact supplier offer.
    /// </summary>
    public sealed class PurchaseOrderLine
    {
        public SupplierOfferDefinition Offer { get; }

        public int PurchasePackCount { get; private set; }

        public long LineTotalCents =>
            checked(Offer.PackPriceCents * PurchasePackCount);


        public PurchaseOrderLine(
            SupplierOfferDefinition offer,
            int purchasePackCount)
        {
            Offer = offer ?? throw new ArgumentNullException(nameof(offer));
            SetPurchasePackCount(purchasePackCount);
        }


        public void SetPurchasePackCount(int purchasePackCount)
        {
            if (purchasePackCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(purchasePackCount),
                    purchasePackCount,
                    "An order line must contain at least one purchase pack.");
            }

            checked
            {
                _ = Offer.PackPriceCents * purchasePackCount;
            }

            PurchasePackCount = purchasePackCount;
        }
    }
}
