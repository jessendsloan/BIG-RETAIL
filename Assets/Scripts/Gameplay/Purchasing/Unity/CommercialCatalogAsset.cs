using System;
using BigRetail.Merchandise.Domain;
using BigRetail.Merchandise.Unity;
using BigRetail.Purchasing.Domain;
using UnityEngine;

namespace BigRetail.Purchasing.Unity
{
    /// <summary>
    /// Single authored entry point for the opening commercial world.
    /// </summary>
    [CreateAssetMenu(
        fileName = "CommercialCatalog",
        menuName = "Big Retail/Purchasing/Commercial Catalog")]
    public sealed class CommercialCatalogAsset : ScriptableObject
    {
        [SerializeField]
        private BrandCatalogAsset brandCatalog;

        [SerializeField]
        private ProductCatalogAsset productCatalog;

        [SerializeField]
        private SupplierCatalogAsset supplierCatalog;

        [SerializeField]
        private SupplierOfferCatalogAsset supplierOfferCatalog;


        public BrandCatalogAsset BrandCatalog =>
            brandCatalog;

        public ProductCatalogAsset ProductCatalog =>
            productCatalog;

        public SupplierCatalogAsset SupplierCatalog =>
            supplierCatalog;

        public SupplierOfferCatalogAsset SupplierOfferCatalog =>
            supplierOfferCatalog;


        public bool TryCreateCatalog(
            out CommercialCatalog catalog,
            out string error)
        {
            if (brandCatalog == null
                || productCatalog == null
                || supplierCatalog == null
                || supplierOfferCatalog == null)
            {
                catalog = null;
                error =
                    $"{name}: Brand, Product, Supplier, and Offer catalogs "
                    + "must all be assigned.";
                return false;
            }

            if (!brandCatalog.TryCreateCatalog(
                    out BrandCatalog brands,
                    out error)
                || !productCatalog.TryCreateCatalog(
                    out ProductCatalog products,
                    out error)
                || !supplierCatalog.TryCreateCatalog(
                    out SupplierCatalog suppliers,
                    out error)
                || !supplierOfferCatalog.TryCreateCatalog(
                    out SupplierOfferCatalog offers,
                    out error))
            {
                catalog = null;
                return false;
            }

            try
            {
                catalog =
                    new CommercialCatalog(
                        brands,
                        products,
                        suppliers,
                        offers);
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
