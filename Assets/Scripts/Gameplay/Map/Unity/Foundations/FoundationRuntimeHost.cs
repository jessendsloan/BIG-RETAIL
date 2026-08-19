using System;
using System.Collections.Generic;
using BigRetail.Map.Domain;
using BigRetail.Map.Construction;
using BigRetail.Map.Foundations;
using BigRetail.Map.Unity.Floors;
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
    [DefaultExecutionOrder(-95)]
    public sealed class FoundationRuntimeHost :
        MonoBehaviour,
        IFoundationSupportQuery,
        IFoundationRemovalValidator
    {
        [SerializeField]
        private GridMapHost mapHost;

        [SerializeField]
        private FloorRuntimeHost floorRuntimeHost;

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
                || mapHost.ConstructionEligibility == null)
            {
                return false;
            }

            FoundationState =
                new FoundationState();

            FoundationConstruction =
                new FoundationConstructionService(
                    mapHost.MapDefinition,
                    mapHost.ConstructionEligibility,
                    FoundationState,
                    this);

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


        public bool HasFoundation(
            GridPosition cell)
        {
            return IsInitialized
                && FoundationState != null
                && FoundationState.HasFoundation(cell);
        }


        public FoundationRemovalValidation ValidateRemoval(
            IReadOnlyList<GridPosition> cells)
        {
            if (cells == null)
            {
                throw new ArgumentNullException(
                    nameof(cells));
            }

            if (cells.Count == 0)
            {
                return FoundationRemovalValidation.Allowed();
            }

            if (!IsInitialized
                || FoundationState == null
                || mapHost == null
                || mapHost.WallState == null
                || floorRuntimeHost == null
                || !floorRuntimeHost.TryInitialize()
                || floorRuntimeHost.FloorState == null)
            {
                return FoundationRemovalValidation.Blocked(
                    cells[0]);
            }

            HashSet<GridPosition> removedCells =
                new HashSet<GridPosition>(
                    cells);

            foreach (GridPosition cell in removedCells)
            {
                if (floorRuntimeHost.FloorState.HasFloor(cell))
                {
                    return FoundationRemovalValidation.Blocked(
                        cell);
                }
            }

            foreach (CellEdge wall in mapHost.WallState.EnumerateWalls())
            {
                bool removesFirst =
                    removedCells.Contains(wall.FirstCell);

                bool removesSecond =
                    removedCells.Contains(wall.SecondCell);

                if (!removesFirst
                    && !removesSecond)
                {
                    continue;
                }

                bool firstRemainsSupported =
                    FoundationState.HasFoundation(wall.FirstCell)
                    && !removesFirst;

                bool secondRemainsSupported =
                    FoundationState.HasFoundation(wall.SecondCell)
                    && !removesSecond;

                if (!firstRemainsSupported
                    && !secondRemainsSupported)
                {
                    return FoundationRemovalValidation.Blocked(
                        removesFirst
                            ? wall.FirstCell
                            : wall.SecondCell);
                }
            }

            return FoundationRemovalValidation.Allowed();
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

            if (floorRuntimeHost == null)
            {
                Debug.LogWarning(
                    "FoundationRuntimeHost requires a FloorRuntimeHost " +
                    "reference to protect supported construction.",
                    this);
            }
        }
    }
}
