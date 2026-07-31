using System;
using BigRetail.Map.Domain;
using BigRetail.Map.View;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace BigRetail.Map.Unity.Walls
{
    /// <summary>
    /// Describes the calculated Unity-world presentation of one CellEdge.
    ///
    /// Permanent walls and temporary construction previews use this
    /// same calculation so they always agree about position,
    /// rotation, and length.
    /// </summary>
    public readonly struct CellEdgeWorldPose
    {
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        public float Length { get; }
        public CellEdge DisplayEdge { get; }
        public GridPosition ViewerFacingCell { get; }
        public WallDisplaySlope DisplaySlope { get; }

        private CellEdgeWorldPose(
            Vector3 position,
            Quaternion rotation,
            float length,
            CellEdge displayEdge,
            GridPosition viewerFacingCell,
            WallDisplaySlope displaySlope)
        {
            Position = position;
            Rotation = rotation;
            Length = length;
            DisplayEdge = displayEdge;
            ViewerFacingCell = viewerFacingCell;
            DisplaySlope = displaySlope;
        }

        /// <summary>
        /// Calculates the world-space midpoint, rotation, and length
        /// needed to display a logical CellEdge.
        /// </summary>
        public static CellEdgeWorldPose Calculate(
            CellEdge edge,
            Tilemap coordinateTilemap,
            int logicalLevel,
            int unityCellZ)
        {
            return Calculate(
                edge,
                coordinateTilemap,
                logicalLevel,
                unityCellZ,
                projection: null);
        }

        /// <summary>
        /// Calculates one logical edge's pose in the supplied rotated
        /// presentation. DisplayEdge is presentation-only; callers
        /// continue to own and edit the original logical edge.
        /// </summary>
        public static CellEdgeWorldPose Calculate(
            CellEdge edge,
            Tilemap coordinateTilemap,
            int logicalLevel,
            int unityCellZ,
            IsometricViewProjection projection)
        {
            if (coordinateTilemap == null)
            {
                throw new ArgumentNullException(
                    nameof(coordinateTilemap));
            }

            ValidateLogicalLevel(
                edge,
                logicalLevel);

            CellEdge displayEdge;
            GridPosition viewerFacingCell;
            WallDisplaySlope displaySlope;

            if (projection != null)
            {
                WallPresentationSelection selection =
                    WallPresentationSelector.Select(
                        edge,
                        projection);

                displayEdge =
                    selection.DisplayEdge;

                viewerFacingCell =
                    selection.ViewerFacingCell;

                displaySlope =
                    selection.DisplaySlope;
            }
            else
            {
                displayEdge =
                    edge;

                viewerFacingCell =
                    edge.FirstCell;

                displaySlope =
                    WallPresentationSelector.GetDisplaySlope(
                        displayEdge);
            }

            GridPosition anchor =
                displayEdge.AnchorCell;

            Vector3Int anchorUnityCell =
                new Vector3Int(
                    anchor.X,
                    anchor.Y,
                    unityCellZ);

            Vector3 anchorCenter =
                coordinateTilemap.GetCellCenterWorld(
                    anchorUnityCell);

            Vector3 positiveXCenter =
                coordinateTilemap.GetCellCenterWorld(
                    anchorUnityCell + Vector3Int.right);

            Vector3 positiveYCenter =
                coordinateTilemap.GetCellCenterWorld(
                    anchorUnityCell + Vector3Int.up);

            Vector3 oppositeCellCenter;
            Vector3 edgeAxis;

            switch (displayEdge.CanonicalDirection)
            {
                case CellEdgeDirection.NorthEast:
                    // The neighboring cell lies at X + 1.
                    // The shared edge runs parallel to the Y direction.
                    oppositeCellCenter =
                        positiveXCenter;

                    edgeAxis =
                        positiveYCenter - anchorCenter;
                    break;

                case CellEdgeDirection.NorthWest:
                    // The neighboring cell lies at Y + 1.
                    // The shared edge runs parallel to the X direction.
                    oppositeCellCenter =
                        positiveYCenter;

                    edgeAxis =
                        positiveXCenter - anchorCenter;
                    break;

                default:
                    throw new InvalidOperationException(
                        "A normalized CellEdge must use "
                        + "NorthEast or NorthWest.");
            }

            // Wall presentation is currently two-dimensional.
            edgeAxis.z = 0f;

            float edgeLength =
                edgeAxis.magnitude;

            if (edgeLength <= Mathf.Epsilon)
            {
                throw new InvalidOperationException(
                    $"The coordinate Tilemap produced a zero-length "
                    + $"world edge for {edge}.");
            }

            Vector3 edgeMidpoint =
                Vector3.Lerp(
                    anchorCenter,
                    oppositeCellCenter,
                    0.5f);

            float angleDegrees =
                Mathf.Atan2(
                    edgeAxis.y,
                    edgeAxis.x)
                * Mathf.Rad2Deg;

            Quaternion rotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    angleDegrees);

            return new CellEdgeWorldPose(
                edgeMidpoint,
                rotation,
                edgeLength,
                displayEdge,
                viewerFacingCell,
                displaySlope);
        }

        private static void ValidateLogicalLevel(
            CellEdge edge,
            int logicalLevel)
        {
            if (edge.FirstCell.Level == logicalLevel
                && edge.SecondCell.Level == logicalLevel)
            {
                return;
            }

            throw new InvalidOperationException(
                $"CellEdge {edge} belongs to logical level "
                + $"{edge.FirstCell.Level}, but the requested Unity view "
                + $"represents logical level {logicalLevel}.");
        }
    }
}
