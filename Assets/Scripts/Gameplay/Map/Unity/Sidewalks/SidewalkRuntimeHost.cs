using System;
using BigRetail.Map.Construction;
using BigRetail.Map.Domain;
using BigRetail.Map.Sidewalks;
using BigRetail.Map.Unity.Foundations;
using UnityEngine;

namespace BigRetail.Map.Unity.Sidewalks
{
    /// <summary>
    /// Owns mutable sidewalk state for one activated map and exposes the
    /// pedestrian-walkability seam used by future route planning.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-94)]
    public sealed class SidewalkRuntimeHost :
        MonoBehaviour,
        ISidewalkOccupancyQuery,
        ISidewalkWalkabilityQuery
    {
        [SerializeField]
        private GridMapHost mapHost;

        [SerializeField]
        private FoundationRuntimeHost foundationRuntimeHost;


        public bool IsInitialized { get; private set; }

        public SidewalkState SidewalkState { get; private set; }

        public SidewalkConstructionService SidewalkConstruction
        {
            get;
            private set;
        }

        public event Action<SidewalkRuntimeHost> Initialized;


        private void OnEnable()
        {
            if (mapHost != null)
            {
                mapHost.Initialized += HandleMapInitialized;
            }

            if (foundationRuntimeHost != null)
            {
                foundationRuntimeHost.Initialized +=
                    HandleFoundationInitialized;
            }
        }


        private void Start()
        {
            if (!TryInitialize())
            {
                Debug.LogError(
                    "SidewalkRuntimeHost could not initialize because its " +
                    "map or foundation dependency is not ready.",
                    this);
            }
        }


        public bool TryInitialize()
        {
            if (IsInitialized)
            {
                return true;
            }

            if (mapHost == null
                || !mapHost.IsInitialized
                || mapHost.MapDefinition == null
                || mapHost.ConstructionEligibility == null
                || foundationRuntimeHost == null
                || !foundationRuntimeHost.TryInitialize())
            {
                return false;
            }

            SidewalkState = new SidewalkState();
            SidewalkConstruction =
                new SidewalkConstructionService(
                    mapHost.MapDefinition,
                    mapHost.ConstructionEligibility,
                    SidewalkState,
                    foundationRuntimeHost);

            IsInitialized = true;
            Initialized?.Invoke(this);

            Debug.Log(
                $"Activated sidewalk subsystem for map " +
                $"'{mapHost.MapDefinition.MapId}'.",
                this);

            return true;
        }


        public bool HasSidewalk(GridPosition cell)
        {
            return IsInitialized
                && SidewalkState != null
                && SidewalkState.HasSidewalk(cell);
        }


        public bool IsSidewalkWalkable(GridPosition cell)
        {
            return IsInitialized
                && SidewalkConstruction != null
                && SidewalkConstruction.IsSidewalkWalkable(cell);
        }


        private void HandleMapInitialized(GridMapHost initializedMapHost)
        {
            TryInitialize();
        }


        private void HandleFoundationInitialized(
            FoundationRuntimeHost initializedFoundationHost)
        {
            TryInitialize();
        }


        private void OnDisable()
        {
            if (mapHost != null)
            {
                mapHost.Initialized -= HandleMapInitialized;
            }

            if (foundationRuntimeHost != null)
            {
                foundationRuntimeHost.Initialized -=
                    HandleFoundationInitialized;
            }
        }


        private void OnValidate()
        {
            if (mapHost == null)
            {
                Debug.LogWarning(
                    "SidewalkRuntimeHost requires a GridMapHost reference.",
                    this);
            }

            if (foundationRuntimeHost == null)
            {
                Debug.LogWarning(
                    "SidewalkRuntimeHost requires a FoundationRuntimeHost " +
                    "reference.",
                    this);
            }
        }
    }
}
