namespace BigRetail.Map.Walls
{
    /// <summary>
    /// Explains why a requested wall-face finish change did not occur.
    /// </summary>
    public enum WallFinishChangeFailure
    {
        None = 0,
        WallNotFound = 1,
        FacingCellNotOnEdge = 2,
        UnknownFinish = 3
    }
}
