using System;
using System.Collections.Generic;
using BigRetail.Map.Domain;
using BigRetail.Map.Fixtures;
using BigRetail.Map.Unity.View;
using BigRetail.Map.Unity.Walls;
using BigRetail.Map.View;
using BigRetail.Merchandise.Domain;
using BigRetail.Merchandise.Unity;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;

namespace BigRetail.Map.Unity.Fixtures
{
    /// <summary>
    /// Event-driven presentation for placed fixtures. Logical orientation
    /// remains stable while the selected sprite follows camera rotation.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-45)]
    public sealed class FixtureViewSystem : MonoBehaviour
    {
        private const float UnfocusedFixtureAlphaMultiplier = 0.28f;
        private const int MerchandisingFocusSortingOrderOffset = 1000;
        private const float DisplayMarkerWidthShare = 0.78f;
        private const float DisplayMarkerHeightShare = 0.62f;
        internal const float AuthoredDisplayProductWidthShare = 1.22f;
        internal const float AuthoredDisplayProductHeightShare = 1.93f;
        private const float AuthoredDisplayProductFrontageSpanShare = 0.86f;
        private const float AuthoredDisplayProductLeftOffsetShare = 0.16f;
        private const float AuthoredDisplayProductForwardOffsetShare = 0.48f;
        private const int DefaultMaximumBackstockCaseMarkerCount = 9;
        private const float DefaultBackstockCaseForwardOffsetShare = 0.30f;
        private const float DefaultBackstockCaseShelfSlopeDegrees =
            26.565052f;
        private const int DefaultPresentationLayerSortingStride = 3;

        [SerializeField]
        private FixtureRuntimeHost runtimeHost;

        [SerializeField]
        private FixturePlanogramRuntimeHost planogramRuntimeHost;

        [SerializeField]
        private IsometricViewHost viewHost;

        [SerializeField]
        private Tilemap coordinateTilemap;

        [SerializeField]
        private Transform viewParent;

        [SerializeField]
        private Color fixtureColor = Color.white;

        [SerializeField]
        private Sprite frontageMarkerSprite;

        [SerializeField]
        private string sortingLayerName = "Default";


        private readonly Dictionary<FixtureInstanceId, FixtureView>
            views = new Dictionary<FixtureInstanceId, FixtureView>();

        private FixtureState subscribedState;
        private FixtureBackstockService subscribedBackstock;
        private FixtureDisplayInventoryService subscribedDisplayInventory;
        private bool hasMerchandisingFocus;
        private FixtureInstanceId merchandisingFocusFixtureId;
        private Texture2D caseMarkerTexture;
        private Sprite caseMarkerSprite;


        public int VisibleFixtureCount => views.Count;

        public bool HasMerchandisingFocus => hasMerchandisingFocus;

        public FixtureInstanceId MerchandisingFocusFixtureId =>
            merchandisingFocusFixtureId;

        public event Action<FixtureInstance, SpriteRenderer>
            FixtureViewShown;

        public event Action<FixtureInstanceId> FixtureViewHidden;


        public bool TryGetPrimaryRenderer(
            FixtureInstanceId fixtureId,
            out SpriteRenderer renderer)
        {
            if (views.TryGetValue(
                    fixtureId,
                    out FixtureView view)
                && view.Renderers.Count > 0)
            {
                renderer = view.Renderers[0];
                return renderer != null;
            }

            renderer = null;
            return false;
        }

        public bool TryGetRenderers(
            FixtureInstanceId fixtureId,
            out IReadOnlyList<SpriteRenderer> renderers)
        {
            if (views.TryGetValue(
                    fixtureId,
                    out FixtureView view)
                && view.Renderers.Count > 0)
            {
                renderers = view.Renderers;
                return true;
            }

            renderers = null;
            return false;
        }

        public bool TryGetFixtureAtWorldPosition(
            Vector2 worldPosition,
            out FixtureInstance fixture)
        {
            FixtureView bestView = null;
            int bestSortingLayerValue = int.MinValue;
            int bestSortingOrder = int.MinValue;

            foreach (FixtureView view in views.Values)
            {
                for (int index = 0;
                     index < view.Renderers.Count;
                     index++)
                {
                    SpriteRenderer renderer = view.Renderers[index];

                    if (renderer == null
                        || renderer.sprite == null
                        || !renderer.bounds.Contains(worldPosition))
                    {
                        continue;
                    }

                    int sortingLayerId =
                        view.SortingGroup != null
                            ? view.SortingGroup.sortingLayerID
                            : renderer.sortingLayerID;
                    int sortingLayerValue =
                        SortingLayer.GetLayerValueFromID(sortingLayerId);
                    int sortingOrder =
                        view.SortingGroup != null
                            ? view.SortingGroup.sortingOrder
                            : renderer.sortingOrder;

                    if (bestView != null
                        && sortingLayerValue < bestSortingLayerValue)
                    {
                        continue;
                    }

                    if (bestView != null
                        && sortingLayerValue == bestSortingLayerValue
                        && sortingOrder <= bestSortingOrder)
                    {
                        continue;
                    }

                    bestView = view;
                    bestSortingLayerValue = sortingLayerValue;
                    bestSortingOrder = sortingOrder;
                }
            }

            if (bestView != null)
            {
                fixture = bestView.Fixture;
                return true;
            }

            fixture = null;
            return false;
        }

        /// <summary>
        /// Raises the fixture being merchandised above the surrounding scene
        /// and softens other fixtures so shelf controls remain readable.
        /// The logical id is retained while camera rotation rebuilds views.
        /// </summary>
        public void SetMerchandisingFocus(FixtureInstanceId fixtureId)
        {
            if (hasMerchandisingFocus
                && merchandisingFocusFixtureId == fixtureId)
            {
                return;
            }

            hasMerchandisingFocus = true;
            merchandisingFocusFixtureId = fixtureId;
            ApplyMerchandisingFocus();
        }

        public void ClearMerchandisingFocus()
        {
            if (!hasMerchandisingFocus)
            {
                return;
            }

            hasMerchandisingFocus = false;
            merchandisingFocusFixtureId = default;
            ApplyMerchandisingFocus();
        }

