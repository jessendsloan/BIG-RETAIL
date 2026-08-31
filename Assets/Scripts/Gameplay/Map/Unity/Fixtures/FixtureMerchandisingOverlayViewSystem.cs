using System;
using System.Collections.Generic;
using BigRetail.Map.Fixtures;
using BigRetail.Map.Unity.View;
using BigRetail.Map.View;
using BigRetail.Merchandise.Domain;
using BigRetail.Merchandise.Unity;
using UnityEngine;

namespace BigRetail.Map.Unity.Fixtures
{
    /// <summary>
    /// Interactive presentation for editable shelf runs and frontage units.
    /// Authored product art previews assigned empty frontages while the
    /// graybox pylon remains the fallback. Logical shelf identities are owned
    /// by FixturePlanogramState.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(150)]
    public sealed class FixtureMerchandisingOverlayViewSystem : MonoBehaviour
    {
        private const float HorizontalInset = 0.17f;
        private const float MarkerWidthShare = 0.19f;
        private const float MarkerHeightShare = 0.07f;
        private const float AuthoredMarkerWidthShare = 0.78f;
        private const float AuthoredMarkerHeightShare = 0.62f;

        [SerializeField]
        private FixtureRuntimeHost fixtureRuntimeHost;

        [SerializeField]
        private FixturePlanogramRuntimeHost planogramRuntimeHost;

        [SerializeField]
        private FixtureViewSystem fixtureViewSystem;

        [SerializeField]
        private FixtureMerchandisingSelectionHost selectionHost;

        [SerializeField]
        private IsometricViewHost viewHost;

        [SerializeField]
        private Sprite frontageMarkerSprite;

        [SerializeField]
        private string sortingLayerName = "Default";


        private readonly List<FrontageMarkerView> markerViews =
            new List<FrontageMarkerView>();

        private readonly List<ShelfMaskHitView> shelfMaskViews =
            new List<ShelfMaskHitView>();

        private FixturePlanogramState subscribedPlanogramState;
        private FixtureDisplayInventoryService subscribedDisplayInventory;
        private FixtureMerchandisingHoverOutlineView objectiveOutlineView;
        private bool objectiveHighlightEnabled;
        private bool hasHoveredMarker;
        private FixtureShelfRunKey hoveredShelfRun;
        private int hoveredFrontageUnitIndex;


        public int VisibleMarkerCount => markerViews.Count;

        public bool ObjectiveHighlightEnabled =>
            objectiveHighlightEnabled;


        private void OnEnable()
        {
            if (!ValidateReferences())
            {
                enabled = false;
                return;
            }

            selectionHost.SelectionChanged += HandleSelectionChanged;
            fixtureViewSystem.FixtureViewShown += HandleFixtureViewShown;
            fixtureViewSystem.FixtureViewHidden += HandleFixtureViewHidden;
            planogramRuntimeHost.Initialized += HandlePlanogramInitialized;

            AttachToPlanogramState();
            AttachToDisplayInventory();
            ResolveObjectiveOutlineView();
            RefreshObjectiveOutline();
            RefreshFixtureFocus();
        }

        private void Start()
        {
            RebuildMarkers();
        }

        private void OnDisable()
        {
            if (selectionHost != null)
            {
                selectionHost.SelectionChanged -= HandleSelectionChanged;
            }

            if (fixtureViewSystem != null)
            {
                fixtureViewSystem.FixtureViewShown -= HandleFixtureViewShown;
                fixtureViewSystem.FixtureViewHidden -= HandleFixtureViewHidden;
            }

            if (planogramRuntimeHost != null)
            {
                planogramRuntimeHost.Initialized -= HandlePlanogramInitialized;
            }

            DetachFromPlanogramState();
            DetachFromDisplayInventory();
            objectiveOutlineView?.ClearPinnedFixture();
            ClearMarkers();
            fixtureViewSystem?.ClearMerchandisingFocus();
        }


        public void SetObjectiveHighlightEnabled(bool isEnabled)
        {
            objectiveHighlightEnabled = isEnabled;
            ResolveObjectiveOutlineView();
            RefreshObjectiveOutline();
        }


