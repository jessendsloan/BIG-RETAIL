using System;
using System.Collections.Generic;
using BigRetail.Map.Domain;

namespace BigRetail.Map.Walls
{
    /// <summary>
    /// Gives every straight door span one stable panel order regardless of
    /// the order in which a caller supplied its supporting wall segments.
    /// </summary>
    public static class DoorAssemblySpanOrder
    {
        public static CellEdge[] Normalize(
            IReadOnlyList<CellEdge> edges)
        {
            if (edges == null)
            {
                throw new ArgumentNullException(
                    nameof(edges));
            }

            CellEdge[] ordered =
                new CellEdge[edges.Count];

            bool isForward =
                IsCanonicalOrder(edges);

            for (int index = 0;
                 index < edges.Count;
                 index++)
            {
                ordered[index] =
                    edges[
                        isForward
                            ? index
                            : edges.Count - 1 - index];
            }

            return ordered;
        }


        public static int GetNormalizedIndex(
            IReadOnlyList<CellEdge> edges,
            int suppliedIndex)
        {
            if (edges == null)
            {
                throw new ArgumentNullException(
                    nameof(edges));
            }

            if (suppliedIndex < 0
                || suppliedIndex >= edges.Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(suppliedIndex));
            }

            return IsCanonicalOrder(edges)
                ? suppliedIndex
                : edges.Count - 1 - suppliedIndex;
        }


        private static bool IsCanonicalOrder(
            IReadOnlyList<CellEdge> edges)
        {
            if (edges.Count < 2)
            {
                return true;
            }

            return Compare(
                    edges[0],
                    edges[edges.Count - 1])
                <= 0;
        }


        private static int Compare(
            CellEdge left,
            CellEdge right)
        {
            int comparison =
                left.AnchorCell.Level.CompareTo(
                    right.AnchorCell.Level);

            if (comparison != 0)
            {
                return comparison;
            }

            comparison =
                left.AnchorCell.X.CompareTo(
                    right.AnchorCell.X);

            if (comparison != 0)
            {
                return comparison;
            }

            comparison =
                left.AnchorCell.Y.CompareTo(
                    right.AnchorCell.Y);

            if (comparison != 0)
            {
                return comparison;
            }

            return left.CanonicalDirection.CompareTo(
                right.CanonicalDirection);
        }
    }
}
