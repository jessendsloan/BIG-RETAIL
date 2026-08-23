namespace BigRetail.Map.Construction
{
    /// <summary>
    /// Selects how one authored location grants construction access.
    /// Existing scenes default to PurchasableLandRegions for serialized
    /// compatibility; authored fixed locations deliberately opt in.
    /// </summary>
    public enum LocationLandPolicyKind
    {
        PurchasableLandRegions = 0,
        FixedFootprint = 1
    }
}
