using System;
using BigRetail.Map.Domain;
using BigRetail.Map.Walls;
using NUnit.Framework;

namespace BigRetail.Map.Walls.Tests
{
    public sealed class DoorPlacementSpanPlannerTests
    {
        [Test]
        public void FourPanelDoor_CentersTwoSegmentsAroundSelectedVertex()
        {
            CellEdge hoveredEdge =
                new CellEdge(
                    new GridPosition(4, 5),
                    CellEdgeDirection.NorthEast);

            GridVertex centerVertex =
                hoveredEdge.SecondVertex;

            WallVertexRunPlanResult plan =
                DoorPlacementSpanPlanner.Plan(
                    hoveredEdge,
                    centerVertex,
                    4);

            Assert.That(plan.Succeeded, Is.True);
            Assert.That(plan.SegmentCount, Is.EqualTo(4));
            Assert.That(plan.Vertices[2], Is.EqualTo(centerVertex));
            Assert.That(plan.Edges, Does.Contain(hoveredEdge));
        }


        [Test]
        public void FourPanelDoor_OtherEndpoint_ShiftsFootprintOneSegment()
        {
            CellEdge hoveredEdge =
                new CellEdge(
                    new GridPosition(4, 5),
                    CellEdgeDirection.NorthWest);

            WallVertexRunPlanResult firstCentered =
                DoorPlacementSpanPlanner.Plan(
                    hoveredEdge,
                    hoveredEdge.FirstVertex,
                    4);

            WallVertexRunPlanResult secondCentered =
                DoorPlacementSpanPlanner.Plan(
                    hoveredEdge,
                    hoveredEdge.SecondVertex,
                    4);

            Assert.That(firstCentered.Succeeded, Is.True);
            Assert.That(secondCentered.Succeeded, Is.True);
            Assert.That(
                firstCentered.StartVertex,
                Is.Not.EqualTo(secondCentered.StartVertex));
            Assert.That(
                firstCentered.EndVertex,
                Is.Not.EqualTo(secondCentered.EndVertex));
            Assert.That(
                firstCentered.Edges,
                Does.Contain(hoveredEdge));
            Assert.That(
                secondCentered.Edges,
                Does.Contain(hoveredEdge));
        }


        [Test]
        public void SinglePanelDoor_UsesExactHoveredWallSegment()
        {
            CellEdge hoveredEdge =
                new CellEdge(
                    new GridPosition(2, 3),
                    CellEdgeDirection.NorthEast);

            WallVertexRunPlanResult plan =
                DoorPlacementSpanPlanner.Plan(
                    hoveredEdge,
                    default,
                    1);

            Assert.That(plan.Succeeded, Is.True);
            Assert.That(plan.SegmentCount, Is.EqualTo(1));
            Assert.That(plan.Edges[0], Is.EqualTo(hoveredEdge));
        }


        [Test]
        public void EvenPanelDoor_CenterOutsideHoveredEdge_IsRejected()
        {
            CellEdge hoveredEdge =
                new CellEdge(
                    new GridPosition(2, 3),
                    CellEdgeDirection.NorthEast);

            Assert.Throws<ArgumentException>(
                () => DoorPlacementSpanPlanner.Plan(
                    hoveredEdge,
                    new GridVertex(50, 50),
                    4));
        }
    }
}
