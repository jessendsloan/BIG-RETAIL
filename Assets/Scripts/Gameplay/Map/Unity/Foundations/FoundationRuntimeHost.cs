using System;
using BigRetail.Map.Domain;
using BigRetail.Map.Foundations;
using UnityEngine;

namespace BigRetail.Map.Unity.Foundations
{
    /// <summary>
    /// Owns the runtime foundation subsystem for one activated grid map.
    ///
    /// GridMapHost owns authored map and construction-area data.
    /// This host owns mutable foundation state derived from that data.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-90)]
    public sealed class FoundationRuntimeHost : MonoBehaviour
    {
        [SerializeField]
        private GridMapHost mapHost;

        public bool IsInitialized { get; private set; }

        public FoundationState FoundationState
        {
            get;
            private set;
        }

        public FoundationConstructionService FoundationConstruction
        {
            get;
            private set;
        }

        public GridMapDefinition MapDefinition =>
            mapHost != null
                ? mapHost.MapDefinition
                : null;

        public event Action<FoundationRuntimeHost> Initialized;

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
                    "FoundationRuntimeHost could not initialize because " +
                    "GridMapHost has not produced its runtime map data.",
                    this);
            }
        }

        /// <summary>
        /// Creates the mutable foundation subsystem once GridMapHost has
        /// activated the authored map.
        /// </summary>
        public bool TryInitialize()
        {
            if (IsInitialized)
            {
                return true;
            }

            if (mapHost == null
                || !mapHost.IsInitialized
                || mapHost.MapDefinition == null
                || mapHost.ConstructionArea == null)
            {
                return false;
            }

            FoundationState =
                new FoundationState();

            FoundationConstruction =
                new FoundationConstructionService(
                    mapHost.MapDefinition,
                    mapHost.ConstructionArea,
                    FoundationState);

            IsInitialized = true;
            Initialized?.Invoke(this);

            Debug.Log(
                $"Activated foundation subsystem for map " +
                $"'{mapHost.MapDefinition.MapId}'. " +
                $"Initial foundations: " +
                $"{FoundationState.FoundationCount}.",
                this);

            return true;
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
                    "FoundationRuntimeHost requires a GridMapHost reference.",
                    this);
            }
        }
    }
}
