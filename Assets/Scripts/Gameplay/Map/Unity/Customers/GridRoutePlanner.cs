using System;
using System.Collections.Generic;
using BigRetail.Map.Domain;

namespace BigRetail.Map.Unity.Customers
{
    /// <summary>
    /// Finds short orthogonal routes through the logical store grid. Surface
    /// ownership and edge traversal rules remain supplied by the caller.
    /// </summary>
    public static class GridRoutePlanner
    {
        private static readonly int[] XOffsets = { 1, -1, 0, 0 };
        private static readonly int[] YOffsets = { 0, 0, 1, -1 };


        public static bool TryFindRoute(
            GridPosition start,
            GridPosition destination,
            int maximumVisitedCellCount,
            Func<GridPosition, bool> isWalkable,
            Func<CellEdge, bool> canCross,
            out IReadOnlyList<GridPosition> route)
        {
            if (maximumVisitedCellCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumVisitedCellCount));
            }

            if (isWalkable == null)
            {
                throw new ArgumentNullException(nameof(isWalkable));
            }

            if (canCross == null)
            {
                throw new ArgumentNullException(nameof(canCross));
            }

            route = Array.Empty<GridPosition>();

            if (!isWalkable(start) || !isWalkable(destination))
            {
                return false;
            }

            if (start == destination)
            {
                route = new[] { start };
                return true;
            }

            Queue<GridPosition> frontier =
                new Queue<GridPosition>();

            Dictionary<GridPosition, GridPosition> previous =
                new Dictionary<GridPosition, GridPosition>();

            HashSet<GridPosition> visited =
                new HashSet<GridPosition>();

            frontier.Enqueue(start);
            visited.Add(start);

            while (frontier.Count > 0
                   && visited.Count <= maximumVisitedCellCount)
            {
                GridPosition current = frontier.Dequeue();

                for (int index = 0; index < XOffsets.Length; index++)
                {
                    GridPosition neighbor = current.Offset(
                        XOffsets[index],
                        YOffsets[index]);

                    if (visited.Contains(neighbor)
                        || !isWalkable(neighbor)
                        || !canCross(CreateSharedEdge(current, neighbor)))
                    {
                        continue;
                    }

                    visited.Add(neighbor);
                    previous.Add(neighbor, current);

                    if (neighbor == destination)
                    {
                        route = ReconstructRoute(
                            start,
                            destination,
                            previous);
                        return true;
                    }

                    frontier.Enqueue(neighbor);
                }
            }

            return false;
        }


        public static CellEdge CreateSharedEdge(
            GridPosition first,
            GridPosition second)
        {
            if (first.Level != second.Level)
            {
                throw new ArgumentException(
                    "Adjacent route cells must use the same level.",
                    nameof(second));
            }

            int xDifference = second.X - first.X;
            int yDifference = second.Y - first.Y;

            if (xDifference == 1 && yDifference == 0)
            {
                return new CellEdge(
                    first,
                    CellEdgeDirection.NorthEast);
            }

            if (xDifference == -1 && yDifference == 0)
            {
                return new CellEdge(
                    first,
                    CellEdgeDirection.SouthWest);
            }

            if (xDifference == 0 && yDifference == 1)
            {
                return new CellEdge(
                    first,
                    CellEdgeDirection.NorthWest);
            }

            if (xDifference == 0 && yDifference == -1)
            {
                return new CellEdge(
                    first,
                    CellEdgeDirection.SouthEast);
            }

            throw new ArgumentException(
                "A route edge requires two orthogonally adjacent cells.",
                nameof(second));
        }


        private static IReadOnlyList<GridPosition> ReconstructRoute(
            GridPosition start,
            GridPosition destination,
            IReadOnlyDictionary<GridPosition, GridPosition> previous)
        {
            List<GridPosition> reversed =
                new List<GridPosition> { destination };

            GridPosition current = destination;

            while (current != start)
            {
                current = previous[current];
                reversed.Add(current);
            }

            reversed.Reverse();
            return reversed.ToArray();
        }
    }
}