        public bool TryHitTest(
            Vector3 pointerWorldPosition,
            out FixtureShelfRunKey shelfRun,
            out int frontageUnitIndex)
        {
            for (int index = shelfMaskViews.Count - 1;
                 index >= 0;
                 index--)
            {
                ShelfMaskHitView shelfMask = shelfMaskViews[index];
                Vector2 localPoint =
                    shelfMask.FixtureTransform
                        .InverseTransformPoint(pointerWorldPosition);

                if (!FixtureShelfMaskGeometry.ContainsLocalPoint(
                        shelfMask.Mask,
                        localPoint))
                {
                    continue;
                }

                int visualFrontageIndex =
                    shelfMask.Geometry.ResolveVisualFrontageIndex(
                        localPoint,
                        shelfMask.FrontageUnitCount);

                shelfRun = shelfMask.ShelfRun;
                frontageUnitIndex =
                    shelfMask.ReverseFrontage
                        ? shelfMask.FrontageUnitCount
                            - 1
                            - visualFrontageIndex
                        : visualFrontageIndex;
                return true;
            }

            for (int index = markerViews.Count - 1;
                 index >= 0;
                 index--)
            {
                FrontageMarkerView marker = markerViews[index];

                if (!marker.AllowsBoundsHitTest)
                {
                    continue;
                }

                Bounds bounds = marker.Renderer.bounds;

                if (pointerWorldPosition.x < bounds.min.x
                    || pointerWorldPosition.x > bounds.max.x
                    || pointerWorldPosition.y < bounds.min.y
                    || pointerWorldPosition.y > bounds.max.y)
                {
                    continue;
                }

                shelfRun = marker.ShelfRun;
                frontageUnitIndex = marker.FrontageUnitIndex;
                return true;
            }

            shelfRun = default;
            frontageUnitIndex = 0;
            return false;
        }

        public void SetHoveredMarker(
            FixtureShelfRunKey shelfRun,
            int frontageUnitIndex)
        {
            if (hasHoveredMarker
                && hoveredShelfRun == shelfRun
                && hoveredFrontageUnitIndex == frontageUnitIndex)
            {
                return;
            }

            hasHoveredMarker = true;
            hoveredShelfRun = shelfRun;
            hoveredFrontageUnitIndex = frontageUnitIndex;
            RefreshMarkerColors();
        }

        public void ClearHoveredMarker()
        {
            if (!hasHoveredMarker)
            {
                return;
            }

            hasHoveredMarker = false;
            hoveredShelfRun = default;
            hoveredFrontageUnitIndex = 0;
            RefreshMarkerColors();
        }


        private void HandleSelectionChanged()
        {
            RefreshFixtureFocus();
            RebuildMarkers();
        }

        private void HandleFixtureViewShown(
            FixtureInstance fixture,
            SpriteRenderer renderer)
        {
            if (selectionHost.HasSelectedFixture
                && fixture.Id == selectionHost.SelectedFixtureId)
            {
                RebuildMarkers();
            }
        }

        private void HandleFixtureViewHidden(
            FixtureInstanceId fixtureId)
        {
            if (selectionHost.HasSelectedFixture
                && fixtureId == selectionHost.SelectedFixtureId)
            {
                ClearMarkers();
            }
        }

        private void RefreshFixtureFocus()
        {
            if (selectionHost.HasSelectedFixture)
            {
                fixtureViewSystem.SetMerchandisingFocus(
                    selectionHost.SelectedFixtureId);
                return;
            }

            fixtureViewSystem.ClearMerchandisingFocus();
        }

        private void HandlePlanogramInitialized(
            FixturePlanogramRuntimeHost initializedHost)
        {
            AttachToPlanogramState();
            AttachToDisplayInventory();
            RefreshObjectiveOutline();
            RebuildMarkers();
        }

        private void HandleShelfRunChanged(
            FixtureShelfRunKey shelfRun)
        {
            RefreshObjectiveOutline();

            if (selectionHost.HasSelectedFixture
                && shelfRun.FixtureId == selectionHost.SelectedFixtureId)
            {
                RefreshMarkerColors();
            }
        }

        private void HandleFixtureStockChanged(
            FixtureInstanceId fixtureId)
        {
            if (selectionHost.HasSelectedFixture
                && fixtureId == selectionHost.SelectedFixtureId)
            {
                RefreshMarkerColors();
            }
        }

        private void AttachToPlanogramState()
        {
            FixturePlanogramState nextState =
                planogramRuntimeHost.PlanogramState;

            if (subscribedPlanogramState == nextState)
            {
                return;
            }

            DetachFromPlanogramState();
            subscribedPlanogramState = nextState;

            if (subscribedPlanogramState != null)
            {
                subscribedPlanogramState.ShelfRunChanged +=
                    HandleShelfRunChanged;
            }
        }

