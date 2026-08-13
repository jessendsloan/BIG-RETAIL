using System;
using System.Collections.Generic;
using BigRetail.Map.Domain;
using BigRetail.Map.Fixtures;
using BigRetail.Map.View;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace BigRetail.Map.Unity.Fixtures
{
    /// <summary>
    /// Resolves the occupied cell that an authored whole-fixture sprite uses
    /// as its pivot anchor in the current isometric presentation.
    /// </summary>
    public static class FixturePresentationAnchorResolver
    {
        /// <summary>
        /// Resolves the occupied cell that controls a whole-fixture sprite's
        /// render order. A one-sided fixture uses the depth extreme nearest
        /// its authored customer face: the near extreme when viewed from the
        /// front and the far extreme when viewed from behind. This keeps one
        /// sprite wholly on one side of every wall segment along its back.
        /// Multi-sided fixtures retain the established viewer-nearest rule.
        /// </summary>
        public static GridPosition ResolveWholeFixtureSortingCell(
            FixtureDefinition definition,
            FixtureFootprint footprint,
            IsometricViewProjection projection)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(
                    nameof(definition));
            }

            if (footprint == null)
            {
                throw new ArgumentNullException(
                    nameof(footprint));
            }

            if (projection == null)
            {
                throw new ArgumentNullException(
                    nameof(projection));
            }

            ResolveDepthExtremes(
                footprint,
                projection,
                out GridPosition nearest,
                out int nearestDepth,
                out GridPosition farthest,
                out int farthestDepth);

            if (!HasExactlyOneAccessSide(
                    definition.AccessProfile))
            {
                return nearest;
            }

            IReadOnlyList<FixtureAccessPoint> accessPoints =
                FixtureAccessPointResolver.Resolve(
                    definition,
                    footprint);

            int nearestAccessDepth = int.MaxValue;
            int farthestAccessDepth = int.MinValue;

            for (int index = 0;
                 index < accessPoints.Count;
                 index++)
            {
                GridPosition displayAccessCell =
                    projection.ToDisplayCell(
                        accessPoints[index].Cell);

                int accessDepth =
                    displayAccessCell.X
                    + displayAccessCell.Y;

                nearestAccessDepth =
                    Math.Min(
                        nearestAccessDepth,
                        accessDepth);

                farthestAccessDepth =
                    Math.Max(
                        farthestAccessDepth,
                        accessDepth);
            }

            if (nearestAccessDepth < nearestDepth)
            {
                return nearest;
            }

            if (farthestAccessDepth > farthestDepth)
            {
                return farthest;
            }

            return nearest;
        }


        public static GridPosition ResolveViewerNearestCell(
            FixtureFootprint footprint,
            IsometricViewProjection projection)
        {
            if (footprint == null)
            {
                throw new ArgumentNullException(
                    nameof(footprint));
            }

            if (projection == null)
            {
                throw new ArgumentNullException(
                    nameof(projection));
            }

            GridPosition nearest =
                footprint.GetCell(0);

            GridPosition nearestDisplay =
                projection.ToDisplayCell(nearest);

            int nearestDepth =
                nearestDisplay.X
                + nearestDisplay.Y;

            for (int index = 1;
                 index < footprint.CellCount;
                 index++)
            {
                GridPosition candidate =
                    footprint.GetCell(index);

                GridPosition candidateDisplay =
                    projection.ToDisplayCell(candidate);

                int candidateDepth =
                    candidateDisplay.X
                    + candidateDisplay.Y;

                if (candidateDepth < nearestDepth)
                {
                    nearest = candidate;
                    nearestDepth = candidateDepth;
                }
            }

            return nearest;
        }


        private static void ResolveDepthExtremes(
            FixtureFootprint footprint,
            IsometricViewProjection projection,
            out GridPosition nearest,
            out int nearestDepth,
            out GridPosition farthest,
            out int farthestDepth)
        {
            nearest = footprint.GetCell(0);
            farthest = nearest;

            GridPosition firstDisplay =
                projection.ToDisplayCell(nearest);

            nearestDepth =
                firstDisplay.X
                + firstDisplay.Y;

            farthestDepth = nearestDepth;

            for (int index = 1;
                 index < footprint.CellCount;
                 index++)
            {
                GridPosition candidate =
                    footprint.GetCell(index);

                GridPosition candidateDisplay =
                    projection.ToDisplayCell(candidate);

                int candidateDepth =
                    candidateDisplay.X
                    + candidateDisplay.Y;

                if (candidateDepth < nearestDepth)
                {
                    nearest = candidate;
                    nearestDepth = candidateDepth;
                }

                if (candidateDepth > farthestDepth)
                {
                    farthest = candidate;
                    farthestDepth = candidateDepth;
                }
            }
        }


        private static bool HasExactlyOneAccessSide(
            FixtureAccessProfile profile)
        {
            int accessSideCount = 0;

            if (profile.North != FixtureAccessMode.None)
            {
                accessSideCount++;
            }

            if (profile.East != FixtureAccessMode.None)
            {
                accessSideCount++;
            }

            if (profile.South != FixtureAccessMode.None)
            {
                accessSideCount++;
            }

            if (profile.West != FixtureAccessMode.None)
            {
                accessSideCount++;
            }

            return accessSideCount == 1;
        }


        /// <summary>
        /// Calculates the bottom/front corner of the viewer-nearest display
        /// cell. A whole-fixture sprite places its authored floor-contact
        /// pivot here while continuing to use the cell itself for sorting.
        /// </summary>
        public static Vector3 CalculateViewerNearestCornerWorld(
            Tilemap coordinateTilemap,
            Vector3Int viewerNearestUnityCell)
        {
            if (coordinateTilemap == null)
            {
                throw new ArgumentNullException(
                    nameof(coordinateTilemap));
            }

            Vector3 center =
                coordinateTilemap.GetCellCenterWorld(
                    viewerNearestUnityCell);

            Vector3 positiveXCenter =
                coordinateTilemap.GetCellCenterWorld(
                    viewerNearestUnityCell + Vector3Int.right);

            Vector3 positiveYCenter =
                coordinateTilemap.GetCellCenterWorld(
                    viewerNearestUnityCell + Vector3Int.up);

            return center
                - (positiveXCenter - center) * 0.5f
                - (positiveYCenter - center) * 0.5f;
        }


        /// <summary>
        /// Resolves the world-space footprint corner corresponding to a
        /// whole-fixture sprite's authored floor-contact pivot.
        /// </summary>
        public static Vector3 CalculateFootprintAnchorWorld(
            Tilemap coordinateTilemap,
            FixtureFootprint footprint,
            IsometricViewProjection projection,
            FixtureSpriteAnchorCorner anchorCorner,
            int unityCellZ)
        {
            if (coordinateTilemap == null)
            {
                throw new ArgumentNullException(
                    nameof(coordinateTilemap));
            }

            if (footprint == null)
            {
                throw new ArgumentNullException(
                    nameof(footprint));
            }

            if (projection == null)
            {
                throw new ArgumentNullException(
                    nameof(projection));
            }

            if (!Enum.IsDefined(
                    typeof(FixtureSpriteAnchorCorner),
                    anchorCorner))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(anchorCorner));
            }

            if (anchorCorner
                == FixtureSpriteAnchorCorner.ViewerNearest)
            {
                GridPosition nearest =
                    ResolveViewerNearestCell(
                        footprint,
                        projection);

                GridPosition nearestDisplay =
                    projection.ToDisplayCell(nearest);

                return CalculateViewerNearestCornerWorld(
                    coordinateTilemap,
                    new Vector3Int(
                        nearestDisplay.X,
                        nearestDisplay.Y,
                        unityCellZ));
            }

            bool chooseLeft =
                anchorCorner
                == FixtureSpriteAnchorCorner.ViewerBackLeft;

            bool hasCandidate = false;
            Vector3 selected = default;

            for (int index = 0;
                 index < footprint.CellCount;
                 index++)
            {
                GridPosition displayCell =
                    projection.ToDisplayCell(
                        footprint.GetCell(index));

                Vector3Int unityCell =
                    new Vector3Int(
                        displayCell.X,
                        displayCell.Y,
                        unityCellZ);

                Vector3 center =
                    coordinateTilemap.GetCellCenterWorld(
                        unityCell);

                Vector3 halfPositiveX =
                    (coordinateTilemap.GetCellCenterWorld(
                        unityCell + Vector3Int.right)
                    - center)
                    * 0.5f;

                Vector3 halfPositiveY =
                    (coordinateTilemap.GetCellCenterWorld(
                        unityCell + Vector3Int.up)
                    - center)
                    * 0.5f;

                ConsiderCandidate(
                    center - halfPositiveX - halfPositiveY);
                ConsiderCandidate(
                    center - halfPositiveX + halfPositiveY);
                ConsiderCandidate(
                    center + halfPositiveX - halfPositiveY);
                ConsiderCandidate(
                    center + halfPositiveX + halfPositiveY);
            }

            return selected;

            void ConsiderCandidate(Vector3 candidate)
            {
                const float Epsilon = 0.00001f;

                if (!hasCandidate)
                {
                    selected = candidate;
                    hasCandidate = true;
                    return;
                }

                bool isFartherSide =
                    chooseLeft
                        ? candidate.x < selected.x - Epsilon
                        : candidate.x > selected.x + Epsilon;

                bool sharesSideAndIsFartherBack =
                    Mathf.Abs(candidate.x - selected.x) <= Epsilon
                    && candidate.y > selected.y;

                if (isFartherSide
                    || sharesSideAndIsFartherBack)
                {
                    selected = candidate;
                }
            }
        }
    }
}
