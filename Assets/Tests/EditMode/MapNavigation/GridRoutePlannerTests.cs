using System.Collections.Generic;
using BigRetail.Map.Domain;
using NUnit.Framework;

namespace BigRetail.Map.Navigation.Tests
{
    public sealed class GridRoutePlannerTests
    {
        [Test]
        public void TryFindRoute_OpenGrid_ReturnsShortestRoute()
        {
            TestSurface surface = TestSurface.CreateRectangle(3, 3);
            GridRoutePlanner planner = new GridRoutePlanner(surface);

            bool succeeded = planner.TryFindRoute(
                new GridPosition(0, 0),
                new GridPosition(2, 0),
                out IReadOnlyList<GridPosition> route);

            Assert.That(succeeded, Is.True);
            Assert.That(
                route,
                Is.EqualTo(
                    new[]
                    {
                        new GridPosition(0, 0),
                        new GridPosition(1, 0),
                        new GridPosition(2, 0)
                    }));
        }

        [Test]
        public void TryFindRoute_OccupiedCell_DetoursAroundIt()
        {
            TestSurface surface = TestSurface.CreateRectangle(3, 2);
            surface.StandingCells.Remove(new GridPosition(1, 0));
            GridRoutePlanner planner = new GridRoutePlanner(surface);

            bool succeeded = planner.TryFindRoute(
                new GridPosition(0, 0),
                new GridPosition(2, 0),
                out IReadOnlyList<GridPosition> route);

            Assert.That(succeeded, Is.True);
            Assert.That(route.Count, Is.EqualTo(5));
            Assert.That(
                new List<GridPosition>(route).Contains(
                    new GridPosition(1, 0)),
                Is.False);
            Assert.That(route[0], Is.EqualTo(new GridPosition(0, 0)));
            Assert.That(route[4], Is.EqualTo(new GridPosition(2, 0)));
        }

        [Test]
        public void TryFindRoute_ClosedWallEdge_UsesAnotherPassage()
        {
            TestSurface surface = TestSurface.CreateRectangle(2, 2);
            surface.BlockedEdges.Add(
                new CellEdge(
                    new GridPosition(0, 0),
                    CellEdgeDirection.NorthEast));
            GridRoutePlanner planner = new GridRoutePlanner(surface);

            bool succeeded = planner.TryFindRoute(
                new GridPosition(0, 0),
                new GridPosition(1, 0),
                out IReadOnlyList<GridPosition> route);

            Assert.That(succeeded, Is.True);
            Assert.That(route.Count, Is.EqualTo(4));
            Assert.That(route[1], Is.EqualTo(new GridPosition(0, 1)));
            Assert.That(route[2], Is.EqualTo(new GridPosition(1, 1)));
        }

        [Test]
        public void TryFindRoute_StartOutsideSurface_IsRejected()
        {
            TestSurface surface = TestSurface.CreateRectangle(2, 2);
            GridRoutePlanner planner = new GridRoutePlanner(surface);

            bool succeeded = planner.TryFindRoute(
                new GridPosition(-1, 0),
                new GridPosition(1, 1),
                out IReadOnlyList<GridPosition> route);

            Assert.That(succeeded, Is.False);
            Assert.That(route.Count, Is.Zero);
        }


        private sealed class TestSurface : IGridRouteSurfaceQuery
        {
            public HashSet<GridPosition> StandingCells { get; } =
                new HashSet<GridPosition>();

            public HashSet<CellEdge> BlockedEdges { get; } =
                new HashSet<CellEdge>();


            public static TestSurface CreateRectangle(
                int width,
                int height)
            {
                TestSurface surface = new TestSurface();

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        surface.StandingCells.Add(
                            new GridPosition(x, y));
                    }
                }

                return surface;
            }


            public bool CanStandAt(GridPosition cell)
            {
                return StandingCells.Contains(cell);
            }

            public bool CanTraverse(CellEdge edge)
            {
                return !BlockedEdges.Contains(edge);
            }
        }
    }
}
