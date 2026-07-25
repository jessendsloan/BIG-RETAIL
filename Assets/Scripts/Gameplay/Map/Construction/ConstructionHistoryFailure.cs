namespace BigRetail.Map.Construction
{
    /// <summary>
    /// Identifies why a construction-history request could not
    /// complete.
    /// </summary>
    public enum ConstructionHistoryFailure
    {
        None,
        NothingToUndo,
        NothingToRedo,
        ActionCouldNotBeApplied
    }
}
