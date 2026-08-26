using System.Collections.Generic;
using BigRetail.Map.Domain;
using NUnit.Framework;

namespace BigRetail.Map.Construction.Tests
{
    public sealed class LocationLandPolicyTests
    {
        [Test]
        public void FixedFootprint_GrantsEveryAuthoredConstructionCell()
        {
            GridMapDefinition map =
                new GridMapDefinition(
                    "fixed-test",
                    new[]
                    {
                        new GridPosition(2, 3),
                        new GridPosition(3, 3),
                        new GridPosition(8, 8)
                    });

            ConstructionAreaDefinition area =
                new ConstructionAreaDefinition(
                    map,
                    new[]
                    {
                        new GridPosition(2, 3),
                        new GridPosition(3, 3)
                    });

            ILocationLandPolicy policy =
                new FixedFootprintLandPolicy(area);

            Assert.That(
                policy.Kind,
                Is.EqualTo(LocationLandPolicyKind.FixedFootprint));
            Assert.That(policy.SupportsLandPurchases, Is.False);
            Assert.That(policy.LandRegions, Is.Null);
            Assert.That(policy.LandRegionOwnership, Is.Null);
            Assert.That(policy.LandRegionPurchases, Is.Null);
            Assert.That(
                policy.ConstructionEligibility.IsEligible(
                    new GridPosition(2, 3)),
                Is.True);
            Assert.That(
                policy.ConstructionEligibility.IsEligible(
                    new GridPosition(8, 8)),
                Is.False);
        }


        [Test]
        public void PurchasablePolicy_PreservesCampaignStartingLot()
        {
            ConstructionAreaDefinition area =
                CreateMainPropertyArea();

            ILocationLandPolicy policy =
                new PurchasableLandRegionPolicy(
                    area,
                    false,
                    new LandRegionPurchaseOption[0]);

            Assert.That(policy.SupportsLandPurchases, Is.True);
            Assert.That(
                policy.LandRegionOwnership.OwnedRegionCount,
                Is.EqualTo(1));
            Assert.That(
                policy.ConstructionEligibility.IsEligible(
                    new GridPosition(0, 0)),
                Is.True);
            Assert.That(
                policy.ConstructionEligibility.IsEligible(
                    new GridPosition(32, 0)),
                Is.False);

            CollectionAssert.Contains(
                new List<string>(
                    policy.EnumerateOwnedLandRegionIds()),
                LandRegionCatalog.FrontCornerRegionId.ToStableId());
        }


        [Test]
        public void GeometryFingerprint_IsOrderIndependentAndMaskSensitive()
        {
            GridPosition a = new GridPosition(-2, 7);
            GridPosition b = new GridPosition(-1, 7);
            GridPosition c = new GridPosition(-2, 8);

            GridMapDefinition firstMap =
                new GridMapDefinition(
                    "first",
                    new[] { a, b, c });
            GridMapDefinition reorderedMap =
                new GridMapDefinition(
                    "second",
                    new[] { c, a, b });

            string first =
                MapGeometryFingerprint.Compute(
                    firstMap,
                    new ConstructionAreaDefinition(
                        firstMap,
                        new[] { a, b }));

            string reordered =
                MapGeometryFingerprint.Compute(
                    reorderedMap,
                    new ConstructionAreaDefinition(
                        reorderedMap,
                        new[] { b, a }));

            string changedMask =
                MapGeometryFingerprint.Compute(
                    reorderedMap,
                    new ConstructionAreaDefinition(
                        reorderedMap,
                        new[] { a, c }));

            Assert.That(reordered, Is.EqualTo(first));
            Assert.That(changedMask, Is.Not.EqualTo(first));
            StringAssert.StartsWith("v1:", first);
        }


        private static ConstructionAreaDefinition
            CreateMainPropertyArea()
        {
            List<GridPosition> cells =
                new List<GridPosition>(
                    LandRegionCatalog.PropertyCellCount);

            for (int y = 0;
                 y < LandRegionCatalog.PropertySideLength;
                 y++)
            {
                for (int x = 0;
                     x < LandRegionCatalog.PropertySideLength;
                     x++)
                {
                    cells.Add(new GridPosition(x, y));
                }
            }

            GridMapDefinition map =
                new GridMapDefinition(
                    "main-property-test",
                    cells);

            return new ConstructionAreaDefinition(
                map,
                cells);
        }
    }
}
