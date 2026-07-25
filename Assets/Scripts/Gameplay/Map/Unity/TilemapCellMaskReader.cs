using System;
using System.Collections.Generic;
using BigRetail.Map.Domain;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace BigRetail.Map.Unity
{
    /// <summary>
    /// Converts occupied cells from a semantic Unity Tilemap
    /// into logical GridPosition values.
    ///
    /// This class reads Tilemap data only.
    /// It does not create map definitions or own runtime state.
    /// </summary>
    public static class TilemapCellMaskReader
    {
        /// <summary>
        /// Reads every occupied cell in the supplied semantic mask.
        ///
        /// Unity cell X and Y become logical grid X and Y.
        /// The supplied logicalLevel becomes GridPosition.Level.
        ///
        /// Unity cell Z is deliberately not treated as a logical
        /// building level because Isometric Z as Y may use it for
        /// visual positioning.
        /// </summary>
        public static HashSet<GridPosition> ReadOccupiedCells(
            Tilemap tilemap,
            int logicalLevel,
            int expectedUnityCellZ)
        {
            if (tilemap == null)
            {
                throw new ArgumentNullException(
                    nameof(tilemap));
            }

            HashSet<GridPosition> occupiedCells =
                new HashSet<GridPosition>();

            BoundsInt cellBounds =
                tilemap.cellBounds;

            foreach (
                Vector3Int unityCell
                in cellBounds.allPositionsWithin)
            {
                if (tilemap.GetTile(unityCell) == null)
                {
                    continue;
                }

                ValidateCellZ(
                    tilemap,
                    unityCell,
                    expectedUnityCellZ);

                GridPosition gridPosition =
                    new GridPosition(
                        unityCell.x,
                        unityCell.y,
                        logicalLevel);

                occupiedCells.Add(gridPosition);
            }

            return occupiedCells;
        }

        /// <summary>
        /// Rejects semantic tiles painted on an unexpected Unity Z layer.
        ///
        /// Silently flattening multiple Unity Z layers into one logical
        /// floor could hide authoring mistakes and create overlapping cells.
        /// </summary>
        private static void ValidateCellZ(
            Tilemap tilemap,
            Vector3Int unityCell,
            int expectedUnityCellZ)
        {
            if (unityCell.z == expectedUnityCellZ)
            {
                return;
            }

            throw new InvalidOperationException(
                $"Semantic Tilemap '{tilemap.name}' contains a tile " +
                $"at Unity cell {unityCell}, but this map authoring " +
                $"expects semantic tiles at Unity cell Z " +
                $"{expectedUnityCellZ}. " +
                "Unity cell Z is not being used as the logical floor.");
        }
    }
}