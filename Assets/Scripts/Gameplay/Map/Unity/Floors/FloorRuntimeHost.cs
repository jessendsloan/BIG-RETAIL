using System;
using BigRetail.Map.Floors;
using UnityEngine;

namespace BigRetail.Map.Unity.Floors
{
    /// <summary>
    /// Owns the runtime floor subsystem for one activated grid map.
    ///
    /// GridMapHost owns the authored map and construction-area data.
    /// FloorRuntimeHost owns the mutable floor state derived from it.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-90)]
    public sealed class FloorRuntimeHost : MonoBehaviour
    {
        [SerializeField]
        private GridMapHost mapHost;


        public bool IsInitialized { get; private set; }

        public FloorState FloorState
        {
            get;
            private set;
        }

        public FloorConstructionService FloorConstruction
        {
            get;
            private set;
        }


        public event Action<FloorRuntimeHost> Initialized;


        private void OnEnable()
        {
            if (mapHost != null)
            {
                mapHost.Initialized +=
                    HandleMapInitialized;
            }
        }


        private void Start()
        {
            if (!TryInitialize())
            {
                Debug.LogError(
                    "FloorRuntimeHost could not initialize because " +
                    "GridMapHost has not produced its runtime map data.",
                    this);
            }
        }


        /// <summary>
        /// Creates the mutable floor subsystem once GridMapHost
        /// has activated the authored map.
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

            FloorState =
                new FloorState();

            FloorConstruction =
                new FloorConstructionService(
                    mapHost.MapDefinition,
                    mapHost.ConstructionArea,
                    FloorState);

            IsInitialized = true;

            Initialized?.Invoke(this);

            Debug.Log(
                $"Activated floor subsystem for map " +
                $"'{mapHost.MapDefinition.MapId}'. " +
                $"Initial floors: {FloorState.FloorCount}.",
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
                mapHost.Initialized -=
                    HandleMapInitialized;
            }
        }


        private void OnValidate()
        {
            if (mapHost == null)
            {
                Debug.LogWarning(
                    "FloorRuntimeHost requires a GridMapHost reference.",
                    this);
            }
        }
    }
}