        public static Color ResolveMerchandisingFocusColor(
            Color baseColor,
            bool focusIsActive,
            bool isFocusedFixture)
        {
            if (!focusIsActive || isFocusedFixture)
            {
                return baseColor;
            }

            baseColor.a *= UnfocusedFixtureAlphaMultiplier;
            return baseColor;
        }

        public static int ResolveMerchandisingFocusSortingOrder(
            int baseSortingOrder,
            bool focusIsActive,
            bool isFocusedFixture)
        {
            return baseSortingOrder
                + (focusIsActive && isFocusedFixture
                    ? MerchandisingFocusSortingOrderOffset
                    : 0);
        }


        private void Awake()
        {
            if (planogramRuntimeHost == null)
            {
                planogramRuntimeHost =
                    GetComponent<FixturePlanogramRuntimeHost>();
            }

            if (!ValidateReferences())
            {
                enabled = false;
                return;
            }

            if (viewParent == null)
            {
                viewParent = transform;
            }
        }


        private void OnEnable()
        {
            runtimeHost.Initialized += HandleRuntimeInitialized;
            viewHost.OrientationChanging += HandleOrientationChanging;
            viewHost.OrientationChanged += HandleOrientationChanged;

            if (planogramRuntimeHost != null)
            {
                planogramRuntimeHost.Initialized +=
                    HandlePlanogramRuntimeInitialized;
            }
        }


        private void Start()
        {
            if (runtimeHost.IsInitialized)
            {
                AttachToState(runtimeHost.FixtureState);
            }

            AttachToBackstock();
            AttachToDisplayInventory();
        }


        private void OnDisable()
        {
            if (runtimeHost != null)
            {
                runtimeHost.Initialized -= HandleRuntimeInitialized;
            }

            if (viewHost != null)
            {
                viewHost.OrientationChanging -= HandleOrientationChanging;
                viewHost.OrientationChanged -= HandleOrientationChanged;
            }

            if (planogramRuntimeHost != null)
            {
                planogramRuntimeHost.Initialized -=
                    HandlePlanogramRuntimeInitialized;
            }

            DetachFromBackstock();
            DetachFromDisplayInventory();
            DetachFromState();
            ClearViews();
        }

        private void OnDestroy()
        {
            if (caseMarkerSprite != null)
            {
                Destroy(caseMarkerSprite);
                caseMarkerSprite = null;
            }

            if (caseMarkerTexture != null)
            {
                Destroy(caseMarkerTexture);
                caseMarkerTexture = null;
            }
        }


        private void HandleRuntimeInitialized(FixtureRuntimeHost initializedHost)
        {
            AttachToState(initializedHost.FixtureState);
        }

        private void HandlePlanogramRuntimeInitialized(
            FixturePlanogramRuntimeHost initializedHost)
        {
            AttachToBackstock();
            AttachToDisplayInventory();
            RebuildViews();
        }

        private void HandleBackstockContentsChanged()
        {
            RebuildViews();
        }


        private void HandleFixtureStockChanged(FixtureInstanceId fixtureId)
        {
            if (subscribedState == null
                || !subscribedState.TryGetFixture(
                    fixtureId,
                    out FixtureInstance fixture))
            {
                return;
            }

            ShowFixture(fixture);
        }


        private void HandleOrientationChanging(
            IsometricViewOrientation previous,
            IsometricViewOrientation next)
        {
            ClearViews();
        }


        private void HandleOrientationChanged(
            IsometricViewOrientation previous,
            IsometricViewOrientation current)
        {
            RebuildViews();
        }


        private void AttachToState(FixtureState state)
        {
            if (state == null)
            {
                Debug.LogError("FixtureViewSystem received a null FixtureState.", this);
                return;
            }

            if (subscribedState == state)
            {
                RebuildViews();
                return;
            }

            DetachFromState();
            subscribedState = state;
            subscribedState.FixtureAdded += HandleFixtureAdded;
            subscribedState.FixtureRemoved += HandleFixtureRemoved;
            RebuildViews();
        }


        private void DetachFromState()
        {
            if (subscribedState == null)
            {
                return;
            }

            subscribedState.FixtureAdded -= HandleFixtureAdded;
            subscribedState.FixtureRemoved -= HandleFixtureRemoved;
            subscribedState = null;
        }


        private void HandleFixtureAdded(FixtureInstance fixture)
        {
            ShowFixture(fixture);
        }


        private void HandleFixtureRemoved(FixtureInstance fixture)
        {
            HideFixture(fixture.Id);
        }


        private void RebuildViews()
        {
            ClearViews();

            if (subscribedState == null)
            {
                return;
            }

            foreach (FixtureInstance fixture in subscribedState.EnumerateFixtures())
            {
                ShowFixture(fixture);
            }
        }


