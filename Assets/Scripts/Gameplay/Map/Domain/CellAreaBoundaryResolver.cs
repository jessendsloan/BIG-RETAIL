using System;
using System.Collections.Generic;

namespace BigRetail.Map.Domain
{
    /// <summary>
    /// Resolves only the outside edges of a set of grid cells.
    /// Shared edges between neighboring cells are removed.
    /// </summary>
    public static class CellAreaBoundaryResolver
    {
        private static readonly CellEdgeDirection[]
            Directions =
            {
                CellEdgeDirection.NorthWest,
                CellEdgeDirection.NorthEast,
                CellEdgeDirection.SouthEast,
                CellEdgeDirection.SouthWest
            };


        public static IReadOnlyList<CellEdge> Resolve(
            IEnumerable<GridPosition> cells)
        {
            if (cells == null)
            {
                throw new ArgumentNullException(
                    nameof(cells));
            }

            HashSet<GridPosition> uniqueCells =
                new HashSet<GridPosition>(cells);

            HashSet<CellEdge> boundaryEdges =
                new HashSet<CellEdge>();

            foreach (GridPosition cell in uniqueCells)
            {
                for (int index = 0;
                     index < Directions.Length;
                     index++)
                {
                    CellEdge edge =
                        new CellEdge(
                            cell,
                            Directions[index]);

                    if (!boundaryEdges.Add(edge))
                    {
                        boundaryEdges.Remove(edge);
                    }
                }
            }

            List<CellEdge> orderedEdges =
                new List<CellEdge>(boundaryEdges);

            orderedEdges.Sort(CompareEdges);

            return orderedEdges.ToArray();
        }


        private static int CompareEdges(
            CellEdge left,
            CellEdge right)
        {
            GridPosition leftAnchor =
                left.AnchorCell;

            GridPosition rightAnchor =
                right.AnchorCell;

            int levelComparison =
                leftAnchor.Level.CompareTo(
                    rightAnchor.Level);

            if (levelComparison != 0)
            {
                return levelComparison;
            }

            int yComparison =
                leftAnchor.Y.CompareTo(
                    rightAnchor.Y);

            if (yComparison != 0)
            {
                return yComparison;
            }

            int xComparison =
                leftAnchor.X.CompareTo(
                    rightAnchor.X);

            return xComparison != 0
                ? xComparison
                : left.CanonicalDirection.CompareTo(
                    right.CanonicalDirection);
        }
    }
}
