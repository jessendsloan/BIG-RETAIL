namespace BigRetail.Map.Construction
{
    /// <summary>
    /// Defines how many successful construction transactions a
    /// gameplay session remembers.
    /// </summary>
    public enum ConstructionHistoryMode
    {
        /// <summary>
        /// Remembers exactly one reversible transaction.
        /// </summary>
        Standard,

        /// <summary>
        /// Remembers every reversible transaction in the session.
        /// </summary>
        Unlimited
    }
}
