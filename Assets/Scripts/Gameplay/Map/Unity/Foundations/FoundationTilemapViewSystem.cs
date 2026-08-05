using System.Collections.Generic;
using BigRetail.Map.Domain;
using BigRetail.Map.Foundations;
using BigRetail.Map.Unity.View;
using BigRetail.Map.View;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace BigRetail.Map.Unity.Foundations
{
    /// <summary>
    /// Synchronizes one dedicated runtime Tilemap with FoundationState.
    ///
    /// FoundationState remains authoritative. This component owns only the
    /// exact display cells that it places and never clears or overwrites an
    /// unowned Tilemap cell.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-50)]
    public sealed class FoundationTilemapViewSystem : MonoBehaviour
    {
        [Header("Runtime Model")]

        [SerializeField]
        private FoundationRuntimeHost foundationRuntimeHost;

        [Header("Tilemap Presentation")]

        [Tooltip(
            "A dedicated runtime Tilemap used only for constructed foundations. " +
            "It must be empty in the authored scene.")]
        [SerializeField]
        private Tilemap foundationTilemap;

        [SerializeField]
        private TileBase foundationTile;

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

        private FoundationState subscribedFoundationState;

        public int VisibleFoundationCount =>
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
            if (foundationRuntimeHost != null)
            {
                foundationRuntimeHost.Initialized +=
                    HandleFoundationRuntimeInitialized;
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
            if (foundationRuntimeHost != null
                && foundationRuntimeHost.IsInitialized)
            {
                AttachToFoundationState(
                    foundationRuntimeHost.FoundationState);
            }
        }

        private void OnDisable()
        {
            if (foundationRuntimeHost != null)
            {
                foundationRuntimeHost.Initialized -=
                    HandleFoundationRuntimeInitialized;
            }

            if (viewHost != null)
            {
                viewHost.OrientationChanging -=
                    HandleOrientationChanging;

                viewHost.OrientationChanged -=
                    HandleOrientationChanged;
            }

            DetachFromFoundationState();
            ClearOwnedViews();
        }

        private void HandleFoundationRuntimeInitialized(
            FoundationRuntimeHost initializedHost)
        {
            AttachToFoundationState(
                initializedHost.FoundationState);
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

        private void AttachToFoundationState(
            FoundationState foundationState)
        {
            if (foundationState == null)
            {
                Debug.LogError(
                    "FoundationTilemapViewSystem received a null " +
                    "FoundationState.",
                    this);

                return;
            }

            if (subscribedFoundationState == foundationState)
            {
                RebuildAllViews();
                return;
            }

            DetachFromFoundationState();

            subscribedFoundationState =
                foundationState;

            subscribedFoundationState.FoundationAdded +=
                HandleFoundationAdded;

            subscribedFoundationState.FoundationRemoved +=
                HandleFoundationRemoved;

            RebuildAllViews();
        }

        private void DetachFromFoundationState()
        {
            if (subscribedFoundationState == null)
            {
                return;
            }

            subscribedFoundationState.FoundationAdded -=
                HandleFoundationAdded;

            subscribedFoundationState.FoundationRemoved -=
                HandleFoundationRemoved;

            subscribedFoundationState = null;
        }

        private void HandleFoundationAdded(
            GridPosition cell)
        {
            ShowFoundation(cell);
        }

        private void HandleFoundationRemoved(
            GridPosition cell)
        {
            HideFoundation(cell);
        }

        private void RebuildAllViews()
        {
            ClearOwnedViews();

            if (subscribedFoundationState == null)
            {
                return;
            }

            foreach (
                GridPosition foundation
                in subscribedFoundationState.EnumerateFoundations())
            {
                ShowFoundation(foundation);
            }
        }

        private void ShowFoundation(
            GridPosition cell)
        {
            if (cell.Level != logicalLevel
                || ownedDisplayCells.ContainsKey(cell))
            {
                return;
            }

            Vector3Int displayCell =
                ToUnityCell(cell);

            if (foundationTilemap.HasTile(displayCell))
            {
                Debug.LogError(
                    $"FoundationTilemapViewSystem refused to overwrite " +
                    $"an unowned tile at {displayCell}. Assign a dedicated " +
                    $"empty Foundation Tilemap.",
                    this);

                return;
            }

            foundationTilemap.SetTile(
                displayCell,
                foundationTile);

            ownedDisplayCells.Add(
                cell,
                displayCell);
        }

        private void HideFoundation(
            GridPosition cell)
        {
            if (!ownedDisplayCells.TryGetValue(
                    cell,
                    out Vector3Int displayCell))
            {
                return;
            }

            foundationTilemap.SetTile(
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
                foundationTilemap.SetTile(
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

            if (foundationRuntimeHost == null)
            {
                Debug.LogError(
                    "FoundationTilemapViewSystem has no " +
                    "FoundationRuntimeHost assigned.",
                    this);

                isValid = false;
            }

            if (foundationTilemap == null)
            {
                Debug.LogError(
                    "FoundationTilemapViewSystem has no Foundation " +
                    "Tilemap assigned.",
                    this);

                isValid = false;
            }

            if (foundationTile == null)
            {
                Debug.LogError(
                    "FoundationTilemapViewSystem has no Foundation " +
                    "Tile assigned.",
                    this);

                isValid = false;
            }

            if (viewHost == null)
            {
                Debug.LogError(
                    "FoundationTilemapViewSystem has no " +
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
                in foundationTilemap.cellBounds.allPositionsWithin)
            {
                if (!foundationTilemap.HasTile(cell))
                {
                    continue;
                }

                Debug.LogError(
                    "FoundationTilemapViewSystem requires an empty, " +
                    "dedicated runtime Tilemap. The assigned Tilemap " +
                    $"already contains a tile at {cell}.",
                    this);

                return false;
            }

            return true;
        }

        private void Reset()
        {
            foundationTilemap =
                GetComponent<Tilemap>();
        }

        private void OnValidate()
        {
            if (foundationTilemap == null)
            {
                foundationTilemap =
                    GetComponent<Tilemap>();
            }
        }
    }
}
