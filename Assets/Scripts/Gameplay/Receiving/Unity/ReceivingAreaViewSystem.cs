using System;
using System.Collections.Generic;
using BigRetail.Map.Domain;
using BigRetail.Map.Unity.View;
using BigRetail.Map.View;
using BigRetail.Receiving.Domain;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace BigRetail.Receiving.Unity
{
    /// <summary>
    /// Draws the Receiving Area management overlay and the current
    /// designation gesture without changing physical floor construction.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(140)]
    public sealed class ReceivingAreaViewSystem : MonoBehaviour
    {
        [SerializeField]
        private ReceivingAreaRuntimeHost runtimeHost;

        [SerializeField]
        private IsometricViewHost viewHost;

        [SerializeField]
        private Tilemap overlayTilemap;

        [SerializeField]
        private TilemapRenderer overlayRenderer;

        [SerializeField]
        private TileBase markerTile;

        [SerializeField]
        private int unityCellZ;

        [Header("Colors")]

        [SerializeField]
        private Color designatedColor =
            new Color(0.08f, 0.56f, 0.50f, 0.55f);

        [SerializeField]
        private Color occupiedColor =
            new Color(0.93f, 0.61f, 0.16f, 0.78f);

        [SerializeField]
        private Color validPreviewColor =
            new Color(0.13f, 0.76f, 0.60f, 0.78f);

        [SerializeField]
        private Color removalPreviewColor =
            new Color(0.93f, 0.31f, 0.22f, 0.76f);

        [SerializeField]
        private Color invalidColor =
            new Color(0.78f, 0.12f, 0.12f, 0.76f);


        private readonly HashSet<GridPosition> visibleCells =
            new HashSet<GridPosition>();
        private readonly List<GridPosition> previewCells =
            new List<GridPosition>();

        private bool managementVisible;
        private bool previewRemovesCells;


        public bool IsManagementVisible =>
            managementVisible;


        private void OnEnable()
        {
            if (runtimeHost != null)
            {
                runtimeHost.Initialized += HandleRuntimeInitialized;
            }

            if (viewHost != null)
            {
                viewHost.OrientationChanging += HandleOrientationChanging;
                viewHost.OrientationChanged += HandleOrientationChanged;
            }

            AttachState();
            RefreshRendererVisibility();
        }

        private void Start()
        {
            if (!ValidateReferences())
            {
                enabled = false;
                return;
            }

            AttachState();
            Rebuild();
        }

        public void SetManagementVisible(
            bool isVisible)
        {
            if (managementVisible == isVisible)
            {
                return;
            }

            managementVisible = isVisible;

            if (!managementVisible)
            {
                previewCells.Clear();
            }

            RefreshRendererVisibility();
            Rebuild();
        }

        public void ShowPreview(
            IReadOnlyList<GridPosition> cells,
            bool removesCells)
        {
            previewCells.Clear();

            if (cells != null)
            {
                for (int index = 0; index < cells.Count; index++)
                {
                    previewCells.Add(cells[index]);
                }
            }

            previewRemovesCells = removesCells;
            Rebuild();
        }

        public void ClearPreview()
        {
            if (previewCells.Count == 0)
            {
                return;
            }

            previewCells.Clear();
            Rebuild();
        }

        private void HandleRuntimeInitialized(
            ReceivingAreaRuntimeHost initializedHost)
        {
            AttachState();
            Rebuild();
        }

        private void HandleAreaChanged()
        {
            Rebuild();
        }

        private void HandleReservationsChanged()
        {
            Rebuild();
        }

        private void HandleOrientationChanging(
            IsometricViewOrientation previous,
            IsometricViewOrientation next)
        {
            ClearTiles();
        }

        private void HandleOrientationChanged(
            IsometricViewOrientation previous,
            IsometricViewOrientation current)
        {
            Rebuild();
        }

        private ReceivingAreaState subscribedState;

        private void AttachState()
        {
            ReceivingAreaState nextState =
                runtimeHost != null && runtimeHost.IsInitialized
                    ? runtimeHost.State
                    : null;

            if (subscribedState == nextState)
            {
                return;
            }

            DetachState();
            subscribedState = nextState;

            if (subscribedState != null)
            {
                subscribedState.AreaChanged += HandleAreaChanged;
                subscribedState.ReservationsChanged +=
                    HandleReservationsChanged;
            }
        }

        private void DetachState()
        {
            if (subscribedState == null)
            {
                return;
            }

            subscribedState.AreaChanged -= HandleAreaChanged;
            subscribedState.ReservationsChanged -=
                HandleReservationsChanged;
            subscribedState = null;
        }

        private void Rebuild()
        {
            ClearTiles();

            if (!managementVisible
                || runtimeHost == null
                || !runtimeHost.IsInitialized
                || runtimeHost.State == null
                || viewHost == null
                || !viewHost.IsInitialized)
            {
                return;
            }

            foreach (GridPosition cell in runtimeHost.State.EnumerateCells())
            {
                Color color = !runtimeHost.IsCellOperational(cell)
                    ? invalidColor
                    : runtimeHost.State.IsReserved(cell)
                        ? occupiedColor
                        : designatedColor;
                SetCell(cell, color);
            }

            for (int index = 0; index < previewCells.Count; index++)
            {
                GridPosition cell = previewCells[index];
                Color color;

                if (previewRemovesCells)
                {
                    color = runtimeHost.State.IsReserved(cell)
                        ? invalidColor
                        : removalPreviewColor;
                }
                else
                {
                    color = runtimeHost.Designations.EvaluateCell(cell)
                        == ReceivingAreaChangeFailure.None
                            ? validPreviewColor
                            : invalidColor;
                }

                SetCell(cell, color);
            }
        }

        private void SetCell(
            GridPosition logicalCell,
            Color color)
        {
            Vector3Int unityCell = ToUnityCell(logicalCell);
            overlayTilemap.SetTile(unityCell, markerTile);
            overlayTilemap.SetTileFlags(unityCell, TileFlags.None);
            overlayTilemap.SetColor(unityCell, color);
            visibleCells.Add(logicalCell);
        }

        private void ClearTiles()
        {
            foreach (GridPosition cell in visibleCells)
            {
                overlayTilemap?.SetTile(ToUnityCell(cell), null);
            }

            visibleCells.Clear();
        }

        private Vector3Int ToUnityCell(
            GridPosition logicalCell)
        {
            GridPosition displayCell =
                viewHost.Projection.ToDisplayCell(logicalCell);

            return new Vector3Int(
                displayCell.X,
                displayCell.Y,
                unityCellZ);
        }

        private void RefreshRendererVisibility()
        {
            if (overlayRenderer != null)
            {
                overlayRenderer.enabled = managementVisible;
            }
        }

        private bool ValidateReferences()
        {
            bool valid = runtimeHost != null
                && viewHost != null
                && overlayTilemap != null
                && overlayRenderer != null
                && markerTile != null;

            if (!valid)
            {
                Debug.LogError(
                    "ReceivingAreaViewSystem requires its runtime host, "
                    + "isometric view, overlay Tilemap/renderer, and marker "
                    + "Tile.",
                    this);
            }

            return valid;
        }

        private void OnDisable()
        {
            if (runtimeHost != null)
            {
                runtimeHost.Initialized -= HandleRuntimeInitialized;
            }

            if (viewHost != null)
            {
                viewHost.OrientationChanging -= HandleOrientationChanging;
                viewHost.OrientationChanged -= HandleOrientationChanged;
            }

            DetachState();
            ClearTiles();
        }
    }
}
