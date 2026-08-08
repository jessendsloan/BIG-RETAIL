namespace BigRetail.Map.Unity.Doors
{
    /// <summary>
    /// Selects the presentation model used by one authored door definition.
    /// The value is serialized, so the existing zero-valued sliding style
    /// remains compatible with automatic-door assets created before hinged
    /// doors were introduced.
    /// </summary>
    public enum DoorPresentationStyle
    {
        SlidingFourPanel = 0,
        HingedSinglePanel = 1,
        StaticDoorway = 2
    }
}
