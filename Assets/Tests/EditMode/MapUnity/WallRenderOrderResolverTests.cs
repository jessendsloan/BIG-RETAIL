using BigRetail.Map.Domain;
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
                    WallRenderOrderResolver.WallBaseOrder - 12));
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
                    WallRenderOrderResolver.WallBaseOrder + 9));
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
                    WallRenderOrderResolver.PylonBaseOrder - 13));
        }


        [Test]
        public void ResolvePylon_RemainsAboveWallAtSameDepth()
        {
            const int displayDepth = 17;

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
                    - WallRenderOrderResolver.WallBaseOrder));
        }
    }
}
