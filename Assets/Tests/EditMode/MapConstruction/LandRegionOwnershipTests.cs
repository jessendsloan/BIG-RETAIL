using System.Collections.Generic;
using BigRetail.Map.Domain;
using NUnit.Framework;

namespace BigRetail.Map.Construction.Tests
{
    public sealed class LandRegionOwnershipTests
    {
        private const int PropertyMinimumX = -67;
        private const int PropertyMinimumY = 28;


        [Test]
        public void Catalog_DividesAuthoredPropertyIntoNineRegions()
        {
            TestContext context = CreateContext();

            int regionCount = 0;

            foreach (LandRegionDefinition region in
                     context.Catalog.EnumerateDefinitions())
            {
                int cellCount = 0;

                foreach (GridPosition _ in region.EnumerateCells())
                {
                    cellCount++;
                }

                Assert.That(
                    cellCount,
                    Is.EqualTo(LandRegionDefinition.CellCount));

                regionCount++;
            }

            Assert.That(regionCount, Is.EqualTo(9));
            Assert.That(
                context.Catalog.PropertyMinimumCell,
                Is.EqualTo(
                    new GridPosition(
                        PropertyMinimumX,
                        PropertyMinimumY)));
            Assert.That(
                context.Catalog
                    .GetDefinition(LandRegionCatalog.FrontCornerRegionId)
                    .MinimumCell,
                Is.EqualTo(
                    new GridPosition(
                        PropertyMinimumX,
                        PropertyMinimumY)));
        }


        [Test]
        public void CampaignOwnership_OnlyMakesStartingRegionBuildable()
        {
            TestContext context = CreateContext();
            LandRegionOwnershipState ownership =
                new LandRegionOwnershipState(context.Catalog);
            ownership.Own(LandRegionCatalog.FrontCornerRegionId);

            LandRegionConstructionEligibility eligibility =
                new LandRegionConstructionEligibility(
                    context.PhysicalArea,
                    context.Catalog,
                    ownership);

            Assert.That(
                eligibility.IsEligible(
                    new GridPosition(
                        PropertyMinimumX,
                        PropertyMinimumY)),
                Is.True);
            Assert.That(
                eligibility.IsEligible(
                    new GridPosition(
                        PropertyMinimumX + 31,
                        PropertyMinimumY + 31)),
                Is.True);
            Assert.That(
                eligibility.IsEligible(
                    new GridPosition(
                        PropertyMinimumX + 32,
                        PropertyMinimumY)),
                Is.False);
            Assert.That(
                eligibility.IsEligible(
                    new GridPosition(
                        PropertyMinimumX - 1,
                        PropertyMinimumY)),
                Is.False);
        }


        [Test]
        public void PurchasingAdjacentRegion_ImmediatelyUnlocksConstruction()
        {
            TestContext context = CreateContext();
            LandRegionOwnershipState ownership =
                new LandRegionOwnershipState(context.Catalog);
            ownership.Own(LandRegionCatalog.FrontCornerRegionId);

            LandRegionId firstExpansion = new LandRegionId(1, 0);
            LandRegionPurchaseService purchases =
                new LandRegionPurchaseService(
                    context.Catalog,
                    ownership,
                    new[]
                    {
                        new LandRegionPurchaseOption(
                            firstExpansion,
                            0,
                            "prototype.first_land_region")
                    });
            LandRegionConstructionEligibility eligibility =
                new LandRegionConstructionEligibility(
                    context.PhysicalArea,
                    context.Catalog,
                    ownership);
            GridPosition expansionCell =
                new GridPosition(
                    PropertyMinimumX + 32,
                    PropertyMinimumY);

            Assert.That(eligibility.IsEligible(expansionCell), Is.False);
            Assert.That(
                purchases.TryGetAvailableOption(firstExpansion, out _),
                Is.True);

            LandRegionPurchaseResult result =
                purchases.TryCompletePurchase(firstExpansion);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(ownership.OwnedRegionCount, Is.EqualTo(2));
            Assert.That(eligibility.IsEligible(expansionCell), Is.True);
        }


        [Test]
        public void Purchase_RejectsOfferedRegionThatIsNotAdjacent()
        {
            TestContext context = CreateContext();
            LandRegionOwnershipState ownership =
                new LandRegionOwnershipState(context.Catalog);
            ownership.Own(LandRegionCatalog.FrontCornerRegionId);

            LandRegionId distantRegion = new LandRegionId(2, 2);
            LandRegionPurchaseService purchases =
                new LandRegionPurchaseService(
                    context.Catalog,
                    ownership,
                    new[]
                    {
                        new LandRegionPurchaseOption(
                            distantRegion,
                            0,
                            string.Empty)
                    });

            LandRegionPurchaseResult result =
                purchases.TryCompletePurchase(distantRegion);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.Failure,
                Is.EqualTo(LandRegionPurchaseFailure.NotAdjacent));
            Assert.That(ownership.OwnedRegionCount, Is.EqualTo(1));
        }


        private static TestContext CreateContext()
        {
            List<GridPosition> cells =
                new List<GridPosition>(
                    LandRegionCatalog.PropertyCellCount);

            for (int y = 0; y < LandRegionCatalog.PropertySideLength; y++)
            {
                for (int x = 0; x < LandRegionCatalog.PropertySideLength; x++)
                {
                    cells.Add(
                        new GridPosition(
                            PropertyMinimumX + x,
                            PropertyMinimumY + y));
                }
            }

            GridMapDefinition map =
                new GridMapDefinition("land-region-test", cells);
            ConstructionAreaDefinition physicalArea =
                new ConstructionAreaDefinition(map, cells);

            return new TestContext(
                physicalArea,
                LandRegionCatalog.CreateFor(physicalArea));
        }


        private sealed class TestContext
        {
            public ConstructionAreaDefinition PhysicalArea { get; }

            public LandRegionCatalog Catalog { get; }

            public TestContext(
                ConstructionAreaDefinition physicalArea,
                LandRegionCatalog catalog)
            {
                PhysicalArea = physicalArea;
                Catalog = catalog;
            }
        }
    }
}
