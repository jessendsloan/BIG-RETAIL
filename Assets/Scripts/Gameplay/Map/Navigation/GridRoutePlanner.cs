using System;
using System.Collections.Generic;
using BigRetail.Map.Domain;

namespace BigRetail.Map.Navigation
{
    /// <summary>
    /// Finds a shortest four-direction route through the logical store grid.
    /// The planner is engine-free so employees and customers can share it.
    /// </summary>
    public sealed class GridRoutePlanner
    {
        private const int DefaultMaximumVisitedCellCount = 16384;

        private static readonly (int X, int Y)[] NeighborOffsets =
        {
            (1, 0),
            (0, 1),
            (-1, 0),
            (0, -1)
        };

        private readonly IGridRouteSurfaceQuery surfaceQuery;
        private readonly int maximumVisitedCellCount;


        public GridRoutePlanner(
            IGridRouteSurfaceQuery surfaceQuery,
            int maximumVisitedCellCount = DefaultMaximumVisitedCellCount)
        {
            this.surfaceQuery =
                surfaceQuery
                ?? throw new ArgumentNullException(nameof(surfaceQuery));

            if (maximumVisitedCellCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumVisitedCellCount));
            }

            this.maximumVisitedCellCount = maximumVisitedCellCount;
        }


        public bool TryFindRoute(
            GridPosition start,
            GridPosition destination,
            out IReadOnlyList<GridPosition> route)
        {
            route = Array.Empty<GridPosition>();

            if (start.Level != destination.Level
                || !surfaceQuery.CanStandAt(start)
                || !surfaceQuery.CanStandAt(destination))
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

            frontier.Enqueue(start);
            previous.Add(start, start);

            while (frontier.Count > 0
                   && previous.Count < maximumVisitedCellCount)
            {
                GridPosition current = frontier.Dequeue();

                for (int index = 0;
                     index < NeighborOffsets.Length;
                     index++)
                {
                    (int xOffset, int yOffset) =
                        NeighborOffsets[index];
                    GridPosition next =
                        current.Offset(xOffset, yOffset);

                    if (previous.ContainsKey(next)
                        || !surfaceQuery.CanStandAt(next)
                        || !surfaceQuery.CanTraverse(
                            CreateSharedEdge(current, next)))
                    {
                        continue;
                    }

                    previous.Add(next, current);

                    if (next == destination)
                    {
                        route = ReconstructRoute(
                            start,
                            destination,
                            previous);
                        return true;
                    }

                    frontier.Enqueue(next);
                }
            }

            return false;
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


        private static CellEdge CreateSharedEdge(
            GridPosition first,
            GridPosition second)
        {
            if (second == first.Offset(1, 0))
            {
                return new CellEdge(
                    first,
                    CellEdgeDirection.NorthEast);
            }

            if (second == first.Offset(-1, 0))
            {
                return new CellEdge(
                    first,
                    CellEdgeDirection.SouthWest);
            }

            if (second == first.Offset(0, 1))
            {
                return new CellEdge(
                    first,
                    CellEdgeDirection.NorthWest);
            }

            if (second == first.Offset(0, -1))
            {
                return new CellEdge(
                    first,
                    CellEdgeDirection.SouthEast);
            }

            throw new ArgumentException(
                "A route edge requires adjacent cells.",
                nameof(second));
        }
    }
}
