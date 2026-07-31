using System;
using System.Collections.Generic;
using BigRetail.Map.Domain;
using BigRetail.Map.Unity.View;
using BigRetail.Map.View;
using BigRetail.Map.Walls;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace BigRetail.Map.Unity.Walls
{
    /// <summary>
    /// Keeps Unity wall views synchronized with model-owned structural and
    /// finish state.
    ///
    /// The runtime model remains the source of truth. This system only creates,
    /// removes, and refreshes presentation objects.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-50)]
    public sealed class WallViewSystem : MonoBehaviour
    {
        [Header("Runtime Model")]

        [SerializeField]
        private GridMapHost mapHost;


        [Header("Coordinate Mapping")]

        [Tooltip(
            "A Tilemap belonging to the same Grid as the authored map. "
            + "Map Visuals is appropriate.")]
        [SerializeField]
        private Tilemap coordinateTilemap;

        [SerializeField]
        private IsometricViewHost viewHost;

        [Tooltip(
            "The logical building level displayed by this view system.")]
        [SerializeField]
        private int logicalLevel = 0;

        [Tooltip(
            "The Unity Tilemap Z layer used to calculate cell centers.")]
        [SerializeField]
        private int unityCellZ = 0;


        [Header("Wall Presentation")]

        [SerializeField]
        private WallSegmentView wallSegmentPrefab;

        [Tooltip(
            "Parent Transform for instantiated wall views. "
            + "When empty, this component's Transform is used.")]
        [SerializeField]
        private Transform wallViewParent;


        private readonly Dictionary<CellEdge, WallSegmentView>
            wallViews =
                new Dictionary<CellEdge, WallSegmentView>();

        private WallState subscribedWallState;
        private WallFinishService subscribedFinishService;
        private WallFinishPresentationResolver finishResolver;


        public int VisibleWallCount =>
            wallViews.Count;


        private void Awake()
        {
            if (!ValidateReferences())
            {
                enabled = false;
                return;
            }

            if (wallViewParent == null)
            {
                wallViewParent =
                    transform;
            }
        }


        private void OnEnable()
        {
            if (mapHost != null)
            {
                mapHost.Initialized +=
                    HandleMapInitialized;
            }

            if (viewHost != null)
            {
                viewHost.OrientationChanged +=
                    HandleOrientationChanged;
            }

            if (mapHost != null
                && mapHost.IsInitialized)
            {
                AttachToRuntimeModel(
                    mapHost.WallState,
                    mapHost.WallFinishes,
                    mapHost.WallFinishAssets);
            }
        }


        private void OnDisable()
        {
            if (mapHost != null)
            {
                mapHost.Initialized -=
                    HandleMapInitialized;
            }

            if (viewHost != null)
            {
                viewHost.OrientationChanged -=
                    HandleOrientationChanged;
            }

            DetachFromRuntimeModel();
            ClearAllViews();
        }


        private void HandleMapInitialized(
            GridMapHost initializedHost)
        {
            AttachToRuntimeModel(
                initializedHost.WallState,
                initializedHost.WallFinishes,
                initializedHost.WallFinishAssets);
        }


        private void AttachToRuntimeModel(
            WallState wallState,
            WallFinishService finishService,
            WallFinishAssetCatalog finishAssets)
        {
            if (wallState == null)
            {
                Debug.LogError(
                    "WallViewSystem received a null WallState.",
                    this);
                return;
            }

            if (finishService == null)
            {
                Debug.LogError(
                    "WallViewSystem received a null WallFinishService.",
                    this);
                return;
            }

            if (finishAssets == null)
            {
                Debug.LogError(
                    "WallViewSystem received a null WallFinishAssetCatalog.",
                    this);
                return;
            }

            if (subscribedWallState == wallState
                && subscribedFinishService == finishService)
            {
                RebuildAllViews();
                return;
            }

            DetachFromRuntimeModel();

            subscribedWallState =
                wallState;

            subscribedFinishService =
                finishService;

            finishResolver =
                new WallFinishPresentationResolver(
                    subscribedFinishService,
                    finishAssets);

            subscribedWallState.WallAdded +=
                HandleWallAdded;

            subscribedWallState.WallRemoved +=
                HandleWallRemoved;

            subscribedFinishService.EffectiveFinishChanged +=
                HandleEffectiveFinishChanged;

            RebuildAllViews();
        }


        private void DetachFromRuntimeModel()
        {
            if (subscribedWallState != null)
            {
                subscribedWallState.WallAdded -=
                    HandleWallAdded;

                subscribedWallState.WallRemoved -=
                    HandleWallRemoved;
            }

            if (subscribedFinishService != null)
            {
                subscribedFinishService.EffectiveFinishChanged -=
                    HandleEffectiveFinishChanged;
            }

            subscribedWallState =
                null;

            subscribedFinishService =
                null;

            finishResolver =
                null;
        }


        private void HandleWallAdded(
            CellEdge edge)
        {
            CreateWallView(edge);
        }


        private void HandleWallRemoved(
            CellEdge edge)
        {
            RemoveWallView(edge);
        }


        private void HandleEffectiveFinishChanged(
            WallFaceKey face,
            WallFinishId finishId)
        {
            if (!wallViews.TryGetValue(
                    face.Edge,
                    out WallSegmentView view)
                || view == null)
            {
                return;
            }

            view.ApplyProjection(
                viewHost.Projection);
        }


        private void RebuildAllViews()
        {
            ClearAllViews();

            if (subscribedWallState == null
                || finishResolver == null)
            {
                return;
            }

            foreach (
                CellEdge wall
                in subscribedWallState.EnumerateWalls())
            {
                CreateWallView(wall);
            }
        }


        private void CreateWallView(
            CellEdge edge)
        {
            if (edge.FirstCell.Level != logicalLevel
                || finishResolver == null)
            {
                return;
            }

            if (wallViews.ContainsKey(edge))
            {
                return;
            }

            WallSegmentView view =
                Instantiate(
                    wallSegmentPrefab,
                    wallViewParent);

            try
            {
                view.Initialize(
                    edge,
                    coordinateTilemap,
                    logicalLevel,
                    unityCellZ,
                    viewHost.Projection,
                    finishResolver);

                wallViews.Add(
                    edge,
                    view);
            }
            catch (Exception exception)
            {
                if (view != null)
                {
                    Destroy(view.gameObject);
                }

                Debug.LogException(
                    exception,
                    this);
            }
        }


        private void RemoveWallView(
            CellEdge edge)
        {
            if (!wallViews.TryGetValue(
                    edge,
                    out WallSegmentView view))
            {
                return;
            }

            wallViews.Remove(edge);

            if (view != null)
            {
                Destroy(view.gameObject);
            }
        }


        private void HandleOrientationChanged(
            IsometricViewOrientation previousOrientation,
            IsometricViewOrientation currentOrientation)
        {
            foreach (
                WallSegmentView view
                in wallViews.Values)
            {
                if (view != null)
                {
                    view.ApplyProjection(
                        viewHost.Projection);
                }
            }
        }


        private void ClearAllViews()
        {
            foreach (
                WallSegmentView view
                in wallViews.Values)
            {
                if (view != null)
                {
                    Destroy(view.gameObject);
                }
            }

            wallViews.Clear();
        }


        private bool ValidateReferences()
        {
            bool isValid = true;

            if (mapHost == null)
            {
                Debug.LogError(
                    "WallViewSystem has no GridMapHost assigned.",
                    this);

                isValid = false;
            }

            if (coordinateTilemap == null)
            {
                Debug.LogError(
                    "WallViewSystem has no Coordinate Tilemap assigned.",
                    this);

                isValid = false;
            }

            if (viewHost == null)
            {
                Debug.LogError(
                    "WallViewSystem has no IsometricViewHost assigned.",
                    this);

                isValid = false;
            }

            if (wallSegmentPrefab == null)
            {
                Debug.LogError(
                    "WallViewSystem has no WallSegmentView prefab assigned.",
                    this);

                isValid = false;
            }

            return isValid;
        }
    }
}
