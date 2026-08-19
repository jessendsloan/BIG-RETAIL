using System;
using BigRetail.Merchandise.Domain;

namespace BigRetail.Purchasing.Domain
{
    /// <summary>
    /// Validates the permanent Brand/Product/Supplier/Offer seam and exposes
    /// its four immutable catalogs as one purchasing input.
    /// </summary>
    public sealed class CommercialCatalog
    {
        public BrandCatalog Brands { get; }

        public ProductCatalog Products { get; }

        public SupplierCatalog Suppliers { get; }

        public SupplierOfferCatalog Offers { get; }


        public CommercialCatalog(
            BrandCatalog brands,
            ProductCatalog products,
            SupplierCatalog suppliers,
            SupplierOfferCatalog offers)
        {
            Brands = brands ?? throw new ArgumentNullException(nameof(brands));
            Products = products ?? throw new ArgumentNullException(nameof(products));
            Suppliers = suppliers ?? throw new ArgumentNullException(nameof(suppliers));
            Offers = offers ?? throw new ArgumentNullException(nameof(offers));

            ValidateProductBrands();
            ValidateOffers();
        }


        private void ValidateProductBrands()
        {
            foreach (ProductDefinition product in Products.EnumerateDefinitions())
            {
                if (product.BrandId != BrandId.Unbranded
                    && !Brands.Contains(product.BrandId))
                {
                    throw new ArgumentException(
                        $"Product '{product.Id}' references unknown brand "
                        + $"'{product.BrandId}'.",
                        nameof(Products));
                }
            }
        }

        private void ValidateOffers()
        {
            foreach (
                SupplierOfferDefinition offer
                in Offers.EnumerateDefinitions())
            {
                if (!Products.Contains(offer.ProductId))
                {
                    throw new ArgumentException(
                        $"Offer '{offer.Id}' references unknown product "
                        + $"'{offer.ProductId}'.",
                        nameof(Offers));
                }

                if (!Suppliers.Contains(offer.SupplierId))
                {
                    throw new ArgumentException(
                        $"Offer '{offer.Id}' references unknown supplier "
                        + $"'{offer.SupplierId}'.",
                        nameof(Offers));
                }
            }
        }
    }
}