        private void DetachFromPlanogramState()
        {
            if (subscribedPlanogramState == null)
            {
                return;
            }

            subscribedPlanogramState.ShelfRunChanged -=
                HandleShelfRunChanged;
            subscribedPlanogramState = null;
        }

        private void AttachToDisplayInventory()
        {
            FixtureDisplayInventoryService nextService =
                planogramRuntimeHost.DisplayInventory;

            if (subscribedDisplayInventory == nextService)
            {
                return;
            }

            DetachFromDisplayInventory();
            subscribedDisplayInventory = nextService;

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

        private void ResolveObjectiveOutlineView()
        {
            if (objectiveOutlineView != null)
            {
                return;
            }

            if (fixtureViewSystem == null)
            {
                return;
            }

            objectiveOutlineView =
                fixtureViewSystem.GetComponent<
                    FixtureMerchandisingHoverOutlineView>();

            if (objectiveOutlineView == null)
            {
                objectiveOutlineView =
                    fixtureViewSystem.gameObject.AddComponent<
                        FixtureMerchandisingHoverOutlineView>();
            }
        }

        private void RefreshObjectiveOutline()
        {
            if (objectiveOutlineView == null)
            {
                return;
            }

            if (objectiveHighlightEnabled
                && subscribedPlanogramState != null
                && subscribedPlanogramState.TryGetSingleAssignedFixture(
                    out FixtureInstanceId fixtureId))
            {
                objectiveOutlineView.PinFixture(fixtureId);
                return;
            }

            objectiveOutlineView.ClearPinnedFixture();
        }

        private void RebuildMarkers()
        {
            ClearMarkers();

            if (!selectionHost.HasSelectedFixture
                || frontageMarkerSprite == null
                || fixtureRuntimeHost.FixtureState == null
                || !fixtureRuntimeHost.FixtureState.TryGetFixture(
                    selectionHost.SelectedFixtureId,
                    out FixtureInstance fixture)
                || !fixtureViewSystem.TryGetPrimaryRenderer(
                    fixture.Id,
                    out SpriteRenderer fixtureRenderer))
            {
                return;
            }

            FixtureMerchandisingProfile profile =
                fixture.Definition.MerchandisingProfile;

            FixtureDefinitionAsset definitionAsset =
                fixtureRuntimeHost.DefinitionAssets.GetAsset(
                    fixture.DefinitionId);

            for (int faceIndex = 0;
                 faceIndex < profile.DisplayFaceCount;
                 faceIndex++)
            {
                FixtureDisplayFaceDefinition displayFace =
                    profile.GetDisplayFace(faceIndex);

                if (definitionAsset.HasMerchandisingShelfMasks(
                        displayFace.LocalSide))
                {
                    IReadOnlyList<Sprite> shelfMasks =
                        definitionAsset.GetMerchandisingShelfMasks(
                            displayFace.LocalSide,
                            fixture.Orientation,
                            viewHost.Orientation);

                    if (shelfMasks.Count > 0)
                    {
                        CreateAuthoredFaceMarkers(
                            fixture,
                            fixtureRenderer,
                            definitionAsset,
                            displayFace,
                            shelfMasks);
                    }

                    continue;
                }

                CreateFaceMarkers(
                    fixture,
                    fixtureRenderer,
                    displayFace);
            }

            RefreshMarkerColors();
        }

        private void CreateAuthoredFaceMarkers(
            FixtureInstance fixture,
            SpriteRenderer fixtureRenderer,
            FixtureDefinitionAsset definitionAsset,
            FixtureDisplayFaceDefinition displayFace,
            IReadOnlyList<Sprite> shelfMasks)
        {
            if (shelfMasks.Count != displayFace.ShelfRunCount)
            {
                Debug.LogError(
                    $"Fixture '{fixture.DefinitionId}' expected "
                    + $"{displayFace.ShelfRunCount} authored shelf masks for "
                    + $"'{displayFace.LocalSide}', but received "
                    + $"{shelfMasks.Count}.",
                    this);
                return;
            }

            FixtureSide worldSide =
                displayFace.LocalSide.Rotate(fixture.Orientation);

            FixtureSide relativeSide =
                (FixtureSide)(
                    ((int)worldSide - (int)viewHost.Orientation + 4) % 4);

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
                Sprite shelfMask = shelfMasks[shelfIndex];

                if (!FixtureShelfMaskGeometry.TryCreate(
                        shelfMask,
                        out FixtureShelfMaskGeometry geometry))
                {
                    Debug.LogError(
                        $"Fixture '{fixture.DefinitionId}' has unusable "
                        + $"shelf mask '{shelfMask?.name ?? "<null>"}'.",
                        this);
                    continue;
                }

                FixtureShelfRunKey shelfRun =
                    new FixtureShelfRunKey(
                        fixture.Id,
                        displayFace.LocalSide,
                        shelfIndex);

                GameObject shelfHighlightObject =
                    new GameObject($"Shelf Highlight {shelfRun}");

                shelfHighlightObject.transform.SetParent(
                    fixtureRenderer.transform,
                    worldPositionStays: false);

                SpriteRenderer shelfHighlightRenderer =
                    shelfHighlightObject.AddComponent<SpriteRenderer>();

                shelfHighlightRenderer.sprite = shelfMask;
                shelfHighlightRenderer.sortingLayerName = sortingLayerName;
                shelfHighlightRenderer.sortingOrder =
                    fixtureRenderer.sortingOrder
                    + (isViewerNear ? 7 : 2);

                shelfMaskViews.Add(
                    new ShelfMaskHitView(
                        shelfRun,
                        shelfMask,
                        fixtureRenderer.transform,
                        geometry,
                        displayFace.FrontageUnitsPerRun,
                        reverseFrontage,
                        shelfHighlightObject,
                        shelfHighlightRenderer));

                for (int unitIndex = 0;
                     unitIndex < displayFace.FrontageUnitsPerRun;
                     unitIndex++)
                {
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

                    CreateAuthoredMarker(
                        fixtureRenderer,
                        geometry,
                        shelfRun,
                        unitIndex,
                        visualUnitIndex,
                        displayFace.FrontageUnitsPerRun,
                        hasAuthoredSlotAnchor,
                        authoredSlotAnchor,
                        isViewerNear);
                }
            }
        }

