using System;
using BigRetail.Map.Domain;
using BigRetail.Map.Unity;
using BigRetail.Map.Unity.Floors;
using BigRetail.Map.Unity.Foundations;
using UnityEngine;

namespace BigRetail.Departments.Unity
{
    /// <summary>
    /// Composes department planning for one activated map. The host owns
    /// mutable player planning state; authored department definitions remain
    /// ScriptableObject assets.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-80)]
    public sealed class DepartmentRuntimeHost :
        MonoBehaviour,
        IDepartmentSurfaceQuery
    {
        [SerializeField]
        private GridMapHost mapHost;

        [SerializeField]
        private FoundationRuntimeHost foundationRuntimeHost;

        [SerializeField]
        private FloorRuntimeHost floorRuntimeHost;

        [SerializeField]
        private DepartmentDefinitionCatalogAsset definitionAssets;


        public bool IsInitialized { get; private set; }

        public DepartmentDefinitionCatalog DefinitionCatalog
        {
            get;
            private set;
        }

        public DepartmentPlanningState PlanningState
        {
            get;
            private set;
        }

        public DepartmentPlanningService Planning
        {
            get;
            private set;
        }

        public DepartmentSpatialReadinessEvaluator SpatialReadiness
        {
            get;
            private set;
        }

        public event Action<DepartmentRuntimeHost> Initialized;


        private void OnEnable()
        {
            if (mapHost != null)
            {
                mapHost.Initialized += HandleMapInitialized;
            }
        }


        private void Start()
        {
            if (!TryInitialize())
            {
                Debug.LogError(
                    "DepartmentRuntimeHost could not initialize because "
                    + "GridMapHost has not produced its runtime map data "
                    + "or the department catalog is invalid.",
                    this);
            }
        }


        public bool TryInitialize()
        {
            if (IsInitialized)
            {
                return true;
            }

            DepartmentDefinitionCatalog catalog = null;
            string error = string.Empty;

            if (mapHost == null
                || !mapHost.IsInitialized
                || mapHost.MapDefinition == null
                || mapHost.ConstructionArea == null
                || definitionAssets == null
                || !definitionAssets.TryCreateCatalog(
                    out catalog,
                    out error))
            {
                if (definitionAssets != null
                    && !string.IsNullOrEmpty(error))
                {
                    Debug.LogError(
                        $"DepartmentRuntimeHost could not initialize: {error}",
                        this);
                }

                return false;
            }

            DefinitionCatalog = catalog;
            PlanningState = new DepartmentPlanningState();
            Planning = new DepartmentPlanningService(
                mapHost.MapDefinition,
                mapHost.ConstructionArea,
                DefinitionCatalog,
                PlanningState,
                this);
            SpatialReadiness =
                new DepartmentSpatialReadinessEvaluator(
                    DefinitionCatalog,
                    PlanningState,
                    this);

            IsInitialized = true;
            Initialized?.Invoke(this);

            Debug.Log(
                $"Activated department planning for map "
                + $"'{mapHost.MapDefinition.MapId}'. "
                + $"Department definitions: "
                + $"{DefinitionCatalog.Count}.",
                this);

            return true;
        }


        public bool HasFoundation(GridPosition cell)
        {
            return foundationRuntimeHost != null
                && foundationRuntimeHost.HasFoundation(cell);
        }


        public bool HasFloor(GridPosition cell)
        {
            return floorRuntimeHost != null
                && floorRuntimeHost.IsInitialized
                && floorRuntimeHost.FloorState != null
                && floorRuntimeHost.FloorState.HasFloor(cell);
        }


        private void HandleMapInitialized(
            GridMapHost initializedMapHost)
        {
            TryInitialize();
        }


        private void OnDisable()
        {
            if (mapHost != null)
            {
                mapHost.Initialized -= HandleMapInitialized;
            }
        }


        private void OnValidate()
        {
            if (mapHost == null)
            {
                Debug.LogWarning(
                    "DepartmentRuntimeHost requires a GridMapHost reference.",
                    this);
            }

            if (foundationRuntimeHost == null)
            {
                Debug.LogWarning(
                    "DepartmentRuntimeHost requires a FoundationRuntimeHost "
                    + "reference for spatial-readiness reporting.",
                    this);
            }

            if (floorRuntimeHost == null)
            {
                Debug.LogWarning(
                    "DepartmentRuntimeHost requires a FloorRuntimeHost "
                    + "reference for spatial-readiness reporting.",
                    this);
            }

            if (definitionAssets == null)
            {
                Debug.LogWarning(
                    "DepartmentRuntimeHost requires a "
                    + "DepartmentDefinitionCatalogAsset reference.",
                    this);
            }
        }
    }
}