        private void ShowFixture(FixtureInstance fixture)
        {
            HideFixture(fixture.Id);

            FixtureDefinitionAsset asset =
                runtimeHost.DefinitionAssets.GetAsset(fixture.DefinitionId);

            Sprite sprite = asset.GetSprite(fixture.Orientation, viewHost.Orientation);

            GameObject root = new GameObject($"Fixture {fixture.Id.Value}");
            root.transform.SetParent(viewParent, worldPositionStays: true);

            List<SpriteRenderer> renderers = new List<SpriteRenderer>();
            SortingGroup sortingGroup = null;
            int presentationLayerCount = 0;
            int presentationLayerSortingStride =
                DefaultPresentationLayerSortingStride;

            if (asset.RepeatSpritePerOccupiedCell)
            {
                for (int index = 0; index < fixture.OccupiedCellCount; index++)
                {
                    GridPosition cell = fixture.GetOccupiedCell(index);
                    SpriteRenderer renderer =
                        CreateRenderer(
                            root.transform,
                            sprite,
                            cell,
                            asset.WorldPositionOffset);
                    renderers.Add(renderer);
                }
            }
            else
            {
                GridPosition presentationAnchor =
                    FixturePresentationAnchorResolver
                        .ResolveViewerNearestCell(
                            fixture.Footprint,
                            viewHost.Projection);

                GridPosition sortingCell =
                    FixturePresentationAnchorResolver
                        .ResolveWholeFixtureSortingCell(
                            fixture.Definition,
                            fixture.Footprint,
                            viewHost.Projection);
                IReadOnlyList<Sprite> presentationLayers =
                    asset.GetPresentationLayers(
                        fixture.Orientation,
                        viewHost.Orientation);

                if (presentationLayers.Count > 0)
                {
                    FixtureMerchandisingProfile merchandisingProfile =
                        fixture.Definition.MerchandisingProfile;

                    for (int faceIndex = 0;
                         faceIndex < merchandisingProfile.DisplayFaceCount;
                         faceIndex++)
                    {
                        presentationLayerSortingStride =
                            Mathf.Max(
                                presentationLayerSortingStride,
                                ResolveDisplayPresentationLayerSortingStride(
                                    merchandisingProfile
                                        .GetDisplayFace(faceIndex)
                                        .FrontageUnitsPerRun));
                    }

                    IReadOnlyList<Sprite> storageShelfMasks =
                        asset.GetStorageShelfMasks(
                            fixture.Orientation,
                            viewHost.Orientation);
                    bool interleavesBackstockCases =
                        storageShelfMasks.Count > 0
                        && presentationLayers.Count
                            == storageShelfMasks.Count + 1;

                    if (interleavesBackstockCases)
                    {
                        presentationLayerSortingStride =
                            Mathf.Max(
                                presentationLayerSortingStride,
                                ResolveBackstockPresentationLayerSortingStride(
                                    asset.BackstockCasesPerShelf));
                    }

                    sortingGroup = root.AddComponent<SortingGroup>();
                    sortingGroup.sortingLayerName = sortingLayerName;
                    sortingGroup.sortingOrder =
                        WallRenderOrderResolver.ResolveCell(
                            viewHost.Projection.ToDisplayCell(sortingCell));
                    presentationLayerCount = presentationLayers.Count;

                    for (int layerIndex = 0;
                         layerIndex < presentationLayers.Count;
                         layerIndex++)
                    {
                        SpriteRenderer layerRenderer =
                            CreateRenderer(
                                root.transform,
                                presentationLayers[layerIndex],
                                presentationAnchor,
                                asset.WorldPositionOffset,
                                fixture.Footprint,
                                asset.GetSpriteAnchorCorner(
                                    fixture.Orientation,
                                    viewHost.Orientation),
                                sortingCell,
                                layerIndex
                                    * presentationLayerSortingStride,
                                useRelativeSortingOrder: true,
                                childName: $"Fixture Layer {layerIndex:00}");

                        renderers.Add(layerRenderer);
                    }
                }
                else
                {
                    SpriteRenderer renderer =
                        CreateRenderer(
                            root.transform,
                            sprite,
                            presentationAnchor,
                            asset.WorldPositionOffset,
                            fixture.Footprint,
                            asset.GetSpriteAnchorCorner(
                                fixture.Orientation,
                                viewHost.Orientation),
                            sortingCell);

                    renderers.Add(renderer);
                }
            }

            AddBackstockCaseMarkers(
                fixture,
                asset,
                presentationLayerCount,
                presentationLayerSortingStride,
                renderers);

            AddStockedDisplayMarkers(
                fixture,
                asset,
                presentationLayerCount,
                presentationLayerSortingStride,
                renderers);

            views.Add(
                fixture.Id,
                new FixtureView(
                    fixture,
                    root,
                    sortingGroup,
                    renderers));

            ApplyMerchandisingFocusToView(views[fixture.Id]);

            if (renderers.Count > 0)
            {
                FixtureViewShown?.Invoke(
                    fixture,
                    renderers[0]);
            }
        }


        private SpriteRenderer CreateRenderer(
            Transform parent,
            Sprite sprite,
            GridPosition cell,
            Vector3 worldPositionOffset,
            FixtureFootprint wholeFootprint = null,
            FixtureSpriteAnchorCorner anchorCorner =
                FixtureSpriteAnchorCorner.ViewerNearest,
            GridPosition? sortingCellOverride = null,
            int sortingOrderOffset = 0,
            bool useRelativeSortingOrder = false,
            string childName = "Fixture Sprite")
        {
            GameObject child = new GameObject(childName);
            child.transform.SetParent(parent, worldPositionStays: true);

            SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = fixtureColor;
            renderer.sortingLayerName = sortingLayerName;

            GridPosition sortingCell =
                sortingCellOverride
                ?? cell;

            GridPosition displayCell =
                viewHost.Projection.ToDisplayCell(
                    sortingCell);
            renderer.sortingOrder =
                useRelativeSortingOrder
                    ? sortingOrderOffset
                    : WallRenderOrderResolver.ResolveCell(displayCell)
                        + sortingOrderOffset;

            Vector3 anchorWorldPosition =
                wholeFootprint != null
                    ? FixturePresentationAnchorResolver
                        .CalculateFootprintAnchorWorld(
                            coordinateTilemap,
                            wholeFootprint,
                            viewHost.Projection,
                            anchorCorner,
                            viewHost.ToUnityCell(cell).z)
                    : viewHost.GetLogicalCellCenterWorld(
                        cell,
                        coordinateTilemap);

            child.transform.position =
                anchorWorldPosition + worldPositionOffset;

            return renderer;
        }

