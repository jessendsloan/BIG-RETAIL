using System;
using BigRetail.Map.Domain;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace BigRetail.Construction.Unity.Walls
{
    /// <summary>
    /// Projects the construction pointer onto the straight map-aligned
    /// line established by a starting wall edge.
    ///
    /// This resolves wall-run geometry only.
    /// It does not validate, construct, or demolish walls.
    /// </summary>
    public static class StraightWallRunEndpointResolver
    {
        public static CellEdge Resolve(
            CellEdge startEdge,
            WallTargetResolver targetResolver)
        {
            if (targetResolver == null)
            {
                throw new ArgumentNullException(
                    nameof(targetResolver));
            }

            Tilemap tilemap =
                targetResolver.CoordinateTilemap;

            if (tilemap == null)
            {
                throw new InvalidOperationException(
                    "The WallTargetResolver has no coordinate Tilemap.");
            }

            Vector3 pointerWorldPosition =
                targetResolver.PointerWorldPosition;

            int unityCellZ =
                targetResolver.UnityCellZ;

            GridPosition startAnchor =
                startEdge.AnchorCell;

            switch (startEdge.CanonicalDirection)
            {
                case CellEdgeDirection.NorthEast:
                    return ResolveNorthEastRun(
                        startAnchor,
                        tilemap,
                        pointerWorldPosition,
                        unityCellZ);

                case CellEdgeDirection.NorthWest:
                    return ResolveNorthWestRun(
                        startAnchor,
                        tilemap,
                        pointerWorldPosition,
                        unityCellZ);

                default:
                    throw new InvalidOperationException(
                        "A normalized CellEdge must use " +
                        "NorthEast or NorthWest.");
            }
        }


        private static CellEdge ResolveNorthEastRun(
            GridPosition startAnchor,
            Tilemap tilemap,
            Vector3 pointerWorldPosition,
            int unityCellZ)
        {
            Vector3Int originUnityCell =
                new Vector3Int(
                    startAnchor.X,
                    0,
                    unityCellZ);

            Vector3 anchorCenter =
                tilemap.GetCellCenterWorld(
                    originUnityCell);

            Vector3 oppositeCenter =
                tilemap.GetCellCenterWorld(
                    originUnityCell
                    + Vector3Int.right);

            Vector3 lineOrigin =
                Vector3.Lerp(
                    anchorCenter,
                    oppositeCenter,
                    0.5f);

            Vector3 runBasis =
                tilemap.GetCellCenterWorld(
                    originUnityCell
                    + Vector3Int.up)
                - anchorCenter;

            int yIndex =
                CalculateNearestRunIndex(
                    pointerWorldPosition,
                    lineOrigin,
                    runBasis);

            GridPosition endAnchor =
                new GridPosition(
                    startAnchor.X,
                    yIndex,
                    startAnchor.Level);

            return new CellEdge(
                endAnchor,
                CellEdgeDirection.NorthEast);
        }


        private static CellEdge ResolveNorthWestRun(
            GridPosition startAnchor,
            Tilemap tilemap,
            Vector3 pointerWorldPosition,
            int unityCellZ)
        {
            Vector3Int originUnityCell =
                new Vector3Int(
                    0,
                    startAnchor.Y,
                    unityCellZ);

            Vector3 anchorCenter =
                tilemap.GetCellCenterWorld(
                    originUnityCell);

            Vector3 oppositeCenter =
                tilemap.GetCellCenterWorld(
                    originUnityCell
                    + Vector3Int.up);

            Vector3 lineOrigin =
                Vector3.Lerp(
                    anchorCenter,
                    oppositeCenter,
                    0.5f);

            Vector3 runBasis =
                tilemap.GetCellCenterWorld(
                    originUnityCell
                    + Vector3Int.right)
                - anchorCenter;

            int xIndex =
                CalculateNearestRunIndex(
                    pointerWorldPosition,
                    lineOrigin,
                    runBasis);

            GridPosition endAnchor =
                new GridPosition(
                    xIndex,
                    startAnchor.Y,
                    startAnchor.Level);

            return new CellEdge(
                endAnchor,
                CellEdgeDirection.NorthWest);
        }


        private static int CalculateNearestRunIndex(
            Vector3 pointerWorldPosition,
            Vector3 lineOrigin,
            Vector3 runBasis)
        {
            float basisLengthSquared =
                runBasis.sqrMagnitude;

            if (basisLengthSquared
                <= Mathf.Epsilon)
            {
                throw new InvalidOperationException(
                    "The Tilemap produced a zero-length " +
                    "wall-run basis.");
            }

            float continuousIndex =
                Vector3.Dot(
                    pointerWorldPosition - lineOrigin,
                    runBasis)
                / basisLengthSquared;

            return Mathf.RoundToInt(
                continuousIndex);
        }
    }
}