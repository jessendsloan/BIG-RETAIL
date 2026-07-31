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
    /// Displays a temporary rectangular Foundation-construction preview.
    ///
    /// Green means a new Foundation will be created.
    /// Blue means a Foundation already exists.
    /// Red means the cell is invalid and will be skipped.
    ///
    /// This component owns only the exact cells it places on its dedicated
    /// preview Tilemap and never changes FoundationState.
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

        private readonly Dictionary<GridPosition, Vector3Int>
            ownedDisplayCells =
                new Dictionary<GridPosition, Vector3Int>();

        public bool IsVisible =>
            ownedDisplayCells.Count > 0;

        public int VisibleCellCount =>
            ownedDisplayCells.Count;

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
            if (!ValidateReferences()
                || !ValidateDedicatedTilemap())
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

            ClearOwnedViews();

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
            ClearOwnedViews();

            BuildableCellCount = 0;
            ExistingCellCount = 0;
            InvalidCellCount = 0;
        }


        private void ShowPreviewCell(
            GridPosition cell,
            Color color)
        {
            Vector3Int displayCell =
                ToUnityCell(cell);

            if (previewTilemap.HasTile(displayCell))
            {
                Debug.LogError(
                    $"FoundationAreaPreviewView refused to overwrite " +
                    $"an unowned tile at {displayCell}. Assign a dedicated " +
                    $"empty Foundation Preview Tilemap.",
                    this);

                return;
            }

            previewTilemap.SetTile(
                displayCell,
                previewTile);

            previewTilemap.SetTileFlags(
                displayCell,
                TileFlags.None);

            previewTilemap.SetColor(
                displayCell,
                color);

            ownedDisplayCells.Add(
                cell,
                displayCell);
        }


        private void ClearOwnedViews()
        {
            foreach (
                Vector3Int displayCell
                in ownedDisplayCells.Values)
            {
                previewTilemap.SetTile(
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


        private bool ValidateDedicatedTilemap()
        {
            foreach (
                Vector3Int cell
                in previewTilemap.cellBounds.allPositionsWithin)
            {
                if (!previewTilemap.HasTile(cell))
                {
                    continue;
                }

                Debug.LogError(
                    "FoundationAreaPreviewView requires an empty, " +
                    "dedicated runtime Tilemap. The assigned Tilemap " +
                    $"already contains a tile at {cell}.",
                    this);

                return false;
            }

            return true;
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
