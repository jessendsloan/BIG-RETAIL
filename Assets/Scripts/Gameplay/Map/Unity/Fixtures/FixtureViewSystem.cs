using System;
using System.Collections.Generic;
using BigRetail.Map.Domain;
using BigRetail.Map.Fixtures;
using BigRetail.Map.Unity.View;
using BigRetail.Map.Unity.Walls;
using BigRetail.Map.View;
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

        [SerializeField]
        private FixtureRuntimeHost runtimeHost;

        [SerializeField]
        private IsometricViewHost viewHost;

        [SerializeField]
        private Tilemap coordinateTilemap;

        [SerializeField]
        private Transform viewParent;

        [SerializeField]
        private Color fixtureColor = Color.white;

        [SerializeField]
        private string sortingLayerName = "Default";


        private readonly Dictionary<FixtureInstanceId, FixtureView>
            views = new Dictionary<FixtureInstanceId, FixtureView>();

        private FixtureState subscribedState;
        private bool hasMerchandisingFocus;
        private FixtureInstanceId merchandisingFocusFixtureId;


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
        }


        private void Start()
        {
            if (runtimeHost.IsInitialized)
            {
                AttachToState(runtimeHost.FixtureState);
            }
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

            DetachFromState();
            ClearViews();
        }


        private void HandleRuntimeInitialized(FixtureRuntimeHost initializedHost)
        {
            AttachToState(initializedHost.FixtureState);
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
