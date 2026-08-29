using System.Linq;
using BigRetail.Merchandise.Domain;
using BigRetail.Merchandise.Unity;
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

        [Test]
        public void OpeningSuppliers_HaveFourDeliveryLoadSprites()
        {
            string[] supplierPaths =
            {
                "Assets/Design/Purchasing/Suppliers/BIGWholesale.asset",
                "Assets/Design/Purchasing/Suppliers/CentralGrocery.asset",
                "Assets/Design/Purchasing/Suppliers/BeaconBeverage.asset"
            };

            for (int supplierIndex = 0;
                 supplierIndex < supplierPaths.Length;
                 supplierIndex++)
            {
                SupplierDefinitionAsset supplier =
                    AssetDatabase.LoadAssetAtPath<SupplierDefinitionAsset>(
                        supplierPaths[supplierIndex]);

                Assert.That(supplier, Is.Not.Null, supplierPaths[supplierIndex]);

                for (int loadTier = 1; loadTier <= 4; loadTier++)
                {
                    Assert.That(
                        supplier.GetDeliveryLoadSprite(loadTier),
                        Is.Not.Null,
                        $"{supplier.name} load tier {loadTier}");
                }
            }
        }

        [Test]
        public void RidgewayChips_HasDirectionalBackstockCasePresentation()
        {
            CommercialCatalogAsset catalog =
                AssetDatabase.LoadAssetAtPath<CommercialCatalogAsset>(
                    CatalogPath);

            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.ProductCatalog, Is.Not.Null);
            Assert.That(
                catalog.ProductCatalog.TryGetAsset(
                    new ProductId(
                        "RIDGEWAY-ORIGINAL-CHIPS-SINGLE"),
                    out ProductDefinitionAsset product),
                Is.True);
            Assert.That(product.CaseRisingLeftImage, Is.Not.Null);
            Assert.That(product.CaseRisingRightImage, Is.Not.Null);
            Assert.That(
                product.CaseRisingLeftImage,
                Is.Not.SameAs(product.CaseRisingRightImage));
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
