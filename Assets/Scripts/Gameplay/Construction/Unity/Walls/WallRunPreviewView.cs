using System.Collections.Generic;
using BigRetail.Map.Domain;
using BigRetail.Map.Unity;
using BigRetail.Map.Unity.Walls;
using BigRetail.Map.Walls;
using UnityEngine;

namespace BigRetail.Construction.Unity.Walls
{
    /// <summary>
    /// Displays a pylon marker at every vertex in a planned straight wall run.
    ///
    /// Segment construction state is summarized through the neighboring pylon
    /// colors while CellEdges remain the geometry that will be committed.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WallRunPreviewView : MonoBehaviour
    {
        private enum SegmentPreviewStatus
        {
            Existing = 0,
            Buildable = 1,
            Invalid = 2
        }


        [Header("Runtime Model")]

        [SerializeField]
        private GridMapHost mapHost;

        [SerializeField]
        private WallVertexTargetResolver targetResolver;


        [Header("Preview Pool")]

        [SerializeField]
        private WallRunPreviewVertexView vertexPrefab;

        [Tooltip(
            "Parent for instantiated vertex pylons. "
            + "When empty, this object's Transform is used.")]
        [SerializeField]
        private Transform vertexParent;


        [Header("Visual")]

        [SerializeField]
        private Color validColor =
            new Color(
                0.2f,
                1f,
                0.3f,
                0.9f);

        [SerializeField]
        private Color existingColor =
            new Color(
                0.15f,
                0.65f,
                1f,
                0.95f);

        [SerializeField]
        private Color invalidColor =
            new Color(
                1f,
                0.2f,
                0.2f,
                0.9f);

        [Tooltip(
            "Optional world-space adjustment applied to every pylon after "
            + "its grid-vertex position has been calculated.")]
        [SerializeField]
        private Vector3 worldPositionOffset =
            Vector3.zero;


        private readonly List<WallRunPreviewVertexView>
            vertexPool =
                new List<WallRunPreviewVertexView>();


        public bool IsVisible { get; private set; }

        public bool IsPlanValid { get; private set; }

        public int VisibleVertexCount { get; private set; }

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

            if (vertexParent == null)
            {
                vertexParent = transform;
            }

            Hide();
        }


        public void ShowAnchor(
            GridVertex vertex)
        {
            EnsurePoolCapacity(1);

            GridVertexWorldPose worldPose =
                GridVertexWorldPose.Calculate(
                    vertex,
                    targetResolver.CoordinateTilemap,
                    targetResolver.LogicalLevel,
                    targetResolver.UnityCellZ,
                    targetResolver.ViewProjection);

            vertexPool[0].Show(
                vertex,
                worldPose,
                worldPositionOffset,
                validColor);

            HideUnusedVertices(1);

            VisibleVertexCount = 1;
            VisibleSegmentCount = 0;
            BuildableSegmentCount = 0;
            ExistingSegmentCount = 0;
            SkippedSegmentCount = 0;
            IsPlanValid = false;
            IsVisible = true;
        }


        public void ShowPlan(
            WallVertexRunPlanResult plan)
        {
            if (!plan.Succeeded
                || !mapHost.IsInitialized
                || mapHost.WallConstruction == null)
            {
                Hide();
                return;
            }

            EnsurePoolCapacity(
                plan.VertexCount);

            SegmentPreviewStatus[] segmentStatuses =
                new SegmentPreviewStatus[plan.SegmentCount];

            BuildableSegmentCount = 0;
            ExistingSegmentCount = 0;
            SkippedSegmentCount = 0;

            for (int index = 0;
                 index < plan.SegmentCount;
                 index++)
            {
                segmentStatuses[index] =
                    EvaluateSegment(
                        plan.Edges[index]);
            }

            for (int index = 0;
                 index < plan.VertexCount;
                 index++)
            {
                GridVertex vertex =
                    plan.Vertices[index];

                GridVertexWorldPose worldPose =
                    GridVertexWorldPose.Calculate(
                        vertex,
                        targetResolver.CoordinateTilemap,
                        targetResolver.LogicalLevel,
                        targetResolver.UnityCellZ,
                        targetResolver.ViewProjection);

                SegmentPreviewStatus markerStatus =
                    ResolveMarkerStatus(
                        segmentStatuses,
                        index);

                vertexPool[index].Show(
                    vertex,
                    worldPose,
                    worldPositionOffset,
                    ResolveColor(markerStatus));
            }

            HideUnusedVertices(
                plan.VertexCount);

            VisibleVertexCount =
                plan.VertexCount;

            VisibleSegmentCount =
                plan.SegmentCount;

            IsPlanValid = true;
            IsVisible = VisibleVertexCount > 0;
        }


        public void Hide()
        {
            for (int index = 0;
                 index < vertexPool.Count;
                 index++)
            {
                vertexPool[index].Hide();
            }

            VisibleVertexCount = 0;
            VisibleSegmentCount = 0;
            BuildableSegmentCount = 0;
            ExistingSegmentCount = 0;
            SkippedSegmentCount = 0;
            IsPlanValid = false;
            IsVisible = false;
        }


        private SegmentPreviewStatus EvaluateSegment(
            CellEdge edge)
        {
            if (mapHost.WallConstruction.HasWall(edge))
            {
                ExistingSegmentCount++;
                return SegmentPreviewStatus.Existing;
            }

            WallChangeResult evaluation =
                mapHost.WallConstruction
                    .EvaluatePlacement(edge);

            if (evaluation.Succeeded)
            {
                BuildableSegmentCount++;
                return SegmentPreviewStatus.Buildable;
            }

            SkippedSegmentCount++;
            return SegmentPreviewStatus.Invalid;
        }


        private static SegmentPreviewStatus ResolveMarkerStatus(
            IReadOnlyList<SegmentPreviewStatus> segmentStatuses,
            int vertexIndex)
        {
            if (vertexIndex == 0)
            {
                return segmentStatuses[0];
            }

            if (vertexIndex == segmentStatuses.Count)
            {
                return segmentStatuses[segmentStatuses.Count - 1];
            }

            SegmentPreviewStatus previous =
                segmentStatuses[vertexIndex - 1];

            SegmentPreviewStatus next =
                segmentStatuses[vertexIndex];

            return previous >= next
                ? previous
                : next;
        }


        private Color ResolveColor(
            SegmentPreviewStatus status)
        {
            switch (status)
            {
                case SegmentPreviewStatus.Existing:
                    return existingColor;

                case SegmentPreviewStatus.Buildable:
                    return validColor;

                case SegmentPreviewStatus.Invalid:
                    return invalidColor;

                default:
                    return invalidColor;
            }
        }


        private void EnsurePoolCapacity(
            int requiredCount)
        {
            while (vertexPool.Count < requiredCount)
            {
                WallRunPreviewVertexView vertexView =
                    Instantiate(
                        vertexPrefab,
                        vertexParent);

                vertexView.Hide();

                vertexPool.Add(
                    vertexView);
            }
        }


        private void HideUnusedVertices(
            int firstUnusedIndex)
        {
            for (int index = firstUnusedIndex;
                 index < vertexPool.Count;
                 index++)
            {
                vertexPool[index].Hide();
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
                    "WallRunPreviewView has no "
                    + "WallVertexTargetResolver assigned.",
                    this);

                isValid = false;
            }

            if (vertexPrefab == null)
            {
                Debug.LogError(
                    "WallRunPreviewView has no preview-vertex "
                    + "prefab assigned.",
                    this);

                isValid = false;
            }

            return isValid;
        }


        private void OnDisable()
        {
            Hide();
        }
    }
}
