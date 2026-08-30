using System.Linq;
using BigRetail.Map.Unity.Fixtures;
using BigRetail.Merchandise.Domain;
using BigRetail.Merchandise.Unity;
using BigRetail.Purchasing.Domain;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

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


        [Test]
        public void RidgewayChips_HasDirectionalShelfAndHandlingPresentation()
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
            Assert.That(product.OnShelfImageCount, Is.EqualTo(3));
            Assert.That(product.DisplayUnitsPerFrontageUnit, Is.EqualTo(3));

            Assert.That(
                product.GetOnShelfImage(
                    risingLeft: true,
                    fillRatio: 1f / 3f).name,
                Does.Contain("OnShelf_x1_RisingLeft"));
            Assert.That(
                product.GetOnShelfImage(
                    risingLeft: true,
                    fillRatio: 2f / 3f).name,
                Does.Contain("OnShelf_x2_RisingLeft"));
            Assert.That(
                product.GetOnShelfImage(
                    risingLeft: false,
                    fillRatio: 1f).name,
                Does.Contain("OnShelf_x3_RisingRight"));
            Assert.That(
                product.GetOnShelfImage(
                    risingLeft: false,
                    fillRatio: 0f),
                Is.Null);

            Assert.That(product.OffShelfRisingLeftImage, Is.Not.Null);
            Assert.That(product.OffShelfRisingRightImage, Is.Not.Null);
            Assert.That(
                product.OffShelfRisingLeftImage,
                Is.Not.SameAs(product.OffShelfRisingRightImage));
        }


        [Test]
        public void RidgewayChips_PlanogramGhostUsesArtUntilStockArrives()
        {
            CommercialCatalogAsset catalog =
                AssetDatabase.LoadAssetAtPath<CommercialCatalogAsset>(
                    CatalogPath);

            Assert.That(catalog, Is.Not.Null);
            Assert.That(
                catalog.ProductCatalog.TryGetAsset(
                    new ProductId(
                        "RIDGEWAY-ORIGINAL-CHIPS-SINGLE"),
                    out ProductDefinitionAsset product),
                Is.True);

            UnityEngine.Sprite fallback =
                product.GetOnShelfImage(
                    risingLeft: false,
                    fillRatio: 1f);

            UnityEngine.Sprite emptyGhost =
                FixtureMerchandisingOverlayViewSystem
                    .ResolvePlanogramMarkerSprite(
                        product,
                        canUseAuthoredProductArt: true,
                        risingLeft: true,
                        fillRatio: 0f,
                        isEmphasized: false,
                        fallbackSprite: fallback);

            Assert.That(emptyGhost, Is.Not.Null);
            Assert.That(
                emptyGhost.name,
                Does.Contain("OnShelf_x1_RisingLeft"));
            Assert.That(
                FixtureMerchandisingOverlayViewSystem
                    .ResolvePlanogramMarkerSprite(
                        product,
                        canUseAuthoredProductArt: true,
                        risingLeft: true,
                        fillRatio: 1f / 3f,
                        isEmphasized: false,
                        fallbackSprite: fallback),
                Is.Null);
            Assert.That(
                FixtureMerchandisingOverlayViewSystem
                    .ResolvePlanogramMarkerSprite(
                        product,
                        canUseAuthoredProductArt: true,
                        risingLeft: true,
                        fillRatio: 1f / 3f,
                        isEmphasized: true,
                        fallbackSprite: fallback),
                Is.Null);
            Assert.That(
                FixtureMerchandisingOverlayViewSystem
                    .ResolvePlanogramMarkerSprite(
                        product,
                        canUseAuthoredProductArt: false,
                        risingLeft: true,
                        fillRatio: 0f,
                        isEmphasized: false,
                        fallbackSprite: fallback),
                Is.SameAs(fallback));
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
