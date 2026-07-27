using System.Collections.Generic;
using BigRetail.Map.Domain;
using BigRetail.Map.Unity;
using BigRetail.Map.Unity.Walls;
using BigRetail.Map.Walls;
using UnityEngine;

namespace BigRetail.Construction.Unity.Walls
{
    /// <summary>
    /// Displays pylon markers at every vertex in a planned straight wall run and
    /// previews the selected directional finish across every usable wall edge.
    ///
    /// Pylons explain the selected span. Full wall sprites explain the exact
    /// viewer-facing surfaces that will be created or changed on release.
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

        [SerializeField]
        private WallFinishSelectionHost finishSelection;


        [Header("Pylon Pool")]

        [SerializeField]
        private WallRunPreviewVertexView vertexPrefab;

        [Tooltip(
            "Parent for instantiated vertex pylons. "
            + "When empty, this object's Transform is used.")]
        [SerializeField]
        private Transform vertexParent;


        [Header("Appearance Preview Pool")]

        [SerializeField]
        private WallRunPreviewSegmentView appearancePreviewPrefab;

        [Tooltip(
            "Parent for instantiated wall-appearance previews. "
            + "When empty, this object's Transform is used.")]
        [SerializeField]
        private Transform appearancePreviewParent;


        [Header("Pylon Visuals")]

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


        [Header("Appearance Preview Visuals")]

        [Tooltip(
            "Tint applied to ghost walls and wall faces that will change.")]
        [SerializeField]
        private Color appearancePreviewColor =
            new Color(
                1f,
                1f,
                1f,
                0.65f);

        [Tooltip(
            "Muted tint for existing wall faces that already use the selected finish.")]
        [SerializeField]
        private Color unchangedAppearancePreviewColor =
            new Color(
                1f,
                1f,
                1f,
                0.3f);

        [Tooltip(
            "Optional world-space adjustment applied to every finish preview.")]
        [SerializeField]
        private Vector3 appearanceWorldPositionOffset =
            Vector3.zero;


        private readonly List<WallRunPreviewVertexView>
            vertexPool =
                new List<WallRunPreviewVertexView>();

        private readonly List<WallRunPreviewSegmentView>
            appearancePool =
                new List<WallRunPreviewSegmentView>();


        public bool IsVisible { get; private set; }

        public bool IsPlanValid { get; private set; }

        public int VisibleVertexCount { get; private set; }

        public int VisibleSegmentCount { get; private set; }

        public int VisibleAppearanceCount { get; private set; }

        public int BuildableSegmentCount { get; private set; }

        public int ExistingSegmentCount { get; private set; }

        public int SkippedSegmentCount { get; private set; }

        public int UnchangedAppearanceCount { get; private set; }


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

            if (appearancePreviewParent == null)
            {
                appearancePreviewParent = transform;
            }

