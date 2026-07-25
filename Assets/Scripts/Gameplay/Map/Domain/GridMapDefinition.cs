using System;
using System.Collections.Generic;

namespace BigRetail.Map.Domain
{
    /// <summary>
    /// Describes the authored, normally unchanging geometry
    /// of a logical grid map.
    ///
    /// It knows:
    /// - The map's stable identity
    /// - Which logical cells belong to the map
    ///
    /// It does not decide whether construction is permitted.
    /// </summary>
    public sealed class GridMapDefinition
    {
        private readonly HashSet<GridPosition> validCells;

        /// <summary>
        /// Stable internal identifier for this authored map.
        ///
        /// This is not intended to be a player-facing display name.
        /// </summary>
        public string MapId { get; }

        /// <summary>
        /// Number of logical cells belonging to the map.
        /// </summary>
        public int ValidCellCount => validCells.Count;

        public GridMapDefinition(
            string mapId,
            IEnumerable<GridPosition> validCells)
        {
            if (string.IsNullOrWhiteSpace(mapId))
            {
                throw new ArgumentException(
                    "A grid-map definition requires " +
                    "a stable map ID.",
                    nameof(mapId));
            }

            if (validCells == null)
            {
                throw new ArgumentNullException(
                    nameof(validCells));
            }

            MapId = mapId.Trim();

            this.validCells =
                new HashSet<GridPosition>(validCells);

            if (this.validCells.Count == 0)
            {
                throw new ArgumentException(
                    "A grid-map definition must contain " +
                    "at least one valid cell.",
                    nameof(validCells));
            }
        }

        /// <summary>
        /// Returns true when the position belongs
        /// to this authored map.
        /// </summary>
        public bool ContainsCell(GridPosition position)
        {
            return validCells.Contains(position);
        }

        /// <summary>
        /// Enumerates every logical cell belonging to the map.
        ///
        /// The private collection itself is not exposed
        /// for outside modification.
        /// </summary>
        public IEnumerable<GridPosition> EnumerateValidCells()
        {
            foreach (GridPosition position in validCells)
            {
                yield return position;
            }
        }
    }
}