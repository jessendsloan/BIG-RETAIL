using BigRetail.Map.Domain;
using BigRetail.Map.Navigation;
using BigRetail.Map.Unity.Fixtures;
using BigRetail.Map.Unity.Sidewalks;
using UnityEngine;

namespace BigRetail.Map.Unity.Navigation
{
    /// <summary>
    /// Central walkability authority for grid-based people. Constructed
    /// foundations and sidewalks are the only traversable surfaces, so
    /// expanding a store or laying a path automatically expands navigation
    /// for employees and customers alike.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-20)]
    public sealed class GridNavigationSurfaceHost :
        MonoBehaviour,
        IGridRouteSurfaceQuery
    {
        [SerializeField]
        private GridMapHost mapHost;

        [SerializeField]
        private SidewalkRuntimeHost sidewalkRuntimeHost;

        [SerializeField]
        private FixtureRuntimeHost fixtureRuntimeHost;

        public bool IsInitialized { get; private set; }


        private void OnEnable()
        {
            TryInitialize();
        }

        private void Start()
        {
            if (!TryInitialize())
            {
                Debug.LogError(
                    "GridNavigationSurfaceHost could not initialize its "
                    + "map, sidewalk, or fixture dependencies.",
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
                || mapHost.FoundationState == null
                || sidewalkRuntimeHost == null
                || !sidewalkRuntimeHost.TryInitialize()
                || fixtureRuntimeHost == null
                || !fixtureRuntimeHost.TryInitialize()
                || fixtureRuntimeHost.FixtureState == null)
            {
                return false;
            }

            IsInitialized = true;
            return true;
        }


        public bool CanStandAt(GridPosition cell)
        {
            if (!IsInitialized && !TryInitialize())
            {
                return false;
            }

            bool hasWalkableSurface =
                mapHost.FoundationState.HasFoundation(cell)
                || sidewalkRuntimeHost.IsSidewalkWalkable(cell);

            return mapHost.MapDefinition.ContainsCell(cell)
                && hasWalkableSurface
                && !fixtureRuntimeHost.FixtureState.IsOccupied(cell);
        }


        public bool CanTraverse(CellEdge edge)
        {
            if (!IsInitialized && !TryInitialize())
            {
                return false;
            }

            if (mapHost.WallState == null
                || !mapHost.WallState.HasWall(edge))
            {
                return true;
            }

            return mapHost.DoorAssemblies != null
                && mapHost.DoorAssemblies.TryGetAssemblyAtEdge(
                    edge,
                    out BigRetail.Map.Walls.DoorAssembly door)
                && door.IsPassageEdge(edge);
        }
    }
}
