using System;

namespace BigRetail.Simulation.Time.Domain
{
    /// <summary>
    /// Primitive-only clock snapshot suitable for a future save-game payload.
    /// Public fields intentionally support field-based serializers.
    /// </summary>
    [Serializable]
    public sealed class SimulationClockState
    {
        public long TotalGameSeconds;
        public double FractionalGameSecond;
        public SimulationSpeed Speed;


        public SimulationClockState()
        {
        }

        public SimulationClockState(
            long totalGameSeconds,
            double fractionalGameSecond,
            SimulationSpeed speed)
        {
            TotalGameSeconds = totalGameSeconds;
            FractionalGameSecond = fractionalGameSecond;
            Speed = speed;
        }
    }
}
