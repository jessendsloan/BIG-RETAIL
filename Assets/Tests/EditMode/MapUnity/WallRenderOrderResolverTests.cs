using BigRetail.Map.Domain;
using BigRetail.Map.View;
using NUnit.Framework;

namespace BigRetail.Map.Unity.Walls.Tests
{
    public sealed class WallRenderOrderResolverTests
    {
        [Test]
        public void ResolveWallDepth_UsesWallBandAndDecreasingDepth()
        {
            int order =
                WallRenderOrderResolver.ResolveWallDepth(12);

            Assert.That(
                order,
                Is.EqualTo(
                    WallRenderOrderResolver.WallBaseOrder
                    - 25));
        }


        [Test]
        public void ResolveWall_SmallerNorthEastDepthRendersAfterLargerDepth()
        {
            CellEdge closer =
                new CellEdge(
                    new GridPosition(4, 7),
                    CellEdgeDirection.NorthEast);

            CellEdge farther =
                new CellEdge(
                    new GridPosition(4, 8),
                    CellEdgeDirection.NorthEast);

            Assert.That(
                WallRenderOrderResolver.ResolveWall(closer),
                Is.GreaterThan(
                    WallRenderOrderResolver.ResolveWall(farther)));
        }


        [Test]
        public void ResolveWall_SmallerNorthWestDepthRendersAfterLargerDepth()
        {
            CellEdge closer =
                new CellEdge(
                    new GridPosition(4, 7),
                    CellEdgeDirection.NorthWest);

            CellEdge farther =
                new CellEdge(
                    new GridPosition(5, 7),
                    CellEdgeDirection.NorthWest);

            Assert.That(
                WallRenderOrderResolver.ResolveWall(closer),
                Is.GreaterThan(
                    WallRenderOrderResolver.ResolveWall(farther)));
        }


        [Test]
        public void ResolveWallDepth_SupportsNegativeDisplayCoordinates()
        {
            int order =
                WallRenderOrderResolver.ResolveWallDepth(-9);

            Assert.That(
                order,
                Is.EqualTo(
                    WallRenderOrderResolver.WallBaseOrder
                    + 17));
        }


        [Test]
        public void ResolveCell_RendersBetweenItsFrontAndBackWalls()
        {
            GridPosition cell =
                new GridPosition(4, 7);

            CellEdge frontWall =
                new CellEdge(
                    new GridPosition(3, 7),
                    CellEdgeDirection.NorthEast);

            CellEdge backWall =
                new CellEdge(
                    cell,
                    CellEdgeDirection.NorthEast);

            int fixtureOrder =
                WallRenderOrderResolver.ResolveCell(cell);

            Assert.That(
                WallRenderOrderResolver.ResolveWall(frontWall),
                Is.GreaterThan(fixtureOrder));

            Assert.That(
                fixtureOrder,
                Is.GreaterThan(
                    WallRenderOrderResolver.ResolveWall(backWall)));
        }


        [Test]
        public void ResolveWall_LowCutawayRetainsStructuralDepth()
        {
            CellEdge wall =
                new CellEdge(
                    new GridPosition(3, 7),
                    CellEdgeDirection.NorthEast);

            int structuralOrder =
                WallRenderOrderResolver.ResolveWall(wall);

            int lowWallOrder =
                WallRenderOrderResolver.ResolveWall(
                    wall,
                    WallPresentationHeight.Low);

            Assert.That(
                lowWallOrder,
                Is.EqualTo(structuralOrder));
        }


        [Test]
        public void ResolveWall_FullHeightRetainsStructuralOcclusion()
        {
            CellEdge frontWall =
                new CellEdge(
                    new GridPosition(3, 7),
                    CellEdgeDirection.NorthEast);

            int structuralOrder =
                WallRenderOrderResolver.ResolveWall(frontWall);

            Assert.That(
                WallRenderOrderResolver.ResolveWall(
                    frontWall,
                    WallPresentationHeight.Full),
                Is.EqualTo(structuralOrder));
        }


        [Test]
        public void ResolveWallPriority_RisingLeftWinsEqualDepthSeam()
        {
            CellEdge risingLeft =
                new CellEdge(
                    new GridPosition(4, 7),
                    CellEdgeDirection.NorthEast);

            CellEdge risingRight =
                new CellEdge(
                    new GridPosition(4, 7),
                    CellEdgeDirection.NorthWest);

            Assert.That(
                WallRenderOrderResolver.ResolveWallPriority(risingLeft),
                Is.GreaterThan(
                    WallRenderOrderResolver.ResolveWallPriority(risingRight)));
        }


        [Test]
        public void ResolveAppearancePreviewPriority_RendersAfterMatchingWall()
        {
            CellEdge edge =
                new CellEdge(
                    new GridPosition(4, 7),
                    CellEdgeDirection.NorthEast);

            int wallPriority =
                WallRenderOrderResolver.ResolveWallPriority(edge);

            int previewPriority =
                WallRenderOrderResolver.ResolveAppearancePreviewPriority(edge);

            Assert.That(
                previewPriority - wallPriority,
                Is.EqualTo(
                    WallRenderOrderResolver.AppearancePreviewPriorityOffset));
        }


        [Test]
        public void ResolvePylon_UsesPylonBandAndRoundedDecreasingDepth()
        {
            int order =
                WallRenderOrderResolver.ResolvePylon(12.6f);

            Assert.That(
                order,
                Is.EqualTo(
                    WallRenderOrderResolver.PylonBaseOrder - 25));
        }


        [TestCase(17)]
        [TestCase(-375)]
        public void ResolvePylon_RemainsAboveWallAtSameDepth(
            int displayDepth)
        {
            int wallOrder =
                WallRenderOrderResolver.ResolveWallDepth(
                    displayDepth);

            int pylonOrder =
                WallRenderOrderResolver.ResolvePylon(
                    displayDepth);

            Assert.That(
                pylonOrder - wallOrder,
                Is.EqualTo(
                    WallRenderOrderResolver.PylonBaseOrder
                    - WallRenderOrderResolver.WallBaseOrder
                    + 1));
        }
    }
}
