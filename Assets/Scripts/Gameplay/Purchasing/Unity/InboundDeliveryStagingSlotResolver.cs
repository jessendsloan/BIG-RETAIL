using System;
using System.Collections.Generic;
using BigRetail.Map.Domain;

namespace BigRetail.Purchasing.Unity
{
    /// <summary>
    /// Finds a short curbside staging strip immediately outside the front
    /// corner of the authored property. These slots are presentation-only;
    /// a future placeable receiving zone can replace this resolver without
    /// changing purchase orders or inbound-load manifests.
    /// </summary>
    public static class InboundDeliveryStagingSlotResolver
    {
        private const int FirstSlotEdgeOffset = 2;
        private const int MaximumEdgeSearchLength = 32;


        public static IReadOnlyList<GridPosition> Resolve(
            GridMapDefinition mapDefinition,
            GridPosition propertyMinimumCell,
            int maximumSlotCount)
        {
            if (mapDefinition == null)
            {
                throw new ArgumentNullException(nameof(mapDefinition));
            }

            if (maximumSlotCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumSlotCount),
                    maximumSlotCount,
                    "Receiving requires at least one staging slot.");
            }

            List<GridPosition> bottomEdge =
                CollectEdge(
                    mapDefinition,
                    propertyMinimumCell,
                    maximumSlotCount,
                    alongX: true);
            List<GridPosition> leftEdge =
                CollectEdge(
                    mapDefinition,
                    propertyMinimumCell,
                    maximumSlotCount,
                    alongX: false);

            List<GridPosition> primary =
                bottomEdge.Count >= leftEdge.Count
                    ? bottomEdge
                    : leftEdge;
            List<GridPosition> secondary =
                ReferenceEquals(primary, bottomEdge)
                    ? leftEdge
                    : bottomEdge;
            List<GridPosition> result =
                new List<GridPosition>(maximumSlotCount);

            AddUnique(
                result,
                primary,
                maximumSlotCount);
            AddUnique(
                result,
                secondary,
                maximumSlotCount);

            return result.AsReadOnly();
        }


        private static List<GridPosition> CollectEdge(
            GridMapDefinition mapDefinition,
            GridPosition propertyMinimumCell,
            int maximumSlotCount,
            bool alongX)
        {
            List<GridPosition> result =
                new List<GridPosition>(maximumSlotCount);

            for (int offset = FirstSlotEdgeOffset;
                 offset < MaximumEdgeSearchLength
                 && result.Count < maximumSlotCount;
                 offset++)
            {
                GridPosition candidate = alongX
                    ? propertyMinimumCell.Offset(offset, -1)
                    : propertyMinimumCell.Offset(-1, offset);

                if (mapDefinition.ContainsCell(candidate))
                {
                    result.Add(candidate);
                }
            }

            return result;
        }

        private static void AddUnique(
            ICollection<GridPosition> destination,
            IReadOnlyList<GridPosition> candidates,
            int maximumSlotCount)
        {
            for (int index = 0;
                 index < candidates.Count
                 && destination.Count < maximumSlotCount;
                 index++)
            {
                if (!destination.Contains(candidates[index]))
                {
                    destination.Add(candidates[index]);
                }
            }
        }
    }
}
