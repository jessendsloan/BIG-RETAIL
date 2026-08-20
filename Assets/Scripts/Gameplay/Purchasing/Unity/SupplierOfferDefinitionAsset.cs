using System;
using BigRetail.Merchandise.Domain;
using BigRetail.Merchandise.Unity;
using BigRetail.Purchasing.Domain;
using UnityEngine;

namespace BigRetail.Purchasing.Unity
{
    [CreateAssetMenu(
        fileName = "SupplierOfferDefinition",
        menuName = "Big Retail/Purchasing/Supplier Offer Definition")]
    public sealed class SupplierOfferDefinitionAsset : ScriptableObject
    {
        [SerializeField]
        private string offerId;

        [SerializeField]
        private SupplierDefinitionAsset supplier;

        [SerializeField]
        private ProductDefinitionAsset product;

        [Min(1)]
        [SerializeField]
        private int purchasePackQuantity = 1;

        [Min(1)]
        [SerializeField]
        private long packPriceCents = 1;

        [SerializeField]
        private bool isAvailable = true;


        public SupplierDefinitionAsset Supplier =>
            supplier;

        public ProductDefinitionAsset Product =>
            product;


        public bool TryCreateDefinition(
            out SupplierOfferDefinition definition,
            out string error)
        {
            if (supplier == null)
            {
                definition = null;
                error = $"{name}: Supplier reference is missing.";
                return false;
            }

            if (product == null)
            {
                definition = null;
                error = $"{name}: Product reference is missing.";
                return false;
            }

            if (!supplier.TryCreateDefinition(
                    out SupplierDefinition supplierDefinition,
                    out error))
            {
                definition = null;
                return false;
            }

            if (!product.TryCreateDefinition(
                    out ProductDefinition productDefinition,
                    out error))
            {
                definition = null;
                return false;
            }

            try
            {
                definition =
                    new SupplierOfferDefinition(
                        new SupplierOfferId(offerId),
                        supplierDefinition.Id,
                        productDefinition.Id,
                        purchasePackQuantity,
                        packPriceCents,
                        isAvailable);
                error = string.Empty;
                return true;
            }
            catch (ArgumentException exception)
            {
                definition = null;
                error = $"{name}: {exception.Message}";
                return false;
            }
        }


#if UNITY_EDITOR
        private void OnValidate()
        {
            offerId = string.IsNullOrWhiteSpace(offerId)
                ? string.Empty
                : offerId.Trim().ToUpperInvariant();
            purchasePackQuantity = Mathf.Max(1, purchasePackQuantity);
            packPriceCents = Math.Max(1, packPriceCents);
        }
#endif
    }
}
