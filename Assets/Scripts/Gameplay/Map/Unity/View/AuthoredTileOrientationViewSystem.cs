using System;
using System.Collections.Generic;
using BigRetail.Map.Domain;
using BigRetail.Map.View;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace BigRetail.Map.Unity.View
{
    /// <summary>
    /// Reprojects directional artwork inside authored Tilemap cells after
    /// IsometricViewHost has moved those cells to a new view orientation.
    ///
    /// The host remains the authority for cell placement. This system owns
    /// only the per-cell visual transform for explicitly assigned authored
    /// layers such as MapVisuals.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-70)]
    public sealed class AuthoredTileOrientationViewSystem : MonoBehaviour
    {
        [Header("Runtime View")]

        [SerializeField]
        private IsometricViewHost viewHost;

        [Tooltip(
            "Authored Tilemaps containing directional artwork that must " +
            "turn inside each projected cell. Assign MapVisuals. Solid " +
            "mask Tilemaps do not need to be included.")]
        [SerializeField]
        private Tilemap[] directionalTilemaps =
            Array.Empty<Tilemap>();

        [SerializeField]
        private int unityCellZ = 0;


        private readonly List<TilemapTransformSnapshot>
            snapshots =
                new List<TilemapTransformSnapshot>();

        private bool isInitialized;


        private void OnEnable()
        {
            if (viewHost != null)
            {
                viewHost.OrientationChanged +=
                    HandleOrientationChanged;
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
                    "AuthoredTileOrientationViewSystem could not " +
                    "initialize because IsometricViewHost is not ready.",
                    this);

                enabled = false;
            }
        }


        private bool TryInitialize()
        {
            if (isInitialized)
            {
                return true;
            }

            if (viewHost == null
                || !viewHost.TryInitialize())
            {
                return false;
            }

            CaptureSnapshots();
            ApplyOrientation(
                viewHost.Orientation);

            isInitialized = true;
            return true;
        }


        private void CaptureSnapshots()
        {
            snapshots.Clear();

            HashSet<Tilemap> capturedTilemaps =
                new HashSet<Tilemap>();

            for (int index = 0;
                 index < directionalTilemaps.Length;
                 index++)
            {
                Tilemap tilemap =
                    directionalTilemaps[index];

                if (tilemap == null
                    || !capturedTilemaps.Add(
                        tilemap))
                {
                    continue;
                }

                snapshots.Add(
                    new TilemapTransformSnapshot(
                        tilemap,
                        viewHost,
                        unityCellZ));
            }
        }


        private void HandleOrientationChanged(
            IsometricViewOrientation previousOrientation,
            IsometricViewOrientation currentOrientation)
        {
            if (!TryInitialize())
            {
                return;
            }

            ApplyOrientation(
                currentOrientation);
        }


        private void ApplyOrientation(
            IsometricViewOrientation orientation)
        {
            for (int index = 0;
                 index < snapshots.Count;
                 index++)
            {
                snapshots[index].Apply(
                    viewHost,
                    orientation);
            }
        }


        private bool ValidateReferences()
        {
            bool isValid = true;

            if (viewHost == null)
            {
                Debug.LogError(
                    "AuthoredTileOrientationViewSystem has no " +
                    "IsometricViewHost assigned.",
                    this);

                isValid = false;
            }

            if (directionalTilemaps == null
                || directionalTilemaps.Length == 0)
            {
                Debug.LogError(
                    "AuthoredTileOrientationViewSystem requires at " +
                    "least one directional authored Tilemap. Assign " +
                    "MapVisuals.",
                    this);

                isValid = false;
            }

            return isValid;
        }


        private void OnDisable()
        {
            if (viewHost != null)
            {
                viewHost.OrientationChanged -=
                    HandleOrientationChanged;
            }
        }


        private sealed class TilemapTransformSnapshot
        {
            private readonly Tilemap tilemap;

            private readonly List<AuthoredTransform>
                canonicalTransforms =
                    new List<AuthoredTransform>();


            public TilemapTransformSnapshot(
                Tilemap tilemap,
                IsometricViewHost viewHost,
                int unityCellZ)
            {
                this.tilemap =
                    tilemap
                    ?? throw new ArgumentNullException(
                        nameof(tilemap));

                foreach (
                    Vector3Int displayCell
                    in tilemap.cellBounds.allPositionsWithin)
                {
                    if (displayCell.z != unityCellZ
                        || !tilemap.HasTile(
                            displayCell))
                    {
                        continue;
                    }

                    GridPosition logicalCell =
                        viewHost.ToLogicalCell(
                            displayCell);

                    canonicalTransforms.Add(
                        new AuthoredTransform(
                            logicalCell,
                            displayCell.z,
                            tilemap.GetTransformMatrix(
                                displayCell)));
                }
            }


            public void Apply(
                IsometricViewHost viewHost,
                IsometricViewOrientation orientation)
            {
                GridLayout gridLayout =
                    tilemap.layoutGrid;

                if (gridLayout == null)
                {
                    throw new InvalidOperationException(
                        $"Tilemap '{tilemap.name}' has no Grid layout.");
                }

                for (int index = 0;
                     index < canonicalTransforms.Count;
                     index++)
                {
                    AuthoredTransform authoredTransform =
                        canonicalTransforms[index];

                    Vector3Int displayCell =
                        viewHost.ToUnityCell(
                            authoredTransform.LogicalCell);

                    displayCell.z =
                        authoredTransform.UnityCellZ;

                    if (!tilemap.HasTile(
                            displayCell))
                    {
                        continue;
                    }

                    TileFlags originalFlags =
                        tilemap.GetTileFlags(
                            displayCell);

                    tilemap.SetTileFlags(
                        displayCell,
                        TileFlags.None);

                    tilemap.SetTransformMatrix(
                        displayCell,
                        AuthoredTileTransformProjector.Project(
                            gridLayout,
                            authoredTransform.Transform,
                            orientation));

                    tilemap.SetTileFlags(
                        displayCell,
                        originalFlags);
                }
            }
        }


        private readonly struct AuthoredTransform
        {
            public GridPosition LogicalCell { get; }
            public int UnityCellZ { get; }
            public Matrix4x4 Transform { get; }


            public AuthoredTransform(
                GridPosition logicalCell,
                int unityCellZ,
                Matrix4x4 transform)
            {
                LogicalCell = logicalCell;
                UnityCellZ = unityCellZ;
                Transform = transform;
            }
        }
    }
}