        private void CreateAuthoredMarker(
            SpriteRenderer fixtureRenderer,
            FixtureShelfMaskGeometry geometry,
            FixtureShelfRunKey shelfRun,
            int frontageUnitIndex,
            int visualFrontageIndex,
            int frontageUnitCount,
            bool hasAuthoredSlotAnchor,
            Vector2 authoredSlotAnchor,
            bool isViewerNear)
        {
            GameObject markerObject =
                new GameObject(
                    $"Frontage {shelfRun} Unit {frontageUnitIndex + 1}");

            markerObject.transform.SetParent(
                fixtureRenderer.transform,
                worldPositionStays: false);

            Vector2 localCenter =
                geometry.GetFrontageCenter(
                    visualFrontageIndex,
                    frontageUnitCount);
            Vector2 authoredProductLocalCenter =
                hasAuthoredSlotAnchor
                    ? authoredSlotAnchor
                    : FixtureViewSystem
                        .ResolveAuthoredDisplayProductCenter(
                            geometry,
                            visualFrontageIndex,
                            frontageUnitCount);

            markerObject.transform.localPosition =
                new Vector3(localCenter.x, localCenter.y, 0f);

            SpriteRenderer renderer =
                markerObject.AddComponent<SpriteRenderer>();

            renderer.sprite = frontageMarkerSprite;
            renderer.sortingLayerName = sortingLayerName;
            int shelfSortingOrder =
                fixtureRenderer.sortingOrder
                + (isViewerNear ? 8 : 3);
            renderer.sortingOrder =
                FixtureViewSystem
                    .ResolveStockedDisplayFrontageSortingOrder(
                        shelfSortingOrder,
                        visualFrontageIndex,
                        frontageUnitCount,
                        geometry.MajorAxisAngleDegrees);

            Bounds spriteBounds = frontageMarkerSprite.bounds;
            float desiredWidth =
                Mathf.Max(
                    geometry.MajorLength
                    / frontageUnitCount
                    * AuthoredMarkerWidthShare,
                    0.03f);
            float desiredHeight =
                Mathf.Max(
                    geometry.MinorLength * AuthoredMarkerHeightShare,
                    0.02f);
            float authoredProductWidth =
                Mathf.Max(
                    geometry.MajorLength
                    / frontageUnitCount
                    * FixtureViewSystem
                        .AuthoredDisplayProductWidthShare,
                    0.03f);
            float authoredProductHeight =
                Mathf.Max(
                    geometry.MajorLength
                    / frontageUnitCount
                    * FixtureViewSystem
                        .AuthoredDisplayProductHeightShare,
                    0.02f);

            markerObject.transform.localScale =
                new Vector3(
                    desiredWidth
                    / Mathf.Max(spriteBounds.size.x, 0.001f),
                    desiredHeight
                    / Mathf.Max(spriteBounds.size.y, 0.001f),
                    1f);

            markerViews.Add(
                new FrontageMarkerView(
                    shelfRun,
                    frontageUnitIndex,
                    markerObject,
                    renderer,
                    allowsBoundsHitTest: false,
                    canUseAuthoredProductArt: true,
                    productRisingLeft:
                        geometry.MajorAxisAngleDegrees < 0f,
                    fallbackDesiredWidth: desiredWidth,
                    fallbackDesiredHeight: desiredHeight,
                    authoredProductDesiredWidth: authoredProductWidth,
                    authoredProductDesiredHeight: authoredProductHeight,
                    fallbackLocalPosition:
                        new Vector3(localCenter.x, localCenter.y, 0f),
                    authoredProductLocalPosition:
                        new Vector3(
                            authoredProductLocalCenter.x,
                            authoredProductLocalCenter.y,
                            0f)));
        }

