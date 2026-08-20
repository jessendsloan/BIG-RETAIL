using System;
using BigRetail.Map.Domain;
using BigRetail.Map.Unity;
using BigRetail.Map.Unity.Fixtures;
using BigRetail.Map.Unity.Floors;
using BigRetail.Receiving.Domain;
using UnityEngine;

namespace BigRetail.Receiving.Unity
{
    /// <summary>
    /// Owns Receiving Area designations and delivery-space reservations for
    /// the active store map.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-45)]
    public sealed class ReceivingAreaRuntimeHost :
        MonoBehaviour,
        IReceivingAreaSurfaceQuery
    {
        [SerializeField]
        private GridMapHost mapHost;

        [SerializeField]
        private FloorRuntimeHost floorRuntimeHost;

        [SerializeField]
        private FixtureRuntimeHost fixtureRuntimeHost;


        public bool IsInitialized { get; private set; }

        public ReceivingAreaState State { get; private set; }

        public ReceivingAreaService Designations { get; private set; }

        public ReceivingAreaReservationService Reservations
        {
            get;
            private set;
        }

        public int OperationalCellCount
        {
            get
            {
                int count = 0;

                if (State == null)
                {
                    return count;
                }

                foreach (GridPosition cell in State.EnumerateCells())
                {
                    if (IsCellOperational(cell))
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public int AvailableOperationalCellCount =>
            Mathf.Max(
                0,
                OperationalCellCount - (State?.ReservationCount ?? 0));


        public event Action<ReceivingAreaRuntimeHost> Initialized;


        private void OnEnable()
        {
            if (mapHost != null)
            {
                mapHost.Initialized += HandleMapInitialized;
            }

            if (floorRuntimeHost != null)
            {
                floorRuntimeHost.Initialized += HandleFloorInitialized;
            }

            if (fixtureRuntimeHost != null)
            {
                fixtureRuntimeHost.Initialized += HandleFixtureInitialized;
            }

            TryInitialize();
        }

        private void Start()
        {
            if (!TryInitialize())
            {
                Debug.LogError(
                    "ReceivingAreaRuntimeHost could not initialize because "
                    + "its map, floor, or fixture runtime is unavailable.",
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
                || floorRuntimeHost == null
                || !floorRuntimeHost.TryInitialize()
                || floorRuntimeHost.FloorState == null
                || fixtureRuntimeHost == null
                || !fixtureRuntimeHost.TryInitialize()
                || fixtureRuntimeHost.FixtureState == null)
            {
                return false;
            }

            State = new ReceivingAreaState();
            Designations = new ReceivingAreaService(
                mapHost.MapDefinition,
                mapHost.ConstructionEligibility,
                this,
                State);
            Reservations = new ReceivingAreaReservationService(
                State,
                IsCellOperational);
            IsInitialized = true;
            Initialized?.Invoke(this);

            Debug.Log(
                $"Activated Receiving Area planning for map "
                + $"'{mapHost.MapDefinition.MapId}'.",
                this);
            return true;
        }

        public bool HasFloor(
            GridPosition cell)
        {
            return floorRuntimeHost != null
                && floorRuntimeHost.FloorState != null
                && floorRuntimeHost.FloorState.HasFloor(cell);
        }

        public bool IsObstructed(
            GridPosition cell)
        {
            return fixtureRuntimeHost != null
                && fixtureRuntimeHost.FixtureState != null
                && fixtureRuntimeHost.FixtureState.IsOccupied(cell);
        }

        public bool IsCellOperational(
            GridPosition cell)
        {
            return Designations != null
                && Designations.EvaluateCell(cell)
                    == ReceivingAreaChangeFailure.None;
        }

        private void HandleMapInitialized(GridMapHost initializedHost)
        {
            TryInitialize();
        }

        private void HandleFloorInitialized(FloorRuntimeHost initializedHost)
        {
            TryInitialize();
        }

        private void HandleFixtureInitialized(
            FixtureRuntimeHost initializedHost)
        {
            TryInitialize();
        }

        private void OnDisable()
        {
            if (mapHost != null)
            {
                mapHost.Initialized -= HandleMapInitialized;
            }

            if (floorRuntimeHost != null)
            {
                floorRuntimeHost.Initialized -= HandleFloorInitialized;
            }

            if (fixtureRuntimeHost != null)
            {
                fixtureRuntimeHost.Initialized -= HandleFixtureInitialized;
            }
        }
    }
}
