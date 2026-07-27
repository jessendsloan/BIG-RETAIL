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
    /// Converts the shared construction pointer position into the nearest
    /// logical grid vertex.
    ///
    /// This resolver is construction-specific. Existing edge targeting remains
    /// available to wall demolition and developer finish controls.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(100)]
    public sealed class WallVertexTargetResolver : MonoBehaviour
    {
        [Header("Pointer")]

        [SerializeField]
        private ConstructionPointerController
            pointerController;


        [Header("World Mapping")]

        [SerializeField]
        private Camera targetCamera;

        [Tooltip(
            "A Tilemap belonging to the authored map Grid. "
            + "MapVisuals is appropriate.")]
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

        public WallVertexTarget CurrentTarget { get; private set; }

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
        /// Runs after normal camera movement so the target is resolved against
        /// the camera's final position for the frame.
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

            GridVertex nearestVertex =
                new GridVertex(
                    requestedCell.X,
                    requestedCell.Y,
                    logicalLevel);

            GridVertexWorldPose nearestPose =
                GridVertexWorldPose.Calculate(
                    nearestVertex,
                    coordinateTilemap,
                    logicalLevel,
                    unityCellZ,
                    viewHost.Projection);

            float nearestSquaredDistance =
                (PointerWorldPosition - nearestPose.Position)
                .sqrMagnitude;

            EvaluateCandidate(
                new GridVertex(
                    requestedCell.X - 1,
                    requestedCell.Y,
                    logicalLevel),
                ref nearestVertex,
                ref nearestPose,
                ref nearestSquaredDistance);

            EvaluateCandidate(
                new GridVertex(
                    requestedCell.X,
                    requestedCell.Y - 1,
                    logicalLevel),
                ref nearestVertex,
                ref nearestPose,
                ref nearestSquaredDistance);

            EvaluateCandidate(
                new GridVertex(
                    requestedCell.X - 1,
                    requestedCell.Y - 1,
                    logicalLevel),
                ref nearestVertex,
                ref nearestPose,
                ref nearestSquaredDistance);

            CurrentTarget =
                new WallVertexTarget(
                    requestedCell,
                    nearestVertex,
                    nearestPose.Position);

            HasTarget = true;
        }


        private void EvaluateCandidate(
            GridVertex candidate,
            ref GridVertex nearestVertex,
            ref GridVertexWorldPose nearestPose,
            ref float nearestSquaredDistance)
        {
            GridVertexWorldPose candidatePose =
                GridVertexWorldPose.Calculate(
                    candidate,
                    coordinateTilemap,
                    logicalLevel,
                    unityCellZ,
                    viewHost.Projection);

            float squaredDistance =
                (PointerWorldPosition - candidatePose.Position)
                .sqrMagnitude;

            if (squaredDistance >= nearestSquaredDistance)
            {
                return;
            }

            nearestVertex = candidate;
            nearestPose = candidatePose;
            nearestSquaredDistance = squaredDistance;
        }


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
                    "WallVertexTargetResolver has no "
                    + "ConstructionPointerController assigned.",
                    this);

                isValid = false;
            }

            if (targetCamera == null)
            {
                Debug.LogError(
                    "WallVertexTargetResolver has no target Camera assigned.",
                    this);

                isValid = false;
            }

            if (coordinateTilemap == null)
            {
                Debug.LogError(
                    "WallVertexTargetResolver has no Coordinate Tilemap assigned.",
                    this);

                isValid = false;
            }

            if (viewHost == null)
            {
                Debug.LogError(
                    "WallVertexTargetResolver has no IsometricViewHost assigned.",
                    this);

                isValid = false;
            }

            return isValid;
        }
    }
}
