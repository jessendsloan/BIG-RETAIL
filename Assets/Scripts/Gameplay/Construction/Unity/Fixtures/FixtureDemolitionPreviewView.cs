using System.Collections.Generic;
using BigRetail.Construction.Unity.Cells;
using BigRetail.Map.Domain;
using BigRetail.Map.Fixtures;
using BigRetail.Map.Unity.Fixtures;
using BigRetail.Map.Unity.View;
using BigRetail.Map.View;
using UnityEngine;

namespace BigRetail.Construction.Unity.Fixtures
{
    /// <summary>
    /// Highlights one complete fixture before demolition. A multi-cell
    /// fixture is always presented as one removable object.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FixtureDemolitionPreviewView : MonoBehaviour
    {
        private const int PreviewBaseSortingOrder = 360;

        [SerializeField]
        private FixtureRuntimeHost runtimeHost;

        [SerializeField]
        private GridCellTargetResolver targetResolver;

        [SerializeField]
        private IsometricViewHost viewHost;

        [SerializeField]
        private Transform previewParent;

        [SerializeField]
        private Color demolitionColor =
            new Color(1f, 0.2f, 0.16f, 0.82f);

        [SerializeField]
        private string sortingLayerName = "Default";


        private readonly List<SpriteRenderer> rendererPool =
            new List<SpriteRenderer>();


        public bool IsVisible { get; private set; }

        public FixtureInstanceId FixtureId { get; private set; }


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
                viewHost.OrientationChanging +=
                    HandleOrientationChanging;
            }
        }


        public void ShowFixture(FixtureInstance fixture)
        {
            if (fixture == null
                || !runtimeHost.TryInitialize()
                || !viewHost.IsInitialized)
            {
                Hide();
                return;
            }

            FixtureDefinitionAsset asset =
                runtimeHost.DefinitionAssets.GetAsset(
                    fixture.DefinitionId);

            ShowPlacement(
                fixture.Id,
                fixture.Footprint,
                fixture.Orientation,
                asset);
        }

        public void ShowPlan(FixtureEquipmentPlan plan)
        {
            if (plan == null
                || !runtimeHost.TryInitialize()
                || !viewHost.IsInitialized)
            {
                Hide();
                return;
            }

            FixtureDefinitionAsset asset =
                runtimeHost.DefinitionAssets.GetAsset(
                    plan.FixtureDefinitionId);

            ShowPlacement(
                plan.Id,
                plan.Footprint,
                plan.Orientation,
                asset);
        }

        private void ShowPlacement(
            FixtureInstanceId instanceId,
            FixtureFootprint footprint,
            FixtureOrientation orientation,
            FixtureDefinitionAsset asset)
        {
            if (asset == null || footprint == null)
            {
                Hide();
                return;
            }

            Sprite sprite = asset.GetSprite(
                orientation,
                viewHost.Orientation);

            int rendererCount =
                asset.RepeatSpritePerOccupiedCell
                    ? footprint.CellCount
                    : 1;

            EnsureCapacity(rendererCount);

            if (asset.RepeatSpritePerOccupiedCell)
            {
                for (int index = 0;
                     index < footprint.CellCount;
                     index++)
                {
                    ShowRenderer(
                        rendererPool[index],
                        sprite,
                        footprint.GetCell(index),
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
                    presentationAnchor,
                    asset.WorldPositionOffset,
                    footprint,
                    asset.GetSpriteAnchorCorner(
                        orientation,
                        viewHost.Orientation));
            }

            HideUnused(rendererCount);
            FixtureId = instanceId;
            IsVisible = true;
        }


        public void Hide()
        {
            HideUnused(0);
            FixtureId = default;
            IsVisible = false;
        }


        private void EnsureCapacity(int requiredCount)
        {
            while (rendererPool.Count < requiredCount)
            {
                GameObject child =
                    new GameObject("Fixture Demolition Preview Sprite");

                child.transform.SetParent(
                    previewParent,
                    worldPositionStays: true);

                SpriteRenderer renderer =
                    child.AddComponent<SpriteRenderer>();

                renderer.sortingLayerName = sortingLayerName;
                rendererPool.Add(renderer);
            }
        }


        private void ShowRenderer(
            SpriteRenderer renderer,
            Sprite sprite,
            GridPosition cell,
            Vector3 offset,
            FixtureFootprint wholeFootprint = null,
            FixtureSpriteAnchorCorner anchorCorner =
                FixtureSpriteAnchorCorner.ViewerNearest)
        {
            renderer.sprite = sprite;
            renderer.color = demolitionColor;

            GridPosition displayCell =
                viewHost.Projection.ToDisplayCell(cell);

            renderer.sortingOrder =
                PreviewBaseSortingOrder
                - (displayCell.X + displayCell.Y)
                * IsometricRenderOrderResolver.DisplayDepthOrderStep;

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
            for (int index = usedCount;
                 index < rendererPool.Count;
                 index++)
            {
                rendererPool[index].gameObject.SetActive(false);
            }
        }


        private bool ValidateReferences()
        {
            bool isValid = true;

            if (runtimeHost == null)
            {
                Debug.LogError(
                    "FixtureDemolitionPreviewView has no FixtureRuntimeHost assigned.",
                    this);
                isValid = false;
            }

            if (targetResolver == null)
            {
                Debug.LogError(
                    "FixtureDemolitionPreviewView has no target resolver assigned.",
                    this);
                isValid = false;
            }

            if (viewHost == null)
            {
                Debug.LogError(
                    "FixtureDemolitionPreviewView has no IsometricViewHost assigned.",
                    this);
                isValid = false;
            }

            return isValid;
        }


        private void HandleOrientationChanging(
            IsometricViewOrientation previousOrientation,
            IsometricViewOrientation nextOrientation)
        {
            Hide();
        }


        private void OnDisable()
        {
            if (viewHost != null)
            {
                viewHost.OrientationChanging -=
                    HandleOrientationChanging;
            }

            Hide();
        }
    }
}