            Hide();
        }


        public void ShowAnchor(
            GridVertex vertex)
        {
            EnsureVertexPoolCapacity(1);

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
            HideUnusedAppearancePreviews(0);

            VisibleVertexCount = 1;
            VisibleSegmentCount = 0;
            VisibleAppearanceCount = 0;
            BuildableSegmentCount = 0;
            ExistingSegmentCount = 0;
            SkippedSegmentCount = 0;
            UnchangedAppearanceCount = 0;
            IsPlanValid = false;
            IsVisible = true;
        }


        public void ShowPlan(
            WallVertexRunPlanResult plan)
        {
            if (finishSelection == null
                || !finishSelection.IsInitialized)
            {
                Hide();
                return;
            }

            ShowPlan(
                plan,
                finishSelection.SelectedFinishAsset);
        }


        public void ShowPlan(
            WallVertexRunPlanResult plan,
            WallFinishAsset selectedFinish)
        {
            if (!plan.Succeeded
                || selectedFinish == null
                || !mapHost.IsInitialized
                || mapHost.WallConstruction == null
                || mapHost.WallFinishes == null
                || targetResolver.ViewProjection == null)
            {
                Hide();
                return;
            }

            EnsureVertexPoolCapacity(
                plan.VertexCount);

            EnsureAppearancePoolCapacity(
                plan.SegmentCount);

            SegmentPreviewStatus[] segmentStatuses =
                new SegmentPreviewStatus[plan.SegmentCount];

            BuildableSegmentCount = 0;
            ExistingSegmentCount = 0;
            SkippedSegmentCount = 0;
            VisibleAppearanceCount = 0;
            UnchangedAppearanceCount = 0;

            for (int index = 0;
                 index < plan.SegmentCount;
                 index++)
            {
                segmentStatuses[index] =
                    EvaluateSegment(
                        plan.Edges[index]);
            }

            ShowAppearancePreviews(
                plan,
                segmentStatuses,
                selectedFinish);

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

            HideUnusedAppearancePreviews(
                plan.SegmentCount);

            VisibleVertexCount =
                plan.VertexCount;

            VisibleSegmentCount =
                plan.SegmentCount;

            IsPlanValid = true;
            IsVisible =
                VisibleVertexCount > 0
                || VisibleAppearanceCount > 0;
        }


        public void Hide()
        {
            for (int index = 0;
                 index < vertexPool.Count;
                 index++)
            {
                vertexPool[index].Hide();
            }

            for (int index = 0;
                 index < appearancePool.Count;
                 index++)
            {
                appearancePool[index].Hide();
            }

            VisibleVertexCount = 0;
            VisibleSegmentCount = 0;
            VisibleAppearanceCount = 0;
            BuildableSegmentCount = 0;
            ExistingSegmentCount = 0;
            SkippedSegmentCount = 0;
            UnchangedAppearanceCount = 0;
            IsPlanValid = false;
            IsVisible = false;
        }


        private void ShowAppearancePreviews(
            WallVertexRunPlanResult plan,
            IReadOnlyList<SegmentPreviewStatus> segmentStatuses,
            WallFinishAsset selectedFinish)
        {
            WallFinishId selectedFinishId =
                selectedFinish.Id;

            for (int index = 0;
                 index < plan.SegmentCount;
                 index++)
            {
                SegmentPreviewStatus status =
                    segmentStatuses[index];

                if (status == SegmentPreviewStatus.Invalid)
                {
                    appearancePool[index].Hide();
                    continue;
                }

                CellEdge edge =
                    plan.Edges[index];

                CellEdgeWorldPose worldPose =
                    CellEdgeWorldPose.Calculate(
                        edge,
                        targetResolver.CoordinateTilemap,
                        targetResolver.LogicalLevel,
                        targetResolver.UnityCellZ,
                        targetResolver.ViewProjection);

                bool isUnchanged =
                    status == SegmentPreviewStatus.Existing
                    && mapHost.WallFinishes.GetEffectiveFinish(
                        edge,
                        worldPose.ViewerFacingCell)
                        == selectedFinishId;

                Color previewColor =
                    isUnchanged
                        ? unchangedAppearancePreviewColor
                        : appearancePreviewColor;

                appearancePool[index].ShowAppearance(
                    edge,
                    worldPose,
                    selectedFinish.GetSprite(
                        worldPose.DisplaySlope),
                    appearanceWorldPositionOffset,
                    previewColor);

                VisibleAppearanceCount++;

                if (isUnchanged)
                {
                    UnchangedAppearanceCount++;
                }
            }
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


        private void EnsureVertexPoolCapacity(
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


        private void EnsureAppearancePoolCapacity(
            int requiredCount)
        {
            while (appearancePool.Count < requiredCount)
            {
                WallRunPreviewSegmentView appearanceView =
                    Instantiate(
                        appearancePreviewPrefab,
                        appearancePreviewParent);

                appearanceView.Hide();
                appearancePool.Add(
                    appearanceView);
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


        private void HideUnusedAppearancePreviews(
            int firstUnusedIndex)
        {
            for (int index = firstUnusedIndex;
                 index < appearancePool.Count;
                 index++)
            {
                appearancePool[index].Hide();
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

            if (finishSelection == null)
            {
                Debug.LogError(
                    "WallRunPreviewView has no "
                    + "WallFinishSelectionHost assigned.",
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

            if (appearancePreviewPrefab == null)
            {
                Debug.LogError(
                    "WallRunPreviewView has no wall-appearance "
                    + "preview prefab assigned.",
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
