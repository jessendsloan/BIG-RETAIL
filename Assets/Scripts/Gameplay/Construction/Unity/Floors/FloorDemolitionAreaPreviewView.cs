using System.Collections.Generic;
using BigRetail.Map.Domain;
using BigRetail.Map.Unity.Floors;
using BigRetail.Map.Unity.View;
using BigRetail.Map.View;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace BigRetail.Construction.Unity.Floors
{
    /// <summary>
    /// Displays a temporary rectangular floor-demolition preview.
    ///
    /// Orange means an existing floor will be removed.
    /// Gray means the cell is already empty and will be skipped.
    ///
    /// The preview uses a dedicated Tilemap and never modifies
    /// FloorState or the permanent floor Tilemap.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FloorDemolitionAreaPreviewView :
        MonoBehaviour
    {
        [Header("Runtime Model")]

        [SerializeField]
        private FloorRuntimeHost floorRuntimeHost;


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
        private Color removableColor =
            new Color(
                1f,
                0.5f,
                0.08f,
                0.95f);

        [SerializeField]
        private Color alreadyEmptyColor =
            new Color(
                0.55f,
                0.55f,
                0.55f,
                0.65f);


        private readonly HashSet<GridPosition>
            visibleCells =
                new HashSet<GridPosition>();


        public bool IsVisible =>
            visibleCells.Count > 0;

        public int VisibleCellCount =>
            visibleCells.Count;

        public int RemovableCellCount
        {
            get;
            private set;
        }

        public int AlreadyEmptyCellCount
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
                || !floorRuntimeHost.TryInitialize()
                || floorRuntimeHost.FloorConstruction == null)
            {
                Hide();
                return;
            }

            ClearVisibleCells();

            RemovableCellCount = 0;
            AlreadyEmptyCellCount = 0;

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

                bool hasFloor =
                    floorRuntimeHost.FloorConstruction
                        .HasFloor(cell);

                if (hasFloor)
                {
                    RemovableCellCount++;
                }
                else
                {
                    AlreadyEmptyCellCount++;
                }

                ShowPreviewCell(
                    cell,
                    hasFloor
                        ? removableColor
                        : alreadyEmptyColor);
            }
        }


        public void Hide()
        {
            ClearVisibleCells();

            RemovableCellCount = 0;
            AlreadyEmptyCellCount = 0;
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

            if (floorRuntimeHost == null)
            {
                Debug.LogError(
                    "FloorDemolitionAreaPreviewView has no " +
                    "FloorRuntimeHost assigned.",
                    this);

                isValid = false;
            }

            if (previewTilemap == null)
            {
                Debug.LogError(
                    "FloorDemolitionAreaPreviewView has no preview " +
                    "Tilemap assigned.",
                    this);

                isValid = false;
            }

            if (previewTile == null)
            {
                Debug.LogError(
                    "FloorDemolitionAreaPreviewView has no preview " +
                    "Tile assigned.",
                    this);

                isValid = false;
            }

            if (viewHost == null)
            {
                Debug.LogError(
                    "FloorDemolitionAreaPreviewView has no " +
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
