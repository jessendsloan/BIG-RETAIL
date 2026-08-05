using System.Collections.Generic;
using BigRetail.Construction.Unity.Cells;
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
    /// A neutral tile and centered pylon mean a new Foundation will be created.
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

        [Tooltip(
            "Use the same apron Tile displayed by the permanent " +
            "FoundationApronViews Tilemap.")]
        [SerializeField]
        private TileBase previewApronTile;

        [SerializeField]
        private IsometricViewHost viewHost;

        [Header("Preview Pylons")]

        [SerializeField]
        private TilePlacementPylonView pylonPrefab;

        [SerializeField]
        private TilePlacementPylonView apronPylonPrefab;

        [Tooltip(
            "Parent for pooled tile pylons. " +
            "When empty, this object's Transform is used.")]
        [SerializeField]
        private Transform pylonParent;

        [Header("Apron Preview")]

        [SerializeField]
        private Color apronColor =
            new Color(
                1f,
                1f,
                1f,
                0.5f);

        [Header("Preview Border")]

        [SerializeField]
        private Color borderColor =
            Color.white;

        [Min(0.001f)]
        [SerializeField]
        private float borderWidth =
            0.04f;

        [Header("Coordinate Mapping")]

        [SerializeField]
        private int logicalLevel = 0;

        [SerializeField]
        private int unityCellZ = 0;

        [Header("Preview Colors")]

        [SerializeField]
        private Color buildableColor =
            new Color(
                1f,
                1f,
                1f,
                0.5f);

        [SerializeField]
        private Color existingColor =
            new Color(
                0.25f,
                0.7f,
                1f,
                0.5f);

        [SerializeField]
        private Color invalidColor =
            new Color(
                1f,
                0.25f,
                0.25f,
                0.5f);

        private readonly Dictionary<GridPosition, Vector3Int>
            ownedDisplayCells =
                new Dictionary<GridPosition, Vector3Int>();

        private readonly Dictionary<GridPosition, Vector3Int>
            ownedApronDisplayCells =
                new Dictionary<GridPosition, Vector3Int>();

        private readonly HashSet<GridPosition>
            previewFoundationCells =
                new HashSet<GridPosition>();

        private TilePlacementPylonPool pylonPool;
        private TilePlacementPylonPool apronPylonPool;
        private TilePlacementBoundaryPool boundaryPool;

        public bool IsVisible =>
            ownedDisplayCells.Count > 0;

        public int VisibleCellCount =>
            ownedDisplayCells.Count;

        public int VisiblePylonCount =>
            pylonPool != null
                ? pylonPool.VisibleCount
                : 0;

        public int VisibleApronCount =>
            ownedApronDisplayCells.Count;

        public int VisibleApronPylonCount =>
            apronPylonPool != null
                ? apronPylonPool.VisibleCount
                : 0;

        public int VisibleBorderSegmentCount =>
            boundaryPool != null
                ? boundaryPool.VisibleCount
                : 0;

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

            if (pylonParent == null)
            {
                pylonParent = transform;
            }

            pylonPool =
                new TilePlacementPylonPool(
                    pylonPrefab,
                    pylonParent);

            apronPylonPool =
                new TilePlacementPylonPool(
                    apronPylonPrefab,
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
                bool previewsFoundation;

                if (foundationRuntimeHost.FoundationConstruction
                    .HasFoundation(cell))
                {
                    ExistingCellCount++;

                    previewsFoundation = true;

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

                        previewsFoundation = true;

                        previewColor =
                            buildableColor;
                    }
                    else
                    {
                        InvalidCellCount++;

                        previewsFoundation = false;

                        previewColor =
                            invalidColor;
                    }
                }

                if (ShowPreviewCell(
                        cell,
                        previewColor)
                    && previewsFoundation)
                {
                    previewFoundationCells.Add(cell);
                }
            }

            pylonPool.HideUnused(
                ownedDisplayCells.Count);

            ShowPreviewApron();

            boundaryPool.Show(
                CellAreaBoundaryResolver.Resolve(
                    ownedDisplayCells.Keys),
                previewTilemap,
                logicalLevel,
                unityCellZ,
                viewHost.Projection,
                borderColor,
                borderWidth);
        }


        public void Hide()
        {
            ClearOwnedViews();

            BuildableCellCount = 0;
            ExistingCellCount = 0;
            InvalidCellCount = 0;
        }


        private bool ShowPreviewCell(
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

                return false;
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

            int pylonIndex =
                ownedDisplayCells.Count - 1;

            pylonPool.Show(
                pylonIndex,
                cell,
                previewTilemap.GetCellCenterWorld(
                    displayCell),
                displayCell.x + displayCell.y,
                ToPylonColor(color));

            return true;
        }


        private void ShowPreviewApron()
        {
            IReadOnlyList<GridPosition> apron =
                FoundationApronPreviewResolver.Resolve(
                    foundationRuntimeHost.MapDefinition,
                    foundationRuntimeHost.FoundationState
                        .EnumerateFoundations(),
                    previewFoundationCells);

            for (int index = 0;
                 index < apron.Count;
                 index++)
            {
                ShowPreviewApronCell(
                    apron[index]);
            }

            apronPylonPool.HideUnused(
                ownedApronDisplayCells.Count);
        }


        private void ShowPreviewApronCell(
            GridPosition cell)
        {
            if (cell.Level != logicalLevel)
            {
                return;
            }

            // A requested cell's placement-status preview takes precedence.
            // This can occur when a dragged rectangle crosses the construction
            // boundary and a skipped cell would become final apron.
            if (ownedDisplayCells.ContainsKey(cell))
            {
                return;
            }

            Vector3Int displayCell =
                ToUnityCell(cell);

            if (previewTilemap.HasTile(displayCell))
            {
                Debug.LogError(
                    $"FoundationAreaPreviewView refused to overwrite " +
                    $"an occupied preview tile with apron at " +
                    $"{displayCell}.",
                    this);

                return;
            }

            previewTilemap.SetTile(
                displayCell,
                previewApronTile);

            previewTilemap.SetTileFlags(
                displayCell,
                TileFlags.None);

            previewTilemap.SetColor(
                displayCell,
                apronColor);

            ownedApronDisplayCells.Add(
                cell,
                displayCell);

            int pylonIndex =
                ownedApronDisplayCells.Count - 1;

            apronPylonPool.Show(
                pylonIndex,
                cell,
                previewTilemap.GetCellCenterWorld(
                    displayCell),
                displayCell.x + displayCell.y,
                ToPylonColor(apronColor));
        }


        private static Color ToPylonColor(
            Color previewColor)
        {
            return new Color(
                previewColor.r,
                previewColor.g,
                previewColor.b,
                1f);
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

            foreach (
                Vector3Int displayCell
                in ownedApronDisplayCells.Values)
            {
                previewTilemap.SetTile(
                    displayCell,
                    null);
            }

            ownedApronDisplayCells.Clear();
            previewFoundationCells.Clear();

            if (pylonPool != null)
            {
                pylonPool.HideAll();
            }

            if (apronPylonPool != null)
            {
                apronPylonPool.HideAll();
            }

            if (boundaryPool != null)
            {
                boundaryPool.HideAll();
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

            if (previewApronTile == null)
            {
                Debug.LogError(
                    "FoundationAreaPreviewView has no apron preview " +
                    "Tile assigned.",
                    this);

                isValid = false;
            }

            if (pylonPrefab == null)
            {
                Debug.LogError(
                    "FoundationAreaPreviewView has no tile-pylon " +
                    "prefab assigned.",
                    this);

                isValid = false;
            }
            else if (pylonPrefab.SharedMaterial == null)
            {
                Debug.LogError(
                    "FoundationAreaPreviewView's tile-pylon prefab " +
                    "has no shared Material for its border lines.",
                    this);

                isValid = false;
            }

            if (apronPylonPrefab == null)
            {
                Debug.LogError(
                    "FoundationAreaPreviewView has no apron-pylon " +
                    "prefab assigned.",
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