        private void CreateFaceMarkers(
            FixtureInstance fixture,
            SpriteRenderer fixtureRenderer,
            FixtureDisplayFaceDefinition displayFace)
        {
            Bounds fixtureBounds = fixtureRenderer.bounds;
            FixtureSide worldSide =
                displayFace.LocalSide.Rotate(fixture.Orientation);

            FixtureSide relativeSide =
                (FixtureSide)(
                    ((int)worldSide - (int)viewHost.Orientation + 4) % 4);

            bool isViewerNear =
                relativeSide == FixtureSide.South
                || relativeSide == FixtureSide.West;

            bool reverseFrontage =
                relativeSide == FixtureSide.North
                || relativeSide == FixtureSide.West;

            for (int shelfIndex = 0;
                 shelfIndex < displayFace.ShelfRunCount;
                 shelfIndex++)
            {
                float rowY =
                    ResolveShelfRowNormalizedY(
                        isViewerNear,
                        shelfIndex,
                        displayFace.ShelfRunCount);

                FixtureShelfRunKey shelfRun =
                    new FixtureShelfRunKey(
                        fixture.Id,
                        displayFace.LocalSide,
                        shelfIndex);

                for (int unitIndex = 0;
                     unitIndex < displayFace.FrontageUnitsPerRun;
                     unitIndex++)
                {
                    int visualUnitIndex =
                        reverseFrontage
                            ? displayFace.FrontageUnitsPerRun - 1 - unitIndex
                            : unitIndex;

                    float unitT =
                        (visualUnitIndex + 0.5f)
                        / displayFace.FrontageUnitsPerRun;

                    float normalizedX =
                        Mathf.Lerp(
                            HorizontalInset,
                            1f - HorizontalInset,
                            unitT);

                    CreateMarker(
                        fixtureRenderer,
                        fixtureBounds,
                        shelfRun,
                        unitIndex,
                        normalizedX,
                        rowY,
                        isViewerNear);
                }
            }
        }

        private void CreateMarker(
            SpriteRenderer fixtureRenderer,
            Bounds fixtureBounds,
            FixtureShelfRunKey shelfRun,
            int frontageUnitIndex,
            float normalizedX,
            float normalizedY,
            bool isViewerNear)
        {
            GameObject markerObject =
                new GameObject(
                    $"Frontage {shelfRun} Unit {frontageUnitIndex + 1}");

            markerObject.transform.SetParent(
                transform,
                worldPositionStays: true);

            SpriteRenderer renderer =
                markerObject.AddComponent<SpriteRenderer>();

            renderer.sprite = frontageMarkerSprite;
            renderer.sortingLayerName = sortingLayerName;
            renderer.sortingOrder =
                fixtureRenderer.sortingOrder
                + (isViewerNear ? 8 : 3);

            Vector3 position =
                new Vector3(
                    Mathf.Lerp(
                        fixtureBounds.min.x,
                        fixtureBounds.max.x,
                        normalizedX),
                    Mathf.Lerp(
                        fixtureBounds.min.y,
                        fixtureBounds.max.y,
                        normalizedY),
                    fixtureRenderer.transform.position.z);

            markerObject.transform.position = position;

            Bounds spriteBounds = frontageMarkerSprite.bounds;
            float desiredWidth =
                Mathf.Max(
                    fixtureBounds.size.x * MarkerWidthShare,
                    0.03f);

            float desiredHeight =
                Mathf.Max(
                    fixtureBounds.size.y * MarkerHeightShare,
                    0.02f);

            markerObject.transform.localScale =
                new Vector3(
                    desiredWidth / Mathf.Max(spriteBounds.size.x, 0.001f),
                    desiredHeight / Mathf.Max(spriteBounds.size.y, 0.001f),
                    1f);

            markerViews.Add(
                new FrontageMarkerView(
                    shelfRun,
                    frontageUnitIndex,
                    markerObject,
                    renderer,
                    allowsBoundsHitTest: true,
                    canUseAuthoredProductArt: false,
                    productRisingLeft: false,
                    fallbackDesiredWidth: desiredWidth,
                    fallbackDesiredHeight: desiredHeight,
                    authoredProductDesiredWidth: desiredWidth,
                    authoredProductDesiredHeight: desiredHeight,
                    fallbackLocalPosition:
                        markerObject.transform.localPosition,
                    authoredProductLocalPosition:
                        markerObject.transform.localPosition));
        }

