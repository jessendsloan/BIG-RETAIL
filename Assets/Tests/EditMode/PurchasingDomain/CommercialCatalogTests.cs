using System;
using System.Linq;
using NUnit.Framework;
using BigRetail.Merchandise.Domain;

namespace BigRetail.Purchasing.Domain.Tests
{
    public sealed class CommercialCatalogTests
    {
        [Test]
        public void Constructor_AcceptsOverlappingOffersForOneProduct()
        {
            ProductDefinition cola = CreateCola();
            SupplierDefinition big = CreateSupplier("BIG", "BIG Wholesale");
            SupplierDefinition central =
                CreateSupplier("CENTRAL", "Central Grocery Supply");

            CommercialCatalog catalog =
                new CommercialCatalog(
                    new BrandCatalog(
                        new[]
                        {
                            new BrandDefinition(
                                new BrandId("BRIGHT"),
                                "Bright Beverage Co.")
                        }),
                    new ProductCatalog(new[] { cola }),
                    new SupplierCatalog(new[] { big, central }),
                    new SupplierOfferCatalog(
                        new[]
                        {
                            CreateOffer("BIG-COLA", big.Id, cola.Id, 12, 1200),
                            CreateOffer("CENTRAL-COLA", central.Id, cola.Id, 24, 2100)
                        }));

            Assert.That(
                catalog.Offers.EnumerateForProduct(cola.Id).Count(),
                Is.EqualTo(2));
        }

        [Test]
        public void Constructor_RejectsOfferForUnknownProduct()
        {
            SupplierDefinition big = CreateSupplier("BIG", "BIG Wholesale");

            Assert.Throws<ArgumentException>(
                () => new CommercialCatalog(
                    new BrandCatalog(Array.Empty<BrandDefinition>()),
                    new ProductCatalog(Array.Empty<ProductDefinition>()),
                    new SupplierCatalog(new[] { big }),
                    new SupplierOfferCatalog(
                        new[]
                        {
                            CreateOffer(
                                "BIG-MISSING",
                                big.Id,
                                new ProductId("MISSING"),
                                12,
                                1200)
                        })));
        }


        internal static ProductDefinition CreateCola()
        {
            return new ProductDefinition(
                new ProductId("BRIGHT-COLA-20OZ"),
                "Bright Cola",
                new BrandId("BRIGHT"),
                "Cola",
                new ProductCategoryId("BEVERAGES"),
                MarketPosition.Standard,
                "20 oz Bottle",
                StockUnit.Each);
        }

        internal static SupplierDefinition CreateSupplier(
            string id,
            string displayName,
            long minimumOrderCents = 0)
        {
            return new SupplierDefinition(
                new SupplierId(id),
                displayName,
                "Test supplier",
                minimumOrderCents,
                SupplierDeliveryRule.NextDay());
        }

        internal static SupplierOfferDefinition CreateOffer(
            string id,
            SupplierId supplierId,
            ProductId productId,
            int packQuantity,
            long packPriceCents)
        {
            return new SupplierOfferDefinition(
                new SupplierOfferId(id),
                supplierId,
                productId,
                packQuantity,
                packPriceCents,
                true);
        }
    }
}
