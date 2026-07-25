namespace BigRetail.Map.Walls
{
    /// <summary>
    /// Identifies why an undo or redo request could not complete.
    /// </summary>
    public enum WallHistoryFailure
    {
        None,
        NothingToUndo,
        NothingToRedo,
        EditCouldNotBeApplied
    }
}
