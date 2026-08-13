using System;
using System.Collections.Generic;
using BigRetail.Map.Domain;
using BigRetail.Map.Fixtures;
using BigRetail.Map.Unity.View;
using BigRetail.Map.Unity.Walls;
using BigRetail.Map.View;
using BigRetail.Merchandise.Domain;
using UnityEngine;
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

                    int sortingLayerValue =
                        SortingLayer.GetLayerValueFromID(
                            renderer.sortingLayerID);

                    if (bestView != null
                        && sortingLayerValue < bestSortingLayerValue)
                    {
                        continue;
                    }

                    if (bestView != null
                        && sortingLayerValue == bestSortingLayerValue
                        && renderer.sortingOrder <= bestSortingOrder)
                    {
                        continue;
                    }

                    bestView = view;
                    bestSortingLayerValue = sortingLayerValue;
                    bestSortingOrder = renderer.sortingOrder;
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

            AddBackstockCaseMarkers(
                fixture,
                renderers);

            AddStockedDisplayMarkers(
                fixture,
                asset,
                renderers);

            views.Add(
                fixture.Id,
                new FixtureView(
                    fixture,
                    root,
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
            GridPosition? sortingCellOverride = null)
        {
            GameObject child = new GameObject("Fixture Sprite");
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
                WallRenderOrderResolver.ResolveCell(displayCell);

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

            int storedUnitCount =
                subscribedBackstock
                    .GetRackStoredUnitCount(fixture.Id);
            int capacityUnitCount =
                subscribedBackstock
                    .GetRackCapacityUnitCount(fixture.Id);

            if (contents.Count == 0
                || storedUnitCount == 0
                || capacityUnitCount == 0)
            {
                return;
            }

            const int maximumMarkerCount = 9;
            int markerCount =
                Mathf.Clamp(
                    Mathf.CeilToInt(
                        storedUnitCount
                        / (float)capacityUnitCount
                        * maximumMarkerCount),
                    1,
                    maximumMarkerCount);

            SpriteRenderer fixtureRenderer = renderers[0];
            Bounds spriteBounds = fixtureRenderer.sprite.bounds;
            IReadOnlyList<Sprite> shelfMasks =
                runtimeHost.DefinitionAssets
                    .GetAsset(fixture.DefinitionId)
                    .GetStorageShelfMasks(
                        fixture.Orientation,
                        viewHost.Orientation);
            bool hasAuthoredShelfMasks = shelfMasks.Count > 0;
            float boxWidth = spriteBounds.size.x * 0.16f;
            float boxHeight = spriteBounds.size.y * 0.055f;
            float slope =
                fixtureRenderer.sprite.name.IndexOf(
                    "RisingLeft",
                    StringComparison.OrdinalIgnoreCase) >= 0
                    ? -26f
                    : 26f;

            int cumulativeQuantity = 0;
            int contentIndex = 0;

            for (int markerIndex = 0;
                 markerIndex < markerCount;
                 markerIndex++)
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

                int column = markerIndex % 3;
                int shelf = markerIndex / 3;
                Vector3 localPosition;
                Vector2 markerSize;
                float markerSlope;

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

                    Vector2 center = geometry.GetFrontageCenter(column, 3);
                    localPosition = new Vector3(center.x, center.y, 0f);
                    markerSize =
                        new Vector2(
                            geometry.MajorLength / 3f * 0.72f,
                            geometry.MinorLength * 0.68f);
                    markerSlope = geometry.MajorAxisAngleDegrees;
                }
                else
                {
                    float normalizedX = 0.28f + column * 0.22f;
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
                }

                AddCaseMarkerRenderer(
                    fixtureRenderer,
                    localPosition,
                    markerSize,
                    markerSlope,
                    FixtureMerchandisingGrayboxPalette
                        .ResolveProductColor(
                            contents[contentIndex].ProductId),
                    renderers);
            }
        }


        private void AddStockedDisplayMarkers(
            FixtureInstance fixture,
            FixtureDefinitionAsset definitionAsset,
            List<SpriteRenderer> renderers)
        {
            if (frontageMarkerSprite == null
                || subscribedDisplayInventory == null
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

                        AddStockedDisplayMarker(
                            fixtureRenderer,
                            geometry,
                            visualUnitIndex,
                            displayFace.FrontageUnitsPerRun,
                            isViewerNear,
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
            bool isViewerNear,
            Color color,
            List<SpriteRenderer> renderers)
        {
            GameObject markerObject =
                new GameObject("Stocked Display Product");
            markerObject.transform.SetParent(
                fixtureRenderer.transform,
                worldPositionStays: false);

            Vector2 localCenter =
                geometry.GetFrontageCenter(
                    visualFrontageIndex,
                    frontageUnitCount);
            markerObject.transform.localPosition =
                new Vector3(localCenter.x, localCenter.y, 0f);

            SpriteRenderer renderer =
                markerObject.AddComponent<SpriteRenderer>();
            renderer.sprite = frontageMarkerSprite;
            renderer.color = color;
            renderer.sortingLayerName = sortingLayerName;
            renderer.sortingOrder =
                fixtureRenderer.sortingOrder
                + (isViewerNear ? 6 : 1);

            Bounds markerBounds = frontageMarkerSprite.bounds;
            float desiredWidth =
                Mathf.Max(
                    geometry.MajorLength
                    / frontageUnitCount
                    * DisplayMarkerWidthShare,
                    0.03f);
            float desiredHeight =
                Mathf.Max(
                    geometry.MinorLength * DisplayMarkerHeightShare,
                    0.02f);
            markerObject.transform.localScale =
                new Vector3(
                    desiredWidth
                    / Mathf.Max(markerBounds.size.x, 0.001f),
                    desiredHeight
                    / Mathf.Max(markerBounds.size.y, 0.001f),
                    1f);
            renderers.Add(renderer);
        }

        private void AddCaseMarkerRenderer(
            SpriteRenderer fixtureRenderer,
            Vector3 localPosition,
            Vector2 size,
            float slope,
            Color color,
            List<SpriteRenderer> renderers)
        {
            Sprite markerSprite = GetOrCreateCaseMarkerSprite();

            GameObject shadowObject =
                new GameObject("Backstock Case Shadow");
            shadowObject.transform.SetParent(
                fixtureRenderer.transform,
                worldPositionStays: false);
            shadowObject.transform.localPosition =
                localPosition + new Vector3(0f, -size.y * 0.16f, 0f);
            shadowObject.transform.localRotation =
                Quaternion.Euler(0f, 0f, slope);
            shadowObject.transform.localScale =
                new Vector3(size.x * 1.12f, size.y * 1.32f, 1f);

            SpriteRenderer shadowRenderer =
                shadowObject.AddComponent<SpriteRenderer>();
            shadowRenderer.sprite = markerSprite;
            shadowRenderer.color = new Color(0.12f, 0.14f, 0.15f, 0.92f);
            shadowRenderer.sortingLayerName = sortingLayerName;
            shadowRenderer.sortingOrder =
                fixtureRenderer.sortingOrder + 1;
            renderers.Add(shadowRenderer);

            GameObject caseObject =
                new GameObject("Backstock Product Case");
            caseObject.transform.SetParent(
                fixtureRenderer.transform,
                worldPositionStays: false);
            caseObject.transform.localPosition = localPosition;
            caseObject.transform.localRotation =
                Quaternion.Euler(0f, 0f, slope);
            caseObject.transform.localScale =
                new Vector3(size.x, size.y, 1f);

            SpriteRenderer caseRenderer =
                caseObject.AddComponent<SpriteRenderer>();
            caseRenderer.sprite = markerSprite;
            caseRenderer.color = color;
            caseRenderer.sortingLayerName = sortingLayerName;
            caseRenderer.sortingOrder =
                fixtureRenderer.sortingOrder + 2;
            renderers.Add(caseRenderer);
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
                    ResolveMerchandisingFocusSortingOrder(
                        baseSortingOrder,
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
                IReadOnlyList<SpriteRenderer> renderers)
            {
                Fixture = fixture;
                Root = root;
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

            public IReadOnlyList<SpriteRenderer> Renderers { get; }

            public Color GetBaseColor(int rendererIndex) =>
                baseColors[rendererIndex];

            public int GetBaseSortingOrder(int rendererIndex) =>
                baseSortingOrders[rendererIndex];
        }
    }
}
