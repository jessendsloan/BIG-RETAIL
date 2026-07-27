using System;
using BigRetail.Map.Domain;
using BigRetail.Map.View;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace BigRetail.Map.Unity.Walls
{
    /// <summary>
    /// Describes the Unity-world presentation of one logical grid vertex.
    ///
    /// The vertex position is the shared corner produced by averaging the
    /// centers of its four neighboring logical cells after view projection.
    /// </summary>
    public readonly struct GridVertexWorldPose
    {
        public Vector3 Position { get; }

        public float DisplayDepth { get; }


        private GridVertexWorldPose(
            Vector3 position,
            float displayDepth)
        {
            Position = position;
            DisplayDepth = displayDepth;
        }


        public static GridVertexWorldPose Calculate(
            GridVertex vertex,
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

            if (vertex.Level != logicalLevel)
            {
                throw new InvalidOperationException(
                    $"GridVertex {vertex} belongs to logical level "
                    + $"{vertex.Level}, but the requested Unity view "
                    + $"represents logical level {logicalLevel}.");
            }

            GridPosition first =
                new GridPosition(
                    vertex.X,
                    vertex.Y,
                    vertex.Level);

            GridPosition second =
                first.Offset(1, 0);

            GridPosition third =
                first.Offset(0, 1);

            GridPosition fourth =
                first.Offset(1, 1);

            GridPosition displayFirst =
                ToDisplayCell(
                    first,
                    projection);

            GridPosition displaySecond =
                ToDisplayCell(
                    second,
                    projection);

            GridPosition displayThird =
                ToDisplayCell(
                    third,
                    projection);

            GridPosition displayFourth =
                ToDisplayCell(
                    fourth,
                    projection);

            Vector3 position =
                (
                    GetCellCenter(
                        coordinateTilemap,
                        displayFirst,
                        unityCellZ)
                    + GetCellCenter(
                        coordinateTilemap,
                        displaySecond,
                        unityCellZ)
                    + GetCellCenter(
                        coordinateTilemap,
                        displayThird,
                        unityCellZ)
                    + GetCellCenter(
                        coordinateTilemap,
                        displayFourth,
                        unityCellZ)
                )
                * 0.25f;

            float displayDepth =
                (
                    displayFirst.X + displayFirst.Y
                    + displaySecond.X + displaySecond.Y
                    + displayThird.X + displayThird.Y
                    + displayFourth.X + displayFourth.Y
                )
                * 0.25f;

            return new GridVertexWorldPose(
                position,
                displayDepth);
        }


        private static GridPosition ToDisplayCell(
            GridPosition logicalCell,
            IsometricViewProjection projection)
        {
            return projection != null
                ? projection.ToDisplayCell(logicalCell)
                : logicalCell;
        }


        private static Vector3 GetCellCenter(
            Tilemap coordinateTilemap,
            GridPosition displayCell,
            int unityCellZ)
        {
            return coordinateTilemap.GetCellCenterWorld(
                new Vector3Int(
                    displayCell.X,
                    displayCell.Y,
                    unityCellZ));
        }
    }
}
