using BigRetail.Map.Domain;
using BigRetail.Map.Walls;
using NUnit.Framework;

namespace BigRetail.Map.Walls.Tests
{
    public sealed class StraightWallRunPlannerTests
    {
        [Test]
        public void Plan_SameEdge_ReturnsOneSegment()
        {
            CellEdge edge =
                CreateEdge(
                    4,
                    7,
                    CellEdgeDirection.NorthEast);

            WallRunPlanResult result =
                StraightWallRunPlanner.Plan(
                    edge,
                    edge);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.SegmentCount, Is.EqualTo(1));
            Assert.That(result.Edges[0], Is.EqualTo(edge));
        }


        [Test]
        public void Plan_NorthEastIncreasingY_ReturnsOrderedRun()
        {
            CellEdge start =
                CreateEdge(
                    4,
                    7,
                    CellEdgeDirection.NorthEast);

            CellEdge end =
                CreateEdge(
                    4,
                    10,
                    CellEdgeDirection.NorthEast);

            WallRunPlanResult result =
                StraightWallRunPlanner.Plan(
                    start,
                    end);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.SegmentCount, Is.EqualTo(4));

            Assert.That(
                result.Edges[0].AnchorCell.Y,
                Is.EqualTo(7));

            Assert.That(
                result.Edges[1].AnchorCell.Y,
                Is.EqualTo(8));

            Assert.That(
                result.Edges[2].AnchorCell.Y,
                Is.EqualTo(9));

            Assert.That(
                result.Edges[3].AnchorCell.Y,
                Is.EqualTo(10));
        }


        [Test]
        public void Plan_NorthEastDecreasingY_ReturnsStartToEndOrder()
        {
            CellEdge start =
                CreateEdge(
                    4,
                    10,
                    CellEdgeDirection.NorthEast);

            CellEdge end =
                CreateEdge(
                    4,
                    7,
                    CellEdgeDirection.NorthEast);

            WallRunPlanResult result =
                StraightWallRunPlanner.Plan(
                    start,
                    end);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.SegmentCount, Is.EqualTo(4));

            Assert.That(
                result.Edges[0].AnchorCell.Y,
                Is.EqualTo(10));

            Assert.That(
                result.Edges[3].AnchorCell.Y,
                Is.EqualTo(7));
        }


        [Test]
        public void Plan_NorthWestIncreasingX_ReturnsOrderedRun()
        {
            CellEdge start =
                CreateEdge(
                    3,
                    8,
                    CellEdgeDirection.NorthWest);

            CellEdge end =
                CreateEdge(
                    6,
                    8,
                    CellEdgeDirection.NorthWest);

            WallRunPlanResult result =
                StraightWallRunPlanner.Plan(
                    start,
                    end);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.SegmentCount, Is.EqualTo(4));

            Assert.That(
                result.Edges[0].AnchorCell.X,
                Is.EqualTo(3));

            Assert.That(
                result.Edges[3].AnchorCell.X,
                Is.EqualTo(6));
        }


        [Test]
        public void Plan_NorthWestDecreasingX_ReturnsStartToEndOrder()
        {
            CellEdge start =
                CreateEdge(
                    6,
                    8,
                    CellEdgeDirection.NorthWest);

            CellEdge end =
                CreateEdge(
                    3,
                    8,
                    CellEdgeDirection.NorthWest);

            WallRunPlanResult result =
                StraightWallRunPlanner.Plan(
                    start,
                    end);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.SegmentCount, Is.EqualTo(4));

            Assert.That(
                result.Edges[0].AnchorCell.X,
                Is.EqualTo(6));

            Assert.That(
                result.Edges[3].AnchorCell.X,
                Is.EqualTo(3));
        }


        [Test]
        public void Plan_DifferentCanonicalDirections_IsRejected()
        {
            CellEdge start =
                CreateEdge(
                    3,
                    8,
                    CellEdgeDirection.NorthEast);

            CellEdge end =
                CreateEdge(
                    3,
                    8,
                    CellEdgeDirection.NorthWest);

            WallRunPlanResult result =
                StraightWallRunPlanner.Plan(
                    start,
                    end);

            Assert.That(result.Succeeded, Is.False);

            Assert.That(
                result.Failure,
                Is.EqualTo(
                    WallRunPlanFailure.DifferentDirection));
        }


        [Test]
        public void Plan_NorthEastEdgesWithDifferentX_AreRejected()
        {
            CellEdge start =
                CreateEdge(
                    3,
                    8,
                    CellEdgeDirection.NorthEast);

            CellEdge end =
                CreateEdge(
                    4,
                    10,
                    CellEdgeDirection.NorthEast);

            WallRunPlanResult result =
                StraightWallRunPlanner.Plan(
                    start,
                    end);

            Assert.That(result.Succeeded, Is.False);

            Assert.That(
                result.Failure,
                Is.EqualTo(
                    WallRunPlanFailure.NotCollinear));
        }


        [Test]
        public void Plan_NorthWestEdgesWithDifferentY_AreRejected()
        {
            CellEdge start =
                CreateEdge(
                    3,
                    8,
                    CellEdgeDirection.NorthWest);

            CellEdge end =
                CreateEdge(
                    6,
                    9,
                    CellEdgeDirection.NorthWest);

            WallRunPlanResult result =
                StraightWallRunPlanner.Plan(
                    start,
                    end);

            Assert.That(result.Succeeded, Is.False);

            Assert.That(
                result.Failure,
                Is.EqualTo(
                    WallRunPlanFailure.NotCollinear));
        }


        [Test]
        public void Plan_EdgesOnDifferentLevels_AreRejected()
        {
            CellEdge start =
                new CellEdge(
                    new GridPosition(3, 8, 0),
                    CellEdgeDirection.NorthEast);

            CellEdge end =
                new CellEdge(
                    new GridPosition(3, 10, 1),
                    CellEdgeDirection.NorthEast);

            WallRunPlanResult result =
                StraightWallRunPlanner.Plan(
                    start,
                    end);

            Assert.That(result.Succeeded, Is.False);

            Assert.That(
                result.Failure,
                Is.EqualTo(
                    WallRunPlanFailure.DifferentLevel));
        }


        [Test]
        public void Plan_OppositeRequestedDescription_UsesCanonicalEdge()
        {
            CellEdge start =
                new CellEdge(
                    new GridPosition(5, 8, 0),
                    CellEdgeDirection.SouthWest);

            CellEdge end =
                new CellEdge(
                    new GridPosition(5, 11, 0),
                    CellEdgeDirection.SouthWest);

            WallRunPlanResult result =
                StraightWallRunPlanner.Plan(
                    start,
                    end);

            Assert.That(result.Succeeded, Is.True);

            Assert.That(
                result.StartEdge.CanonicalDirection,
                Is.EqualTo(
                    CellEdgeDirection.NorthEast));

            Assert.That(
                result.EndEdge.CanonicalDirection,
                Is.EqualTo(
                    CellEdgeDirection.NorthEast));

            Assert.That(result.SegmentCount, Is.EqualTo(4));
        }


        private static CellEdge CreateEdge(
            int x,
            int y,
            CellEdgeDirection direction)
        {
            return new CellEdge(
                new GridPosition(
                    x,
                    y,
                    0),
                direction);
        }
    }
}