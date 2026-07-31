using System.Collections.Generic;
using BigRetail.Map.Domain;
using BigRetail.Map.Floors;
using BigRetail.Map.Unity.Floors;
using BigRetail.Map.Unity.View;
using BigRetail.Map.View;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace BigRetail.Construction.Unity.Floors
{
    /// <summary>
    /// Displays a temporary rectangular floor-construction preview.
    ///
    /// Green means a new floor will be created.
    /// Blue means a floor already exists.
    /// Red means the cell is invalid and will be skipped.
    ///
    /// The preview uses a dedicated Tilemap and never modifies
    /// FloorState or the permanent floor Tilemap.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FloorAreaPreviewView :
        MonoBehaviour
    {
        [Header("Runtime Model")]

        [SerializeField]
        private FloorRuntimeHost floorRuntimeHost;

        [SerializeField]
        private FloorFinishSelectionHost finishSelectionHost;


        [Header("Preview Tilemap")]

        [SerializeField]
        private Tilemap previewTilemap;

        [Tooltip(
            "Use the same legitimate floor Tile displayed by the " +
            "permanent Floor Views Tilemap.")]
        [SerializeField]
        private TileBase previewTile;

        [SerializeField]
        private IsometricViewHost viewHost;


        [Header("Coordinate Mapping")]

        [SerializeField]
        private int logicalLevel = 0;

        [SerializeField]
        private int unityCellZ = 0;


        [Header("Preview Colors")]

        [SerializeField]
        private Color buildableColor =
            new Color(
                0.35f,
                1f,
                0.35f,
                0.85f);

        [SerializeField]
        private Color existingColor =
            new Color(
                0.25f,
                0.7f,
                1f,
                0.9f);

        [SerializeField]
        private Color invalidColor =
            new Color(
                1f,
                0.25f,
                0.25f,
                0.88f);


        private readonly HashSet<GridPosition>
            visibleCells =
                new HashSet<GridPosition>();

        private readonly Dictionary<GridPosition, Color>
            visibleColors =
                new Dictionary<GridPosition, Color>();


        public bool IsVisible =>
            visibleCells.Count > 0;

        public int VisibleCellCount =>
            visibleCells.Count;

        public int BuildableCellCount
        {
            get;
            private set;
        }

        public int ExistingCellCount
        {
            get;
            private set;
        }

        public int InvalidCellCount
        {
            get;
            private set;
        }


        private void Awake()
        {
            if (!ValidateReferences())
            {
                enabled = false;
                return;
            }

            Hide();
        }


        private void OnEnable()
        {
            if (viewHost != null)
            {
                viewHost.OrientationChanging +=
                    HandleOrientationChanging;
            }

            if (finishSelectionHost != null)
            {
                finishSelectionHost.SelectedFinishChanged +=
                    HandleSelectedFinishChanged;
            }
        }


        public void ShowCell(
            GridPosition cell)
        {
            RectangularCellAreaPlanResult plan =
                RectangularCellAreaPlanner.Plan(
                    cell,
                    cell);

            ShowPlan(plan);
        }


        public void ShowPlan(
            RectangularCellAreaPlanResult plan)
        {
            if (!plan.Succeeded
                || !floorRuntimeHost.TryInitialize()
                || floorRuntimeHost.FloorConstruction == null)
            {
                Hide();
                return;
            }

            ClearVisibleCells();

            BuildableCellCount = 0;
            ExistingCellCount = 0;
            InvalidCellCount = 0;

            for (int index = 0;
                 index < plan.CellCount;
                 index++)
            {
                GridPosition cell =
                    plan.Cells[index];

                if (cell.Level != logicalLevel)
                {
                    continue;
                }

                Color previewColor;

                if (floorRuntimeHost.FloorConstruction
                    .HasFloor(cell))
                {
                    ExistingCellCount++;

                    previewColor =
                        existingColor;
                }
                else
                {
                    FloorChangeResult evaluation =
                        floorRuntimeHost.FloorConstruction
                            .EvaluatePlacement(cell);

                    if (evaluation.Succeeded)
                    {
                        BuildableCellCount++;

                        previewColor =
                            buildableColor;
                    }
                    else
                    {
                        InvalidCellCount++;

                        previewColor =
                            invalidColor;
                    }
                }

                ShowPreviewCell(
                    cell,
                    previewColor);
            }
        }


        public void Hide()
        {
            ClearVisibleCells();

            BuildableCellCount = 0;
            ExistingCellCount = 0;
            InvalidCellCount = 0;
        }


        private void ShowPreviewCell(
            GridPosition cell,
            Color color)
        {
            Vector3Int unityCell =
                ToUnityCell(cell);

            previewTilemap.SetTile(
                unityCell,
                GetSelectedPreviewTile());

            // Tile assets frequently lock their authored color.
            // Removing the per-cell flag allows this dedicated
            // preview Tilemap to apply its feedback tint.
            previewTilemap.SetTileFlags(
                unityCell,
                TileFlags.None);

            previewTilemap.SetColor(
                unityCell,
                color);

            visibleCells.Add(cell);
            visibleColors[cell] =
                color;
        }


        private void ClearVisibleCells()
        {
            foreach (
                GridPosition cell
                in visibleCells)
            {
                previewTilemap.SetTile(
                    ToUnityCell(cell),
                    null);
            }

            visibleCells.Clear();
            visibleColors.Clear();
        }


        private TileBase GetSelectedPreviewTile()
        {
            if (finishSelectionHost != null
                && finishSelectionHost.IsInitialized
                && finishSelectionHost.SelectedFinishAsset != null)
            {
                return finishSelectionHost
                    .SelectedFinishAsset
                    .FloorTile;
            }

            return previewTile;
        }


        private void HandleSelectedFinishChanged(
            FloorFinishId finishId)
        {
            TileBase selectedTile =
                GetSelectedPreviewTile();

            foreach (
                GridPosition cell
                in visibleCells)
            {
                Vector3Int unityCell =
                    ToUnityCell(cell);

                previewTilemap.SetTile(
                    unityCell,
                    selectedTile);

                previewTilemap.SetTileFlags(
                    unityCell,
                    TileFlags.None);

                previewTilemap.SetColor(
                    unityCell,
                    visibleColors[cell]);
            }
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
                    "FloorAreaPreviewView has no " +
                    "FloorRuntimeHost assigned.",
                    this);

                isValid = false;
            }

            if (previewTilemap == null)
            {
                Debug.LogError(
                    "FloorAreaPreviewView has no preview " +
                    "Tilemap assigned.",
                    this);

                isValid = false;
            }

            if (finishSelectionHost == null)
            {
                Debug.LogError(
                    "FloorAreaPreviewView has no "
                    + "FloorFinishSelectionHost assigned.",
                    this);

                isValid = false;
            }

            if (previewTile == null)
            {
                Debug.LogError(
                    "FloorAreaPreviewView has no preview Tile assigned.",
                    this);

                isValid = false;
            }

            if (viewHost == null)
            {
                Debug.LogError(
                    "FloorAreaPreviewView has no " +
                    "IsometricViewHost assigned.",
                    this);

                isValid = false;
            }

            return isValid;
        }


        private void Reset()
        {
            previewTilemap =
                GetComponent<Tilemap>();
        }


        private void OnValidate()
        {
            if (previewTilemap == null)
            {
                previewTilemap =
                    GetComponent<Tilemap>();
            }
        }


        private void OnDisable()
        {
            if (viewHost != null)
            {
                viewHost.OrientationChanging -=
                    HandleOrientationChanging;
            }

            if (finishSelectionHost != null)
            {
                finishSelectionHost.SelectedFinishChanged -=
                    HandleSelectedFinishChanged;
            }

            Hide();
        }


        private void HandleOrientationChanging(
            IsometricViewOrientation previousOrientation,
            IsometricViewOrientation nextOrientation)
        {
            Hide();
        }
    }
}
