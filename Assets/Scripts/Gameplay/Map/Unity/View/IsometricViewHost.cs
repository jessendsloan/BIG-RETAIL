using System;
using System.Collections.Generic;
using BigRetail.Map.Domain;
using BigRetail.Map.View;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace BigRetail.Map.Unity.View
{
    /// <summary>
    /// Owns the active isometric presentation state for one runtime map.
    ///
    /// Logical map data never rotates. This host re-renders authored
    /// Tilemap layers from canonical snapshots and publishes one shared
    /// projection to every runtime presentation and targeting system.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-80)]
    public sealed class IsometricViewHost : MonoBehaviour
    {
        [Header("Runtime Map")]

        [SerializeField]
        private GridMapHost mapHost;

        [SerializeField]
        private int logicalLevel = 0;

        [SerializeField]
        private int unityCellZ = 0;


        [Header("Starting View")]

        [SerializeField]
        private IsometricViewOrientation startingOrientation =
            IsometricViewOrientation.North;


        [Header("Authored Presentation")]

        [Tooltip(
            "Authored Tilemaps whose initial cells are canonical logical " +
            "coordinates. MapVisuals belongs here. Runtime floor and " +
            "preview Tilemaps rebuild through their own view systems.")]
        [SerializeField]
        private Tilemap[] authoredTilemaps =
            Array.Empty<Tilemap>();


        private readonly List<AuthoredTilemapSnapshot>
            authoredSnapshots =
                new List<AuthoredTilemapSnapshot>();


        public bool IsInitialized { get; private set; }

        public IsometricViewProjection Projection
        {
            get;
            private set;
        }

        public IsometricViewOrientation Orientation =>
            Projection != null
                ? Projection.Orientation
                : startingOrientation;


        public event Action<
            IsometricViewOrientation,
            IsometricViewOrientation>
            OrientationChanging;

        public event Action<
            IsometricViewOrientation,
            IsometricViewOrientation>
            OrientationChanged;


        private void OnEnable()
        {
            if (mapHost != null)
            {
                mapHost.Initialized +=
                    HandleMapInitialized;
            }
        }


        private void Awake()
        {
            if (!ValidateReferences())
            {
                enabled = false;
                return;
            }

            TryInitialize();
        }


        private void Start()
        {
            if (!TryInitialize())
            {
                Debug.LogError(
                    "IsometricViewHost could not initialize because " +
                    "GridMapHost has not produced its runtime map.",
                    this);

                enabled = false;
            }
        }


        public bool TryInitialize()
        {
            if (IsInitialized)
            {
                return true;
            }

            if (mapHost == null
                || !mapHost.IsInitialized
                || mapHost.MapDefinition == null)
            {
                return false;
            }

            IsometricMapFootprint footprint =
                IsometricMapFootprint.FromMapDefinition(
                    mapHost.MapDefinition,
                    logicalLevel);

            Projection =
                new IsometricViewProjection(
                    footprint,
                    startingOrientation);

            CaptureAuthoredTilemaps();

            IsInitialized = true;

            if (startingOrientation
                != IsometricViewOrientation.North)
            {
                RenderAuthoredTilemaps();
            }

            Debug.Log(
                $"Activated isometric view for map " +
                $"'{mapHost.MapDefinition.MapId}'. " +
                $"Orientation: {Orientation}. " +
                $"Footprint: {footprint.Width} x " +
                $"{footprint.Height}.",
                this);

            return true;
        }


        public bool RotateClockwise()
        {
            return TrySetOrientation(
                Orientation.RotateClockwise());
        }


        public bool RotateCounterClockwise()
        {
            return TrySetOrientation(
                Orientation.RotateCounterClockwise());
        }


        public bool TrySetOrientation(
            IsometricViewOrientation orientation)
        {
            if (!TryInitialize()
                || orientation == Orientation)
            {
                return false;
            }

            IsometricViewOrientation previousOrientation =
                Orientation;

            IsometricViewProjection nextProjection =
                Projection.WithOrientation(
                    orientation);

            OrientationChanging?.Invoke(
                previousOrientation,
                orientation);

            Projection =
                nextProjection;

            RenderAuthoredTilemaps();

            OrientationChanged?.Invoke(
                previousOrientation,
                orientation);

            return true;
        }


        public Vector3Int ToUnityCell(
            GridPosition logicalCell)
        {
            EnsureInitialized();

            GridPosition displayCell =
                Projection.ToDisplayCell(
                    logicalCell);

            return new Vector3Int(
                displayCell.X,
                displayCell.Y,
                unityCellZ);
        }


        public GridPosition ToLogicalCell(
            Vector3Int unityCell)
        {
            EnsureInitialized();

            return Projection.ToLogicalCell(
                new GridPosition(
                    unityCell.x,
                    unityCell.y,
                    logicalLevel));
        }


        public GridPosition WorldToLogicalCell(
            Vector3 worldPosition,
            Tilemap coordinateTilemap)
        {
            if (coordinateTilemap == null)
            {
                throw new ArgumentNullException(
                    nameof(coordinateTilemap));
            }

            worldPosition.z =
                coordinateTilemap.transform.position.z;

            Vector3Int displayCell =
                coordinateTilemap.WorldToCell(
                    worldPosition);

            return ToLogicalCell(
                displayCell);
        }


        public Vector3 GetLogicalCellCenterWorld(
            GridPosition logicalCell,
            Tilemap coordinateTilemap)
        {
            if (coordinateTilemap == null)
            {
                throw new ArgumentNullException(
                    nameof(coordinateTilemap));
            }

            return coordinateTilemap.GetCellCenterWorld(
                ToUnityCell(
                    logicalCell));
        }


        public Bounds CalculateProjectedWorldBounds(
            Tilemap coordinateTilemap)
        {
            if (coordinateTilemap == null)
            {
                throw new ArgumentNullException(
                    nameof(coordinateTilemap));
            }

            EnsureInitialized();

            int minimumX =
                Projection.DisplayMinimumX;

            int minimumY =
                Projection.DisplayMinimumY;

            int maximumX =
                Projection.DisplayMaximumX;

            int maximumY =
                Projection.DisplayMaximumY;

            Vector3 firstCenter =
                coordinateTilemap.GetCellCenterWorld(
                    new Vector3Int(
                        minimumX,
                        minimumY,
                        unityCellZ));

            Bounds bounds =
                new Bounds(
                    firstCenter,
                    Vector3.zero);

            EncapsulateCellCenter(
                ref bounds,
                coordinateTilemap,
                maximumX,
                minimumY,
                unityCellZ);

            EncapsulateCellCenter(
                ref bounds,
                coordinateTilemap,
                minimumX,
                maximumY,
                unityCellZ);

            EncapsulateCellCenter(
                ref bounds,
                coordinateTilemap,
                maximumX,
                maximumY,
                unityCellZ);

            Grid layoutGrid =
                coordinateTilemap.layoutGrid;

            if (layoutGrid != null)
            {
                Vector3 cellSize =
                    layoutGrid.cellSize;

                bounds.Expand(
                    new Vector3(
                        Mathf.Abs(cellSize.x),
                        Mathf.Abs(cellSize.y),
                        0f));
            }

            return bounds;
        }


        private static void EncapsulateCellCenter(
            ref Bounds bounds,
            Tilemap coordinateTilemap,
            int x,
            int y,
            int unityCellZ)
        {
            bounds.Encapsulate(
                coordinateTilemap.GetCellCenterWorld(
                    new Vector3Int(
                        x,
                        y,
                        unityCellZ)));
        }


        private void CaptureAuthoredTilemaps()
        {
            authoredSnapshots.Clear();

            for (int index = 0;
                 index < authoredTilemaps.Length;
                 index++)
            {
                Tilemap tilemap =
                    authoredTilemaps[index];

                if (tilemap == null)
                {
                    continue;
                }

                authoredSnapshots.Add(
                    new AuthoredTilemapSnapshot(
                        tilemap,
                        logicalLevel,
                        unityCellZ));
            }
        }


        private void RenderAuthoredTilemaps()
        {
            for (int index = 0;
                 index < authoredSnapshots.Count;
                 index++)
            {
                authoredSnapshots[index].Render(
                    Projection);
            }
        }


        private void HandleMapInitialized(
            GridMapHost initializedHost)
        {
            TryInitialize();
        }


        private void EnsureInitialized()
        {
            if (TryInitialize())
            {
                return;
            }

            throw new InvalidOperationException(
                "The isometric view has not been initialized.");
        }


        private bool ValidateReferences()
        {
            bool isValid = true;

            if (mapHost == null)
            {
                Debug.LogError(
                    "IsometricViewHost has no GridMapHost assigned.",
                    this);

                isValid = false;
            }

            if (authoredTilemaps == null
                || authoredTilemaps.Length == 0)
            {
                Debug.LogError(
                    "IsometricViewHost requires at least one authored " +
                    "Tilemap. Assign MapVisuals.",
                    this);

                isValid = false;
            }

            return isValid;
        }


        private void OnDisable()
        {
            if (mapHost != null)
            {
                mapHost.Initialized -=
                    HandleMapInitialized;
            }
        }


        private sealed class AuthoredTilemapSnapshot
        {
            private readonly Tilemap tilemap;

            private readonly List<AuthoredTile>
                canonicalTiles =
                    new List<AuthoredTile>();

            private readonly HashSet<Vector3Int>
                renderedCells =
                    new HashSet<Vector3Int>();

            public AuthoredTilemapSnapshot(
                Tilemap tilemap,
                int logicalLevel,
                int unityCellZ)
            {
                this.tilemap =
                    tilemap
                    ?? throw new ArgumentNullException(
                        nameof(tilemap));

                foreach (
                    Vector3Int unityCell
                    in tilemap.cellBounds.allPositionsWithin)
                {
                    if (unityCell.z != unityCellZ)
                    {
                        continue;
                    }

                    TileBase tile =
                        tilemap.GetTile(
                            unityCell);

                    if (tile == null)
                    {
                        continue;
                    }

                    canonicalTiles.Add(
                        new AuthoredTile(
                            new GridPosition(
                                unityCell.x,
                                unityCell.y,
                                logicalLevel),
                            unityCell.z,
                            tile,
                            tilemap.GetColor(
                                unityCell),
                            tilemap.GetTransformMatrix(
                                unityCell),
                            tilemap.GetTileFlags(
                                unityCell)));

                    renderedCells.Add(
                        unityCell);
                }
            }

            public void Render(
                IsometricViewProjection projection)
            {
                foreach (
                    Vector3Int renderedCell
                    in renderedCells)
                {
                    tilemap.SetTile(
                        renderedCell,
                        null);
                }

                renderedCells.Clear();

                for (int index = 0;
                     index < canonicalTiles.Count;
                     index++)
                {
                    AuthoredTile authoredTile =
                        canonicalTiles[index];

                    GridPosition displayCell =
                        projection.ToDisplayCell(
                            authoredTile.LogicalCell);

                    Vector3Int unityCell =
                        new Vector3Int(
                            displayCell.X,
                            displayCell.Y,
                            authoredTile.UnityCellZ);

                    tilemap.SetTile(
                        unityCell,
                        authoredTile.Tile);

                    tilemap.SetTileFlags(
                        unityCell,
                        TileFlags.None);

                    tilemap.SetColor(
                        unityCell,
                        authoredTile.Color);

                    tilemap.SetTransformMatrix(
                        unityCell,
                        authoredTile.Transform);

                    tilemap.SetTileFlags(
                        unityCell,
                        authoredTile.Flags);

                    renderedCells.Add(
                        unityCell);
                }
            }
        }


        private readonly struct AuthoredTile
        {
            public GridPosition LogicalCell { get; }
            public int UnityCellZ { get; }
            public TileBase Tile { get; }
            public Color Color { get; }
            public Matrix4x4 Transform { get; }
            public TileFlags Flags { get; }

            public AuthoredTile(
                GridPosition logicalCell,
                int unityCellZ,
                TileBase tile,
                Color color,
                Matrix4x4 transform,
                TileFlags flags)
            {
                LogicalCell = logicalCell;
                UnityCellZ = unityCellZ;
                Tile = tile;
                Color = color;
                Transform = transform;
                Flags = flags;
            }
        }
    }
}
