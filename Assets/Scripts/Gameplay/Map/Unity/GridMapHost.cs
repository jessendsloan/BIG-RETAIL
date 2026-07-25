using System;
using BigRetail.Map.Construction;
using BigRetail.Map.Domain;
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

        /// <summary>
        /// Creates the runtime map and its initial wall system.
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

            try
            {
                MapDefinition =
                    mapAuthoring.CreateMapDefinition();

                ConstructionArea =
                    mapAuthoring
                        .CreateConstructionAreaDefinition(
                            MapDefinition);

                // The current map begins with no runtime walls.
                // Save loading can supply restored walls here later.
                WallState =
                    new WallState();

                WallConstruction =
                    new WallConstructionService(
                        MapDefinition,
                        ConstructionArea,
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
                Debug.LogException(
                    exception,
                    this);

                enabled = false;
            }
        }

        private void LogInitializationSummary()
        {
            Debug.Log(
                $"Activated grid map '{MapDefinition.MapId}'. " +
                $"Logical level: {mapAuthoring.LogicalLevel}. " +
                $"Valid cells: {MapDefinition.ValidCellCount}. " +
                $"Construction-eligible cells: " +
                $"{ConstructionArea.EligibleCellCount}. " +
                $"Initial walls: {WallState.WallCount}.",
                this);
        }
    }
}