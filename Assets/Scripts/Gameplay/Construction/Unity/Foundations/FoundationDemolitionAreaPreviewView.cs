using System.Collections.Generic;
using BigRetail.Map.Domain;
using BigRetail.Map.Unity.Foundations;
using BigRetail.Map.Unity.View;
using BigRetail.Map.View;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace BigRetail.Construction.Unity.Foundations
{
    /// <summary>
    /// Displays a temporary rectangular Foundation-demolition preview.
    ///
    /// Orange means an existing Foundation will be removed.
    /// Gray means the cell is already empty and will be skipped.
    ///
    /// This component owns only the exact cells it places on its dedicated
    /// preview Tilemap and never changes FoundationState.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FoundationDemolitionAreaPreviewView :
        MonoBehaviour
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
        private Color removableColor =
            new Color(
                1f,
                0.5f,
                0.08f,
                0.95f);

        [SerializeField]
        private Color emptyColor =
            new Color(
                0.42f,
                0.42f,
                0.42f,
                0.72f);


        private readonly Dictionary<GridPosition, Vector3Int>
            ownedDisplayCells =
                new Dictionary<GridPosition, Vector3Int>();


        public bool IsVisible =>
            ownedDisplayCells.Count > 0;

        public int VisibleCellCount =>
            ownedDisplayCells.Count;

        public int RemovableCellCount
        {
            get;
            private set;
        }

        public int EmptyCellCount
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

            RemovableCellCount = 0;
            EmptyCellCount = 0;

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

                bool hasFoundation =
                    foundationRuntimeHost.FoundationConstruction
                        .HasFoundation(cell);

                Color previewColor;

                if (hasFoundation)
                {
                    RemovableCellCount++;
                    previewColor = removableColor;
                }
                else
                {
                    EmptyCellCount++;
                    previewColor = emptyColor;
                }

                ShowPreviewCell(
                    cell,
                    previewColor);
            }
        }


        public void Hide()
        {
            ClearOwnedViews();

            RemovableCellCount = 0;
            EmptyCellCount = 0;
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
                    $"FoundationDemolitionAreaPreviewView refused to " +
                    $"overwrite an unowned tile at {displayCell}. Assign " +
                    $"a dedicated empty Foundation Demolition Preview " +
                    $"Tilemap.",
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
                    "FoundationDemolitionAreaPreviewView has no " +
                    "FoundationRuntimeHost assigned.",
                    this);

                isValid = false;
            }

            if (previewTilemap == null)
            {
                Debug.LogError(
                    "FoundationDemolitionAreaPreviewView has no preview " +
                    "Tilemap assigned.",
                    this);

                isValid = false;
            }

            if (previewTile == null)
            {
                Debug.LogError(
                    "FoundationDemolitionAreaPreviewView has no preview " +
                    "Tile assigned.",
                    this);

                isValid = false;
            }

            if (viewHost == null)
            {
                Debug.LogError(
                    "FoundationDemolitionAreaPreviewView has no " +
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
                    "FoundationDemolitionAreaPreviewView requires an " +
                    "empty, dedicated runtime Tilemap. The assigned " +
                    $"Tilemap already contains a tile at {cell}.",
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
