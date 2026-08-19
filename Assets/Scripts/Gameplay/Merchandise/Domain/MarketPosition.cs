namespace BigRetail.Merchandise.Domain
{
    /// <summary>
    /// Describes the commercial lane occupied by a SKU. Product lines are not
    /// required to provide an entry in every lane.
    /// </summary>
    public enum MarketPosition
    {
        Value = 0,
        Standard = 1,
        Premium = 2
    }
}
