using System.Collections.Generic;
using BigRetail.Construction.Unity.Cells;
using BigRetail.Map.Domain;
using BigRetail.Map.Sidewalks;
using BigRetail.Map.Unity.Sidewalks;
using BigRetail.Map.Unity.View;
using BigRetail.Map.View;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace BigRetail.Construction.Unity.Sidewalks
{
    /// <summary>
    /// Shows a sidewalk plan on a dedicated preview tilemap. The tile can be
    /// the same apron art used around foundations without sharing apron state.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SidewalkAreaPreviewView : MonoBehaviour
    {
        [Header("Runtime Model")]

        [SerializeField]
        private SidewalkRuntimeHost sidewalkRuntimeHost;


        [Header("Preview Tilemap")]

        [SerializeField]
        private Tilemap previewTilemap;

        [SerializeField]
        private TileBase previewTile;

        [SerializeField]
        private IsometricViewHost viewHost;


        [Header("Preview Pylons")]

        [SerializeField]
        private TilePlacementPylonView pylonPrefab;

        [SerializeField]
        private Transform pylonParent;


        [Header("Preview Border")]

        [SerializeField]
        private Color borderColor = Color.white;

        [Min(0.001f)]
        [SerializeField]
        private float borderWidth = 0.04f;


        [Header("Coordinate Mapping")]

        [SerializeField]
        private int logicalLevel;

        [SerializeField]
        private int unityCellZ;


        [Header("Preview Colors")]

        [SerializeField]
        private Color buildableColor =
            new Color(1f, 1f, 1f, 0.62f);

        [SerializeField]
        private Color existingColor =
            new Color(0.25f, 0.7f, 1f, 0.62f);

        [SerializeField]
        private Color invalidColor =
            new Color(1f, 0.25f, 0.25f, 0.62f);


        private readonly HashSet<GridPosition> visibleCells =
            new HashSet<GridPosition>();

        private TilePlacementPylonPool pylonPool;

        private TilePlacementBoundaryPool boundaryPool;


        public bool IsVisible => visibleCells.Count > 0;

        public int VisibleCellCount => visibleCells.Count;

        public int BuildableCellCount { get; private set; }

        public int ExistingCellCount { get; private set; }

        public int InvalidCellCount { get; private set; }


        private void Awake()
        {
            if (!ValidateReferences())
            {
                enabled = false;
                return;
            }

            if (pylonParent == null)
            {
                pylonParent = transform;
            }

            pylonPool =
                new TilePlacementPylonPool(
                    pylonPrefab,
                    pylonParent);

            boundaryPool =
                new TilePlacementBoundaryPool(
                    pylonParent,
                    pylonPrefab.SharedMaterial);

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


        public void ShowCell(GridPosition cell)
        {
            ShowPlan(
                RectangularCellAreaPlanner.Plan(cell, cell));
        }


        public void ShowPlan(RectangularCellAreaPlanResult plan)
        {
            if (!plan.Succeeded
                || !sidewalkRuntimeHost.TryInitialize()
                || sidewalkRuntimeHost.SidewalkConstruction == null)
            {
                Hide();
                return;
            }

            ClearVisibleCells();
            BuildableCellCount = 0;
            ExistingCellCount = 0;
            InvalidCellCount = 0;

            for (int index = 0; index < plan.CellCount; index++)
            {
                GridPosition cell = plan.Cells[index];

                if (cell.Level != logicalLevel)
                {
                    continue;
                }

                Color color;

                if (sidewalkRuntimeHost.SidewalkConstruction
                    .HasSidewalk(cell))
                {
                    ExistingCellCount++;
                    color = existingColor;
                }
                else
                {
                    SidewalkChangeResult evaluation =
                        sidewalkRuntimeHost.SidewalkConstruction
                            .EvaluatePlacement(cell);

                    if (evaluation.Succeeded)
                    {
                        BuildableCellCount++;
                        color = buildableColor;
                    }
                    else
                    {
                        InvalidCellCount++;
                        color = invalidColor;
                    }
                }

                ShowPreviewCell(cell, color);
            }

            pylonPool.HideUnused(visibleCells.Count);

            boundaryPool.Show(
                CellAreaBoundaryResolver.Resolve(visibleCells),
                previewTilemap,
                logicalLevel,
                unityCellZ,
                viewHost.Projection,
                borderColor,
                borderWidth);
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
            Vector3Int unityCell = ToUnityCell(cell);

            previewTilemap.SetTile(unityCell, previewTile);
            previewTilemap.SetTileFlags(unityCell, TileFlags.None);
            previewTilemap.SetColor(unityCell, color);

            visibleCells.Add(cell);

            pylonPool.Show(
                visibleCells.Count - 1,
                cell,
                previewTilemap.GetCellCenterWorld(unityCell),
                unityCell.x + unityCell.y,
                new Color(color.r, color.g, color.b, 1f));
        }


        private void ClearVisibleCells()
        {
            foreach (GridPosition cell in visibleCells)
            {
                previewTilemap.SetTile(ToUnityCell(cell), null);
            }

            visibleCells.Clear();

            pylonPool?.HideAll();
            boundaryPool?.HideAll();
        }


        private Vector3Int ToUnityCell(GridPosition cell)
        {
            GridPosition displayCell =
                viewHost.Projection.ToDisplayCell(cell);

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
                    "SidewalkAreaPreviewView has no " +
                    "SidewalkRuntimeHost assigned.",
                    this);
                isValid = false;
            }

            if (previewTilemap == null)
            {
                Debug.LogError(
                    "SidewalkAreaPreviewView has no preview Tilemap assigned.",
                    this);
                isValid = false;
            }

            if (previewTile == null)
            {
                Debug.LogError(
                    "SidewalkAreaPreviewView has no preview Tile assigned.",
                    this);
                isValid = false;
            }

            if (viewHost == null)
            {
                Debug.LogError(
                    "SidewalkAreaPreviewView has no IsometricViewHost assigned.",
                    this);
                isValid = false;
            }

            if (pylonPrefab == null
                || pylonPrefab.SharedMaterial == null)
            {
                Debug.LogError(
                    "SidewalkAreaPreviewView requires a pylon prefab with " +
                    "a shared material.",
                    this);
                isValid = false;
            }

            return isValid;
        }


        private void Reset()
        {
            previewTilemap = GetComponent<Tilemap>();
        }


        private void OnValidate()
        {
            if (previewTilemap == null)
            {
                previewTilemap = GetComponent<Tilemap>();
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
