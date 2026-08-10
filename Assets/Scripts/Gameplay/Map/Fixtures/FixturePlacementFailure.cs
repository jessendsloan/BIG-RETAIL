namespace BigRetail.Map.Fixtures
{
    /// <summary>
    /// Identifies why a requested fixture placement or removal was rejected.
    /// </summary>
    public enum FixturePlacementFailure
    {
        None = 0,
        InvalidInstanceId,
        UnknownDefinition,
        FixtureAlreadyExists,
        UnsupportedOrientation,
        OutsideMap,
        OutsideConstructionArea,
        MissingFloor,
        OverlapsFixture,
        BlocksDoorPassage,
        CrossesWall,
        BlockedAccess,
        FixtureNotFound,
        EmptyEdit,
        StateConflict
    }
}
