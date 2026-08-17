using System.Collections.Generic;
using BigRetail.Map.Domain;
using BigRetail.Map.Unity.Customers;
using NUnit.Framework;

namespace BigRetail.Map.Unity.Tests
{
    public sealed class GridRoutePlannerTests
    {
        [Test]
        public void TryFindRoute_OpenCells_ReturnsDirectRoute()
        {
            GridPosition start = new GridPosition(0, 0);
            GridPosition destination = new GridPosition(2, 0);

            bool found = GridRoutePlanner.TryFindRoute(
                start,
                destination,
                10,
                cell => cell.Y == 0 && cell.X >= 0 && cell.X <= 2,
                edge => true,
                out IReadOnlyList<GridPosition> route);

            Assert.That(found, Is.True);
            Assert.That(route.Count, Is.EqualTo(3));
            Assert.That(route[0], Is.EqualTo(start));
            Assert.That(route[2], Is.EqualTo(destination));
        }


        [Test]
        public void TryFindRoute_BlockedCell_RoutesAroundIt()
        {
            GridPosition start = new GridPosition(0, 0);
            GridPosition blocked = new GridPosition(1, 0);
            GridPosition destination = new GridPosition(2, 0);

            bool found = GridRoutePlanner.TryFindRoute(
                start,
                destination,
                20,
                cell =>
                    cell.X >= 0
                    && cell.X <= 2
                    && cell.Y >= 0
                    && cell.Y <= 1
                    && cell != blocked,
                edge => true,
                out IReadOnlyList<GridPosition> route);

            Assert.That(found, Is.True);
            Assert.That(route.Count, Is.EqualTo(5));

            for (int index = 0; index < route.Count; index++)
            {
                Assert.That(route[index], Is.Not.EqualTo(blocked));
            }
        }


        [Test]
        public void TryFindRoute_WallBlocksOnlyConnection_ReturnsFalse()
        {
            GridPosition start = new GridPosition(0, 0);
            GridPosition destination = new GridPosition(1, 0);
            CellEdge wall =
                GridRoutePlanner.CreateSharedEdge(start, destination);

            bool found = GridRoutePlanner.TryFindRoute(
                start,
                destination,
                5,
                cell => cell == start || cell == destination,
                edge => edge != wall,
                out IReadOnlyList<GridPosition> route);

            Assert.That(found, Is.False);
            Assert.That(route.Count, Is.EqualTo(0));
        }


        [Test]
        public void TryFindRoute_DoorAllowsWallEdge_ReturnsRoute()
        {
            GridPosition start = new GridPosition(0, 0);
            GridPosition destination = new GridPosition(1, 0);
            CellEdge doorEdge =
                GridRoutePlanner.CreateSharedEdge(start, destination);

            bool found = GridRoutePlanner.TryFindRoute(
                start,
                destination,
                5,
                cell => cell == start || cell == destination,
                edge => edge == doorEdge,
                out IReadOnlyList<GridPosition> route);

            Assert.That(found, Is.True);
            Assert.That(route.Count, Is.EqualTo(2));
        }
    }
}
