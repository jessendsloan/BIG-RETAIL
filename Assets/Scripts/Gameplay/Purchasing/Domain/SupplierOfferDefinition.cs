using System;
using BigRetail.Merchandise.Domain;

namespace BigRetail.Purchasing.Domain
{
    /// <summary>
    /// One supplier's commercial offer for one sellable SKU.
    /// </summary>
    public sealed class SupplierOfferDefinition
    {
        public SupplierOfferId Id { get; }

        public SupplierId SupplierId { get; }

        public ProductId ProductId { get; }

        public int PurchasePackQuantity { get; }

        public long PackPriceCents { get; }

        public bool IsAvailable { get; }

        public decimal UnitCostCents =>
            PackPriceCents / (decimal)PurchasePackQuantity;


        public SupplierOfferDefinition(
            SupplierOfferId id,
            SupplierId supplierId,
            ProductId productId,
            int purchasePackQuantity,
            long packPriceCents,
            bool isAvailable)
        {
            if (!id.IsValid)
            {
                throw new ArgumentException(
                    "A supplier offer requires a valid identifier.",
                    nameof(id));
            }

            if (!supplierId.IsValid)
            {
                throw new ArgumentException(
                    "A supplier offer requires a valid supplier.",
                    nameof(supplierId));
            }

            if (!productId.IsValid)
            {
                throw new ArgumentException(
                    "A supplier offer requires a valid product.",
                    nameof(productId));
            }

            if (purchasePackQuantity <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(purchasePackQuantity),
                    purchasePackQuantity,
                    "A purchase pack must contain at least one sellable unit.");
            }

            if (packPriceCents <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(packPriceCents),
                    packPriceCents,
                    "A purchase pack price must be greater than zero.");
            }

            Id = id;
            SupplierId = supplierId;
            ProductId = productId;
            PurchasePackQuantity = purchasePackQuantity;
            PackPriceCents = packPriceCents;
            IsAvailable = isAvailable;
        }
    }
}
