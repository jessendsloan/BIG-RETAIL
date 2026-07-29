using BigRetail.Map.Domain;
using NUnit.Framework;

namespace BigRetail.Map.Unity.Walls.Tests
{
    public sealed class WallRenderOrderResolverTests
    {
        [Test]
        public void ResolveWallDepth_UsesWallBand()
        {
            int order =
                WallRenderOrderResolver.ResolveWallDepth(12);

            Assert.That(
                order,
                Is.EqualTo(
                    WallRenderOrderResolver.WallBaseOrder + 12));
        }


        [Test]
        public void ResolveWall_HigherNorthEastDepthRendersAfterLowerDepth()
        {
            CellEdge lowerDepth =
                new CellEdge(
                    new GridPosition(4, 7),
                    CellEdgeDirection.NorthEast);

            CellEdge higherDepth =
                new CellEdge(
                    new GridPosition(4, 8),
                    CellEdgeDirection.NorthEast);

            Assert.That(
                WallRenderOrderResolver.ResolveWall(higherDepth),
                Is.GreaterThan(
                    WallRenderOrderResolver.ResolveWall(lowerDepth)));
        }


        [Test]
        public void ResolveWall_HigherNorthWestDepthRendersAfterLowerDepth()
        {
            CellEdge lowerDepth =
                new CellEdge(
                    new GridPosition(4, 7),
                    CellEdgeDirection.NorthWest);

            CellEdge higherDepth =
                new CellEdge(
                    new GridPosition(5, 7),
                    CellEdgeDirection.NorthWest);

            Assert.That(
                WallRenderOrderResolver.ResolveWall(higherDepth),
                Is.GreaterThan(
                    WallRenderOrderResolver.ResolveWall(lowerDepth)));
        }


        [Test]
        public void ResolveWallDepth_SupportsNegativeDisplayCoordinates()
        {
            int order =
                WallRenderOrderResolver.ResolveWallDepth(-9);

            Assert.That(
                order,
                Is.EqualTo(
                    WallRenderOrderResolver.WallBaseOrder - 9));
        }


        [Test]
        public void ResolvePylon_UsesPylonBandAndRoundedDepth()
        {
            int order =
                WallRenderOrderResolver.ResolvePylon(12.6f);

            Assert.That(
                order,
                Is.EqualTo(
                    WallRenderOrderResolver.PylonBaseOrder + 13));
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
