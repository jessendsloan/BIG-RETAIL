using System;
using System.Collections.Generic;
using BigRetail.Merchandise.Domain;
using NUnit.Framework;

namespace BigRetail.Purchasing.Domain.Tests
{
    public sealed class PurchasingServiceTests
    {
        [Test]
        public void SetPurchasePackCount_CreatesSupplierSpecificDrafts()
        {
            ProductDefinition cola = CommercialCatalogTests.CreateCola();
            SupplierDefinition big =
                CommercialCatalogTests.CreateSupplier("BIG", "BIG Wholesale");
            SupplierDefinition central =
                CommercialCatalogTests.CreateSupplier(
                    "CENTRAL",
                    "Central Grocery Supply",
                    10000);
            SupplierOfferDefinition bigOffer =
                CommercialCatalogTests.CreateOffer(
                    "BIG-COLA",
                    big.Id,
                    cola.Id,
                    12,
                    1200);
            SupplierOfferDefinition centralOffer =
                CommercialCatalogTests.CreateOffer(
                    "CENTRAL-COLA",
                    central.Id,
                    cola.Id,
                    24,
                    2100);

            PurchasingService purchasing =
                new PurchasingService(
                    CreateCatalog(
                        cola,
                        new[] { big, central },
                        new[] { bigOffer, centralOffer }));

            purchasing.SetPurchasePackCount(bigOffer.Id, 2);
            purchasing.SetPurchasePackCount(centralOffer.Id, 3);

            Assert.That(
                purchasing.TryGetDraft(big.Id, out DraftPurchaseOrder bigDraft),
                Is.True);
            Assert.That(bigDraft.TotalCents, Is.EqualTo(2400));

            Assert.That(
                purchasing.TryGetDraft(
                    central.Id,
                    out DraftPurchaseOrder centralDraft),
                Is.True);
            Assert.That(centralDraft.TotalCents, Is.EqualTo(6300));
            Assert.That(
                centralDraft.GetAmountRemainingForMinimum(central),
                Is.EqualTo(3700));
        }

        [Test]
        public void SetPurchasePackCount_ZeroRemovesEmptyDraft()
        {
            ProductDefinition cola = CommercialCatalogTests.CreateCola();
            SupplierDefinition big =
                CommercialCatalogTests.CreateSupplier("BIG", "BIG Wholesale");
            SupplierOfferDefinition offer =
                CommercialCatalogTests.CreateOffer(
                    "BIG-COLA",
                    big.Id,
                    cola.Id,
                    12,
                    1200);
            PurchasingService purchasing =
                new PurchasingService(
                    CreateCatalog(
                        cola,
                        new[] { big },
                        new[] { offer }));

            purchasing.SetPurchasePackCount(offer.Id, 1);
            purchasing.SetPurchasePackCount(offer.Id, 0);

            Assert.That(
                purchasing.TryGetDraft(big.Id, out DraftPurchaseOrder _),
                Is.False);
        }

        [Test]
        public void PlaceDrafts_CreatesScheduledSnapshotsAndClearsDrafts()
        {
            ProductDefinition cola = CommercialCatalogTests.CreateCola();
            SupplierDefinition big =
                CommercialCatalogTests.CreateSupplier("BIG", "BIG Wholesale");
            SupplierOfferDefinition offer =
                CommercialCatalogTests.CreateOffer(
                    "BIG-COLA",
                    big.Id,
                    cola.Id,
                    12,
                    1200);
            PurchasingService purchasing =
                new PurchasingService(
                    CreateCatalog(
                        cola,
                        new[] { big },
                        new[] { offer }));
            purchasing.SetPurchasePackCount(offer.Id, 2);

            IReadOnlyList<PlacedPurchaseOrder> placed =
                purchasing.PlaceDrafts(new CommercialTime(0, 9, 0));

            Assert.That(placed, Has.Count.EqualTo(1));
            Assert.That(placed[0].OrderNumber, Is.EqualTo(1));
            Assert.That(placed[0].SupplierId, Is.EqualTo(big.Id));
            Assert.That(placed[0].TotalCents, Is.EqualTo(2400));
            Assert.That(placed[0].Lines, Has.Count.EqualTo(1));
            Assert.That(
                placed[0].Lines[0].TotalUnits,
                Is.EqualTo(24));
            Assert.That(
                placed[0].DeliveryEstimate.EarliestArrival.DayIndex,
                Is.EqualTo(1));
            Assert.That(
                purchasing.TryGetDraft(
                    big.Id,
                    out DraftPurchaseOrder _),
                Is.False);
        }

