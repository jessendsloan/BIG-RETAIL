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


        public int VisibleFixtureCount => views.Count;


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

            views.Add(fixture.Id, new FixtureView(root, renderers));
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
            Destroy(view.Root);
        }


        private void ClearViews()
        {
            foreach (FixtureView view in views.Values)
            {
                Destroy(view.Root);
            }

            views.Clear();
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
            public FixtureView(GameObject root, IReadOnlyList<SpriteRenderer> renderers)
            {
                Root = root;
                Renderers = renderers;
            }

            public GameObject Root { get; }

            public IReadOnlyList<SpriteRenderer> Renderers { get; }
        }
    }
}