        private void RefreshMarkerColors()
        {
            bool hasSelectionPreview =
                TryResolveSelectionPreview(
                    out FixtureShelfRunKey selectedShelfRun,
                    out int selectedStart,
                    out int selectedCount,
                    out bool selectionIsInvalid);

            for (int index = 0;
                 index < shelfMaskViews.Count;
                 index++)
            {
                ShelfMaskHitView shelfMask = shelfMaskViews[index];
                Color color =
                    FixtureMerchandisingGrayboxPalette.ShelfNeutral;

                if (hasHoveredMarker
                    && shelfMask.ShelfRun == hoveredShelfRun)
                {
                    color =
                        FixtureMerchandisingGrayboxPalette.ShelfHover;
                }

                if (hasSelectionPreview
                    && shelfMask.ShelfRun == selectedShelfRun)
                {
                    color =
                        selectionIsInvalid
                            ? FixtureMerchandisingGrayboxPalette.ShelfInvalid
                            : FixtureMerchandisingGrayboxPalette.ShelfSelected;
                }

                shelfMask.Renderer.color = color;
            }

            for (int index = 0;
                 index < markerViews.Count;
                 index++)
            {
                FrontageMarkerView marker = markerViews[index];
                Color color = FixtureMerchandisingGrayboxPalette.Neutral;
                float fillRatio = 0f;
                ProductDefinitionAsset productAsset = null;
                ProductId productId = default;
                bool hasAssignedProduct =
                    subscribedPlanogramState != null
                    && subscribedPlanogramState.TryGetProductAt(
                        marker.ShelfRun,
                        marker.FrontageUnitIndex,
                        out productId);

                if (hasAssignedProduct)
                {
                    fillRatio =
                        subscribedDisplayInventory != null
                            ? subscribedDisplayInventory
                                .GetFrontageFillRatio(
                                    marker.ShelfRun,
                                    marker.FrontageUnitIndex)
                            : 0f;

                    color =
                        FixtureMerchandisingGrayboxPalette
                            .ResolveStockColor(
                                productId,
                                fillRatio);

                    planogramRuntimeHost.TryGetProductAsset(
                        productId,
                        out productAsset);
                }

                bool isHovered =
                    hasHoveredMarker
                    && marker.ShelfRun == hoveredShelfRun
                    && marker.FrontageUnitIndex == hoveredFrontageUnitIndex;

                bool isSelected =
                    hasSelectionPreview
                    && marker.ShelfRun == selectedShelfRun
                    && marker.FrontageUnitIndex >= selectedStart
                    && marker.FrontageUnitIndex
                        < selectedStart + selectedCount;

                Sprite markerSprite =
                    ResolvePlanogramMarkerSprite(
                        productAsset,
                        marker.CanUseAuthoredProductArt,
                        marker.ProductRisingLeft,
                        fillRatio,
                        isHovered || isSelected,
                        frontageMarkerSprite);

                bool usesAuthoredProductArt =
                    markerSprite != null
                    && markerSprite != frontageMarkerSprite;

                marker.SetSprite(
                    markerSprite,
                    usesAuthoredProductArt);

                if (usesAuthoredProductArt)
                {
                    color =
                        FixtureMerchandisingGrayboxPalette.ProductGhost;
                }

                if (isHovered)
                {
                    color = usesAuthoredProductArt
                        ? FixtureMerchandisingGrayboxPalette.ProductGhostHover
                        : FixtureMerchandisingGrayboxPalette.Hover;
                }

                if (isSelected)
                {
                    color =
                        usesAuthoredProductArt
                            ? selectionIsInvalid
                                ? FixtureMerchandisingGrayboxPalette
                                    .ProductGhostInvalid
                                : FixtureMerchandisingGrayboxPalette
                                    .ProductGhostSelected
                            : selectionIsInvalid
                                ? FixtureMerchandisingGrayboxPalette.Invalid
                                : FixtureMerchandisingGrayboxPalette.Selected;
                }

                marker.Renderer.color = color;
            }
        }


