using System;
using BigRetail.Map.Domain;
using BigRetail.Map.Fixtures;
using BigRetail.Map.Unity.Floors;
using BigRetail.Map.Walls;
using UnityEngine;

namespace BigRetail.Map.Unity.Fixtures
{
    /// <summary>
    /// Owns fixture state and placement services for the active map.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-70)]
    public sealed class FixtureRuntimeHost :
        MonoBehaviour,
        IFixturePlacementSurfaceQuery,
        IFixtureAccessSurfaceQuery,
        IWallPlacementConstraint
    {
        [SerializeField]
        private GridMapHost mapHost;

        [SerializeField]
        private FloorRuntimeHost floorRuntimeHost;

        [SerializeField]
        private FixtureDefinitionAssetCatalog definitionAssets;


        public bool IsInitialized { get; private set; }

        public FixtureDefinitionCatalog Definitions { get; private set; }

        public FixtureState FixtureState { get; private set; }

        public FixturePlacementService FixturePlacement { get; private set; }

        public FixtureAccessQueryService FixtureAccess { get; private set; }

        public FixtureDefinitionAssetCatalog DefinitionAssets =>
            definitionAssets;


        public event Action<FixtureRuntimeHost> Initialized;


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

            if (IsInitialized)
            {
                RegisterWallPlacementConstraint();
            }
        }


        private void Start()
        {
            if (!TryInitialize())
            {
                Debug.LogError(
                    "FixtureRuntimeHost could not initialize because its map, floor, or catalog data is unavailable.",
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
                || mapHost.ConstructionArea == null
                || floorRuntimeHost == null
                || !floorRuntimeHost.TryInitialize()
                || floorRuntimeHost.FloorState == null
                || definitionAssets == null)
            {
                return false;
            }

            try
            {
                Definitions = definitionAssets.CreateDomainCatalog();
                FixtureState = new FixtureState();
                FixturePlacement =
                    new FixturePlacementService(
                        mapHost.MapDefinition,
                        mapHost.ConstructionArea,
                        Definitions,
                        FixtureState,
                        this);
                FixtureAccess =
                    new FixtureAccessQueryService(
                        FixtureState,
                        this);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                return false;
            }

            IsInitialized = true;
            RegisterWallPlacementConstraint();
            Initialized?.Invoke(this);

            Debug.Log(
                $"Activated fixture subsystem with {Definitions.Count} fixture definition(s).",
                this);

            return true;
        }


        public bool HasFloor(GridPosition cell)
        {
            return floorRuntimeHost != null
                && floorRuntimeHost.FloorState != null
                && floorRuntimeHost.FloorState.HasFloor(cell);
        }


        public bool HasWall(CellEdge edge)
        {
            return mapHost != null
                && mapHost.WallState != null
                && mapHost.WallState.HasWall(edge);
        }


        public bool IsReservedForDoorPassage(
            GridPosition cell)
        {
            return mapHost != null
                && mapHost.DoorAssemblies != null
                && mapHost.DoorAssemblies
                    .IsPassageCellReserved(cell);
        }


        public bool CanUseAccessPoint(
            FixtureAccessPoint accessPoint)
        {
            GridPosition cell = accessPoint.Cell;

            return mapHost != null
                && mapHost.MapDefinition != null
                && mapHost.MapDefinition.ContainsCell(cell)
                && mapHost.ConstructionArea != null
                && mapHost.ConstructionArea.IsEligible(cell)
                && HasFloor(cell)
                && !HasWall(accessPoint.BoundaryEdge);
        }


        public WallChangeFailure EvaluateWallPlacement(
            CellEdge edge)
        {
            return FixtureState != null
                && FixtureState.IsAccessBoundaryReserved(edge)
                    ? WallChangeFailure.BlocksFixtureAccess
                    : WallChangeFailure.None;
        }


        private void HandleMapInitialized(GridMapHost initializedHost)
        {
            TryInitialize();
        }


        private void HandleFloorInitialized(FloorRuntimeHost initializedHost)
        {
            TryInitialize();
        }


        private void RegisterWallPlacementConstraint()
        {
            mapHost?.WallConstruction
                ?.RegisterPlacementConstraint(this);
        }


        private void OnDisable()
        {
            mapHost?.WallConstruction
                ?.UnregisterPlacementConstraint(this);

            if (mapHost != null)
            {
                mapHost.Initialized -= HandleMapInitialized;
            }

            if (floorRuntimeHost != null)
            {
                floorRuntimeHost.Initialized -= HandleFloorInitialized;
            }
        }


        private void OnValidate()
        {
            if (mapHost == null || floorRuntimeHost == null || definitionAssets == null)
            {
                Debug.LogWarning(
                    "FixtureRuntimeHost requires map, floor, and fixture-catalog references.",
                    this);
            }
        }
    }
}
