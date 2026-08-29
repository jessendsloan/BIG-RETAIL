using BigRetail.Core.Session;
using BigRetail.Departments.Unity;
using BigRetail.Map.Unity;
using BigRetail.Map.Unity.Fixtures;
using BigRetail.Map.Unity.Floors;
using BigRetail.Map.Unity.Foundations;
using BigRetail.Map.Unity.Sidewalks;
using BigRetail.Purchasing.Unity;
using BigRetail.Receiving.Unity;
using BigRetail.Simulation.Time.Unity;
using UnityEngine;

namespace BigRetail.StoreLayouts.Unity
{
    /// <summary>
    /// Loads a scene's authored starter store for a campaign session. Direct
    /// scene launches and Map Workshop sessions retain their empty runtime so
    /// the same location remains safe to author and test.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-42)]
    public sealed class StoreLayoutSceneBootstrap : MonoBehaviour
    {
        [SerializeField]
        private StoreLayoutAsset initialLayout;

        [SerializeField]
        private StoreScenarioAsset initialScenario;

        [SerializeField]
        private GridMapHost mapHost;

        [SerializeField]
        private FoundationRuntimeHost foundationRuntimeHost;

        [SerializeField]
        private SidewalkRuntimeHost sidewalkRuntimeHost;

        [SerializeField]
        private FloorRuntimeHost floorRuntimeHost;

        [SerializeField]
        private FixtureRuntimeHost fixtureRuntimeHost;

        [SerializeField]
        private FixtureEquipmentRuntimeHost fixtureEquipmentRuntimeHost;

        [SerializeField]
        private DepartmentRuntimeHost departmentRuntimeHost;

        [SerializeField]
        private ReceivingAreaRuntimeHost receivingAreaRuntimeHost;

        [SerializeField]
        private SimulationTimeRuntimeHost simulationTimeRuntimeHost;

        [SerializeField]
        private FixturePlanogramRuntimeHost fixturePlanogramRuntimeHost;

        [SerializeField]
        private PurchasingRuntimeHost purchasingRuntimeHost;


        public StoreLayoutAsset InitialLayout => initialLayout;

        public StoreScenarioAsset InitialScenario => initialScenario;

        public bool HasLoadedInitialLayout { get; private set; }

        public bool HasLoadedInitialScenario { get; private set; }

        public string LastFailure { get; private set; } = string.Empty;


        private void Start()
        {
            if (!ShouldLoadForCurrentSession())
            {
                return;
            }

            if (!TryLoadInitialStore())
            {
                Debug.LogError(LastFailure, this);
            }
        }


        public bool TryLoadInitialStore()
        {
            return TryLoadInitialLayout()
                && TryLoadInitialScenario();
        }


        public bool TryLoadInitialLayout()
        {
            if (HasLoadedInitialLayout)
            {
                return true;
            }

            if (initialLayout == null
                || mapHost == null
                || foundationRuntimeHost == null
                || sidewalkRuntimeHost == null
                || floorRuntimeHost == null
                || fixtureRuntimeHost == null
                || fixtureEquipmentRuntimeHost == null
                || departmentRuntimeHost == null
                || receivingAreaRuntimeHost == null)
            {
                LastFailure =
                    "The scene's starter store is missing its layout or one "
                    + "of its required runtime hosts.";
                return false;
            }

            if (!fixtureEquipmentRuntimeHost.TryInitialize()
                || fixtureEquipmentRuntimeHost.Plans == null)
            {
                LastFailure = string.IsNullOrWhiteSpace(
                    fixtureEquipmentRuntimeHost.InitializationError)
                    ? "Fixture equipment was not ready for the starter store."
                    : fixtureEquipmentRuntimeHost.InitializationError;
                return false;
            }

            StoreLayoutRuntimeLoader loader =
                new StoreLayoutRuntimeLoader(
                    mapHost,
                    foundationRuntimeHost,
                    sidewalkRuntimeHost,
                    floorRuntimeHost,
                    fixtureRuntimeHost,
                    fixtureEquipmentRuntimeHost.Plans,
                    departmentRuntimeHost,
                    receivingAreaRuntimeHost);
            StoreLayoutLoadResult result =
                loader.Load(initialLayout);

            if (!result.Succeeded)
            {
                LastFailure =
                    $"Could not load starter store '{initialLayout.name}': "
                    + result.Message;
                return false;
            }

            HasLoadedInitialLayout = true;
            LastFailure = string.Empty;

            Debug.Log(result.Message, this);
            return true;
        }

        public bool TryLoadInitialScenario()
        {
            if (HasLoadedInitialScenario)
            {
                return true;
            }

            if (!HasLoadedInitialLayout)
            {
                LastFailure =
                    "The scene's starter scenario requires its layout to "
                    + "load first.";
                return false;
            }

            if (initialScenario == null
                || simulationTimeRuntimeHost == null
                || fixturePlanogramRuntimeHost == null
                || purchasingRuntimeHost == null)
            {
                LastFailure =
                    "The scene's starter store is missing its scenario, "
                    + "simulation clock, merchandising runtime, or "
                    + "Purchasing runtime host.";
                return false;
            }

            StoreScenarioRuntimeLoader loader =
                new StoreScenarioRuntimeLoader(
                    fixtureRuntimeHost,
                    fixturePlanogramRuntimeHost,
                    simulationTimeRuntimeHost,
                    purchasingRuntimeHost);
            StoreScenarioLoadResult result =
                loader.Load(initialScenario, initialLayout);

            if (!result.Succeeded)
            {
                LastFailure =
                    $"Could not load starter scenario "
                    + $"'{initialScenario.name}': {result.Message}";
                return false;
            }

            HasLoadedInitialScenario = true;
            LastFailure = string.Empty;

            Debug.Log(result.Message, this);
            return true;
        }


        private static bool ShouldLoadForCurrentSession()
        {
            return GameSessionHost.ActiveMode == GameMode.Campaign
                && !MapWorkshopSession.IsActive;
        }
    }
}
