using System;
using BigRetail.Map.Domain;
using BigRetail.Map.Unity.Walls;
using UnityEngine;

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

            if (targetResolver.CoordinateTilemap == null)
            {
                throw new InvalidOperationException(
                    "The WallTargetResolver has no coordinate Tilemap.");
            }

            GridPosition startAnchor =
                startEdge.AnchorCell;

            switch (startEdge.CanonicalDirection)
            {
                case CellEdgeDirection.NorthEast:
                    return ResolveNorthEastRun(
                        startAnchor,
                        targetResolver);

                case CellEdgeDirection.NorthWest:
                    return ResolveNorthWestRun(
                        startAnchor,
                        targetResolver);

                default:
                    throw new InvalidOperationException(
                        "A normalized CellEdge must use " +
                        "NorthEast or NorthWest.");
            }
        }


        private static CellEdge ResolveNorthEastRun(
            GridPosition startAnchor,
            WallTargetResolver targetResolver)
        {
            CellEdge originEdge =
                new CellEdge(
                    startAnchor,
                    CellEdgeDirection.NorthEast);

            CellEdge nextEdge =
                new CellEdge(
                    startAnchor.Offset(
                        0,
                        1),
                    CellEdgeDirection.NorthEast);

            int runOffset =
                CalculateNearestRunOffset(
                    originEdge,
                    nextEdge,
                    targetResolver);

            GridPosition endAnchor =
                new GridPosition(
                    startAnchor.X,
                    startAnchor.Y + runOffset,
                    startAnchor.Level);

            return new CellEdge(
                endAnchor,
                CellEdgeDirection.NorthEast);
        }


        private static CellEdge ResolveNorthWestRun(
            GridPosition startAnchor,
            WallTargetResolver targetResolver)
        {
            CellEdge originEdge =
                new CellEdge(
                    startAnchor,
                    CellEdgeDirection.NorthWest);

            CellEdge nextEdge =
                new CellEdge(
                    startAnchor.Offset(
                        1,
                        0),
                    CellEdgeDirection.NorthWest);

            int runOffset =
                CalculateNearestRunOffset(
                    originEdge,
                    nextEdge,
                    targetResolver);

            GridPosition endAnchor =
                new GridPosition(
                    startAnchor.X + runOffset,
                    startAnchor.Y,
                    startAnchor.Level);

            return new CellEdge(
                endAnchor,
                CellEdgeDirection.NorthWest);
        }


        private static int CalculateNearestRunOffset(
            CellEdge originEdge,
            CellEdge nextEdge,
            WallTargetResolver targetResolver)
        {
            CellEdgeWorldPose originPose =
                CellEdgeWorldPose.Calculate(
                    originEdge,
                    targetResolver.CoordinateTilemap,
                    targetResolver.LogicalLevel,
                    targetResolver.UnityCellZ,
                    targetResolver.ViewProjection);

            CellEdgeWorldPose nextPose =
                CellEdgeWorldPose.Calculate(
                    nextEdge,
                    targetResolver.CoordinateTilemap,
                    targetResolver.LogicalLevel,
                    targetResolver.UnityCellZ,
                    targetResolver.ViewProjection);

            Vector3 runBasis =
                nextPose.Position
                - originPose.Position;

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
                    targetResolver.PointerWorldPosition
                        - originPose.Position,
                    runBasis)
                / basisLengthSquared;

            return Mathf.RoundToInt(
                continuousIndex);
        }
    }
}