        private void AddBackstockCaseMarkers(
            FixtureInstance fixture,
            FixtureDefinitionAsset definitionAsset,
            int presentationLayerCount,
            int presentationLayerSortingStride,
            List<SpriteRenderer> renderers)
        {
            if (subscribedBackstock == null
                || !fixture.Definition.StorageProfile
                    .ProvidesBackstockStorage
                || renderers.Count == 0
                || renderers[0] == null
                || renderers[0].sprite == null)
            {
                return;
            }

            List<FixtureBackstockProductSnapshot> contents =
                new List<FixtureBackstockProductSnapshot>(
                    subscribedBackstock
                        .EnumerateRackContents(fixture.Id));
            List<FixtureBackstockCaseSnapshot> storedCases =
                new List<FixtureBackstockCaseSnapshot>(
                    subscribedBackstock
                        .EnumerateRackCases(fixture.Id));

            int storedUnitCount =
                subscribedBackstock
                    .GetRackStoredUnitCount(fixture.Id);

            if (contents.Count == 0
                || storedUnitCount == 0)
            {
                return;
            }

            int markerCount =
                ResolveBackstockCaseMarkerCount(
                    storedCases.Count,
                    storedUnitCount,
                    definitionAsset.BackstockCaseSlotCapacity > 0
                        ? definitionAsset.BackstockCaseSlotCapacity
                        : DefaultMaximumBackstockCaseMarkerCount);

            SpriteRenderer fixtureRenderer = renderers[0];
            Bounds spriteBounds = fixtureRenderer.sprite.bounds;
            IReadOnlyList<Sprite> shelfMasks =
                definitionAsset.GetStorageShelfMasks(
                    fixture.Orientation,
                    viewHost.Orientation);
            bool hasAuthoredShelfMasks = shelfMasks.Count > 0;
            bool interleaveWithPresentationLayers =
                presentationLayerCount == shelfMasks.Count + 1;
            int casesPerShelf = definitionAsset.BackstockCasesPerShelf;
            float caseWidthPerSlot =
                definitionAsset.BackstockCaseWidthPerSlot;
            float caseSpacingShare =
                definitionAsset.BackstockCaseSpacingShare;
            float caseRowOffsetShare =
                definitionAsset.BackstockCaseRowOffsetShare;
            float caseFrontOffsetShare =
                definitionAsset.BackstockCaseFrontOffsetShare;
            float boxWidth =
                spriteBounds.size.x
                / casesPerShelf
                * caseWidthPerSlot;
            float boxHeight = spriteBounds.size.y * 0.055f;
            float slope =
                fixtureRenderer.sprite.name.IndexOf(
                    "RisingLeft",
                    StringComparison.OrdinalIgnoreCase) >= 0
                    ? -DefaultBackstockCaseShelfSlopeDegrees
                    : DefaultBackstockCaseShelfSlopeDegrees;

            int cumulativeQuantity = 0;
            int contentIndex = 0;

            for (int markerIndex = 0;
                 markerIndex < markerCount;
                 markerIndex++)
            {
                ProductId markerProductId;

                if (storedCases.Count > 0)
                {
                    int storedCaseIndex = Mathf.Clamp(
                        Mathf.FloorToInt(
                            (markerIndex + 0.5f)
                            * storedCases.Count
                            / markerCount),
                        0,
                        storedCases.Count - 1);
                    markerProductId =
                        storedCases[storedCaseIndex].ProductId;
                }
                else
                {
                    float sampledUnit =
                        (markerIndex + 0.5f)
                        * storedUnitCount
                        / markerCount;

                    while (contentIndex < contents.Count - 1
                           && sampledUnit
                           > cumulativeQuantity
                             + contents[contentIndex].Quantity)
                    {
                        cumulativeQuantity +=
                            contents[contentIndex].Quantity;
                        contentIndex++;
                    }

                    markerProductId =
                        contents[contentIndex].ProductId;
                }

                int column = markerIndex % casesPerShelf;
                int shelf = markerIndex / casesPerShelf;
                Vector3 localPosition;
                Vector2 markerSize;
                float markerSlope;
                float forwardOffsetBasis;

                if (hasAuthoredShelfMasks
                    && shelf < shelfMasks.Count)
                {
                    int shelfMaskIndex = shelfMasks.Count - 1 - shelf;
                    Sprite shelfMask = shelfMasks[shelfMaskIndex];

                    if (!FixtureShelfMaskGeometry.TryCreate(
                            shelfMask,
                            out FixtureShelfMaskGeometry geometry))
                    {
                        continue;
                    }

                    Vector2 shelfCenter =
                        geometry.GetFrontageCenter(
                            visualFrontageIndex: 0,
                            frontageUnitCount: 1);
                    Vector2 center =
                        ResolveBackstockCasePackedCenter(
                            shelfCenter,
                            geometry.GetFrontageCenter(
                                column,
                                casesPerShelf),
                            caseSpacingShare);
                    Vector2 frontageStep =
                        casesPerShelf > 1
                            ? geometry.GetFrontageCenter(
                                visualFrontageIndex: 1,
                                frontageUnitCount: casesPerShelf)
                                - geometry.GetFrontageCenter(
                                    visualFrontageIndex: 0,
                                    frontageUnitCount: casesPerShelf)
                            : Vector2.zero;
                    center =
                        ResolveBackstockCaseRowCenter(
                            center,
                            frontageStep,
                            caseRowOffsetShare);
                    center =
                        ResolveBackstockCaseRailAlignedCenter(
                            shelfCenter,
                            center,
                            slope);
                    localPosition = new Vector3(center.x, center.y, 0f);
                    markerSize =
                        new Vector2(
                            geometry.MajorLength
                                / casesPerShelf
                                * caseWidthPerSlot,
                            geometry.MinorLength * 0.68f);
                    markerSlope = slope;
                    forwardOffsetBasis = geometry.MinorLength;
                }
                else
                {
                    float normalizedX =
                        Mathf.Lerp(
                            0.18f,
                            0.82f,
                            (column + 0.5f) / casesPerShelf);
                    normalizedX =
                        Mathf.Lerp(
                            0.5f,
                            normalizedX,
                            caseSpacingShare);
                    normalizedX +=
                        0.64f
                        / casesPerShelf
                        * caseRowOffsetShare;
                    float normalizedY = 0.22f + shelf * 0.205f;
                    localPosition =
                        new Vector3(
                            Mathf.Lerp(
                                spriteBounds.min.x,
                                spriteBounds.max.x,
                                normalizedX),
                            Mathf.Lerp(
                                spriteBounds.min.y,
                                spriteBounds.max.y,
                                normalizedY),
                            0f);
                    markerSize = new Vector2(boxWidth, boxHeight);
                    markerSlope = slope;
                    forwardOffsetBasis = markerSize.y;
                }

                localPosition =
                    ResolveBackstockCaseShelfPosition(
                        localPosition,
                        forwardOffsetBasis,
                        caseFrontOffsetShare);
                int caseDepthOrder =
                    ResolveBackstockCaseDepthOrder(
                        column,
                        casesPerShelf,
                        markerSlope);

                AddCaseMarkerRenderer(
                    fixtureRenderer,
                    localPosition,
                    markerSize,
                    markerSlope,
                    fixtureRenderer.sortingOrder
                        + (interleaveWithPresentationLayers
                            ? shelf
                                * presentationLayerSortingStride
                            : 0),
                    caseDepthOrder,
                    ResolveCaseSprite(
                        markerProductId,
                        markerSlope < 0f),
                    FixtureMerchandisingGrayboxPalette
                        .ResolveProductColor(
                            markerProductId),
                    renderers);
            }
        }


