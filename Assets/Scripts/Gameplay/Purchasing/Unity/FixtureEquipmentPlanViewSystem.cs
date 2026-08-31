using System.Collections.Generic;
using BigRetail.Map.Domain;
using BigRetail.Map.Fixtures;
using BigRetail.Map.Unity.Fixtures;
using BigRetail.Map.Unity.View;
using BigRetail.Map.View;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace BigRetail.Purchasing.Unity
{
    /// <summary>
    /// Draws free fixture plans as translucent authored fixture sprites.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-44)]
    public sealed class FixtureEquipmentPlanViewSystem : MonoBehaviour
    {
        [SerializeField]
        private FixtureEquipmentRuntimeHost equipmentRuntimeHost;

        [SerializeField]
        private FixtureRuntimeHost fixtureRuntimeHost;

        [SerializeField]
        private IsometricViewHost viewHost;

        [SerializeField]
        private Tilemap coordinateTilemap;

        [SerializeField]
        private Transform viewParent;

        [SerializeField]
        private Color plannedColor =
            new Color(0.32f, 0.78f, 0.88f, 0.52f);

        [SerializeField]
        private string sortingLayerName = "Default";

        private readonly Dictionary<FixtureInstanceId, GameObject> views =
            new Dictionary<FixtureInstanceId, GameObject>();
        private FixtureEquipmentPlanState subscribedPlans;


        public int VisiblePlanCount => views.Count;


        private void Awake()
        {
            if (viewParent == null)
            {
                viewParent = transform;
            }
        }

        private void OnEnable()
        {
            equipmentRuntimeHost.Initialized += HandleRuntimeInitialized;
            viewHost.OrientationChanging += HandleOrientationChanging;
            viewHost.OrientationChanged += HandleOrientationChanged;
        }

        private void Start()
        {
            AttachPlans();
            RebuildViews();
        }

        private void OnDisable()
        {
            if (equipmentRuntimeHost != null)
            {
                equipmentRuntimeHost.Initialized -=
                    HandleRuntimeInitialized;
            }

            if (viewHost != null)
            {
                viewHost.OrientationChanging -= HandleOrientationChanging;
                viewHost.OrientationChanged -= HandleOrientationChanged;
            }

            DetachPlans();
            ClearViews();
        }

        private void HandleRuntimeInitialized(
            FixtureEquipmentRuntimeHost initializedHost)
        {
            AttachPlans();
            RebuildViews();
        }

        private void HandlePlansChanged()
        {
            RebuildViews();
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

        private void AttachPlans()
        {
            FixtureEquipmentPlanState next =
                equipmentRuntimeHost != null
                && equipmentRuntimeHost.IsInitialized
                    ? equipmentRuntimeHost.Plans
                    : null;

            if (subscribedPlans == next)
            {
                return;
            }

            DetachPlans();
            subscribedPlans = next;

            if (subscribedPlans != null)
            {
                subscribedPlans.PlansChanged += HandlePlansChanged;
            }
        }

        private void DetachPlans()
        {
            if (subscribedPlans == null)
            {
                return;
            }

            subscribedPlans.PlansChanged -= HandlePlansChanged;
            subscribedPlans = null;
        }

        public void RebuildViews()
        {
            ClearViews();

            if (subscribedPlans == null
                || fixtureRuntimeHost?.DefinitionAssets == null
                || viewHost == null
                || !viewHost.IsInitialized
                || coordinateTilemap == null)
            {
                return;
            }

            foreach (FixtureEquipmentPlan plan
                     in subscribedPlans.EnumeratePlans())
            {
                views.Add(plan.Id, CreatePlanView(plan));
            }
        }

        private GameObject CreatePlanView(FixtureEquipmentPlan plan)
        {
            FixtureDefinitionAsset asset =
                fixtureRuntimeHost.DefinitionAssets.GetAsset(
                    plan.FixtureDefinitionId);
            Sprite sprite = asset.GetSprite(
                plan.Orientation,
                viewHost.Orientation);
            GameObject root = new GameObject(
                $"Planned {asset.DisplayName} {plan.Id}");
            root.transform.SetParent(viewParent, false);

            if (asset.RepeatSpritePerOccupiedCell)
            {
                for (int index = 0;
                     index < plan.Footprint.CellCount;
                     index++)
                {
                    GridPosition cell = plan.Footprint.GetCell(index);
                    CreateRenderer(
                        root.transform,
                        sprite,
                        cell,
                        asset.WorldPositionOffset);
                }
            }
            else
            {
                GridPosition presentationAnchor =
                    FixturePresentationAnchorResolver
                        .ResolveViewerNearestCell(
                            plan.Footprint,
                            viewHost.Projection);
                Vector3 position =
                    FixturePresentationAnchorResolver
                        .CalculateFootprintAnchorWorld(
                            coordinateTilemap,
                            plan.Footprint,
                            viewHost.Projection,
                            asset.GetSpriteAnchorCorner(
                                plan.Orientation,
                                viewHost.Orientation),
                            viewHost.ToUnityCell(presentationAnchor).z)
                    + asset.WorldPositionOffset;
                CreateRenderer(
                    root.transform,
                    sprite,
                    presentationAnchor,
                    Vector3.zero,
                    position);
            }

            return root;
        }

        private void CreateRenderer(
            Transform parent,
            Sprite sprite,
            GridPosition logicalCell,
            Vector3 offset,
            Vector3? explicitPosition = null)
        {
            GameObject child = new GameObject("Planned Fixture Sprite");
            child.transform.SetParent(parent, true);
            child.transform.position = explicitPosition
                ?? viewHost.GetLogicalCellCenterWorld(
                    logicalCell,
                    coordinateTilemap) + offset;

            GridPosition displayCell =
                viewHost.Projection.ToDisplayCell(logicalCell);
            SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = plannedColor;
            renderer.sortingLayerName = sortingLayerName;
            renderer.sortingOrder =
                300
                - (displayCell.X + displayCell.Y)
                * IsometricRenderOrderResolver.DisplayDepthOrderStep;
        }

        private void ClearViews()
        {
            foreach (GameObject view in views.Values)
            {
                if (view != null)
                {
                    Destroy(view);
                }
            }

            views.Clear();
        }
    }
}
