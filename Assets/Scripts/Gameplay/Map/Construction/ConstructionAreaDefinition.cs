using System;
using System.Collections.Generic;
using BigRetail.Map.Domain;

namespace BigRetail.Map.Construction
{
    /// <summary>
    /// Describes the authored cells that are physically eligible
    /// for construction.
    ///
    /// Eligibility does not guarantee that a requested construction
    /// action is currently allowed.
    ///
    /// Ownership, progression, cost, conflicts, and other rules
    /// may still reject a construction request.
    /// </summary>
    public sealed class ConstructionAreaDefinition :
        IConstructionCellEligibility
    {
        private readonly HashSet<GridPosition> eligibleCells;

        /// <summary>
        /// Number of cells that are physically eligible
        /// for construction.
        /// </summary>
        public int EligibleCellCount => eligibleCells.Count;

        public ConstructionAreaDefinition(
            GridMapDefinition mapDefinition,
            IEnumerable<GridPosition> eligibleCells)
        {
            if (mapDefinition == null)
            {
                throw new ArgumentNullException(
                    nameof(mapDefinition));
            }

            if (eligibleCells == null)
            {
                throw new ArgumentNullException(
                    nameof(eligibleCells));
            }

            this.eligibleCells =
                new HashSet<GridPosition>(eligibleCells);

            ValidateEligibleCells(mapDefinition);
        }

        /// <summary>
        /// Returns true when this position is physically eligible
        /// for construction.
        /// </summary>
        public bool IsEligible(GridPosition position)
        {
            return eligibleCells.Contains(position);
        }

        /// <summary>
        /// Enumerates construction-eligible cells without exposing
        /// the private collection for modification.
        /// </summary>
        public IEnumerable<GridPosition> EnumerateEligibleCells()
        {
            foreach (GridPosition position in eligibleCells)
            {
                yield return position;
            }
        }

        /// <summary>
        /// Ensures construction eligibility never extends outside
        /// the authored grid map.
        /// </summary>
        private void ValidateEligibleCells(
            GridMapDefinition mapDefinition)
        {
            foreach (GridPosition position in eligibleCells)
            {
                if (mapDefinition.ContainsCell(position))
                {
                    continue;
                }

                throw new ArgumentException(
                    $"Construction-eligible cell {position} " +
                    "does not belong to the grid map.",
                    nameof(eligibleCells));
            }
        }
    }
}
