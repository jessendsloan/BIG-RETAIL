using System;
using System.Collections.Generic;
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

        private readonly Dictionary<string, ReceivingLoadId[]>
            readyLoadsBySource =
                new Dictionary<string, ReceivingLoadId[]>(
                    StringComparer.Ordinal);


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

        public event Action ReservationsSynchronized;


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

        public void SetReadyLoads(
            string source,
            IReadOnlyList<ReceivingLoadId> readyLoadIds)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                throw new ArgumentException(
                    "A Receiving load set requires a source.",
                    nameof(source));
            }

            if (readyLoadIds == null)
            {
                throw new ArgumentNullException(nameof(readyLoadIds));
            }

            ReceivingLoadId[] snapshot =
                new ReceivingLoadId[readyLoadIds.Count];

            for (int index = 0; index < readyLoadIds.Count; index++)
            {
                snapshot[index] = readyLoadIds[index];
            }

            readyLoadsBySource[source.Trim()] = snapshot;
            SynchronizeReadyLoads();
        }

        public void ClearReadyLoads(string source)
        {
            if (string.IsNullOrWhiteSpace(source)
                || !readyLoadsBySource.Remove(source.Trim()))
            {
                return;
            }

            SynchronizeReadyLoads();
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

        private void SynchronizeReadyLoads()
        {
            if (!TryInitialize() || Reservations == null)
            {
                return;
            }

            List<string> sources =
                new List<string>(readyLoadsBySource.Keys);
            sources.Sort(StringComparer.Ordinal);

            List<ReceivingLoadId> combined =
                new List<ReceivingLoadId>();

            for (int sourceIndex = 0;
                 sourceIndex < sources.Count;
                 sourceIndex++)
            {
                ReceivingLoadId[] sourceLoads =
                    readyLoadsBySource[sources[sourceIndex]];

                for (int loadIndex = 0;
                     loadIndex < sourceLoads.Length;
                     loadIndex++)
                {
                    combined.Add(sourceLoads[loadIndex]);
                }
            }

            Reservations.Synchronize(combined);
            ReservationsSynchronized?.Invoke();
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
