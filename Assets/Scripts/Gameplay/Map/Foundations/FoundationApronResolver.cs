using System;
using System.Collections.Generic;
using BigRetail.Map.Domain;

namespace BigRetail.Map.Foundations
{
    /// <summary>
    /// Derives the one-cell apron surrounding constructed foundations.
    ///
    /// Apron cells are presentation data only. They are never stored in
    /// FoundationState, construction history, or save data.
    /// </summary>
    public static class FoundationApronResolver
    {
        public static IReadOnlyList<GridPosition> Resolve(
            GridMapDefinition mapDefinition,
            IEnumerable<GridPosition> foundationCells)
        {
            if (mapDefinition == null)
            {
                throw new ArgumentNullException(
                    nameof(mapDefinition));
            }

            if (foundationCells == null)
            {
                throw new ArgumentNullException(
                    nameof(foundationCells));
            }

            HashSet<GridPosition> foundations =
                new HashSet<GridPosition>(
                    foundationCells);

            HashSet<GridPosition> apron =
                new HashSet<GridPosition>();

            foreach (GridPosition foundation in foundations)
            {
                if (!mapDefinition.ContainsCell(foundation))
                {
                    continue;
                }

                AddValidNeighbors(
                    mapDefinition,
                    foundations,
                    apron,
                    foundation);
            }

            List<GridPosition> orderedApron =
                new List<GridPosition>(apron);

            orderedApron.Sort(CompareCells);

            return orderedApron.ToArray();
        }


        private static void AddValidNeighbors(
            GridMapDefinition mapDefinition,
            HashSet<GridPosition> foundations,
            HashSet<GridPosition> apron,
            GridPosition foundation)
        {
            for (int yOffset = -1;
                 yOffset <= 1;
                 yOffset++)
            {
                for (int xOffset = -1;
                     xOffset <= 1;
                     xOffset++)
                {
                    if (xOffset == 0
                        && yOffset == 0)
                    {
                        continue;
                    }

                    GridPosition candidate =
                        foundation.Offset(
                            xOffset,
                            yOffset);

                    if (!mapDefinition.ContainsCell(candidate)
                        || foundations.Contains(candidate))
                    {
                        continue;
                    }

                    apron.Add(candidate);
                }
            }
        }


        private static int CompareCells(
            GridPosition left,
            GridPosition right)
        {
            int levelComparison =
                left.Level.CompareTo(
                    right.Level);

            if (levelComparison != 0)
            {
                return levelComparison;
            }

            int yComparison =
                left.Y.CompareTo(
                    right.Y);

            return yComparison != 0
                ? yComparison
                : left.X.CompareTo(
                    right.X);
        }
    }
}