        public static Sprite ResolvePlanogramMarkerSprite(
            ProductDefinitionAsset productAsset,
            bool canUseAuthoredProductArt,
            bool risingLeft,
            float fillRatio,
            bool isEmphasized,
            Sprite fallbackSprite)
        {
            if (!canUseAuthoredProductArt
                || productAsset == null
                || productAsset.OnShelfImageCount <= 0)
            {
                return fallbackSprite;
            }

            // Stocked product art is owned by FixtureViewSystem. Never draw
            // the planogram preview over a physical package, including while
            // its frontage is selected or hovered.
            if (fillRatio > 0f)
            {
                return null;
            }

            float previewFillRatio =
                1f / productAsset.OnShelfImageCount;

            Sprite authoredSprite =
                productAsset.GetOnShelfImage(
                    risingLeft,
                    previewFillRatio);

            if (authoredSprite == null)
            {
                return fallbackSprite;
            }

            return authoredSprite;
        }

        private bool TryResolveSelectionPreview(
            out FixtureShelfRunKey shelfRun,
            out int startFrontageUnit,
            out int frontageUnitCount,
            out bool isInvalid)
        {
            shelfRun = default;
            startFrontageUnit = 0;
            frontageUnitCount = 0;
            isInvalid = false;

            if (!selectionHost.HasSelectedFrontageUnit)
            {
                return false;
            }

            shelfRun = selectionHost.SelectedShelfRun;

            if (subscribedPlanogramState != null
                && subscribedPlanogramState.TryGetFacingAt(
                    shelfRun,
                    selectionHost.SelectedFrontageUnitIndex,
                    out ProductFacing facing))
            {
                startFrontageUnit = facing.StartFrontageUnit;
                frontageUnitCount = facing.FrontageUnitCount;
                return true;
            }

            startFrontageUnit =
                selectionHost.SelectedFrontageUnitIndex;
            frontageUnitCount =
                selectionHost.RequestedFrontageUnitCount;

            int availableUnitCount = 0;

            for (int index = 0;
                 index < markerViews.Count;
                 index++)
            {
                FrontageMarkerView marker = markerViews[index];

                if (marker.ShelfRun == shelfRun)
                {
                    availableUnitCount =
                        Mathf.Max(
                            availableUnitCount,
                            marker.FrontageUnitIndex + 1);
                }
            }

            int endExclusive =
                startFrontageUnit + frontageUnitCount;

            isInvalid =
                startFrontageUnit < 0
                || frontageUnitCount <= 0
                || endExclusive > availableUnitCount;

            if (isInvalid || subscribedPlanogramState == null)
            {
                return true;
            }

            for (int index = startFrontageUnit;
                 index < endExclusive;
                 index++)
            {
                if (subscribedPlanogramState.TryGetProductAt(
                        shelfRun,
                        index,
                        out _))
                {
                    isInvalid = true;
                    break;
                }
            }

            return true;
        }

        private void ClearMarkers()
        {
            for (int index = 0;
                 index < markerViews.Count;
                 index++)
            {
                Destroy(markerViews[index].Root);
            }

            markerViews.Clear();

            for (int index = 0;
                 index < shelfMaskViews.Count;
                 index++)
            {
                Destroy(shelfMaskViews[index].Root);
            }

            shelfMaskViews.Clear();
            hasHoveredMarker = false;
        }

        private static float ResolveShelfRowNormalizedY(
            bool isViewerNear,
            int shelfIndex,
            int shelfRunCount)
        {
            float top = isViewerNear ? 0.54f : 0.82f;
            float bottom = isViewerNear ? 0.20f : 0.58f;

            if (shelfRunCount <= 1)
            {
                return (top + bottom) * 0.5f;
            }

            return Mathf.Lerp(
                top,
                bottom,
                shelfIndex / (float)(shelfRunCount - 1));
        }

        private bool ValidateReferences()
        {
            bool isValid = true;

            if (fixtureRuntimeHost == null
                || planogramRuntimeHost == null
                || fixtureViewSystem == null
                || selectionHost == null
                || viewHost == null)
            {
                Debug.LogError(
                    "FixtureMerchandisingOverlayViewSystem requires fixture, planogram, view, selection, and isometric view hosts.",
                    this);
                isValid = false;
            }

            if (frontageMarkerSprite == null)
            {
                Debug.LogError(
                    "FixtureMerchandisingOverlayViewSystem has no frontage marker sprite assigned.",
                    this);
                isValid = false;
            }

            return isValid;
        }


