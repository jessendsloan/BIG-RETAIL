using System.Collections.Generic;
using BigRetail.Map.Domain;
using BigRetail.Map.Floors;
using BigRetail.Map.Unity.View;
using BigRetail.Map.View;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace BigRetail.Map.Unity.Floors
{
    /// <summary>
    /// Keeps a dedicated Unity Tilemap synchronized with model-owned
    /// FloorState.
    ///
    /// FloorState remains authoritative.
    /// This component performs presentation only.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-50)]
    public sealed class FloorTilemapViewSystem :
        MonoBehaviour
    {
        [Header("Runtime Model")]

        [SerializeField]
        private FloorRuntimeHost floorRuntimeHost;


        [Header("Tilemap Presentation")]

        [Tooltip(
            "Dedicated runtime Tilemap used only for constructed floors.")]
        [SerializeField]
        private Tilemap floorTilemap;

        [SerializeField]
        private TileBase floorTile;

        [SerializeField]
        private IsometricViewHost viewHost;


        [Header("Coordinate Mapping")]

        [SerializeField]
        private int logicalLevel = 0;

        [SerializeField]
        private int unityCellZ = 0;


        private readonly HashSet<GridPosition>
            visibleFloors =
                new HashSet<GridPosition>();

        private FloorState subscribedFloorState;


        public int VisibleFloorCount =>
            visibleFloors.Count;


        private void Awake()
        {
            if (!ValidateReferences())
            {
                enabled = false;
            }
        }


        private void OnEnable()
        {
            if (floorRuntimeHost != null)
            {
                floorRuntimeHost.Initialized +=
                    HandleFloorRuntimeInitialized;
            }

            if (viewHost != null)
            {
                viewHost.OrientationChanging +=
                    HandleOrientationChanging;

                viewHost.OrientationChanged +=
                    HandleOrientationChanged;
            }
        }


        private void Start()
        {
            if (floorRuntimeHost != null
                && floorRuntimeHost.IsInitialized)
            {
                AttachToFloorState(
                    floorRuntimeHost.FloorState);
            }
        }


        private void OnDisable()
        {
            if (floorRuntimeHost != null)
            {
                floorRuntimeHost.Initialized -=
                    HandleFloorRuntimeInitialized;
            }

            if (viewHost != null)
            {
                viewHost.OrientationChanging -=
                    HandleOrientationChanging;

                viewHost.OrientationChanged -=
                    HandleOrientationChanged;
            }

            DetachFromFloorState();
            ClearAllViews();
        }


        private void HandleFloorRuntimeInitialized(
            FloorRuntimeHost initializedHost)
        {
            AttachToFloorState(
                initializedHost.FloorState);
        }


        private void HandleOrientationChanging(
            IsometricViewOrientation previousOrientation,
            IsometricViewOrientation nextOrientation)
        {
            ClearAllViews();
        }


        private void HandleOrientationChanged(
            IsometricViewOrientation previousOrientation,
            IsometricViewOrientation currentOrientation)
        {
            RebuildAllViews();
        }


        private void AttachToFloorState(
            FloorState floorState)
        {
            if (floorState == null)
            {
                Debug.LogError(
                    "FloorTilemapViewSystem received a null FloorState.",
                    this);

                return;
            }

            if (subscribedFloorState == floorState)
            {
                RebuildAllViews();
                return;
            }

            DetachFromFloorState();

            subscribedFloorState =
                floorState;

            subscribedFloorState.FloorAdded +=
                HandleFloorAdded;

            subscribedFloorState.FloorRemoved +=
                HandleFloorRemoved;

            RebuildAllViews();
        }


        private void DetachFromFloorState()
        {
            if (subscribedFloorState == null)
            {
                return;
            }

            subscribedFloorState.FloorAdded -=
                HandleFloorAdded;

            subscribedFloorState.FloorRemoved -=
                HandleFloorRemoved;

            subscribedFloorState =
                null;
        }


        private void HandleFloorAdded(
            GridPosition cell)
        {
            ShowFloor(cell);
        }


        private void HandleFloorRemoved(
            GridPosition cell)
        {
            HideFloor(cell);
        }


        private void RebuildAllViews()
        {
            ClearAllViews();

            if (subscribedFloorState == null)
            {
                return;
            }

            foreach (
                GridPosition floor
                in subscribedFloorState.EnumerateFloors())
            {
                ShowFloor(floor);
            }
        }


        private void ShowFloor(
            GridPosition cell)
        {
            if (cell.Level != logicalLevel
                || visibleFloors.Contains(cell))
            {
                return;
            }

            Vector3Int unityCell =
                ToUnityCell(cell);

            floorTilemap.SetTile(
                unityCell,
                floorTile);

            visibleFloors.Add(cell);
        }


        private void HideFloor(
            GridPosition cell)
        {
            if (!visibleFloors.Remove(cell))
            {
                return;
            }

            floorTilemap.SetTile(
                ToUnityCell(cell),
                null);
        }


        /// <summary>
        /// Clears only cells placed and tracked by this view system.
        ///
        /// It deliberately avoids ClearAllTiles so an incorrect
        /// Inspector assignment cannot wipe an authored Tilemap.
        /// </summary>
        private void ClearAllViews()
        {
            foreach (
                GridPosition cell
                in visibleFloors)
            {
                floorTilemap.SetTile(
                    ToUnityCell(cell),
                    null);
            }

            visibleFloors.Clear();
        }


        private Vector3Int ToUnityCell(
            GridPosition cell)
        {
            GridPosition displayCell =
                viewHost.Projection.ToDisplayCell(
                    cell);

            return new Vector3Int(
                displayCell.X,
                displayCell.Y,
                unityCellZ);
        }


        private bool ValidateReferences()
        {
            bool isValid = true;

            if (floorRuntimeHost == null)
            {
                Debug.LogError(
                    "FloorTilemapViewSystem has no " +
                    "FloorRuntimeHost assigned.",
                    this);

                isValid = false;
            }

            if (floorTilemap == null)
            {
                Debug.LogError(
                    "FloorTilemapViewSystem has no floor Tilemap assigned.",
                    this);

                isValid = false;
            }

            if (floorTile == null)
            {
                Debug.LogError(
                    "FloorTilemapViewSystem has no floor Tile assigned.",
                    this);

                isValid = false;
            }

            if (viewHost == null)
            {
                Debug.LogError(
                    "FloorTilemapViewSystem has no " +
                    "IsometricViewHost assigned.",
                    this);

                isValid = false;
            }

            return isValid;
        }


        private void Reset()
        {
            floorTilemap =
                GetComponent<Tilemap>();
        }


        private void OnValidate()
        {
            if (floorTilemap == null)
            {
                floorTilemap =
                    GetComponent<Tilemap>();
            }
        }
    }
}
