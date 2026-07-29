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
    /// Keeps a dedicated Unity Tilemap synchronized with model-owned
    /// FoundationState.
    ///
    /// FoundationState remains authoritative. This component performs
    /// presentation only and rebuilds from canonical state after rotation.
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
            "Dedicated runtime Tilemap used only for constructed foundations.")]
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

        private readonly HashSet<GridPosition>
            visibleFoundations =
                new HashSet<GridPosition>();

        private FoundationState subscribedFoundationState;

        public int VisibleFoundationCount =>
            visibleFoundations.Count;

        private void Awake()
        {
            if (!ValidateReferences())
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
            ClearAllViews();
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
            ClearAllViews();
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

            subscribedFoundationState =
                null;
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
            ClearAllViews();

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
                || visibleFoundations.Contains(cell))
            {
                return;
            }

            foundationTilemap.SetTile(
                ToUnityCell(cell),
                foundationTile);

            visibleFoundations.Add(cell);
        }

        private void HideFoundation(
            GridPosition cell)
        {
            if (!visibleFoundations.Remove(cell))
            {
                return;
            }

            foundationTilemap.SetTile(
                ToUnityCell(cell),
                null);
        }

        /// <summary>
        /// Clears only cells placed and tracked by this view system.
        /// It deliberately avoids ClearAllTiles so an incorrect Inspector
        /// assignment cannot wipe an authored Tilemap.
        /// </summary>
        private void ClearAllViews()
        {
            foreach (
                GridPosition cell
                in visibleFoundations)
            {
                foundationTilemap.SetTile(
                    ToUnityCell(cell),
                    null);
            }

            visibleFoundations.Clear();
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
                    "FoundationTilemapViewSystem has no foundation " +
                    "Tilemap assigned.",
                    this);

                isValid = false;
            }

            if (foundationTile == null)
            {
                Debug.LogError(
                    "FoundationTilemapViewSystem has no foundation " +
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
