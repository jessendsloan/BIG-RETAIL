using System;
using System.Collections.Generic;
using BigRetail.Construction.Unity.Walls;
using BigRetail.Map.Domain;
using BigRetail.Map.Unity;
using BigRetail.Map.Unity.Doors;
using BigRetail.Map.Unity.Walls;
using BigRetail.Map.View;
using BigRetail.Map.Walls;
using UnityEngine;
using UnityEngine.Rendering;

namespace BigRetail.Construction.Unity.Doors
{
    /// <summary>
    /// Previews one complete door assembly across a planned straight wall run.
    /// Segment placeholders remain available while artwork is incomplete;
    /// a complete generic set adds the frame and two independent door layers.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DoorRunPreviewView : MonoBehaviour
    {
        private const int PreviewAssemblySortingOrderOffset = 1;

        private static readonly DoorAssemblyId PreviewAssemblyId =
            new DoorAssemblyId("door-placement-preview");


        [SerializeField]
        private GridMapHost mapHost;

        [SerializeField]
        private WallVertexTargetResolver targetResolver;

        [SerializeField]
        private DoorDefinitionSelectionHost definitionSelection;

        [SerializeField]
        private WallRunPreviewVertexView vertexPrefab;

        [SerializeField]
        private Transform vertexParent;

        [SerializeField]
        private WallRunPreviewSegmentView panelPrefab;

        [SerializeField]
        private Transform panelParent;

        [SerializeField]
        private Color validColor =
            new Color(0.55f, 0.9f, 1f, 0.8f);

        [SerializeField]
        private Color invalidColor =
            new Color(1f, 0.2f, 0.2f, 0.85f);

        [SerializeField]
        private Color placeholderPanelColor =
            new Color(0.58f, 0.82f, 0.92f, 0.72f);

        [SerializeField]
        private Vector3 pylonWorldPositionOffset =
            Vector3.zero;

        [SerializeField]
        private Vector3 panelWorldPositionOffset =
            Vector3.zero;


        private readonly List<WallRunPreviewVertexView> vertexPool =
            new List<WallRunPreviewVertexView>();

        private readonly List<WallRunPreviewSegmentView> panelPool =
            new List<WallRunPreviewSegmentView>();

        private DoorAssemblyView assemblyPreview;
        private SortingGroup assemblyPreviewSortingGroup;


        public bool IsVisible { get; private set; }

        public bool IsPlanValid { get; private set; }

        public DoorAssemblyChangeFailure CurrentFailure { get; private set; }


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

            if (panelParent == null)
            {
                panelParent = transform;
            }

            Hide();
        }


        public void ShowPlan(
            WallVertexRunPlanResult plan)
        {
            if (!plan.Succeeded
                || !mapHost.IsInitialized
                || mapHost.DoorConstruction == null
                || mapHost.WallFinishes == null
                || definitionSelection == null
                || !definitionSelection.IsInitialized
                || targetResolver.ViewProjection == null)
            {
                Hide();
                return;
            }

            DoorDefinitionAsset definitionAsset =
                definitionSelection.SelectedDefinitionAsset;

            DoorAssemblyChangeResult evaluation =
                mapHost.DoorConstruction.EvaluatePlacement(
                    PreviewAssemblyId,
                    definitionAsset.Id,
                    plan.Edges);

            IsPlanValid =
                evaluation.Succeeded;

            CurrentFailure =
                evaluation.Failure;

            Color pylonColor =
                IsPlanValid
                    ? validColor
                    : invalidColor;

            EnsureVertexCapacity(
                plan.VertexCount);

            EnsurePanelCapacity(
                plan.SegmentCount);

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

                vertexPool[index].Show(
                    vertex,
                    worldPose,
                    pylonWorldPositionOffset,
                    pylonColor);
            }

            for (int index = 0;
                 index < plan.SegmentCount;
                 index++)
            {
                ShowPanel(
                    plan,
                    definitionAsset,
                    index);
            }

            ShowAssemblyPreview(
                plan,
                definitionAsset);

            HideUnusedVertices(
                plan.VertexCount);

            HideUnusedPanels(
                plan.SegmentCount);

