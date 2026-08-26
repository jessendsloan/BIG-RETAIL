namespace BigRetail.StoreLayouts
{
    /// <summary>
    /// Owns the currently supported authored-data versions.
    /// Version changes require an explicit migration instead of best-effort
    /// loading because a partial store bootstrap is not recoverable safely.
    /// </summary>
    public static class StoreLayoutSchema
    {
        public const int CurrentLayoutVersion = 1;

        public const int CurrentScenarioVersion = 1;
    }
}
