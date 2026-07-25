using BigRetail.Map.Domain;
using BigRetail.Map.Walls;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace BigRetail.Map.Unity.Walls
{
    /// <summary>
    /// Determines which diagnostic wall operation should run
    /// automatically when Play Mode begins.
    /// </summary>
    public enum WallDiagnosticStartupAction
    {
        None,
        Place,
        Remove,
        Toggle
    }

    /// <summary>
    /// Temporary development tool for placing, removing, and toggling
    /// one real model-owned wall at the Tilemap cell beneath this marker.
    ///
    /// This component preserves the player-facing requested direction
    /// while also reporting the wall's normalized canonical identity.
    ///
    /// Remove this tool after the real construction cursor is working.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(50)]
    public sealed class WallPlacementDiagnostic : MonoBehaviour
    {
        [Header("Runtime Model")]

        [SerializeField]
        private GridMapHost mapHost;


        [Header("Coordinate Mapping")]

        [SerializeField]
        private Tilemap coordinateTilemap;

        [SerializeField]
        private int logicalLevel = 0;


        [Header("Requested Wall")]

        [SerializeField]
        private CellEdgeDirection edgeDirection =
            CellEdgeDirection.NorthEast;


        [Header("Automatic Test")]

        [Tooltip(
            "Optional operation performed automatically when Play Mode begins.")]
        [SerializeField]
        private WallDiagnosticStartupAction startupAction =
            WallDiagnosticStartupAction.None;

        [SerializeField]
        private bool logResult = true;


        private void Start()
        {
            switch (startupAction)
            {
                case WallDiagnosticStartupAction.None:
                    break;

                case WallDiagnosticStartupAction.Place:
                    PlaceWallAtMarker();
                    break;

                case WallDiagnosticStartupAction.Remove:
                    RemoveWallAtMarker();
                    break;

                case WallDiagnosticStartupAction.Toggle:
                    ToggleWallAtMarker();
                    break;

                default:
                    Debug.LogError(
                        $"Unsupported diagnostic startup action: " +
                        $"{startupAction}.",
                        this);
                    break;
            }
        }


        [ContextMenu("Place Wall At Marker")]
        public void PlaceWallAtMarker()
        {
            if (!TryCreateWallRequest(
                out Vector3Int unityCell,
                out GridPosition requestedCell,
                out CellEdge canonicalEdge))
            {
                return;
            }

            WallChangeResult result =
                mapHost.WallConstruction
                    .TryPlaceWall(canonicalEdge);

            LogOperationResult(
                "Place",
                unityCell,
                requestedCell,
                canonicalEdge,
                result);
        }


        [ContextMenu("Remove Wall At Marker")]
        public void RemoveWallAtMarker()
        {
            if (!TryCreateWallRequest(
                out Vector3Int unityCell,
                out GridPosition requestedCell,
                out CellEdge canonicalEdge))
            {
                return;
            }

            WallChangeResult result =
                mapHost.WallConstruction
                    .TryRemoveWall(canonicalEdge);

            LogOperationResult(
                "Remove",
                unityCell,
                requestedCell,
                canonicalEdge,
                result);
        }


        [ContextMenu("Toggle Wall At Marker")]
        public void ToggleWallAtMarker()
        {
            if (!TryCreateWallRequest(
                out Vector3Int unityCell,
                out GridPosition requestedCell,
                out CellEdge canonicalEdge))
            {
                return;
            }

            bool wallAlreadyExists =
                mapHost.WallConstruction
                    .HasWall(canonicalEdge);

            string operationName;
            WallChangeResult result;

            if (wallAlreadyExists)
            {
                operationName = "Toggle Remove";

                result =
                    mapHost.WallConstruction
                        .TryRemoveWall(canonicalEdge);
            }
            else
            {
                operationName = "Toggle Place";

                result =
                    mapHost.WallConstruction
                        .TryPlaceWall(canonicalEdge);
            }

            LogOperationResult(
                operationName,
                unityCell,
                requestedCell,
                canonicalEdge,
                result);
        }


        /// <summary>
        /// Converts the marker's world position into both:
        ///
        /// 1. The player-facing requested cell and direction.
        /// 2. The normalized CellEdge used by WallState.
        /// </summary>
        private bool TryCreateWallRequest(
            out Vector3Int unityCell,
            out GridPosition requestedCell,
            out CellEdge canonicalEdge)
        {
            unityCell = default;
            requestedCell = default;
            canonicalEdge = default;

            if (!Application.isPlaying)
            {
                Debug.LogWarning(
                    "Wall operations require Play Mode because the " +
                    "runtime GridMapHost does not exist in Edit Mode.",
                    this);

                return false;
            }

            if (!ValidateReferences())
            {
                return false;
            }

            if (!mapHost.IsInitialized)
            {
                mapHost.Initialize();
            }

            if (!mapHost.IsInitialized
                || mapHost.WallConstruction == null)
            {
                Debug.LogError(
                    "WallPlacementDiagnostic could not access an " +
                    "initialized WallConstructionService.",
                    this);

                return false;
            }

            unityCell =
                coordinateTilemap.WorldToCell(
                    transform.position);

            requestedCell =
                new GridPosition(
                    unityCell.x,
                    unityCell.y,
                    logicalLevel);

            canonicalEdge =
                new CellEdge(
                    requestedCell,
                    edgeDirection);

            return true;
        }


        private void LogOperationResult(
            string operationName,
            Vector3Int unityCell,
            GridPosition requestedCell,
            CellEdge canonicalEdge,
            WallChangeResult result)
        {
            if (!logResult)
            {
                return;
            }

            string requestDescription =
                $"Requested cell: {requestedCell}. " +
                $"Requested direction: {edgeDirection}. " +
                $"Canonical identity: {canonicalEdge}. " +
                $"Unity cell: {unityCell}.";

            if (result.Succeeded)
            {
                Debug.Log(
                    $"{operationName} wall operation succeeded. " +
                    requestDescription,
                    this);

                return;
            }

            Debug.LogWarning(
                $"{operationName} wall operation was rejected: " +
                $"{result.Failure}. {requestDescription}",
                this);
        }


        [ContextMenu("Snap Marker To Current Cell Center")]
        private void SnapMarkerToCurrentCellCenter()
        {
            if (coordinateTilemap == null)
            {
                Debug.LogWarning(
                    "Assign a Coordinate Tilemap before snapping " +
                    "the diagnostic marker.",
                    this);

                return;
            }

            Vector3Int unityCell =
                coordinateTilemap.WorldToCell(
                    transform.position);

            transform.position =
                coordinateTilemap.GetCellCenterWorld(
                    unityCell);
        }


        private bool ValidateReferences()
        {
            bool isValid = true;

            if (mapHost == null)
            {
                Debug.LogError(
                    "WallPlacementDiagnostic has no GridMapHost assigned.",
                    this);

                isValid = false;
            }

            if (coordinateTilemap == null)
            {
                Debug.LogError(
                    "WallPlacementDiagnostic has no Coordinate " +
                    "Tilemap assigned.",
                    this);

                isValid = false;
            }

            return isValid;
        }


        private void OnDrawGizmosSelected()
        {
            if (coordinateTilemap == null)
            {
                return;
            }

            Vector3Int unityCell =
                coordinateTilemap.WorldToCell(
                    transform.position);

            Vector3 cellCenter =
                coordinateTilemap.GetCellCenterWorld(
                    unityCell);

            Gizmos.color = Color.yellow;

            Gizmos.DrawWireSphere(
                cellCenter,
                0.1f);

            Gizmos.DrawLine(
                transform.position,
                cellCenter);
        }
    }
}
