using System;
using System.Collections.Generic;
using BigRetail.Map.Domain;

namespace BigRetail.Map.Foundations
{
    /// <summary>
    /// Resolves the final apron cells affected by a temporary foundation
    /// placement without modifying FoundationState.
    /// </summary>
    public static class FoundationApronPreviewResolver
    {
        public static IReadOnlyList<GridPosition> Resolve(
            GridMapDefinition mapDefinition,
            IEnumerable<GridPosition> currentFoundations,
            IEnumerable<GridPosition> previewFoundations)
        {
            if (mapDefinition == null)
            {
                throw new ArgumentNullException(
                    nameof(mapDefinition));
            }

            if (currentFoundations == null)
            {
                throw new ArgumentNullException(
                    nameof(currentFoundations));
            }

            if (previewFoundations == null)
            {
                throw new ArgumentNullException(
                    nameof(previewFoundations));
            }

            HashSet<GridPosition> preview =
                new HashSet<GridPosition>();

            foreach (GridPosition cell in previewFoundations)
            {
                if (mapDefinition.ContainsCell(cell))
                {
                    preview.Add(cell);
                }
            }

            if (preview.Count == 0)
            {
                return Array.Empty<GridPosition>();
            }

            HashSet<GridPosition> projectedFoundations =
                new HashSet<GridPosition>(
                    currentFoundations);

            projectedFoundations.UnionWith(preview);

            HashSet<GridPosition> affectedNeighborhood =
                ResolveAffectedNeighborhood(preview);

            IReadOnlyList<GridPosition> finalApron =
                FoundationApronResolver.Resolve(
                    mapDefinition,
                    projectedFoundations);

            List<GridPosition> affectedApron =
                new List<GridPosition>();

            for (int index = 0;
                 index < finalApron.Count;
                 index++)
            {
                GridPosition apronCell =
                    finalApron[index];

                if (affectedNeighborhood.Contains(apronCell))
                {
                    affectedApron.Add(apronCell);
                }
            }

            return affectedApron.ToArray();
        }


        private static HashSet<GridPosition>
            ResolveAffectedNeighborhood(
                IEnumerable<GridPosition> previewFoundations)
        {
            HashSet<GridPosition> neighborhood =
                new HashSet<GridPosition>();

            foreach (GridPosition foundation in previewFoundations)
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

                        neighborhood.Add(
                            foundation.Offset(
                                xOffset,
                                yOffset));
                    }
                }
            }

            return neighborhood;
        }
    }
}