            IsVisible = true;
        }


        public void Hide()
        {
            HideUnusedVertices(0);
            HideUnusedPanels(0);

            if (assemblyPreview != null)
            {
                assemblyPreview.gameObject.SetActive(
                    false);
            }

            IsVisible = false;
            IsPlanValid = false;
            CurrentFailure = DoorAssemblyChangeFailure.None;
        }


        private void ShowPanel(
            WallVertexRunPlanResult plan,
            DoorDefinitionAsset definitionAsset,
            int suppliedIndex)
        {
            CellEdge edge =
                plan.Edges[suppliedIndex];

            CellEdgeWorldPose worldPose =
                CellEdgeWorldPose.Calculate(
                    edge,
                    targetResolver.CoordinateTilemap,
                    targetResolver.LogicalLevel,
                    targetResolver.UnityCellZ,
                    targetResolver.ViewProjection);

            WallFinishAsset finishAsset =
                ResolveVisibleFinish(
                    edge,
                    worldPose);

            Sprite panelSprite =
                finishAsset.GetSprite(
                    worldPose.DisplaySlope);

            Color panelColor =
                IsPlanValid
                    ? placeholderPanelColor
                    : invalidColor;

            panelPool[suppliedIndex].ShowAppearance(
                edge,
                worldPose,
                panelSprite,
                panelWorldPositionOffset,
                panelColor);
        }


        private void ShowAssemblyPreview(
            WallVertexRunPlanResult plan,
            DoorDefinitionAsset definitionAsset)
        {
            if (plan.SegmentCount
                != DoorAssemblyView.RequiredPanelCount)
            {
                if (assemblyPreview != null)
                {
                    assemblyPreview.gameObject.SetActive(
                        false);
                }

                return;
            }

            Vector3 worldPosition =
                Vector3.zero;

            Vector3[] panelWorldPositions =
                new Vector3[DoorAssemblyView.RequiredPanelCount];

            WallDisplaySlope displaySlope =
                default;

            int sortingOrder =
                int.MinValue;

            int rendererPriority =
                int.MinValue;

            for (int index = 0;
                 index < plan.SegmentCount;
                 index++)
            {
                CellEdgeWorldPose worldPose =
                    CellEdgeWorldPose.Calculate(
                        plan.Edges[index],
                        targetResolver.CoordinateTilemap,
                        targetResolver.LogicalLevel,
                        targetResolver.UnityCellZ,
                        targetResolver.ViewProjection);

                if (index == 0)
                {
                    displaySlope =
                        worldPose.DisplaySlope;
                }

                worldPosition +=
                    worldPose.Position
                    + panelWorldPositionOffset;

                panelWorldPositions[index] =
                    worldPose.Position
                    + panelWorldPositionOffset;

                sortingOrder =
                    Math.Max(
                        sortingOrder,
                        WallRenderOrderResolver.ResolveWall(
                            worldPose.DisplayEdge));

                rendererPriority =
                    Math.Max(
                        rendererPriority,
                        WallRenderOrderResolver.ResolveWallPriority(
                            worldPose.DisplayEdge));
            }

            if (!definitionAsset.TryGetAssemblySprites(
                    displaySlope,
                    out DoorAssemblySprites sprites))
            {
                if (assemblyPreview != null)
                {
                    assemblyPreview.gameObject.SetActive(
                        false);
                }

                return;
            }

            EnsureAssemblyPreview();

            assemblyPreview.gameObject.SetActive(
                true);

            Array.Sort(
                panelWorldPositions,
                ComparePanelWorldPositions);

            int assemblySortingOrder =
                sortingOrder
                + DoorAssemblyView
                    .SortingOrderOffsetFromSupportingWall;

            // Keep the layered door preview together when it competes with
            // the translucent wall panels. Its children still use their
            // individual renderer priorities inside this preview-only group.
            assemblyPreviewSortingGroup.sortingLayerID = 0;
            assemblyPreviewSortingGroup.sortingOrder =
                assemblySortingOrder
                + PreviewAssemblySortingOrderOffset;

            assemblyPreview.ApplyPresentation(
                sprites,
                panelWorldPositions,
                worldPosition / plan.SegmentCount,
                sortingLayerId: 0,
                sortingOrder: assemblySortingOrder,
                rendererPriority: rendererPriority + 1,
                sharedMaterial: null,
                tint: IsPlanValid
                    ? new Color(1f, 1f, 1f, validColor.a)
                    : invalidColor);
        }


        private static int ComparePanelWorldPositions(
            Vector3 left,
            Vector3 right)
        {
            int comparison =
                left.x.CompareTo(
                    right.x);

            return comparison != 0
                ? comparison
                : right.y.CompareTo(
                    left.y);
        }


        private void EnsureAssemblyPreview()
        {
            if (assemblyPreview != null)
            {
                return;
            }

            GameObject previewObject =
                new GameObject();

            previewObject.transform.SetParent(
                panelParent,
                false);

            assemblyPreviewSortingGroup =
                previewObject.AddComponent<SortingGroup>();

            assemblyPreview =
                previewObject.AddComponent<DoorAssemblyView>();

            assemblyPreview.Initialize(
                PreviewAssemblyId);
        }


        private WallFinishAsset ResolveVisibleFinish(
            CellEdge edge,
            CellEdgeWorldPose worldPose)
        {
            if (!mapHost.WallState.HasWall(edge))
            {
                return mapHost.WallFinishAssets.DefaultFinish;
            }

            WallFinishId finishId =
                mapHost.WallFinishes.GetEffectiveFinish(
                    edge,
                    worldPose.ViewerFacingCell);

            return mapHost.WallFinishAssets.GetAsset(
                finishId);
        }


        private void EnsureVertexCapacity(
            int requiredCount)
        {
            while (vertexPool.Count < requiredCount)
            {
                WallRunPreviewVertexView view =
                    Instantiate(
                        vertexPrefab,
                        vertexParent);

                view.Hide();
                vertexPool.Add(view);
            }
        }


        private void EnsurePanelCapacity(
            int requiredCount)
        {
            while (panelPool.Count < requiredCount)
            {
                WallRunPreviewSegmentView view =
                    Instantiate(
                        panelPrefab,
                        panelParent);

                view.Hide();
                panelPool.Add(view);
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


        private void HideUnusedPanels(
            int firstUnusedIndex)
        {
            for (int index = firstUnusedIndex;
                 index < panelPool.Count;
                 index++)
            {
                panelPool[index].Hide();
            }
        }


        private bool ValidateReferences()
        {
            bool isValid = true;

            isValid &= RequireReference(
                mapHost,
                "GridMapHost");

            isValid &= RequireReference(
                targetResolver,
                "WallVertexTargetResolver");

            isValid &= RequireReference(
                definitionSelection,
                "DoorDefinitionSelectionHost");

            isValid &= RequireReference(
                vertexPrefab,
                "WallRunPreviewVertexView prefab");

            isValid &= RequireReference(
                panelPrefab,
                "WallRunPreviewSegmentView prefab");

            return isValid;
        }


        private bool RequireReference(
            UnityEngine.Object reference,
            string label)
        {
            if (reference != null)
            {
                return true;
            }

            Debug.LogError(
                $"DoorRunPreviewView has no {label} assigned.",
                this);

            return false;
        }
    }
}
