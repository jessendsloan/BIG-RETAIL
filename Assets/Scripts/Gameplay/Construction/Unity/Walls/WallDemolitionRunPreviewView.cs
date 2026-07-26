using System.Collections.Generic;
using BigRetail.Map.Domain;
using BigRetail.Map.Unity;
using BigRetail.Map.Unity.Walls;
using BigRetail.Map.Walls;
using UnityEngine;

namespace BigRetail.Construction.Unity.Walls
{
    /// <summary>
    /// Displays a complete planned wall-demolition run.
    ///
    /// Orange segments contain walls that will be removed.
    /// Gray segments are already empty and will be skipped.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WallDemolitionRunPreviewView :
        MonoBehaviour
    {
        [Header("Runtime Model")]

        [SerializeField]
        private GridMapHost mapHost;

        [SerializeField]
        private WallTargetResolver targetResolver;


        [Header("Preview Pool")]

        [SerializeField]
        private WallRunPreviewSegmentView segmentPrefab;

        [SerializeField]
        private Transform segmentParent;


        [Header("Visual")]

        [SerializeField, Min(0.001f)]
        private float previewThickness = 0.12f;

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


        private readonly List<WallRunPreviewSegmentView>
            segmentPool =
                new List<WallRunPreviewSegmentView>();


        public bool IsVisible { get; private set; }

        public int VisibleSegmentCount { get; private set; }

        public int RemovableSegmentCount { get; private set; }

        public int AlreadyEmptySegmentCount { get; private set; }


        private void Awake()
        {
            if (!ValidateReferences())
            {
                enabled = false;
                return;
            }

            if (segmentParent == null)
            {
                segmentParent =
                    transform;
            }

            Hide();
        }


        public void ShowPlan(
            WallRunPlanResult plan)
        {
            if (!plan.Succeeded
                || !mapHost.IsInitialized
                || mapHost.WallConstruction == null)
            {
                Hide();
                return;
            }

            EnsurePoolCapacity(
                plan.SegmentCount);

            RemovableSegmentCount = 0;
            AlreadyEmptySegmentCount = 0;

            for (int index = 0;
                 index < plan.SegmentCount;
                 index++)
            {
                CellEdge edge =
                    plan.Edges[index];

                bool hasWall =
                    mapHost.WallConstruction
                        .HasWall(edge);

                if (hasWall)
                {
                    RemovableSegmentCount++;
                }
                else
                {
                    AlreadyEmptySegmentCount++;
                }

                CellEdgeWorldPose worldPose =
                    CellEdgeWorldPose.Calculate(
                        edge,
                        targetResolver.CoordinateTilemap,
                        targetResolver.LogicalLevel,
                        targetResolver.UnityCellZ,
                        targetResolver.ViewProjection);

                segmentPool[index].Show(
                    edge,
                    worldPose,
                    previewThickness,
                    hasWall
                        ? removableColor
                        : alreadyEmptyColor);
            }

            HideUnusedSegments(
                plan.SegmentCount);

            VisibleSegmentCount =
                plan.SegmentCount;

            IsVisible =
                VisibleSegmentCount > 0;
        }


        public void Hide()
        {
            for (int index = 0;
                 index < segmentPool.Count;
                 index++)
            {
                segmentPool[index].Hide();
            }

            VisibleSegmentCount = 0;
            RemovableSegmentCount = 0;
            AlreadyEmptySegmentCount = 0;
            IsVisible = false;
        }


        private void EnsurePoolCapacity(
            int requiredCount)
        {
            while (segmentPool.Count < requiredCount)
            {
                WallRunPreviewSegmentView segment =
                    Instantiate(
                        segmentPrefab,
                        segmentParent);

                segment.Hide();

                segmentPool.Add(
                    segment);
            }
        }


        private void HideUnusedSegments(
            int firstUnusedIndex)
        {
            for (int index = firstUnusedIndex;
                 index < segmentPool.Count;
                 index++)
            {
                segmentPool[index].Hide();
            }
        }


        private bool ValidateReferences()
        {
            bool isValid = true;

            if (mapHost == null)
            {
                Debug.LogError(
                    "WallDemolitionRunPreviewView has no " +
                    "GridMapHost assigned.",
                    this);

                isValid = false;
            }

            if (targetResolver == null)
            {
                Debug.LogError(
                    "WallDemolitionRunPreviewView has no " +
                    "WallTargetResolver assigned.",
                    this);

                isValid = false;
            }

            if (segmentPrefab == null)
            {
                Debug.LogError(
                    "WallDemolitionRunPreviewView has no " +
                    "preview-segment prefab assigned.",
                    this);

                isValid = false;
            }

            return isValid;
        }


        private void OnDisable()
        {
            Hide();
        }


        private void OnValidate()
        {
            previewThickness =
                Mathf.Max(
                    previewThickness,
                    0.001f);
        }
    }
}
