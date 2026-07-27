namespace BigRetail.Map.Walls
{
    public enum WallAppearanceStrokeFailure
    {
        None = 0,
        EmptyRequest = 1,
        DuplicateEdge = 2,
        UnknownFinish = 3,
        WallEnsureRejected = 4,
        FinishChangeRejected = 5,
        RollbackFailed = 6
    }
}
