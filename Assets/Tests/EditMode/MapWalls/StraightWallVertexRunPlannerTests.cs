using BigRetail.Map.Domain;
using NUnit.Framework;

namespace BigRetail.Map.Walls.Tests
{
    public sealed class StraightWallVertexRunPlannerTests
    {
        [Test]
        public void PositiveYRun_CreatesNorthEastEdgesAndInclusiveVertices()
        {
            GridVertex start =
                new GridVertex(4, 2, 1);

            GridVertex end =
                new GridVertex(4, 5, 1);

            WallVertexRunPlanResult result =
                StraightWallVertexRunPlanner.Plan(
                    start,
                    end);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.VertexCount, Is.EqualTo(4));
            Assert.That(result.SegmentCount, Is.EqualTo(3));

            Assert.That(result.Vertices[0], Is.EqualTo(start));
            Assert.That(result.Vertices[3], Is.EqualTo(end));

            Assert.That(
                result.Edges[0],
                Is.EqualTo(
                    new CellEdge(
                        new GridPosition(4, 3, 1),
                        CellEdgeDirection.NorthEast)));

            Assert.That(
                result.Edges[2],
                Is.EqualTo(
                    new CellEdge(
                        new GridPosition(4, 5, 1),
                        CellEdgeDirection.NorthEast)));
        }


        [Test]
        public void NegativeYRun_PreservesRequestedVertexOrder()
        {
            GridVertex start =
                new GridVertex(4, 5, 1);

            GridVertex end =
                new GridVertex(4, 2, 1);

            WallVertexRunPlanResult result =
                StraightWallVertexRunPlanner.Plan(
                    start,
                    end);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Vertices[0], Is.EqualTo(start));
            Assert.That(result.Vertices[1], Is.EqualTo(new GridVertex(4, 4, 1)));
            Assert.That(result.Vertices[3], Is.EqualTo(end));

            Assert.That(
                result.Edges[0],
                Is.EqualTo(
                    new CellEdge(
                        new GridPosition(4, 5, 1),
                        CellEdgeDirection.NorthEast)));
        }


        [Test]
        public void PositiveXRun_CreatesNorthWestEdges()
        {
            GridVertex start =
                new GridVertex(2, 6, 1);

            GridVertex end =
                new GridVertex(5, 6, 1);

            WallVertexRunPlanResult result =
                StraightWallVertexRunPlanner.Plan(
                    start,
                    end);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.SegmentCount, Is.EqualTo(3));

            Assert.That(
                result.Edges[0],
                Is.EqualTo(
                    new CellEdge(
                        new GridPosition(3, 6, 1),
                        CellEdgeDirection.NorthWest)));

            Assert.That(
                result.Edges[2],
                Is.EqualTo(
                    new CellEdge(
                        new GridPosition(5, 6, 1),
                        CellEdgeDirection.NorthWest)));
        }


        [Test]
        public void NegativeXRun_PreservesRequestedVertexOrder()
        {
            GridVertex start =
                new GridVertex(5, 6, 1);

            GridVertex end =
                new GridVertex(2, 6, 1);

            WallVertexRunPlanResult result =
                StraightWallVertexRunPlanner.Plan(
                    start,
                    end);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Vertices[0], Is.EqualTo(start));
            Assert.That(result.Vertices[1], Is.EqualTo(new GridVertex(4, 6, 1)));
            Assert.That(result.Vertices[3], Is.EqualTo(end));

            Assert.That(
                result.Edges[0],
                Is.EqualTo(
                    new CellEdge(
                        new GridPosition(5, 6, 1),
                        CellEdgeDirection.NorthWest)));
        }


        [Test]
        public void SameVertex_IsRejected()
        {
            GridVertex vertex =
                new GridVertex(3, 4, 1);

            WallVertexRunPlanResult result =
                StraightWallVertexRunPlanner.Plan(
                    vertex,
                    vertex);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.Failure,
                Is.EqualTo(
                    WallVertexRunPlanFailure.SameVertex));
            Assert.That(result.SegmentCount, Is.EqualTo(0));
        }


        [Test]
        public void DiagonalVertices_AreRejected()
        {
            WallVertexRunPlanResult result =
                StraightWallVertexRunPlanner.Plan(
                    new GridVertex(2, 3),
                    new GridVertex(5, 7));

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.Failure,
                Is.EqualTo(
                    WallVertexRunPlanFailure.NotAxisAligned));
        }


        [Test]
        public void VerticesOnDifferentLevels_AreRejected()
        {
            WallVertexRunPlanResult result =
                StraightWallVertexRunPlanner.Plan(
                    new GridVertex(2, 3, 0),
                    new GridVertex(2, 7, 1));

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.Failure,
                Is.EqualTo(
                    WallVertexRunPlanFailure.DifferentLevel));
        }
    }
}
