using System;
using System.Collections.Generic;
using BigRetail.Core.Session;
using BigRetail.Map.Construction;
using BigRetail.Map.Domain;
using BigRetail.Map.Foundations;
using BigRetail.Map.Unity.Doors;
using BigRetail.Map.Unity.Walls;
using BigRetail.Map.Unity.Foundations;
using BigRetail.Map.Walls;
using UnityEngine;

namespace BigRetail.Map.Unity
{
    /// <summary>
    /// Composes the active runtime map when Gameplay loads.
    ///
    /// This host connects Unity-authored semantic data to the plain
    /// C# map, construction, and wall systems.
    ///
    /// It is a composition point, not a general-purpose map manager.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-100)]
    public sealed class GridMapHost : MonoBehaviour
    {
        [Header("Authoring")]

        [SerializeField]
        private GridMapAuthoring mapAuthoring;

        [SerializeField]
        private WallFinishAssetCatalog wallFinishAssets;

        [SerializeField]
        private DoorDefinitionAssetCatalog doorDefinitionAssets;

        [SerializeField]
        private FoundationRuntimeHost foundationRuntimeHost;


        [Header("Prototype Land Region Progression")]

        [SerializeField]
        [Min(0)]
        private int firstExpansionPriceCents;

        [SerializeField]
        private string firstExpansionQualificationId =
            "prototype.first_land_region";


        [Header("Diagnostics")]

        [SerializeField]
        private bool logInitializationSummary = true;


        public GridMapDefinition MapDefinition
        {
            get;
            private set;
        }

        public ConstructionAreaDefinition ConstructionArea
        {
            get;
            private set;
        }

        /// <summary>
        /// The current construction gate. This combines the permanent
        /// authored property boundary with mutable Land Region ownership.
        /// </summary>
        public IConstructionCellEligibility ConstructionEligibility
        {
            get;
            private set;
        }

        public LandRegionCatalog LandRegions
        {
            get;
            private set;
        }

        public LandRegionOwnershipState LandRegionOwnership
        {
            get;
            private set;
        }

        public LandRegionPurchaseService LandRegionPurchases
        {
            get;
            private set;
        }

        public WallState WallState
        {
            get;
            private set;
        }

        public WallConstructionService WallConstruction
        {
            get;
            private set;
        }

        public WallFinishCatalog WallFinishCatalog
        {
            get;
            private set;
        }

        public WallFinishState WallFinishState
        {
            get;
            private set;
        }

        public WallFinishService WallFinishes
        {
            get;
            private set;
        }

        public WallAppearanceStrokeService WallAppearanceStrokes
        {
            get;
            private set;
        }

        public WallFinishAssetCatalog WallFinishAssets =>
            wallFinishAssets;

        public DoorDefinitionCatalog DoorDefinitions
        {
            get;
            private set;
        }

        public DoorAssemblyState DoorAssemblies
        {
            get;
            private set;
        }

        public DoorConstructionService DoorConstruction
        {
            get;
            private set;
        }

        public DoorDefinitionAssetCatalog DoorDefinitionAssets =>
            doorDefinitionAssets;

        public FoundationState FoundationState =>
            foundationRuntimeHost != null
                ? foundationRuntimeHost.FoundationState
                : null;

        public bool IsInitialized
        {
            get;
            private set;
        }


        /// <summary>
        /// Raised after all runtime map services have been created.
        /// </summary>
        public event Action<GridMapHost> Initialized;


        private void Awake()
        {
            Initialize();
        }


        private void OnDestroy()
        {
            DoorConstruction?.Dispose();
            DoorConstruction = null;
            WallAppearanceStrokes = null;
            WallFinishes?.Dispose();
            WallFinishes = null;
        }


