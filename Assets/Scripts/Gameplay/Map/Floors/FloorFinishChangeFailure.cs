namespace BigRetail.Map.Floors
{
    /// <summary>
    /// Explains why a requested floor-finish change did not occur.
    /// </summary>
    public enum FloorFinishChangeFailure
    {
        None = 0,
        FloorNotFound = 1,
        UnknownFinish = 2
    }
}
