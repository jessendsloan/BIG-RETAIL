using System.Collections.Generic;
using BigRetail.Map.Domain;
using BigRetail.Map.Foundations;
using BigRetail.Map.Unity.Foundations;
using BigRetail.Map.Unity.View;
using BigRetail.Map.View;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace BigRetail.Construction.Unity.Foundations
{
    /// <summary>
    /// Displays a temporary rectangular foundation-construction preview.
    ///
    /// Green means a new foundation will be created.
    /// Blue means a foundation already exists.
    /// Red means the cell is invalid and will be skipped.
    ///
    /// The preview uses a dedicated Tilemap and never modifies
    /// FoundationState or the permanent foundation Tilemap.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FoundationAreaPreviewView : MonoBehaviour
    {
        [Header("Runtime Model")]

        [SerializeField]
        private FoundationRuntimeHost foundationRuntimeHost;


        [Header("Preview Tilemap")]

        [SerializeField]
        private Tilemap previewTilemap;

        [Tooltip(
            "Use the same legitimate Foundation Tile displayed by the " +
            "permanent FoundationViews Tilemap.")]
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
                || !foundationRuntimeHost.TryInitialize()
                || foundationRuntimeHost.FoundationConstruction == null)
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
                if (foundationRuntimeHost.FoundationConstruction
                    .HasFoundation(cell))
                {
                    ExistingCellCount++;

                    previewColor =
                        existingColor;
                }
                else
                {
                    FoundationChangeResult evaluation =
                        foundationRuntimeHost.FoundationConstruction
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
                previewTile);

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
                    "FoundationAreaPreviewView has no " +
                    "FoundationRuntimeHost assigned.",
                    this);

                isValid = false;
            }

            if (previewTilemap == null)
            {
                Debug.LogError(
                    "FoundationAreaPreviewView has no preview " +
                    "Tilemap assigned.",
                    this);

                isValid = false;
            }

            if (previewTile == null)
            {
                Debug.LogError(
                    "FoundationAreaPreviewView has no preview Tile assigned.",
                    this);

                isValid = false;
            }

            if (viewHost == null)
            {
                Debug.LogError(
                    "FoundationAreaPreviewView has no " +
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
