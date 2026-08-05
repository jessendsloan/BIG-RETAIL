using System.Collections.Generic;
using BigRetail.Map.Domain;
using BigRetail.Map.Unity;
using BigRetail.Map.Unity.Walls;
using BigRetail.Map.Walls;
using UnityEngine;

namespace BigRetail.Construction.Unity.Walls
{
    /// <summary>
    /// Displays a pylon marker at every vertex in a planned straight
    /// wall-demolition run.
    ///
    /// Orange markers touch at least one wall that will be removed. Gray
    /// markers touch only already-empty planned segments.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WallDemolitionRunPreviewView : MonoBehaviour
    {
        private enum SegmentPreviewStatus
        {
            AlreadyEmpty = 0,
            Removable = 1,
            ProtectedByDoor = 2
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
            "Parent for instantiated demolition pylons. "
            + "When empty, this object's Transform is used.")]
        [SerializeField]
        private Transform vertexParent;


        [Header("Visual")]

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

        [SerializeField]
        private Color protectedByDoorColor =
            new Color(
                1f,
                0.2f,
                0.2f,
                0.95f);

        [Tooltip(
            "Optional world-space adjustment applied to every demolition "
            + "pylon after its grid-vertex position has been calculated.")]
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

        public int RemovableSegmentCount { get; private set; }

        public int AlreadyEmptySegmentCount { get; private set; }

        public int ProtectedByDoorSegmentCount { get; private set; }


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
                removableColor);

            HideUnusedVertices(1);

            VisibleVertexCount = 1;
            VisibleSegmentCount = 0;
            RemovableSegmentCount = 0;
            AlreadyEmptySegmentCount = 0;
            ProtectedByDoorSegmentCount = 0;
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

            RemovableSegmentCount = 0;
            AlreadyEmptySegmentCount = 0;
            ProtectedByDoorSegmentCount = 0;

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

            IsPlanValid =
                ProtectedByDoorSegmentCount == 0;
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
            RemovableSegmentCount = 0;
            AlreadyEmptySegmentCount = 0;
            ProtectedByDoorSegmentCount = 0;
            IsPlanValid = false;
            IsVisible = false;
        }


        private SegmentPreviewStatus EvaluateSegment(
            CellEdge edge)
        {
            if (mapHost.DoorAssemblies != null
                && mapHost.DoorAssemblies.TryGetAssemblyAtEdge(
                    edge,
                    out _))
            {
                ProtectedByDoorSegmentCount++;
                return SegmentPreviewStatus.ProtectedByDoor;
            }

            if (mapHost.WallConstruction.HasWall(edge))
            {
                RemovableSegmentCount++;
                return SegmentPreviewStatus.Removable;
            }

            AlreadyEmptySegmentCount++;
            return SegmentPreviewStatus.AlreadyEmpty;
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
                case SegmentPreviewStatus.Removable:
                    return removableColor;

                case SegmentPreviewStatus.AlreadyEmpty:
                    return alreadyEmptyColor;

                case SegmentPreviewStatus.ProtectedByDoor:
                    return protectedByDoorColor;

                default:
                    return alreadyEmptyColor;
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
                    "WallDemolitionRunPreviewView has no "
                    + "GridMapHost assigned.",
                    this);

                isValid = false;
            }

            if (targetResolver == null)
            {
                Debug.LogError(
                    "WallDemolitionRunPreviewView has no "
                    + "WallVertexTargetResolver assigned.",
                    this);

                isValid = false;
            }

            if (vertexPrefab == null)
            {
                Debug.LogError(
                    "WallDemolitionRunPreviewView has no preview-vertex "
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
