namespace BigRetail.Simulation.Time.Domain
{
    /// <summary>
    /// Player-selectable simulation rates. The numeric value is the actual
    /// multiplier used by the deterministic clock.
    /// </summary>
    public enum SimulationSpeed
    {
        Paused = 0,
        OneTimes = 1,
        TwoTimes = 2,
        FourTimes = 4
    }
}
