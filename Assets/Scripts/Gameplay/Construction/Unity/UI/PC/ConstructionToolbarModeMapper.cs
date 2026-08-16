using BigRetail.Construction.Unity.Tools;

namespace BigRetail.Construction.Unity.UI.PC
{
    /// <summary>
    /// Translates authoritative construction tool state into the primary
    /// section highlighted by the PC toolbar.
    /// </summary>
    public static class ConstructionToolbarModeMapper
    {
        public static ConstructionToolbarSection ToSection(
            ConstructionToolMode mode)
        {
            return mode switch
            {
                ConstructionToolMode.BuildWalls =>
                    ConstructionToolbarSection.Walls,

                ConstructionToolMode.BuildDoors =>
                    ConstructionToolbarSection.Doors,

                ConstructionToolMode.BuildFixtures =>
                    ConstructionToolbarSection.Fixtures,

                ConstructionToolMode.BuildFoundations =>
                    ConstructionToolbarSection.Foundations,

                ConstructionToolMode.BuildSidewalks =>
                    ConstructionToolbarSection.Sidewalks,

                ConstructionToolMode.DemolishFoundations =>
                    ConstructionToolbarSection.Demolition,

                ConstructionToolMode.DemolishSidewalks =>
                    ConstructionToolbarSection.Demolition,

                ConstructionToolMode.BuildFloors =>
                    ConstructionToolbarSection.Floors,

                ConstructionToolMode.DemolishWalls =>
                    ConstructionToolbarSection.Demolition,

                ConstructionToolMode.DemolishFloors =>
                    ConstructionToolbarSection.Demolition,

                ConstructionToolMode.DemolishFixtures =>
                    ConstructionToolbarSection.Demolition,

                _ =>
                    ConstructionToolbarSection.None
            };
        }
    }
}
