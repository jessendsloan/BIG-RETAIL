using System;
using BigRetail.Map.Domain;
using BigRetail.Map.Unity.Walls;
using UnityEngine;

namespace BigRetail.Construction.Unity.Walls
{
    /// <summary>
    /// Snaps the construction pointer to the nearest map-aligned vertex axis
    /// established by one starting grid vertex.
    /// </summary>
    public static class StraightWallVertexRunEndpointResolver
    {
        public static GridVertex Resolve(
            GridVertex startVertex,
            WallVertexTargetResolver targetResolver)
        {
            if (targetResolver == null)
            {
                throw new ArgumentNullException(
                    nameof(targetResolver));
            }

            if (targetResolver.CoordinateTilemap == null)
            {
                throw new InvalidOperationException(
                    "The WallVertexTargetResolver has no coordinate Tilemap.");
            }

            GridVertexWorldPose originPose =
                GridVertexWorldPose.Calculate(
                    startVertex,
                    targetResolver.CoordinateTilemap,
                    targetResolver.LogicalLevel,
                    targetResolver.UnityCellZ,
                    targetResolver.ViewProjection);

            GridVertexWorldPose positiveXPose =
                GridVertexWorldPose.Calculate(
                    startVertex.Offset(1, 0),
                    targetResolver.CoordinateTilemap,
                    targetResolver.LogicalLevel,
                    targetResolver.UnityCellZ,
                    targetResolver.ViewProjection);

            GridVertexWorldPose positiveYPose =
                GridVertexWorldPose.Calculate(
                    startVertex.Offset(0, 1),
                    targetResolver.CoordinateTilemap,
                    targetResolver.LogicalLevel,
                    targetResolver.UnityCellZ,
                    targetResolver.ViewProjection);

            Vector3 xBasis =
                positiveXPose.Position
                - originPose.Position;

            Vector3 yBasis =
                positiveYPose.Position
                - originPose.Position;

            AxisCandidate xCandidate =
                CalculateCandidate(
                    targetResolver.PointerWorldPosition,
                    originPose.Position,
                    xBasis);

            AxisCandidate yCandidate =
                CalculateCandidate(
                    targetResolver.PointerWorldPosition,
                    originPose.Position,
                    yBasis);

            if (xCandidate.SquaredDistance
                <= yCandidate.SquaredDistance)
            {
                return startVertex.Offset(
                    xCandidate.Offset,
                    0);
            }

            return startVertex.Offset(
                0,
                yCandidate.Offset);
        }


        private static AxisCandidate CalculateCandidate(
            Vector3 pointerPosition,
            Vector3 originPosition,
            Vector3 basis)
        {
            float basisLengthSquared =
                basis.sqrMagnitude;

            if (basisLengthSquared <= Mathf.Epsilon)
            {
                throw new InvalidOperationException(
                    "The Tilemap produced a zero-length vertex-run basis.");
            }

            float continuousOffset =
                Vector3.Dot(
                    pointerPosition - originPosition,
                    basis)
                / basisLengthSquared;

            int offset =
                Mathf.RoundToInt(
                    continuousOffset);

            Vector3 snappedPosition =
                originPosition
                + basis * offset;

            float squaredDistance =
                (pointerPosition - snappedPosition)
                .sqrMagnitude;

            return new AxisCandidate(
                offset,
                squaredDistance);
        }


        private readonly struct AxisCandidate
        {
            public int Offset { get; }

            public float SquaredDistance { get; }


            public AxisCandidate(
                int offset,
                float squaredDistance)
            {
                Offset = offset;
                SquaredDistance = squaredDistance;
            }
        }
    }
}
