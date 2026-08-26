using System;
using BigRetail.Departments;
using BigRetail.Departments.Unity;
using BigRetail.Map.Floors;
using BigRetail.Map.Fixtures;
using BigRetail.Map.Unity;
using BigRetail.Map.Unity.Fixtures;
using BigRetail.Map.Unity.Floors;
using BigRetail.Map.Walls;

namespace BigRetail.StoreLayouts.Unity
{
    internal sealed class StoreRuntimeDefinitionCatalog :
        IStoreDefinitionCatalog
    {
        private readonly GridMapHost mapHost;
        private readonly FloorRuntimeHost floorHost;
        private readonly FixtureRuntimeHost fixtureHost;
        private readonly DepartmentRuntimeHost departmentHost;


        public StoreRuntimeDefinitionCatalog(
            GridMapHost mapHost,
            FloorRuntimeHost floorHost,
            FixtureRuntimeHost fixtureHost,
            DepartmentRuntimeHost departmentHost)
        {
            this.mapHost = mapHost;
            this.floorHost = floorHost;
            this.fixtureHost = fixtureHost;
            this.departmentHost = departmentHost;
        }


        public bool Contains(
            StoreDefinitionKind kind,
            string definitionId)
        {
            if (string.IsNullOrWhiteSpace(definitionId))
            {
                return false;
            }

            try
            {
                switch (kind)
                {
                    case StoreDefinitionKind.FloorFinish:
                        return floorHost.FloorFinishCatalog.Contains(
                            new FloorFinishId(definitionId));

                    case StoreDefinitionKind.WallFinish:
                        return mapHost.WallFinishCatalog.Contains(
                            new WallFinishId(definitionId));

                    case StoreDefinitionKind.Opening:
                        return mapHost.DoorDefinitions.Contains(
                            new DoorDefinitionId(definitionId));

                    case StoreDefinitionKind.Fixture:
                        return fixtureHost.Definitions.Contains(
                            new FixtureDefinitionId(definitionId));

                    case StoreDefinitionKind.Department:
                        return departmentHost.DefinitionCatalog
                            .TryGetDefinition(
                                new DepartmentDefinitionId(definitionId),
                                out _);

                    default:
                        return false;
                }
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
    }
}
