using System;
using System.Collections.Generic;
using BigRetail.Map.Domain;
using BigRetail.Map.Walls;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace BigRetail.Map.Unity.Walls
{
    /// <summary>
    /// Keeps Unity wall views synchronized with the model-owned WallState.
    ///
    /// WallState remains the source of truth.
    /// This system only creates and removes presentation objects.
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
            "A Tilemap belonging to the same Grid as the authored map. " +
            "Map Visuals is appropriate.")]
        [SerializeField]
        private Tilemap coordinateTilemap;

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
            "Parent Transform for instantiated wall views. " +
            "When empty, this component's Transform is used.")]
        [SerializeField]
        private Transform wallViewParent;


        private readonly Dictionary<CellEdge, WallSegmentView>
            wallViews =
                new Dictionary<CellEdge, WallSegmentView>();

        private WallState subscribedWallState;


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
        }


        private void Start()
        {
            if (mapHost != null
                && mapHost.IsInitialized)
            {
                AttachToWallState(
                    mapHost.WallState);
            }
        }


        private void OnDisable()
        {
            if (mapHost != null)
            {
                mapHost.Initialized -=
                    HandleMapInitialized;
            }

            DetachFromWallState();
            ClearAllViews();
        }


        private void HandleMapInitialized(
            GridMapHost initializedHost)
        {
            AttachToWallState(
                initializedHost.WallState);
        }


        private void AttachToWallState(
            WallState wallState)
        {
            if (wallState == null)
            {
                Debug.LogError(
                    "WallViewSystem received a null WallState.",
                    this);

                return;
            }

            if (subscribedWallState == wallState)
            {
                RebuildAllViews();
                return;
            }

            DetachFromWallState();

            subscribedWallState =
                wallState;

            subscribedWallState.WallAdded +=
                HandleWallAdded;

            subscribedWallState.WallRemoved +=
                HandleWallRemoved;

            RebuildAllViews();
        }


        private void DetachFromWallState()
        {
            if (subscribedWallState == null)
            {
                return;
            }

            subscribedWallState.WallAdded -=
                HandleWallAdded;

            subscribedWallState.WallRemoved -=
                HandleWallRemoved;

            subscribedWallState =
                null;
        }


        private void HandleWallAdded(CellEdge edge)
        {
            CreateWallView(edge);
        }


        private void HandleWallRemoved(CellEdge edge)
        {
            RemoveWallView(edge);
        }


        private void RebuildAllViews()
        {
            ClearAllViews();

            if (subscribedWallState == null)
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


        private void CreateWallView(CellEdge edge)
        {
            if (edge.FirstCell.Level != logicalLevel)
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
                    unityCellZ);

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


        private void RemoveWallView(CellEdge edge)
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