using System.Linq;
using BigRetail.Merchandise.Domain;
using BigRetail.Purchasing.Domain;
using NUnit.Framework;
using UnityEditor;

namespace BigRetail.Purchasing.Unity.Tests
{
    public sealed class OpeningCommercialCatalogAssetTests
    {
        private const string CatalogPath =
            "Assets/Design/Purchasing/Catalogs/OpeningCommercialCatalog.asset";


        [Test]
        public void OpeningCatalog_ContainsAcceptedCommercialWorld()
        {
            CommercialCatalogAsset asset =
                AssetDatabase.LoadAssetAtPath<CommercialCatalogAsset>(CatalogPath);

            Assert.That(asset, Is.Not.Null);
            Assert.That(
                asset.TryCreateCatalog(
                    out CommercialCatalog catalog,
                    out string error),
                Is.True,
                error);
            Assert.That(catalog.Brands.Count, Is.EqualTo(10));
            Assert.That(catalog.Products.Count, Is.EqualTo(12));
            Assert.That(catalog.Suppliers.Count, Is.EqualTo(3));
            Assert.That(catalog.Offers.Count, Is.EqualTo(24));
        }

        [Test]
        public void OpeningCatalog_UsesAcceptedSupplierAssortmentShape()
        {
            CommercialCatalog catalog = LoadCatalog();

            Assert.That(
                catalog.Offers.EnumerateForSupplier(new SupplierId("BIG")).Count(),
                Is.EqualTo(12));
            Assert.That(
                catalog.Offers.EnumerateForSupplier(
                    new SupplierId("CENTRAL")).Count(),
                Is.EqualTo(10));
            Assert.That(
                catalog.Offers.EnumerateForSupplier(
                    new SupplierId("BEACON")).Count(),
                Is.EqualTo(2));
        }

        [Test]
        public void BrightCola_ExposesThreeDistinctOpeningOffers()
        {
            CommercialCatalog catalog = LoadCatalog();
            SupplierOfferDefinition[] offers =
                catalog.Offers
                    .EnumerateForProduct(new ProductId("BRIGHT-COLA-20OZ"))
                    .ToArray();

            Assert.That(offers, Has.Length.EqualTo(3));
            Assert.That(
                offers.Select(offer => offer.SupplierId.Value),
                Is.EqualTo(new[] { "BIG", "CENTRAL", "BEACON" }));
            Assert.That(
                offers.Select(offer => offer.PurchasePackQuantity),
                Is.EqualTo(new[] { 12, 24, 24 }));
            Assert.That(
                offers.Select(offer => offer.PackPriceCents),
                Is.EqualTo(new long[] { 1200, 2100, 1920 }));
        }


        private static CommercialCatalog LoadCatalog()
        {
            CommercialCatalogAsset asset =
                AssetDatabase.LoadAssetAtPath<CommercialCatalogAsset>(CatalogPath);

            Assert.That(asset, Is.Not.Null);
            Assert.That(
                asset.TryCreateCatalog(
                    out CommercialCatalog catalog,
                    out string error),
                Is.True,
                error);
            return catalog;
        }
    }
}
