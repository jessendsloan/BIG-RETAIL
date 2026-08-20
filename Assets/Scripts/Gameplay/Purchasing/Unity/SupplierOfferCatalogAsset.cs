using System;
using System.Collections.Generic;
using BigRetail.Purchasing.Domain;
using UnityEngine;

namespace BigRetail.Purchasing.Unity
{
    [CreateAssetMenu(
        fileName = "SupplierOfferCatalog",
        menuName = "Big Retail/Purchasing/Supplier Offer Catalog")]
    public sealed class SupplierOfferCatalogAsset : ScriptableObject
    {
        [SerializeField]
        private SupplierOfferDefinitionAsset[] offers =
            Array.Empty<SupplierOfferDefinitionAsset>();


        public IReadOnlyList<SupplierOfferDefinitionAsset> Offers =>
            offers;


        public bool TryCreateCatalog(
            out SupplierOfferCatalog catalog,
            out string error)
        {
            if (offers == null)
            {
                catalog = null;
                error = $"{name}: Supplier offer collection is missing.";
                return false;
            }

            List<SupplierOfferDefinition> definitions =
                new List<SupplierOfferDefinition>(offers.Length);

            for (int index = 0; index < offers.Length; index++)
            {
                SupplierOfferDefinitionAsset offer = offers[index];

                if (offer == null)
                {
                    catalog = null;
                    error = $"{name}: Supplier offer entry {index} is missing.";
                    return false;
                }

                if (!offer.TryCreateDefinition(
                        out SupplierOfferDefinition definition,
                        out error))
                {
                    catalog = null;
                    return false;
                }

                definitions.Add(definition);
            }

            try
            {
                catalog = new SupplierOfferCatalog(definitions);
                error = string.Empty;
                return true;
            }
            catch (ArgumentException exception)
            {
                catalog = null;
                error = $"{name}: {exception.Message}";
                return false;
            }
        }
    }
}
