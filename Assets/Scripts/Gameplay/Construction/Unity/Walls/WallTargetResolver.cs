using BigRetail.Construction.Unity.Input;
using BigRetail.Map.Domain;
using BigRetail.Map.Unity.View;
using BigRetail.Map.Unity.Walls;
using BigRetail.Map.View;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace BigRetail.Construction.Unity.Walls
{
    /// <summary>
    /// Converts the shared construction pointer position into
    /// the nearest logical wall edge.
    ///
    /// This component:
    /// - Converts screen position into a world position.
    /// - Finds the Tilemap cell beneath that position.
    /// - Determines which of the cell's four edges is nearest.
    /// - Produces one normalized WallTarget.
    ///
    /// It does not validate or place walls.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(100)]
    public sealed class WallTargetResolver : MonoBehaviour
    {
        private static readonly CellEdgeDirection[]
            CandidateDirections =
            {
                CellEdgeDirection.NorthWest,
                CellEdgeDirection.NorthEast,
                CellEdgeDirection.SouthEast,
                CellEdgeDirection.SouthWest
            };


        [Header("Pointer")]

        [SerializeField]
        private ConstructionPointerController
            pointerController;


        [Header("World Mapping")]

        [SerializeField]
        private Camera targetCamera;

        [Tooltip(
            "A Tilemap belonging to the authored map Grid. " +
            "MapVisuals is appropriate.")]
        [SerializeField]
        private Tilemap coordinateTilemap;

        [SerializeField]
        private IsometricViewHost viewHost;

        [SerializeField]
        private int logicalLevel = 0;

        [Tooltip(
            "The Unity Tilemap Z coordinate used for this logical level.")]
        [SerializeField]
        private int unityCellZ = 0;


        public bool HasTarget { get; private set; }

        public WallTarget CurrentTarget { get; private set; }

        public Vector3 PointerWorldPosition { get; private set; }

        public Vector3Int UnityCell { get; private set; }


        public Tilemap CoordinateTilemap =>
            coordinateTilemap;

        public int LogicalLevel =>
            logicalLevel;

        public int UnityCellZ =>
            unityCellZ;

        public IsometricViewProjection ViewProjection =>
            viewHost != null
                ? viewHost.Projection
                : null;


        private void Awake()
        {
            if (!ValidateReferences())
            {
                enabled = false;
            }
        }


        /// <summary>
        /// This runs after the camera's normal LateUpdate movement.
        ///
        /// As a result, edge-panning moves the camera first and then
        /// the target is resolved against the camera's new position.
        /// </summary>
        private void LateUpdate()
        {
            ResolveTarget();
        }


        private void ResolveTarget()
        {
            if (!TryConvertScreenToWorld(
                pointerController.ScreenPosition,
                out Vector3 pointerWorldPosition))
            {
                ClearTarget();
                return;
            }

            PointerWorldPosition =
                pointerWorldPosition;

            UnityCell =
                coordinateTilemap.WorldToCell(
                    PointerWorldPosition);

            GridPosition requestedCell =
                viewHost.Projection.ToLogicalCell(
                    new GridPosition(
                        UnityCell.x,
                        UnityCell.y,
                        logicalLevel));

            CellEdgeDirection nearestDirection =
                FindNearestEdgeDirection(
                    requestedCell,
                    PointerWorldPosition);

            CurrentTarget =
                new WallTarget(
                    requestedCell,
                    nearestDirection);

            HasTarget = true;
        }


        /// <summary>
        /// Projects the pointer ray onto the plane occupied by
        /// the coordinate Tilemap.
        /// </summary>
        private bool TryConvertScreenToWorld(
            Vector2 screenPosition,
            out Vector3 worldPosition)
        {
            worldPosition = default;

            Ray pointerRay =
                targetCamera.ScreenPointToRay(
                    screenPosition);

            Plane tilemapPlane =
                new Plane(
                    coordinateTilemap.transform.forward,
                    coordinateTilemap.transform.position);

            if (!tilemapPlane.Raycast(
                pointerRay,
                out float distanceAlongRay))
            {
                return false;
            }

            worldPosition =
                pointerRay.GetPoint(
                    distanceAlongRay);

            return true;
        }


        private CellEdgeDirection FindNearestEdgeDirection(
            GridPosition requestedCell,
            Vector3 pointerWorldPosition)
        {
            CellEdgeDirection nearestDirection =
                CandidateDirections[0];

            float nearestSquaredDistance =
                float.PositiveInfinity;

            for (int index = 0;
                 index < CandidateDirections.Length;
                 index++)
            {
                CellEdgeDirection candidateDirection =
                    CandidateDirections[index];

                CellEdge candidateEdge =
                    new CellEdge(
                        requestedCell,
                        candidateDirection);

                CellEdgeWorldPose candidatePose =
                    CellEdgeWorldPose.Calculate(
                        candidateEdge,
                        coordinateTilemap,
                        logicalLevel,
                        unityCellZ,
                        viewHost.Projection);

                CalculateSegmentEndpoints(
                    candidatePose,
                    out Vector3 segmentStart,
                    out Vector3 segmentEnd);

                float squaredDistance =
                    CalculateSquaredDistanceToSegment(
                        pointerWorldPosition,
                        segmentStart,
                        segmentEnd);

                if (squaredDistance
                    >= nearestSquaredDistance)
                {
                    continue;
                }

                nearestSquaredDistance =
                    squaredDistance;

                nearestDirection =
                    candidateDirection;
            }

            return nearestDirection;
        }


        private static void CalculateSegmentEndpoints(
            CellEdgeWorldPose worldPose,
            out Vector3 segmentStart,
            out Vector3 segmentEnd)
        {
            Vector3 edgeDirection =
                worldPose.Rotation
                * Vector3.right;

            edgeDirection.Normalize();

            Vector3 halfEdge =
                edgeDirection
                * worldPose.Length
                * 0.5f;

            segmentStart =
                worldPose.Position
                - halfEdge;

            segmentEnd =
                worldPose.Position
                + halfEdge;
        }


        private static float
            CalculateSquaredDistanceToSegment(
                Vector3 point,
                Vector3 segmentStart,
                Vector3 segmentEnd)
        {
            Vector3 segment =
                segmentEnd
                - segmentStart;

            float segmentLengthSquared =
                segment.sqrMagnitude;

            if (segmentLengthSquared
                <= Mathf.Epsilon)
            {
                return
                    (point - segmentStart)
                    .sqrMagnitude;
            }

            float positionAlongSegment =
                Vector3.Dot(
                    point - segmentStart,
                    segment)
                / segmentLengthSquared;

            positionAlongSegment =
                Mathf.Clamp01(
                    positionAlongSegment);

            Vector3 closestPoint =
                segmentStart
                + segment
                * positionAlongSegment;

            return
                (point - closestPoint)
                .sqrMagnitude;
        }


        private void ClearTarget()
        {
            HasTarget = false;
            CurrentTarget = default;
            PointerWorldPosition = default;
            UnityCell = default;
        }


        private bool ValidateReferences()
        {
            bool isValid = true;

            if (pointerController == null)
            {
                Debug.LogError(
                    "WallTargetResolver has no " +
                    "ConstructionPointerController assigned.",
                    this);

                isValid = false;
            }

            if (targetCamera == null)
            {
                Debug.LogError(
                    "WallTargetResolver has no target Camera assigned.",
                    this);

                isValid = false;
            }

            if (coordinateTilemap == null)
            {
                Debug.LogError(
                    "WallTargetResolver has no Coordinate Tilemap assigned.",
                    this);

                isValid = false;
            }

            if (viewHost == null)
            {
                Debug.LogError(
                    "WallTargetResolver has no IsometricViewHost assigned.",
                    this);

                isValid = false;
            }

            return isValid;
        }
    }
}
