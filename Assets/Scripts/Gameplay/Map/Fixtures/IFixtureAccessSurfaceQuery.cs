namespace BigRetail.Map.Fixtures
{
    /// <summary>
    /// Narrow physical-world boundary used to decide whether an actor may
    /// stand on a potential fixture-access cell. Route reachability remains
    /// a navigation concern.
    /// </summary>
    public interface IFixtureAccessSurfaceQuery
    {
        bool CanUseAccessPoint(FixtureAccessPoint accessPoint);
    }
}