        public static int ResolveBackstockCaseMarkerCount(
            int physicalCaseCount,
            int storedUnitCount)
        {
            return ResolveBackstockCaseMarkerCount(
                physicalCaseCount,
                storedUnitCount,
                DefaultMaximumBackstockCaseMarkerCount);
        }


        public static int ResolveBackstockCaseMarkerCount(
            int physicalCaseCount,
            int storedUnitCount,
            int maximumMarkerCount)
        {
            maximumMarkerCount = Mathf.Max(1, maximumMarkerCount);

            if (physicalCaseCount > 0)
            {
                return Mathf.Clamp(
                    physicalCaseCount,
                    1,
                    maximumMarkerCount);
            }

            if (storedUnitCount <= 0)
            {
                return 0;
            }

            return 1;
        }


        public static Vector3 ResolveBackstockCaseShelfPosition(
            Vector3 shelfCenter,
            float shelfDepth)
        {
            return ResolveBackstockCaseShelfPosition(
                shelfCenter,
                shelfDepth,
                DefaultBackstockCaseForwardOffsetShare);
        }


        public static int ResolveBackstockPresentationLayerSortingStride(
            int casesPerShelf)
        {
            return Mathf.Max(
                DefaultPresentationLayerSortingStride,
                Mathf.Max(1, casesPerShelf) + 3);
        }


        public static int ResolveBackstockCaseDepthOrder(
            int column,
            int casesPerShelf,
            float shelfSlopeDegrees)
        {
            int clampedCasesPerShelf = Mathf.Max(1, casesPerShelf);
            int clampedColumn = Mathf.Clamp(
                column,
                0,
                clampedCasesPerShelf - 1);

            return shelfSlopeDegrees < 0f
                ? clampedColumn
                : clampedCasesPerShelf - 1 - clampedColumn;
        }


        public static Vector2 ResolveBackstockCasePackedCenter(
            Vector2 shelfCenter,
            Vector2 slotCenter,
            float spacingShare)
        {
            return Vector2.Lerp(
                shelfCenter,
                slotCenter,
                Mathf.Clamp01(spacingShare));
        }


        public static Vector2 ResolveBackstockCaseRowCenter(
            Vector2 caseCenter,
            Vector2 frontageStep,
            float rowOffsetShare)
        {
            return caseCenter
                + frontageStep
                    * Mathf.Clamp(rowOffsetShare, -0.5f, 0.5f);
        }


        public static Vector2 ResolveBackstockCaseRailAlignedCenter(
            Vector2 shelfCenter,
            Vector2 caseCenter,
            float shelfSlopeDegrees)
        {
            float horizontalOffset = caseCenter.x - shelfCenter.x;
            float slope =
                Mathf.Tan(
                    Mathf.Clamp(
                        shelfSlopeDegrees,
                        -80f,
                        80f)
                    * Mathf.Deg2Rad);

            return new Vector2(
                caseCenter.x,
                shelfCenter.y + horizontalOffset * slope);
        }


        public static Vector3 ResolveBackstockCaseShelfPosition(
            Vector3 shelfCenter,
            float shelfDepth,
            float forwardOffsetShare)
        {
            return shelfCenter
                + Vector3.down
                    * Mathf.Max(0f, shelfDepth)
                    * Mathf.Clamp01(forwardOffsetShare);
        }


        private void AddStockedDisplayMarkers(
            FixtureInstance fixture,
            FixtureDefinitionAsset definitionAsset,
            int presentationLayerCount,
            int presentationLayerSortingStride,
            List<SpriteRenderer> renderers)
        {
            if (subscribedDisplayInventory == null
                || planogramRuntimeHost?.PlanogramState == null
                || fixture.Definition.MerchandisingProfile.DisplayFaceCount == 0
                || renderers.Count == 0
                || renderers[0] == null)
            {
                return;
            }

            SpriteRenderer fixtureRenderer = renderers[0];
            FixtureMerchandisingProfile profile =
                fixture.Definition.MerchandisingProfile;

            for (int faceIndex = 0;
                 faceIndex < profile.DisplayFaceCount;
                 faceIndex++)
            {
                FixtureDisplayFaceDefinition displayFace =
                    profile.GetDisplayFace(faceIndex);
                IReadOnlyList<Sprite> shelfMasks =
                    definitionAsset.GetMerchandisingShelfMasks(
                        displayFace.LocalSide,
                        fixture.Orientation,
                        viewHost.Orientation);

                if (shelfMasks.Count != displayFace.ShelfRunCount)
                {
                    continue;
                }

                FixtureSide worldSide =
                    displayFace.LocalSide.Rotate(fixture.Orientation);
                FixtureSide relativeSide =
                    (FixtureSide)(
                        ((int)worldSide
                            - (int)viewHost.Orientation
                            + 4)
                        % 4);
                bool isViewerNear =
                    relativeSide == FixtureSide.South
                    || relativeSide == FixtureSide.West;
                bool reverseFrontage =
                    relativeSide == FixtureSide.North
                    || relativeSide == FixtureSide.West;

                for (int shelfIndex = 0;
                     shelfIndex < shelfMasks.Count;
                     shelfIndex++)
                {
                    if (!FixtureShelfMaskGeometry.TryCreate(
                            shelfMasks[shelfIndex],
                            out FixtureShelfMaskGeometry geometry))
                    {
                        continue;
                    }

                    FixtureShelfRunKey shelfRun =
                        new FixtureShelfRunKey(
                            fixture.Id,
                            displayFace.LocalSide,
                            shelfIndex);

                    for (int unitIndex = 0;
                         unitIndex < displayFace.FrontageUnitsPerRun;
                         unitIndex++)
                    {
                        if (!planogramRuntimeHost.PlanogramState
                                .TryGetProductAt(
                                    shelfRun,
                                    unitIndex,
                                    out ProductId productId))
                        {
                            continue;
                        }

                        float fillRatio =
                            subscribedDisplayInventory.GetFrontageFillRatio(
                                shelfRun,
                                unitIndex);

                        if (fillRatio <= 0f)
                        {
                            continue;
                        }

                        int visualUnitIndex =
                            reverseFrontage
                                ? displayFace.FrontageUnitsPerRun
                                    - 1
                                    - unitIndex
                                : unitIndex;

                        bool hasAuthoredSlotAnchor =
                            definitionAsset
                                .TryGetMerchandisingProductAnchor(
                                    displayFace.LocalSide,
                                    fixture.Orientation,
                                    viewHost.Orientation,
                                    shelfIndex,
                                    visualUnitIndex,
                                    displayFace.FrontageUnitsPerRun,
                                    out Vector2 authoredSlotAnchor);

                        Sprite authoredProductSprite =
                            ResolveOnShelfProductSprite(
                                productId,
                                geometry.MajorAxisAngleDegrees < 0f,
                                fillRatio);

                        if (authoredProductSprite == null
                            && frontageMarkerSprite == null)
                        {
                            continue;
                        }

                        int shelfSortingOrder =
                            ResolveStockedDisplayMarkerSortingOrder(
                                fixtureRenderer.sortingOrder,
                                shelfIndex,
                                shelfMasks.Count,
                                presentationLayerCount,
                                presentationLayerSortingStride,
                                isViewerNear);
                        int frontageSortingOrder =
                            ResolveStockedDisplayFrontageSortingOrder(
                                shelfSortingOrder,
                                visualUnitIndex,
                                displayFace.FrontageUnitsPerRun,
                                geometry.MajorAxisAngleDegrees);

                        AddStockedDisplayMarker(
                            fixtureRenderer,
                            geometry,
                            visualUnitIndex,
                            displayFace.FrontageUnitsPerRun,
                            frontageSortingOrder,
                            authoredProductSprite,
                            hasAuthoredSlotAnchor,
                            authoredSlotAnchor,
                            FixtureMerchandisingGrayboxPalette
                                .ResolveStockColor(productId, fillRatio),
                            renderers);
                    }
                }
            }
        }


