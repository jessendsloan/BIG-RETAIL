using System;
using System.Collections.Generic;

namespace BigRetail.Purchasing.Domain
{
    /// <summary>
    /// Owns supplier-specific draft purchase orders for the product-first
    /// Purchasing workspace and commits valid drafts into scheduled commercial
    /// records. Spending, delivery, receiving, and inventory remain downstream.
    /// </summary>
    public sealed class PurchasingService
    {
        private readonly Dictionary<SupplierId, DraftPurchaseOrder> drafts =
            new Dictionary<SupplierId, DraftPurchaseOrder>();
        private readonly List<PlacedPurchaseOrder> placedOrders =
            new List<PlacedPurchaseOrder>();

        private long nextOrderNumber = 1;


        public CommercialCatalog Catalog { get; }


        public PurchasingService(CommercialCatalog catalog)
        {
            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }


        public event Action DraftsChanged;


        public int GetPurchasePackCount(SupplierOfferId offerId)
        {
            SupplierOfferDefinition offer = Catalog.Offers.GetRequired(offerId);

            return drafts.TryGetValue(
                    offer.SupplierId,
                    out DraftPurchaseOrder draft)
                ? draft.GetPurchasePackCount(offerId)
                : 0;
        }

        public void SetPurchasePackCount(
            SupplierOfferId offerId,
            int purchasePackCount)
        {
            SupplierOfferDefinition offer = Catalog.Offers.GetRequired(offerId);

            if (!offer.IsAvailable)
            {
                throw new InvalidOperationException(
                    $"Supplier offer '{offer.Id}' is not currently available.");
            }

            if (purchasePackCount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(purchasePackCount),
                    purchasePackCount,
                    "A draft quantity cannot be negative.");
            }

            if (!drafts.TryGetValue(
                    offer.SupplierId,
                    out DraftPurchaseOrder draft))
            {
                if (purchasePackCount == 0)
                {
                    return;
                }

                draft = new DraftPurchaseOrder(offer.SupplierId);
                drafts.Add(offer.SupplierId, draft);
            }

            draft.SetPurchasePackCount(offer, purchasePackCount);

            if (draft.IsEmpty)
            {
                drafts.Remove(offer.SupplierId);
            }

            DraftsChanged?.Invoke();
        }

        public bool TryGetDraft(
            SupplierId supplierId,
            out DraftPurchaseOrder draft)
        {
            return drafts.TryGetValue(supplierId, out draft);
        }

        public IEnumerable<DraftPurchaseOrder> EnumerateDrafts()
        {
            foreach (
                SupplierDefinition supplier
                in Catalog.Suppliers.EnumerateDefinitions())
            {
                if (drafts.TryGetValue(
                        supplier.Id,
                        out DraftPurchaseOrder draft))
                {
                    yield return draft;
                }
            }
        }

        public IReadOnlyList<PlacedPurchaseOrder> PlaceDrafts(
            CommercialTime placedAt)
        {
            if (drafts.Count == 0)
            {
                throw new InvalidOperationException(
                    "At least one draft purchase order is required.");
            }

            foreach (DraftPurchaseOrder draft in EnumerateDrafts())
            {
                SupplierDefinition supplier =
                    Catalog.Suppliers.GetRequired(draft.SupplierId);
                long remaining =
                    draft.GetAmountRemainingForMinimum(supplier);

                if (remaining > 0)
                {
                    throw new InvalidOperationException(
                        $"'{supplier.DisplayName}' requires {remaining} more cents "
                        + "before its order can be placed.");
                }
            }

            List<PlacedPurchaseOrder> placedBatch =
                new List<PlacedPurchaseOrder>(drafts.Count);

            foreach (DraftPurchaseOrder draft in EnumerateDrafts())
            {
                SupplierDefinition supplier =
                    Catalog.Suppliers.GetRequired(draft.SupplierId);
                PlacedPurchaseOrder order =
                    new PlacedPurchaseOrder(
                        nextOrderNumber,
                        draft,
                        placedAt,
                        supplier.DeliveryRule.EstimateDelivery(placedAt));
                nextOrderNumber = checked(nextOrderNumber + 1);
                placedBatch.Add(order);
                placedOrders.Add(order);
            }

            drafts.Clear();
            DraftsChanged?.Invoke();
            return placedBatch.AsReadOnly();
        }

        public IEnumerable<PlacedPurchaseOrder> EnumeratePlacedOrders()
        {
            for (int index = 0; index < placedOrders.Count; index++)
            {
                yield return placedOrders[index];
            }
        }
    }
}
