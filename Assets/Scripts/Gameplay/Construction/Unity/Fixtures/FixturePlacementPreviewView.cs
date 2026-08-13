using System.Collections.Generic;
using BigRetail.Construction.Unity.Cells;
using BigRetail.Map.Domain;
using BigRetail.Map.Fixtures;
using BigRetail.Map.View;
using BigRetail.Map.Unity.Fixtures;
using BigRetail.Map.Unity.View;
using BigRetail.Map.Unity.Walls;
using UnityEngine;

namespace BigRetail.Construction.Unity.Fixtures
{
    /// <summary>
    /// Shows the selected fixture footprint and its current placement result.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FixturePlacementPreviewView : MonoBehaviour
    {
        private const int PreviewBaseSortingOrder = 350;

        private static readonly FixtureInstanceId PreviewInstanceId =
            new FixtureInstanceId("fixture-placement-preview");

        [SerializeField]
        private FixtureRuntimeHost runtimeHost;

        [SerializeField]
        private FixtureDefinitionSelectionHost definitionSelection;

        [SerializeField]
        private GridCellTargetResolver targetResolver;

        [SerializeField]
        private IsometricViewHost viewHost;

        [SerializeField]
        private Transform previewParent;

        [SerializeField]
        private Color validColor = new Color(0.55f, 0.9f, 1f, 0.75f);

        [SerializeField]
        private Color invalidColor = new Color(1f, 0.2f, 0.2f, 0.82f);

        [SerializeField]
        private string sortingLayerName = "Default";


        private readonly List<SpriteRenderer> rendererPool =
            new List<SpriteRenderer>();

        private GridPosition requestedAnchorCell;
        private FixtureDefinitionId requestedDefinitionId;
        private FixtureOrientation requestedOrientation;
        private bool hasPlacementRequest;


        public bool IsVisible { get; private set; }

        public bool IsPlacementValid { get; private set; }

        public FixturePlacementFailure CurrentFailure { get; private set; }


        private void Awake()
        {
            if (!ValidateReferences())
            {
                enabled = false;
                return;
            }

            if (previewParent == null)
            {
                previewParent = transform;
            }

            Hide();
        }


        private void OnEnable()
        {
            if (viewHost != null)
            {
                viewHost.OrientationChanged +=
                    HandleViewOrientationChanged;
            }
        }


        public void ShowPlacement(
            GridPosition anchorCell,
            FixtureDefinitionId definitionId,
            FixtureOrientation orientation)
        {
            requestedAnchorCell = anchorCell;
            requestedDefinitionId = definitionId;
            requestedOrientation = orientation;
            hasPlacementRequest = true;

            if (!runtimeHost.TryInitialize()
                || !definitionSelection.IsInitialized
                || !viewHost.IsInitialized)
            {
                Hide();
                return;
            }

            FixturePlacementResult evaluation =
                runtimeHost.FixturePlacement.EvaluatePlacement(
                    PreviewInstanceId,
                    definitionId,
                    anchorCell,
                    orientation);

            FixtureFootprint footprint = evaluation.Footprint;

            if (footprint == null)
            {
                Hide();
                return;
            }

            FixtureDefinitionAsset asset =
                runtimeHost.DefinitionAssets.GetAsset(definitionId);

            Sprite sprite = asset.GetSprite(orientation, viewHost.Orientation);
            Color color = evaluation.Succeeded ? validColor : invalidColor;

            int rendererCount =
                asset.RepeatSpritePerOccupiedCell
                    ? footprint.CellCount
                    : 1;

            EnsureCapacity(rendererCount);

            if (asset.RepeatSpritePerOccupiedCell)
            {
                for (int index = 0; index < footprint.CellCount; index++)
                {
                    GridPosition cell = footprint.GetCell(index);
                    ShowRenderer(
                        rendererPool[index],
                        sprite,
                        color,
                        cell,
                        asset.WorldPositionOffset);
                }
            }
            else
            {
                GridPosition presentationAnchor =
                    FixturePresentationAnchorResolver
                        .ResolveViewerNearestCell(
                            footprint,
                            viewHost.Projection);

                ShowRenderer(
                    rendererPool[0],
                    sprite,
                    color,
                    presentationAnchor,
                    asset.WorldPositionOffset,
                    footprint,
                    asset.GetSpriteAnchorCorner(
                        orientation,
                        viewHost.Orientation));
            }

            HideUnused(rendererCount);

            IsVisible = true;
            IsPlacementValid = evaluation.Succeeded;
            CurrentFailure = evaluation.Failure;
        }


        public void Hide()
        {
            hasPlacementRequest = false;
            HideUnused(0);
            IsVisible = false;
            IsPlacementValid = false;
            CurrentFailure = FixturePlacementFailure.None;
        }


        private void HandleViewOrientationChanged(
            IsometricViewOrientation previousOrientation,
            IsometricViewOrientation currentOrientation)
        {
            if (!hasPlacementRequest)
            {
                return;
            }

            ShowPlacement(
                requestedAnchorCell,
                requestedDefinitionId,
                requestedOrientation);
        }


        private void EnsureCapacity(int requiredCount)
        {
            while (rendererPool.Count < requiredCount)
            {
                GameObject child = new GameObject("Fixture Preview Sprite");
                child.transform.SetParent(previewParent, worldPositionStays: true);
                SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
                renderer.sortingLayerName = sortingLayerName;
                rendererPool.Add(renderer);
            }
        }


        private void ShowRenderer(
            SpriteRenderer renderer,
            Sprite sprite,
            Color color,
            GridPosition cell,
            Vector3 offset,
            FixtureFootprint wholeFootprint = null,
            FixtureSpriteAnchorCorner anchorCorner =
                FixtureSpriteAnchorCorner.ViewerNearest)
        {
            renderer.sprite = sprite;
            renderer.color = color;

            GridPosition displayCell = viewHost.Projection.ToDisplayCell(cell);
            renderer.sortingOrder =
                PreviewBaseSortingOrder
                - (displayCell.X + displayCell.Y)
                * WallRenderOrderResolver.DisplayDepthOrderStep;

            Vector3 anchorWorldPosition =
                wholeFootprint != null
                    ? FixturePresentationAnchorResolver
                        .CalculateFootprintAnchorWorld(
                            targetResolver.CoordinateTilemap,
                            wholeFootprint,
                            viewHost.Projection,
                            anchorCorner,
                            viewHost.ToUnityCell(cell).z)
                    : viewHost.GetLogicalCellCenterWorld(
                        cell,
                        targetResolver.CoordinateTilemap);

            renderer.transform.position =
                anchorWorldPosition + offset;

            renderer.gameObject.SetActive(true);
        }


        private void HideUnused(int usedCount)
        {
            for (int index = usedCount; index < rendererPool.Count; index++)
            {
                rendererPool[index].gameObject.SetActive(false);
            }
        }


        private bool ValidateReferences()
        {
            bool isValid = true;

            if (runtimeHost == null)
            {
                Debug.LogError("FixturePlacementPreviewView has no FixtureRuntimeHost assigned.", this);
                isValid = false;
            }

            if (definitionSelection == null)
            {
                Debug.LogError("FixturePlacementPreviewView has no selection host assigned.", this);
                isValid = false;
            }

            if (targetResolver == null)
            {
                Debug.LogError("FixturePlacementPreviewView has no target resolver assigned.", this);
                isValid = false;
            }

            if (viewHost == null)
            {
                Debug.LogError("FixturePlacementPreviewView has no IsometricViewHost assigned.", this);
                isValid = false;
            }

            return isValid;
        }


        private void OnDisable()
        {
            if (viewHost != null)
            {
                viewHost.OrientationChanged -=
                    HandleViewOrientationChanged;
            }

            Hide();
        }
    }
}