        private void AddStockedDisplayMarker(
            SpriteRenderer fixtureRenderer,
            FixtureShelfMaskGeometry geometry,
            int visualFrontageIndex,
            int frontageUnitCount,
            int sortingOrder,
            Sprite authoredProductSprite,
            bool hasAuthoredSlotAnchor,
            Vector2 authoredSlotAnchor,
            Color color,
            List<SpriteRenderer> renderers)
        {
            GameObject markerObject =
                new GameObject("Stocked Display Product");
            markerObject.transform.SetParent(
                fixtureRenderer.transform,
                worldPositionStays: false);

            Vector2 localCenter =
                authoredProductSprite != null
                    && hasAuthoredSlotAnchor
                ? authoredSlotAnchor
                : authoredProductSprite != null
                ? ResolveAuthoredDisplayProductCenter(
                    geometry,
                    visualFrontageIndex,
                    frontageUnitCount)
                : geometry.GetFrontageCenter(
                    visualFrontageIndex,
                    frontageUnitCount);
            markerObject.transform.localPosition =
                new Vector3(localCenter.x, localCenter.y, 0f);

            SpriteRenderer renderer =
                markerObject.AddComponent<SpriteRenderer>();
            Sprite markerSprite =
                authoredProductSprite != null
                    ? authoredProductSprite
                    : frontageMarkerSprite;
            renderer.sprite = markerSprite;
            renderer.color =
                authoredProductSprite != null
                    ? Color.white
                    : color;
            renderer.sortingLayerName = sortingLayerName;
            renderer.sortingOrder = sortingOrder;

            Bounds markerBounds = markerSprite.bounds;
            float frontageUnitLength =
                geometry.MajorLength / frontageUnitCount;
            float desiredWidth =
                Mathf.Max(
                    frontageUnitLength
                    * (authoredProductSprite != null
                        ? AuthoredDisplayProductWidthShare
                        : DisplayMarkerWidthShare),
                    0.03f);
            float desiredHeight =
                authoredProductSprite != null
                    ? Mathf.Max(
                        frontageUnitLength
                        * AuthoredDisplayProductHeightShare,
                        0.02f)
                    : Mathf.Max(
                        geometry.MinorLength * DisplayMarkerHeightShare,
                        0.02f);
            float widthScale =
                desiredWidth
                / Mathf.Max(markerBounds.size.x, 0.001f);
            float authoredScale =
                ResolveAuthoredProductUniformScale(
                    markerBounds,
                    desiredWidth,
                    desiredHeight);
            markerObject.transform.localScale =
                authoredProductSprite != null
                    ? new Vector3(
                        authoredScale,
                        authoredScale,
                        1f)
                    : new Vector3(
                        widthScale,
                        desiredHeight
                        / Mathf.Max(markerBounds.size.y, 0.001f),
                        1f);
            renderers.Add(renderer);
        }


        public static float ResolveAuthoredProductUniformScale(
            Bounds spriteBounds,
            float maximumWidth,
            float maximumHeight)
        {
            float widthScale =
                Mathf.Max(0f, maximumWidth)
                / Mathf.Max(spriteBounds.size.x, 0.001f);
            float heightScale =
                Mathf.Max(0f, maximumHeight)
                / Mathf.Max(spriteBounds.size.y, 0.001f);

            return Mathf.Min(widthScale, heightScale);
        }


        public static Vector2 ResolveAuthoredDisplayProductCenter(
            FixtureShelfMaskGeometry geometry,
            int visualFrontageIndex,
            int frontageUnitCount)
        {
            Vector2 defaultCenter =
                geometry.GetFrontageCenter(
                    visualFrontageIndex,
                    frontageUnitCount);
            Vector2 firstCenter =
                geometry.GetFrontageCenter(
                    visualFrontageIndex: 0,
                    frontageUnitCount: frontageUnitCount);
            Vector2 lastCenter =
                geometry.GetFrontageCenter(
                    visualFrontageIndex: frontageUnitCount - 1,
                    frontageUnitCount: frontageUnitCount);
            Vector2 shelfCenter = (firstCenter + lastCenter) * 0.5f;
            float frontageUnitLength =
                geometry.MajorLength / frontageUnitCount;

            return Vector2.Lerp(
                    shelfCenter,
                    defaultCenter,
                    AuthoredDisplayProductFrontageSpanShare)
                + Vector2.left
                    * frontageUnitLength
                    * AuthoredDisplayProductLeftOffsetShare
                + Vector2.down
                    * geometry.MinorLength
                    * AuthoredDisplayProductForwardOffsetShare;
        }


