using System;
using System.Collections.Generic;
using BigRetail.Map.Domain;
using BigRetail.Map.View;
using BigRetail.Map.Unity.View;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace BigRetail.Map.Unity
{
    /// <summary>
    /// Resolves stable location marker IDs and keeps their scene transforms
    /// aligned with the active isometric projection.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LocationMarkerHost : MonoBehaviour
    {
        [SerializeField]
        private IsometricViewHost viewHost;

        [SerializeField]
        private Tilemap coordinateTilemap;


        private readonly Dictionary<string, LocationMarkerAuthoring>
            markersById =
                new Dictionary<string, LocationMarkerAuthoring>(
                    StringComparer.Ordinal);

        private bool hasBuiltIndex;


        private void OnEnable()
        {
            if (!Application.isPlaying
                || viewHost == null)
            {
                return;
            }

            viewHost.OrientationChanged +=
                HandleOrientationChanged;
        }


        private void Start()
        {
            if (!TryRebuildMarkerIndex(
                    out string validationFailure))
            {
                Debug.LogError(
                    validationFailure,
                    this);

                enabled = false;
                return;
            }

            RefreshWorldPositions();
        }


        private void OnDisable()
        {
            if (viewHost != null)
            {
                viewHost.OrientationChanged -=
                    HandleOrientationChanged;
            }
        }


        public bool TryGetMarker(
            string markerId,
            out LocationMarkerAuthoring marker)
        {
            marker = null;

            string normalizedId =
                markerId != null
                    ? markerId.Trim()
                    : string.Empty;

            if (normalizedId.Length == 0)
            {
                return false;
            }

            if (!hasBuiltIndex
                && !TryRebuildMarkerIndex(out _))
            {
                return false;
            }

            return markersById.TryGetValue(
                normalizedId,
                out marker);
        }


        public bool TryRebuildMarkerIndex(
            out string validationFailure)
        {
            markersById.Clear();
            hasBuiltIndex = false;

            LocationMarkerAuthoring[] markers =
                GetComponentsInChildren<LocationMarkerAuthoring>(true);

            for (int index = 0;
                 index < markers.Length;
                 index++)
            {
                LocationMarkerAuthoring marker = markers[index];
                string markerId = marker.MarkerId;

                if (markerId.Length == 0)
                {
                    validationFailure =
                        $"Location marker '{marker.name}' has no stable "
                        + "marker ID.";

                    return false;
                }

                if (!markersById.TryAdd(
                        markerId,
                        marker))
                {
                    validationFailure =
                        $"Location marker ID '{markerId}' is duplicated "
                        + $"under '{name}'.";

                    return false;
                }
            }

            if (viewHost == null)
            {
                validationFailure =
                    $"Location marker host '{name}' has no isometric "
                    + "view host assigned.";

                return false;
            }

            if (coordinateTilemap == null)
            {
                validationFailure =
                    $"Location marker host '{name}' has no coordinate "
                    + "Tilemap assigned.";

                return false;
            }

            hasBuiltIndex = true;
            validationFailure = string.Empty;
            return true;
        }


        public void RefreshWorldPositions()
        {
            if (coordinateTilemap == null)
            {
                return;
            }

            LocationMarkerAuthoring[] markers =
                GetComponentsInChildren<LocationMarkerAuthoring>(true);

            for (int index = 0;
                 index < markers.Length;
                 index++)
            {
                LocationMarkerAuthoring marker = markers[index];
                Vector3 worldPosition;

                if (viewHost != null
                    && viewHost.IsInitialized)
                {
                    Vector3Int logicalCell =
                        marker.LogicalCell;

                    worldPosition =
                        viewHost.GetLogicalCellCenterWorld(
                            new GridPosition(
                                logicalCell.x,
                                logicalCell.y,
                                logicalCell.z),
                            coordinateTilemap);
                }
                else
                {
                    worldPosition =
                        coordinateTilemap.GetCellCenterWorld(
                            marker.LogicalCell);
                }

                marker.transform.position =
                    worldPosition
                    + marker.WorldOffset;
            }
        }


        private void HandleOrientationChanged(
            IsometricViewOrientation previousOrientation,
            IsometricViewOrientation nextOrientation)
        {
            RefreshWorldPositions();
        }
    }
}