        /// <summary>
        /// Creates the runtime map and its initial wall systems.
        ///
        /// Calling this more than once has no effect after successful
        /// initialization.
        /// </summary>
        public void Initialize()
        {
            if (IsInitialized)
            {
                return;
            }

            if (mapAuthoring == null)
            {
                Debug.LogError(
                    "GridMapHost has no GridMapAuthoring assigned.",
                    this);

                enabled = false;
                return;
            }

            if (wallFinishAssets == null)
            {
                Debug.LogError(
                    "GridMapHost has no WallFinishAssetCatalog assigned.",
                    this);

                enabled = false;
                return;
            }

            if (doorDefinitionAssets == null)
            {
                Debug.LogError(
                    "GridMapHost has no DoorDefinitionAssetCatalog assigned.",
                    this);

                enabled = false;
                return;
            }

            if (foundationRuntimeHost == null)
            {
                Debug.LogError(
                    "GridMapHost has no FoundationRuntimeHost assigned.",
                    this);

                enabled = false;
                return;
            }

            try
            {
                MapDefinition =
                    mapAuthoring.CreateMapDefinition();

                ConstructionArea =
                    mapAuthoring
                        .CreateConstructionAreaDefinition(
                            MapDefinition);

                LandRegions =
                    LandRegionCatalog.CreateFor(ConstructionArea);

                LandRegionOwnership =
                    new LandRegionOwnershipState(LandRegions);

                if (GameSessionHost.ActiveMode == GameMode.Campaign)
                {
                    LandRegionOwnership.Own(
                        LandRegionCatalog.FrontCornerRegionId);
                }
                else
                {
                    // Direct Gameplay launches and Sandbox sessions preserve
                    // the established unrestricted construction workflow.
                    LandRegionOwnership.OwnAll();
                }

                ConstructionEligibility =
                    new LandRegionConstructionEligibility(
                        ConstructionArea,
                        LandRegions,
                        LandRegionOwnership);

                LandRegionPurchases =
                    new LandRegionPurchaseService(
                        LandRegions,
                        LandRegionOwnership,
                        CreatePrototypePurchaseOptions());

                // The current map begins with no runtime walls.
                // Save loading can supply restored walls here later.
                WallState =
                    new WallState();

                WallConstruction =
                    new WallConstructionService(
                        MapDefinition,
                        ConstructionEligibility,
                        WallState,
                        foundationRuntimeHost);

                WallFinishCatalog =
                    wallFinishAssets.CreateDomainCatalog();

                WallFinishState =
                    new WallFinishState();

                WallFinishes =
                    new WallFinishService(
                        WallState,
                        WallFinishCatalog,
                        WallFinishState);

                WallAppearanceStrokes =
                    new WallAppearanceStrokeService(
                        WallConstruction,
                        WallFinishes,
                        WallFinishCatalog);

                DoorDefinitions =
                    doorDefinitionAssets.CreateDomainCatalog();

                DoorAssemblies =
                    new DoorAssemblyState();

                DoorConstruction =
                    new DoorConstructionService(
                        DoorDefinitions,
                        DoorAssemblies,
                        WallState);

                IsInitialized = true;

                mapAuthoring.ApplyRuntimeVisibility();

                if (logInitializationSummary)
                {
                    LogInitializationSummary();
                }

                Initialized?.Invoke(this);
            }
            catch (Exception exception)
            {
                DoorConstruction?.Dispose();
                DoorConstruction = null;
                DoorAssemblies = null;
                DoorDefinitions = null;
                LandRegionPurchases = null;
                ConstructionEligibility = null;
                LandRegionOwnership = null;
                LandRegions = null;
                WallAppearanceStrokes = null;
                WallFinishes?.Dispose();
                WallFinishes = null;

                Debug.LogException(
                    exception,
                    this);

                enabled = false;
            }
        }


        private void LogInitializationSummary()
        {
            Debug.Log(
                $"Activated grid map '{MapDefinition.MapId}'. "
                + $"Logical level: {mapAuthoring.LogicalLevel}. "
                + $"Valid cells: {MapDefinition.ValidCellCount}. "
                + $"Construction-eligible cells: "
                + $"{ConstructionArea.EligibleCellCount}. "
                + $"Owned Land Regions: "
                + $"{LandRegionOwnership.OwnedRegionCount}/"
                + $"{LandRegionCatalog.RegionCount}. "
                + $"Initial walls: {WallState.WallCount}. "
                + $"Wall finishes: {WallFinishCatalog.Count}. "
                + $"Door definitions: {DoorDefinitionAssets.Count}.",
                this);
        }


        /// <summary>
        /// Temporary, inspector-accessible proof of the ownership transition.
        /// A future campaign/economy flow will authorize payment and permit
        /// requirements before calling the same purchase service.
        /// </summary>
        [ContextMenu("Testing/Purchase First Available Land Region")]
        public void PurchaseFirstAvailableLandRegionForTesting()
        {
            if (LandRegionPurchases == null)
            {
                Debug.LogWarning(
                    "Land Region purchases are not initialized.",
                    this);
                return;
            }

            foreach (LandRegionPurchaseOption option in
                     LandRegionPurchases.EnumerateAvailableOptions())
            {
                LandRegionPurchaseResult result =
                    LandRegionPurchases.TryCompletePurchase(
                        option.RegionId);

                Debug.Log(
                    result.Succeeded
                        ? $"Purchased {result.RegionId} for testing."
                        : $"Could not purchase {result.RegionId}: "
                            + $"{result.Failure}.",
                    this);
                return;
            }

            Debug.LogWarning(
                "No offered adjacent Land Region is currently available.",
                this);
        }


        private IEnumerable<LandRegionPurchaseOption>
            CreatePrototypePurchaseOptions()
        {
            // This is intentionally only the first adjacent expansion. The
            // final price, permit ladder, and remaining region offers are not
            // locked by the current design patch.
            yield return new LandRegionPurchaseOption(
                new LandRegionId(1, 0),
                firstExpansionPriceCents,
                firstExpansionQualificationId);
        }
    }
}