        [Test]
        public void PlaceDrafts_RejectsMinimumAtomically()
        {
            ProductDefinition cola = CommercialCatalogTests.CreateCola();
            SupplierDefinition central =
                CommercialCatalogTests.CreateSupplier(
                    "CENTRAL",
                    "Central Grocery Supply",
                    10000);
            SupplierOfferDefinition offer =
                CommercialCatalogTests.CreateOffer(
                    "CENTRAL-COLA",
                    central.Id,
                    cola.Id,
                    24,
                    2100);
            PurchasingService purchasing =
                new PurchasingService(
                    CreateCatalog(
                        cola,
                        new[] { central },
                        new[] { offer }));
            purchasing.SetPurchasePackCount(offer.Id, 1);

            InvalidOperationException exception =
                Assert.Throws<InvalidOperationException>(
                    () => purchasing.PlaceDrafts(
                        new CommercialTime(0, 9, 0)));

            Assert.That(exception.Message, Does.Contain("7900"));
            Assert.That(
                purchasing.TryGetDraft(
                    central.Id,
                    out DraftPurchaseOrder draft),
                Is.True);
            Assert.That(draft.TotalCents, Is.EqualTo(2100));
            Assert.That(
                new List<PlacedPurchaseOrder>(purchasing.EnumeratePlacedOrders()),
                Is.Empty);
        }

        [Test]
        public void PlaceDrafts_RejectedPaymentLeavesEveryDraftUntouched()
        {
            ProductDefinition cola = CommercialCatalogTests.CreateCola();
            SupplierDefinition big =
                CommercialCatalogTests.CreateSupplier("BIG", "BIG Wholesale");
            SupplierOfferDefinition offer =
                CommercialCatalogTests.CreateOffer(
                    "BIG-COLA",
                    big.Id,
                    cola.Id,
                    12,
                    1200);
            PurchasingService purchasing =
                new PurchasingService(
                    CreateCatalog(
                        cola,
                        new[] { big },
                        new[] { offer }));
            purchasing.SetPurchasePackCount(offer.Id, 2);
            long requestedPaymentCents = 0;

            InvalidOperationException exception =
                Assert.Throws<InvalidOperationException>(
                    () => purchasing.PlaceDrafts(
                        new CommercialTime(0, 9, 0),
                        amountCents =>
                        {
                            requestedPaymentCents = amountCents;
                            return false;
                        }));

            Assert.That(exception.Message, Does.Contain("enough cash"));
            Assert.That(requestedPaymentCents, Is.EqualTo(2400));
            Assert.That(
                purchasing.TryGetDraft(
                    big.Id,
                    out DraftPurchaseOrder draft),
                Is.True);
            Assert.That(draft.TotalCents, Is.EqualTo(2400));
            Assert.That(
                new List<PlacedPurchaseOrder>(purchasing.EnumeratePlacedOrders()),
                Is.Empty);

            IReadOnlyList<PlacedPurchaseOrder> placed =
                purchasing.PlaceDrafts(
                    new CommercialTime(0, 9, 0),
                    _ => true);

            Assert.That(placed[0].OrderNumber, Is.EqualTo(1));
        }


        private static CommercialCatalog CreateCatalog(
            ProductDefinition product,
            SupplierDefinition[] suppliers,
            SupplierOfferDefinition[] offers)
        {
            return new CommercialCatalog(
                new BrandCatalog(
                    new[]
                    {
                        new BrandDefinition(
                            new BrandId("BRIGHT"),
                            "Bright Beverage Co.")
                    }),
                new ProductCatalog(new[] { product }),
                new SupplierCatalog(suppliers),
                new SupplierOfferCatalog(offers));
        }
    }
}
