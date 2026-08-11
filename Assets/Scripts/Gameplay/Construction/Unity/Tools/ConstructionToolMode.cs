namespace BigRetail.Construction.Unity.Tools
{
    /// <summary>
    /// Identifies the construction interaction currently owned
    /// by the player's shared construction pointer.
    /// </summary>
    public enum ConstructionToolMode
    {
        None = 0,
        BuildWalls = 1,
        DemolishWalls = 2,
        BuildFloors = 3,
        DemolishFloors = 4,
        BuildFoundations = 5,
        DemolishFoundations = 6,
        BuildDoors = 7,
        BuildFixtures = 8,
        DemolishFixtures = 9,
        MerchandiseFixtures = 10
    }
}
