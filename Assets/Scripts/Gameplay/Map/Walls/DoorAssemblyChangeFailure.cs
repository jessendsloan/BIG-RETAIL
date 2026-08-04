namespace BigRetail.Map.Walls
{
    /// <summary>
    /// Identifies why a requested door-assembly change was rejected.
    /// </summary>
    public enum DoorAssemblyChangeFailure
    {
        None = 0,
        InvalidAssemblyId,
        UnknownDefinition,
        AssemblyAlreadyExists,
        EmptySpan,
        IncorrectSegmentCount,
        InvalidSpan,
        MissingWall,
        OverlapsAssembly,
        AssemblyNotFound,
        StateConflict
    }
}
