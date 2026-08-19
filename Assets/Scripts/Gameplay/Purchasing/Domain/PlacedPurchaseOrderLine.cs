using System;
using BigRetail.Merchandise.Domain;

namespace BigRetail.Purchasing.Domain
{
    /// <summary>
    /// Immutable commercial snapshot of one line at placement time.
    /// </summary>
    public sealed class PlacedPurchaseOrderLine
    {
        public SupplierOfferId OfferId { get; }

        public ProductId ProductId { get; }

        public int UnitsPerPurchasePack { get; }

        public long PackPriceCents { get; }

        public int PurchasePackCount { get; }

        public int TotalUnits =>
            checked(UnitsPerPurchasePack * PurchasePackCount);

        public long LineTotalCents =>
            checked(PackPriceCents * PurchasePackCount);


        internal PlacedPurchaseOrderLine(PurchaseOrderLine line)
        {
            if (line == null)
            {
                throw new ArgumentNullException(nameof(line));
            }

            OfferId = line.Offer.Id;
            ProductId = line.Offer.ProductId;
            UnitsPerPurchasePack = line.Offer.PurchasePackQuantity;
            PackPriceCents = line.Offer.PackPriceCents;
            PurchasePackCount = line.PurchasePackCount;
        }
    }
}
