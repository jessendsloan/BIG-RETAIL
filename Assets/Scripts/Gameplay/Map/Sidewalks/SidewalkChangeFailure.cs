namespace BigRetail.Map.Sidewalks
{
    public enum SidewalkChangeFailure
    {
        None,
        EmptyRequest,
        OutsideMap,
        OutsideConstructionArea,
        FoundationOccupied,
        AlreadyExists,
        NotFound
    }
}
