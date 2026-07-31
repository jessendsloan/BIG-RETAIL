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
    /// Renders the derived one-cell apron surrounding FoundationState.
    ///
    /// The apron is presentation only. This component owns only the exact
    /// display cells it places and never clears or overwrites an unowned
    /// Tilemap cell.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-49)]
    public sealed class FoundationApronTilemapViewSystem : MonoBehaviour
    {
        [Header("Runtime Model")]

        [SerializeField]
        private FoundationRuntimeHost foundationRuntimeHost;

        [Header("Tilemap Presentation")]

        [Tooltip(
            "A dedicated runtime Tilemap used only for the derived Foundation " +
            "apron. It must be empty in the authored scene.")]
        [SerializeField]
        private Tilemap apronTilemap;

        [SerializeField]
        private TileBase apronTile;

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
        private GridMapDefinition subscribedMapDefinition;
        private bool isRebuildPending;

        public int VisibleApronCount =>
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
                    foundationRuntimeHost.FoundationState,
                    foundationRuntimeHost.MapDefinition);
            }
        }


        private void LateUpdate()
        {
            if (!isRebuildPending)
            {
                return;
            }

            isRebuildPending = false;
            RebuildAllViews();
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

            isRebuildPending = false;
            DetachFromFoundationState();
            ClearOwnedViews();
        }


        private void HandleFoundationRuntimeInitialized(
            FoundationRuntimeHost initializedHost)
        {
            AttachToFoundationState(
                initializedHost.FoundationState,
                initializedHost.MapDefinition);
        }


        private void HandleOrientationChanging(
            IsometricViewOrientation previousOrientation,
            IsometricViewOrientation nextOrientation)
        {
            isRebuildPending = false;
            ClearOwnedViews();
        }


        private void HandleOrientationChanged(
            IsometricViewOrientation previousOrientation,
            IsometricViewOrientation currentOrientation)
        {
            RebuildAllViews();
        }


        private void AttachToFoundationState(
            FoundationState foundationState,
            GridMapDefinition mapDefinition)
        {
            if (foundationState == null)
            {
                Debug.LogError(
                    "FoundationApronTilemapViewSystem received a null " +
                    "FoundationState.",
                    this);

                return;
            }

            if (mapDefinition == null)
            {
                Debug.LogError(
                    "FoundationApronTilemapViewSystem received a null " +
                    "GridMapDefinition.",
                    this);

                return;
            }

            if (subscribedFoundationState == foundationState
                && subscribedMapDefinition == mapDefinition)
            {
                RebuildAllViews();
                return;
            }

            DetachFromFoundationState();

            subscribedFoundationState =
                foundationState;

            subscribedMapDefinition =
                mapDefinition;

            subscribedFoundationState.FoundationAdded +=
                HandleFoundationChanged;

            subscribedFoundationState.FoundationRemoved +=
                HandleFoundationChanged;

            RebuildAllViews();
        }


        private void DetachFromFoundationState()
        {
            if (subscribedFoundationState != null)
            {
                subscribedFoundationState.FoundationAdded -=
                    HandleFoundationChanged;

                subscribedFoundationState.FoundationRemoved -=
                    HandleFoundationChanged;
            }

            subscribedFoundationState = null;
            subscribedMapDefinition = null;
        }


        private void HandleFoundationChanged(
            GridPosition cell)
        {
            isRebuildPending = true;
        }


        private void RebuildAllViews()
        {
            ClearOwnedViews();

            if (subscribedFoundationState == null
                || subscribedMapDefinition == null)
            {
                return;
            }

            IReadOnlyList<GridPosition> apron =
                FoundationApronResolver.Resolve(
                    subscribedMapDefinition,
                    subscribedFoundationState
                        .EnumerateFoundations());

            for (int index = 0;
                 index < apron.Count;
                 index++)
            {
                ShowApron(apron[index]);
            }
        }


        private void ShowApron(
            GridPosition cell)
        {
            if (cell.Level != logicalLevel
                || ownedDisplayCells.ContainsKey(cell))
            {
                return;
            }

            Vector3Int displayCell =
                ToUnityCell(cell);

            if (apronTilemap.HasTile(displayCell))
            {
                Debug.LogError(
                    $"FoundationApronTilemapViewSystem refused to overwrite " +
                    $"an unowned tile at {displayCell}. Assign a dedicated " +
                    $"empty Foundation Apron Tilemap.",
                    this);

                return;
            }

            apronTilemap.SetTile(
                displayCell,
                apronTile);

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
                apronTilemap.SetTile(
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
                    "FoundationApronTilemapViewSystem has no " +
                    "FoundationRuntimeHost assigned.",
                    this);

                isValid = false;
            }

            if (apronTilemap == null)
            {
                Debug.LogError(
                    "FoundationApronTilemapViewSystem has no Apron " +
                    "Tilemap assigned.",
                    this);

                isValid = false;
            }

            if (apronTile == null)
            {
                Debug.LogError(
                    "FoundationApronTilemapViewSystem has no Apron " +
                    "Tile assigned.",
                    this);

                isValid = false;
            }

            if (viewHost == null)
            {
                Debug.LogError(
                    "FoundationApronTilemapViewSystem has no " +
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
                in apronTilemap.cellBounds.allPositionsWithin)
            {
                if (!apronTilemap.HasTile(cell))
                {
                    continue;
                }

                Debug.LogError(
                    "FoundationApronTilemapViewSystem requires an empty, " +
                    "dedicated runtime Tilemap. The assigned Tilemap " +
                    $"already contains a tile at {cell}.",
                    this);

                return false;
            }

            return true;
        }


        private void Reset()
        {
            apronTilemap =
                GetComponent<Tilemap>();
        }


        private void OnValidate()
        {
            if (apronTilemap == null)
            {
                apronTilemap =
                    GetComponent<Tilemap>();
            }
        }
    }
}
