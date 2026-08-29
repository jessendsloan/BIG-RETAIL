using BigRetail.Map.Unity.Fixtures;
using NUnit.Framework;
using UnityEngine;

namespace BigRetail.Map.Unity.Tests
{
    public sealed class BackstockCaseVisualTests
    {
        [Test]
        public void PhysicalCaseCountControlsRackMarkers()
        {
            Assert.That(
                FixtureViewSystem.ResolveBackstockCaseMarkerCount(
                    physicalCaseCount: 2,
                    storedUnitCount: 24),
                Is.EqualTo(2));

            Assert.That(
                FixtureViewSystem.ResolveBackstockCaseMarkerCount(
                    physicalCaseCount: 0,
                    storedUnitCount: 24),
                Is.EqualTo(1));

            Assert.That(
                FixtureViewSystem.ResolveBackstockCaseMarkerCount(
                    physicalCaseCount: 12,
                    storedUnitCount: 144,
                    maximumMarkerCount: 12),
                Is.EqualTo(12));
        }

        [Test]
        public void AuthoredShelfCaseMovesTowardTheViewer()
        {
            Vector3 shelfCenter = new Vector3(0.25f, 0.5f, 0f);

            Vector3 casePosition =
                FixtureViewSystem.ResolveBackstockCaseShelfPosition(
                    shelfCenter,
                    shelfDepth: 0.5f);

            Assert.That(casePosition.x, Is.EqualTo(shelfCenter.x));
            Assert.That(casePosition.y, Is.EqualTo(0.35f).Within(0.0001f));
            Assert.That(casePosition.z, Is.EqualTo(shelfCenter.z));
        }

        [Test]
        public void AuthoredRackFrontEdgeOffsetMatchesPhysicalLayout()
        {
            Vector3 shelfCenter = new Vector3(0.25f, 0.5f, 0f);

            Vector3 casePosition =
                FixtureViewSystem.ResolveBackstockCaseShelfPosition(
                    shelfCenter,
                    shelfDepth: 0.5f,
                    forwardOffsetShare: 0.30f);

            Assert.That(casePosition.x, Is.EqualTo(shelfCenter.x));
            Assert.That(casePosition.y, Is.EqualTo(0.35f).Within(0.0001f));
            Assert.That(casePosition.z, Is.EqualTo(shelfCenter.z));
        }

        [Test]
        public void NearerCaseDrawsOverFartherCaseOnEitherRackSlope()
        {
            Assert.That(
                FixtureViewSystem.ResolveBackstockCaseDepthOrder(
                    column: 0,
                    casesPerShelf: 4,
                    shelfSlopeDegrees: 26f),
                Is.GreaterThan(
                    FixtureViewSystem.ResolveBackstockCaseDepthOrder(
                        column: 1,
                        casesPerShelf: 4,
                        shelfSlopeDegrees: 26f)));

            Assert.That(
                FixtureViewSystem.ResolveBackstockCaseDepthOrder(
                    column: 3,
                    casesPerShelf: 4,
                    shelfSlopeDegrees: -26f),
                Is.GreaterThan(
                    FixtureViewSystem.ResolveBackstockCaseDepthOrder(
                        column: 2,
                        casesPerShelf: 4,
                        shelfSlopeDegrees: -26f)));
        }


        [TestCase(26.565052f, 0.5f)]
        [TestCase(-26.565052f, -0.5f)]
        public void CaseCentersFollowTheAuthoredIsometricShelfRail(
            float shelfSlopeDegrees,
            float expectedRisePerUnit)
        {
            Vector2 shelfCenter = new Vector2(2f, 3f);
            Vector2 driftedLeftCenter = new Vector2(0.5f, 2.31f);
            Vector2 driftedRightCenter = new Vector2(3.5f, 3.69f);

            Vector2 alignedLeftCenter =
                FixtureViewSystem.ResolveBackstockCaseRailAlignedCenter(
                    shelfCenter,
                    driftedLeftCenter,
                    shelfSlopeDegrees);
            Vector2 alignedRightCenter =
                FixtureViewSystem.ResolveBackstockCaseRailAlignedCenter(
                    shelfCenter,
                    driftedRightCenter,
                    shelfSlopeDegrees);

            Assert.That(
                alignedLeftCenter.x,
                Is.EqualTo(driftedLeftCenter.x));
            Assert.That(
                alignedRightCenter.x,
                Is.EqualTo(driftedRightCenter.x));
            Assert.That(
                (alignedRightCenter.y - alignedLeftCenter.y)
                    / (alignedRightCenter.x - alignedLeftCenter.x),
                Is.EqualTo(expectedRisePerUnit).Within(0.0001f));
        }

        [Test]
        public void FourCaseDepthOrdersFitBetweenRackPresentationLayers()
        {
            const int casesPerShelf = 4;
            int stride =
                FixtureViewSystem
                    .ResolveBackstockPresentationLayerSortingStride(
                        casesPerShelf);
            int nearestCaseOffset =
                2
                + FixtureViewSystem.ResolveBackstockCaseDepthOrder(
                    column: 0,
                    casesPerShelf: casesPerShelf,
                    shelfSlopeDegrees: 26f);

            Assert.That(stride, Is.EqualTo(7));
            Assert.That(nearestCaseOffset, Is.LessThan(stride));
        }

        [Test]
        public void PackedCaseCenterClosesGapWithoutChangingCaseSize()
        {
            Vector2 shelfCenter = new Vector2(0.5f, 0.25f);
            Vector2 slotCenter = new Vector2(1.5f, 0.75f);

            Vector2 packedCenter =
                FixtureViewSystem.ResolveBackstockCasePackedCenter(
                    shelfCenter,
                    slotCenter,
                    spacingShare: 0.90f);

            Assert.That(
                Vector2.Distance(shelfCenter, packedCenter),
                Is.EqualTo(
                    Vector2.Distance(shelfCenter, slotCenter) * 0.90f)
                    .Within(0.0001f));
        }

        [Test]
        public void CaseRowOffsetMovesAlongTheShelfFrontage()
        {
            Vector2 caseCenter = new Vector2(0.5f, 0.25f);
            Vector2 frontageStep = new Vector2(0.4f, 0.2f);

            Vector2 shiftedCenter =
                FixtureViewSystem.ResolveBackstockCaseRowCenter(
                    caseCenter,
                    frontageStep,
                    rowOffsetShare: 0.10f);

            Assert.That(
                shiftedCenter.x,
                Is.EqualTo(0.54f).Within(0.0001f));
            Assert.That(
                shiftedCenter.y,
                Is.EqualTo(0.27f).Within(0.0001f));
        }
    }
}
