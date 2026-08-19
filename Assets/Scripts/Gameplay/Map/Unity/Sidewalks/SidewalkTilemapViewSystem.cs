using System.Collections.Generic;
using BigRetail.Map.Domain;
using BigRetail.Map.Sidewalks;
using BigRetail.Map.Unity.View;
using BigRetail.Map.View;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace BigRetail.Map.Unity.Sidewalks
{
    /// <summary>
    /// Synchronizes one dedicated runtime Tilemap with SidewalkState.
    ///
    /// SidewalkState remains authoritative. This component owns only the
    /// exact display cells that it places and never clears or overwrites an
    /// unowned Tilemap cell.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-50)]
    public sealed class SidewalkTilemapViewSystem : MonoBehaviour
    {
        [Header("Runtime Model")]

        [SerializeField]
        private SidewalkRuntimeHost sidewalkRuntimeHost;

        [Header("Tilemap Presentation")]

        [Tooltip(
            "A dedicated runtime Tilemap used only for constructed sidewalks. " +
            "It must be empty in the authored scene.")]
        [SerializeField]
        private Tilemap sidewalkTilemap;

        [SerializeField]
        private TileBase sidewalkTile;

        [SerializeField]
        private IsometricViewHost viewHost;

        [Header("Coordinate Mapping")]

        [SerializeField]
        private int logicalLevel = 0;

        [SerializeField]
        private int unityCellZ = 0;

        private readonly Dictionary<GridPosition, Vector3Int>
            ownedDisplayCells =
                new Dictionary<GridPosition, Vector3Int>();

        private SidewalkState subscribedSidewalkState;

        public int VisibleSidewalkCount =>
            ownedDisplayCells.Count;

        private void Awake()
        {
            if (!ValidateReferences()
                || !ValidateDedicatedTilemap())
            {
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (sidewalkRuntimeHost != null)
            {
                sidewalkRuntimeHost.Initialized +=
                    HandleSidewalkRuntimeInitialized;
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
            if (sidewalkRuntimeHost != null
                && sidewalkRuntimeHost.IsInitialized)
            {
                AttachToSidewalkState(
                    sidewalkRuntimeHost.SidewalkState);
            }
        }

        private void OnDisable()
        {
            if (sidewalkRuntimeHost != null)
            {
                sidewalkRuntimeHost.Initialized -=
                    HandleSidewalkRuntimeInitialized;
            }

            if (viewHost != null)
            {
                viewHost.OrientationChanging -=
                    HandleOrientationChanging;

                viewHost.OrientationChanged -=
                    HandleOrientationChanged;
            }

            DetachFromSidewalkState();
            ClearOwnedViews();
        }

        private void HandleSidewalkRuntimeInitialized(
            SidewalkRuntimeHost initializedHost)
        {
            AttachToSidewalkState(
                initializedHost.SidewalkState);
        }

        private void HandleOrientationChanging(
            IsometricViewOrientation previousOrientation,
            IsometricViewOrientation nextOrientation)
        {
            ClearOwnedViews();
        }

        private void HandleOrientationChanged(
            IsometricViewOrientation previousOrientation,
            IsometricViewOrientation currentOrientation)
        {
            RebuildAllViews();
        }

        private void AttachToSidewalkState(
            SidewalkState sidewalkState)
        {
            if (sidewalkState == null)
            {
                Debug.LogError(
                    "SidewalkTilemapViewSystem received a null " +
                    "SidewalkState.",
                    this);

                return;
            }

            if (subscribedSidewalkState == sidewalkState)
            {
                RebuildAllViews();
                return;
            }

            DetachFromSidewalkState();

            subscribedSidewalkState =
                sidewalkState;

            subscribedSidewalkState.SidewalkAdded +=
                HandleSidewalkAdded;

            subscribedSidewalkState.SidewalkRemoved +=
                HandleSidewalkRemoved;

            RebuildAllViews();
        }

        private void DetachFromSidewalkState()
        {
            if (subscribedSidewalkState == null)
            {
                return;
            }

            subscribedSidewalkState.SidewalkAdded -=
                HandleSidewalkAdded;

            subscribedSidewalkState.SidewalkRemoved -=
                HandleSidewalkRemoved;

            subscribedSidewalkState = null;
        }

        private void HandleSidewalkAdded(
            GridPosition cell)
        {
            ShowSidewalk(cell);
        }

        private void HandleSidewalkRemoved(
            GridPosition cell)
        {
            HideSidewalk(cell);
        }

        private void RebuildAllViews()
        {
            ClearOwnedViews();

            if (subscribedSidewalkState == null)
            {
                return;
            }

            foreach (
                GridPosition sidewalk
                in subscribedSidewalkState.EnumerateSidewalks())
            {
                ShowSidewalk(sidewalk);
            }
        }

        private void ShowSidewalk(
            GridPosition cell)
        {
            if (cell.Level != logicalLevel
                || ownedDisplayCells.ContainsKey(cell))
            {
                return;
            }

            Vector3Int displayCell =
                ToUnityCell(cell);

            if (sidewalkTilemap.HasTile(displayCell))
            {
                Debug.LogError(
                    $"SidewalkTilemapViewSystem refused to overwrite " +
                    $"an unowned tile at {displayCell}. Assign a dedicated " +
                    $"empty Sidewalk Tilemap.",
                    this);

                return;
            }

            sidewalkTilemap.SetTile(
                displayCell,
                sidewalkTile);

            ownedDisplayCells.Add(
                cell,
                displayCell);
        }

        private void HideSidewalk(
            GridPosition cell)
        {
            if (!ownedDisplayCells.TryGetValue(
                    cell,
                    out Vector3Int displayCell))
            {
                return;
            }

            sidewalkTilemap.SetTile(
                displayCell,
                null);

            ownedDisplayCells.Remove(cell);
        }

        private void ClearOwnedViews()
        {
            foreach (
                Vector3Int displayCell
                in ownedDisplayCells.Values)
            {
                sidewalkTilemap.SetTile(
                    displayCell,
                    null);
            }

            ownedDisplayCells.Clear();
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

            if (sidewalkRuntimeHost == null)
            {
                Debug.LogError(
                    "SidewalkTilemapViewSystem has no " +
                    "SidewalkRuntimeHost assigned.",
                    this);

                isValid = false;
            }

            if (sidewalkTilemap == null)
            {
                Debug.LogError(
                    "SidewalkTilemapViewSystem has no Sidewalk " +
                    "Tilemap assigned.",
                    this);

                isValid = false;
            }

            if (sidewalkTile == null)
            {
                Debug.LogError(
                    "SidewalkTilemapViewSystem has no Sidewalk " +
                    "Tile assigned.",
                    this);

                isValid = false;
            }

            if (viewHost == null)
            {
                Debug.LogError(
                    "SidewalkTilemapViewSystem has no " +
                    "IsometricViewHost assigned.",
                    this);

                isValid = false;
            }

            return isValid;
        }

        private bool ValidateDedicatedTilemap()
        {
            foreach (
                Vector3Int cell
                in sidewalkTilemap.cellBounds.allPositionsWithin)
            {
                if (!sidewalkTilemap.HasTile(cell))
                {
                    continue;
                }

                Debug.LogError(
                    "SidewalkTilemapViewSystem requires an empty, " +
                    "dedicated runtime Tilemap. The assigned Tilemap " +
                    $"already contains a tile at {cell}.",
                    this);

                return false;
            }

            return true;
        }

        private void Reset()
        {
            sidewalkTilemap =
                GetComponent<Tilemap>();
        }

        private void OnValidate()
        {
            if (sidewalkTilemap == null)
            {
                sidewalkTilemap =
                    GetComponent<Tilemap>();
            }
        }
    }
}