        private Sprite ResolveOnShelfProductSprite(
            ProductId productId,
            bool risingLeft,
            float fillRatio)
        {
            if (planogramRuntimeHost == null
                || !planogramRuntimeHost.TryGetProductAsset(
                    productId,
                    out ProductDefinitionAsset productAsset))
            {
                return null;
            }

            return productAsset.GetOnShelfImage(
                risingLeft,
                fillRatio);
        }


        public static int ResolveStockedDisplayMarkerSortingOrder(
            int baseSortingOrder,
            int shelfIndex,
            int shelfCount,
            int presentationLayerCount,
            int presentationLayerSortingStride,
            bool isViewerNear)
        {
            int clampedShelfCount = Mathf.Max(0, shelfCount);

            if (clampedShelfCount > 0
                && presentationLayerCount == clampedShelfCount)
            {
                int clampedShelfIndex = Mathf.Clamp(
                    shelfIndex,
                    0,
                    clampedShelfCount - 1);
                int supportingLayerIndex =
                    clampedShelfCount - 1 - clampedShelfIndex;

                return baseSortingOrder
                    + supportingLayerIndex
                        * Mathf.Max(1, presentationLayerSortingStride)
                    + 1;
            }

            return baseSortingOrder + (isViewerNear ? 6 : 1);
        }


        public static int ResolveDisplayPresentationLayerSortingStride(
            int frontageUnitCount)
        {
            return Mathf.Max(
                DefaultPresentationLayerSortingStride,
                Mathf.Max(1, frontageUnitCount) + 3);
        }


        public static int ResolveStockedDisplayFrontageSortingOrder(
            int shelfSortingOrder,
            int visualFrontageIndex,
            int frontageUnitCount,
            float majorAxisAngleDegrees)
        {
            int clampedFrontageCount = Mathf.Max(1, frontageUnitCount);
            int clampedVisualIndex = Mathf.Clamp(
                visualFrontageIndex,
                0,
                clampedFrontageCount - 1);
            int depthOffset =
                majorAxisAngleDegrees >= 0f
                    ? clampedFrontageCount - 1 - clampedVisualIndex
                    : clampedVisualIndex;

            return shelfSortingOrder + depthOffset;
        }

        private void AddCaseMarkerRenderer(
            SpriteRenderer fixtureRenderer,
            Vector3 localPosition,
            Vector2 size,
            float slope,
            int baseSortingOrder,
            int caseDepthOrder,
            Sprite authoredCaseSprite,
            Color color,
            List<SpriteRenderer> renderers)
        {
            Sprite markerSprite =
                authoredCaseSprite != null
                    ? authoredCaseSprite
                    : GetOrCreateCaseMarkerSprite();
            bool usesAuthoredCaseSprite = authoredCaseSprite != null;
            Quaternion rotation =
                usesAuthoredCaseSprite
                    ? Quaternion.identity
                    : Quaternion.Euler(0f, 0f, slope);
            Vector3 markerScale =
                usesAuthoredCaseSprite
                    ? ResolveAuthoredCaseScale(
                        authoredCaseSprite,
                        size.x)
                    : new Vector3(size.x, size.y, 1f);
            float shadowOffset =
                usesAuthoredCaseSprite
                    ? markerScale.y
                        * authoredCaseSprite.bounds.size.y
                        * 0.035f
                    : size.y * 0.16f;

            GameObject shadowObject =
                new GameObject("Backstock Case Shadow");
            shadowObject.transform.SetParent(
                fixtureRenderer.transform,
                worldPositionStays: false);
            shadowObject.transform.localPosition =
                localPosition + new Vector3(0f, -shadowOffset, 0f);
            shadowObject.transform.localRotation =
                rotation;
            shadowObject.transform.localScale =
                usesAuthoredCaseSprite
                    ? new Vector3(
                        markerScale.x * 1.04f,
                        markerScale.y * 1.04f,
                        1f)
                    : new Vector3(
                        size.x * 1.12f,
                        size.y * 1.32f,
                        1f);

            SpriteRenderer shadowRenderer =
                shadowObject.AddComponent<SpriteRenderer>();
            shadowRenderer.sprite = markerSprite;
            shadowRenderer.color = new Color(0.12f, 0.14f, 0.15f, 0.92f);
            shadowRenderer.sortingLayerName = sortingLayerName;
            shadowRenderer.sortingOrder =
                baseSortingOrder + 1;
            renderers.Add(shadowRenderer);

            GameObject caseObject =
                new GameObject("Backstock Product Case");
            caseObject.transform.SetParent(
                fixtureRenderer.transform,
                worldPositionStays: false);
            caseObject.transform.localPosition = localPosition;
            caseObject.transform.localRotation =
                rotation;
            caseObject.transform.localScale = markerScale;

            SpriteRenderer caseRenderer =
                caseObject.AddComponent<SpriteRenderer>();
            caseRenderer.sprite = markerSprite;
            caseRenderer.color =
                usesAuthoredCaseSprite
                    ? Color.white
                    : color;
            caseRenderer.sortingLayerName = sortingLayerName;
            caseRenderer.sortingOrder =
                baseSortingOrder
                + 2
                + Mathf.Max(0, caseDepthOrder);
            renderers.Add(caseRenderer);
        }

        private Sprite ResolveCaseSprite(
            ProductId productId,
            bool risingLeft)
        {
            return planogramRuntimeHost != null
                && planogramRuntimeHost.TryGetProductAsset(
                    productId,
                    out ProductDefinitionAsset productAsset)
                ? productAsset.GetCaseImage(risingLeft)
                : null;
        }

        private static Vector3 ResolveAuthoredCaseScale(
            Sprite caseSprite,
            float desiredWidth)
        {
            float scale =
                Mathf.Max(desiredWidth, 0.03f)
                / Mathf.Max(caseSprite.bounds.size.x, 0.001f);

            return new Vector3(scale, scale, 1f);
        }

