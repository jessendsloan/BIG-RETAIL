using System.Collections.Generic;
using BigRetail.Map.Domain;
using BigRetail.Map.Unity;
using BigRetail.Map.Unity.Walls;
using BigRetail.Map.Walls;
using UnityEngine;

namespace BigRetail.Construction.Unity.Walls
{
    /// <summary>
    /// Displays every segment in a planned straight wall run.
    ///
    /// Green means a new wall will be created.
    /// Blue means a wall already exists and will be preserved.
    /// Red means the segment is invalid and will be skipped.
    ///
    /// Invalid or existing segments do not reject valid segments.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WallRunPreviewView : MonoBehaviour
    {
        [Header("Runtime Model")]

        [SerializeField]
        private GridMapHost mapHost;

        [SerializeField]
        private WallTargetResolver targetResolver;


        [Header("Preview Pool")]

        [SerializeField]
        private WallRunPreviewSegmentView segmentPrefab;

        [Tooltip(
            "Parent for instantiated preview segments. " +
            "When empty, this object's Transform is used.")]
        [SerializeField]
        private Transform segmentParent;


        [Header("Visual")]

        [SerializeField, Min(0.001f)]
        private float previewThickness = 0.1f;

        [SerializeField]
        private Color validColor =
            new Color(
                0.2f,
                1f,
                0.3f,
                0.85f);

        [SerializeField]
        private Color existingColor =
            new Color(
                0.15f,
                0.65f,
                1f,
                0.9f);

        [SerializeField]
        private Color invalidColor =
            new Color(
                1f,
                0.2f,
                0.2f,
                0.85f);


        private readonly List<WallRunPreviewSegmentView>
            segmentPool =
                new List<WallRunPreviewSegmentView>();


        public bool IsVisible { get; private set; }

        public bool IsPlanValid { get; private set; }

        public int VisibleSegmentCount { get; private set; }

        public int BuildableSegmentCount { get; private set; }

        public int ExistingSegmentCount { get; private set; }

        public int SkippedSegmentCount { get; private set; }


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

            BuildableSegmentCount = 0;
            ExistingSegmentCount = 0;
            SkippedSegmentCount = 0;

            for (int index = 0;
                 index < plan.SegmentCount;
                 index++)
            {
                CellEdge edge =
                    plan.Edges[index];

                Color segmentColor;

                if (mapHost.WallConstruction.HasWall(edge))
                {
                    ExistingSegmentCount++;

                    segmentColor =
                        existingColor;
                }
                else
                {
                    WallChangeResult evaluation =
                        mapHost.WallConstruction
                            .EvaluatePlacement(edge);

                    if (evaluation.Succeeded)
                    {
                        BuildableSegmentCount++;

                        segmentColor =
                            validColor;
                    }
                    else
                    {
                        SkippedSegmentCount++;

                        segmentColor =
                            invalidColor;
                    }
                }

                CellEdgeWorldPose worldPose =
                    CellEdgeWorldPose.Calculate(
                        edge,
                        targetResolver.CoordinateTilemap,
                        targetResolver.LogicalLevel,
                        targetResolver.UnityCellZ);

                segmentPool[index].Show(
                    edge,
                    worldPose,
                    previewThickness,
                    segmentColor);
            }

            HideUnusedSegments(
                plan.SegmentCount);

            VisibleSegmentCount =
                plan.SegmentCount;

            IsPlanValid =
                plan.Succeeded;

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
            BuildableSegmentCount = 0;
            ExistingSegmentCount = 0;
            SkippedSegmentCount = 0;
            IsPlanValid = false;
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
                    "WallRunPreviewView has no GridMapHost assigned.",
                    this);

                isValid = false;
            }

            if (targetResolver == null)
            {
                Debug.LogError(
                    "WallRunPreviewView has no WallTargetResolver assigned.",
                    this);

                isValid = false;
            }

            if (segmentPrefab == null)
            {
                Debug.LogError(
                    "WallRunPreviewView has no preview-segment " +
                    "prefab assigned.",
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