        private sealed class FrontageMarkerView
        {
            public FrontageMarkerView(
                FixtureShelfRunKey shelfRun,
                int frontageUnitIndex,
                GameObject root,
                SpriteRenderer renderer,
                bool allowsBoundsHitTest,
                bool canUseAuthoredProductArt,
                bool productRisingLeft,
                float fallbackDesiredWidth,
                float fallbackDesiredHeight,
                float authoredProductDesiredWidth,
                float authoredProductDesiredHeight,
                Vector3 fallbackLocalPosition,
                Vector3 authoredProductLocalPosition)
            {
                ShelfRun = shelfRun;
                FrontageUnitIndex = frontageUnitIndex;
                Root = root;
                Renderer = renderer;
                AllowsBoundsHitTest = allowsBoundsHitTest;
                CanUseAuthoredProductArt = canUseAuthoredProductArt;
                ProductRisingLeft = productRisingLeft;
                FallbackDesiredWidth = fallbackDesiredWidth;
                FallbackDesiredHeight = fallbackDesiredHeight;
                AuthoredProductDesiredWidth = authoredProductDesiredWidth;
                AuthoredProductDesiredHeight = authoredProductDesiredHeight;
                FallbackLocalPosition = fallbackLocalPosition;
                AuthoredProductLocalPosition = authoredProductLocalPosition;
            }

            public FixtureShelfRunKey ShelfRun { get; }

            public int FrontageUnitIndex { get; }

            public GameObject Root { get; }

            public SpriteRenderer Renderer { get; }

            public bool AllowsBoundsHitTest { get; }

            public bool CanUseAuthoredProductArt { get; }

            public bool ProductRisingLeft { get; }

            public float FallbackDesiredWidth { get; }

            public float FallbackDesiredHeight { get; }

            public float AuthoredProductDesiredWidth { get; }

            public float AuthoredProductDesiredHeight { get; }

            public Vector3 FallbackLocalPosition { get; }

            public Vector3 AuthoredProductLocalPosition { get; }


            public void SetSprite(
                Sprite sprite,
                bool usesAuthoredProductArt)
            {
                Renderer.enabled = sprite != null;
                Root.transform.localPosition =
                    usesAuthoredProductArt
                        ? AuthoredProductLocalPosition
                        : FallbackLocalPosition;

                if (sprite == null)
                {
                    return;
                }

                Renderer.sprite = sprite;

                Bounds spriteBounds = sprite.bounds;
                float width = usesAuthoredProductArt
                    ? AuthoredProductDesiredWidth
                    : FallbackDesiredWidth;
                float widthScale = width
                    / Mathf.Max(spriteBounds.size.x, 0.001f);
                float authoredScale =
                    FixtureViewSystem
                        .ResolveAuthoredProductUniformScale(
                            spriteBounds,
                            AuthoredProductDesiredWidth,
                            AuthoredProductDesiredHeight);

                Root.transform.localScale =
                    usesAuthoredProductArt
                        ? new Vector3(
                            authoredScale,
                            authoredScale,
                            1f)
                        : new Vector3(
                            widthScale,
                            FallbackDesiredHeight
                            / Mathf.Max(spriteBounds.size.y, 0.001f),
                            1f);
            }
        }

        private sealed class ShelfMaskHitView
        {
            public ShelfMaskHitView(
                FixtureShelfRunKey shelfRun,
                Sprite mask,
                Transform fixtureTransform,
                FixtureShelfMaskGeometry geometry,
                int frontageUnitCount,
                bool reverseFrontage,
                GameObject root,
                SpriteRenderer renderer)
            {
                ShelfRun = shelfRun;
                Mask = mask;
                FixtureTransform = fixtureTransform;
                Geometry = geometry;
                FrontageUnitCount = frontageUnitCount;
                ReverseFrontage = reverseFrontage;
                Root = root;
                Renderer = renderer;
            }

            public FixtureShelfRunKey ShelfRun { get; }

            public Sprite Mask { get; }

            public Transform FixtureTransform { get; }

            public FixtureShelfMaskGeometry Geometry { get; }

            public int FrontageUnitCount { get; }

            public bool ReverseFrontage { get; }

            public GameObject Root { get; }

            public SpriteRenderer Renderer { get; }
        }
    }
}