        private Sprite GetOrCreateCaseMarkerSprite()
        {
            if (caseMarkerSprite != null)
            {
                return caseMarkerSprite;
            }

            caseMarkerTexture =
                new Texture2D(
                    1,
                    1,
                    TextureFormat.RGBA32,
                    mipChain: false);
            caseMarkerTexture.name = "Backstock Case Marker Texture";
            caseMarkerTexture.SetPixel(0, 0, Color.white);
            caseMarkerTexture.Apply(
                updateMipmaps: false,
                makeNoLongerReadable: true);

            caseMarkerSprite =
                Sprite.Create(
                    caseMarkerTexture,
                    new Rect(0f, 0f, 1f, 1f),
                    new Vector2(0.5f, 0.5f),
                    1f);
            caseMarkerSprite.name = "Backstock Case Marker";
            return caseMarkerSprite;
        }

        private void AttachToBackstock()
        {
            FixtureBackstockService nextBackstock =
                planogramRuntimeHost != null
                    ? planogramRuntimeHost.Backstock
                    : null;

            if (subscribedBackstock == nextBackstock)
            {
                return;
            }

            DetachFromBackstock();
            subscribedBackstock = nextBackstock;

            if (subscribedBackstock != null)
            {
                subscribedBackstock.ContentsChanged +=
                    HandleBackstockContentsChanged;
            }
        }

        private void DetachFromBackstock()
        {
            if (subscribedBackstock == null)
            {
                return;
            }

            subscribedBackstock.ContentsChanged -=
                HandleBackstockContentsChanged;
            subscribedBackstock = null;
        }


        private void AttachToDisplayInventory()
        {
            FixtureDisplayInventoryService nextDisplayInventory =
                planogramRuntimeHost != null
                    ? planogramRuntimeHost.DisplayInventory
                    : null;

            if (subscribedDisplayInventory == nextDisplayInventory)
            {
                return;
            }

            DetachFromDisplayInventory();
            subscribedDisplayInventory = nextDisplayInventory;

            if (subscribedDisplayInventory != null)
            {
                subscribedDisplayInventory.FixtureStockChanged +=
                    HandleFixtureStockChanged;
            }
        }


        private void DetachFromDisplayInventory()
        {
            if (subscribedDisplayInventory == null)
            {
                return;
            }

            subscribedDisplayInventory.FixtureStockChanged -=
                HandleFixtureStockChanged;
            subscribedDisplayInventory = null;
        }


        private void HideFixture(FixtureInstanceId fixtureId)
        {
            if (!views.TryGetValue(fixtureId, out FixtureView view))
            {
                return;
            }

            views.Remove(fixtureId);
            FixtureViewHidden?.Invoke(fixtureId);
            Destroy(view.Root);
        }


        private void ClearViews()
        {
            List<FixtureInstanceId> fixtureIds =
                new List<FixtureInstanceId>(views.Keys);

            for (int index = 0;
                 index < fixtureIds.Count;
                 index++)
            {
                HideFixture(fixtureIds[index]);
            }
        }


        private void ApplyMerchandisingFocus()
        {
            foreach (FixtureView view in views.Values)
            {
                ApplyMerchandisingFocusToView(view);
            }
        }


        private void ApplyMerchandisingFocusToView(FixtureView view)
        {
            bool isFocusedFixture =
                hasMerchandisingFocus
                && view.Fixture.Id == merchandisingFocusFixtureId;

            for (int index = 0;
                 index < view.Renderers.Count;
                 index++)
            {
                SpriteRenderer renderer = view.Renderers[index];

                if (renderer == null)
                {
                    continue;
                }

                Color baseColor = view.GetBaseColor(index);
                int baseSortingOrder = view.GetBaseSortingOrder(index);

                renderer.color =
                    ResolveMerchandisingFocusColor(
                        baseColor,
                        hasMerchandisingFocus,
                        isFocusedFixture);

                renderer.sortingOrder =
                    view.SortingGroup != null
                        ? baseSortingOrder
                        : ResolveMerchandisingFocusSortingOrder(
                            baseSortingOrder,
                            hasMerchandisingFocus,
                            isFocusedFixture);
            }

            if (view.SortingGroup != null)
            {
                view.SortingGroup.sortingOrder =
                    ResolveMerchandisingFocusSortingOrder(
                        view.BaseSortingGroupOrder,
                        hasMerchandisingFocus,
                        isFocusedFixture);
            }
        }


        private bool ValidateReferences()
        {
            bool isValid = true;

            if (runtimeHost == null)
            {
                Debug.LogError("FixtureViewSystem has no FixtureRuntimeHost assigned.", this);
                isValid = false;
            }

            if (viewHost == null)
            {
                Debug.LogError("FixtureViewSystem has no IsometricViewHost assigned.", this);
                isValid = false;
            }

            if (coordinateTilemap == null)
            {
                Debug.LogError("FixtureViewSystem has no coordinate Tilemap assigned.", this);
                isValid = false;
            }

            return isValid;
        }


        private sealed class FixtureView
        {
            private readonly Color[] baseColors;
            private readonly int[] baseSortingOrders;

            public FixtureView(
                FixtureInstance fixture,
                GameObject root,
                SortingGroup sortingGroup,
                IReadOnlyList<SpriteRenderer> renderers)
            {
                Fixture = fixture;
                Root = root;
                SortingGroup = sortingGroup;
                BaseSortingGroupOrder =
                    sortingGroup != null
                        ? sortingGroup.sortingOrder
                        : 0;
                Renderers = renderers;
                baseColors = new Color[renderers.Count];
                baseSortingOrders = new int[renderers.Count];

                for (int index = 0;
                     index < renderers.Count;
                     index++)
                {
                    SpriteRenderer renderer = renderers[index];
                    baseColors[index] =
                        renderer != null
                            ? renderer.color
                            : Color.white;
                    baseSortingOrders[index] =
                        renderer != null
                            ? renderer.sortingOrder
                            : 0;
                }
            }

            public FixtureInstance Fixture { get; }

            public GameObject Root { get; }

            public SortingGroup SortingGroup { get; }

            public int BaseSortingGroupOrder { get; }

            public IReadOnlyList<SpriteRenderer> Renderers { get; }

            public Color GetBaseColor(int rendererIndex) =>
                baseColors[rendererIndex];

            public int GetBaseSortingOrder(int rendererIndex) =>
                baseSortingOrders[rendererIndex];
        }
    }
}
