using System;

namespace BigRetail.Simulation.Time.Domain
{
    /// <summary>
    /// Authoritative deterministic timeline for one store simulation.
    /// It knows only elapsed time, calendar projection, and player speed.
    /// Store hours and economic systems subscribe without becoming clock rules.
    /// </summary>
    public sealed class SimulationClock
    {
        private long totalGameSeconds;
        private double fractionalGameSecond;
        private SimulationSpeed speed;


        public SimulationDateTime CurrentTime =>
            SimulationDateTime.FromTotalGameSeconds(
                totalGameSeconds);

        public SimulationSpeed Speed =>
            speed;

        public double GameSecondsPerRealSecond { get; }


        public SimulationClock(
            int startingDay,
            int startingHour,
            int startingMinute,
            SimulationSpeed initialSpeed,
            double gameSecondsPerRealSecond)
        {
            ValidateSpeed(initialSpeed);

            if (double.IsNaN(gameSecondsPerRealSecond)
                || double.IsInfinity(gameSecondsPerRealSecond)
                || gameSecondsPerRealSecond <= 0d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(gameSecondsPerRealSecond),
                    "Clock rate must be a finite positive number.");
            }

            totalGameSeconds =
                SimulationDateTime.FromCalendar(
                    startingDay,
                    startingHour,
                    startingMinute)
                .TotalGameSeconds;
            speed = initialSpeed;
            GameSecondsPerRealSecond =
                gameSecondsPerRealSecond;
        }


        public event Action<SimulationDateTime> TimeChanged;

        public event Action<SimulationDateTime> DayChanged;

        public event Action<SimulationSpeed> SpeedChanged;


        public void Advance(
            double realSeconds)
        {
            if (double.IsNaN(realSeconds)
                || double.IsInfinity(realSeconds)
                || realSeconds < 0d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(realSeconds),
                    "Elapsed real time must be finite and non-negative.");
            }

            if (realSeconds == 0d
                || speed == SimulationSpeed.Paused)
            {
                return;
            }

            fractionalGameSecond +=
                realSeconds
                * GameSecondsPerRealSecond
                * (int)speed;

            long wholeGameSeconds =
                checked((long)Math.Floor(
                    fractionalGameSecond));

            if (wholeGameSeconds <= 0)
            {
                return;
            }

            fractionalGameSecond -=
                wholeGameSeconds;

            int previousDay =
                CurrentTime.DayNumber;

            totalGameSeconds = checked(
                totalGameSeconds + wholeGameSeconds);

            SimulationDateTime currentTime =
                CurrentTime;

            for (int day = previousDay + 1;
                 day <= currentTime.DayNumber;
                 day++)
            {
                DayChanged?.Invoke(
                    SimulationDateTime.FromCalendar(
                        day,
                        0,
                        0));
            }

            TimeChanged?.Invoke(
                currentTime);
        }

        public void SetSpeed(
            SimulationSpeed newSpeed)
        {
            ValidateSpeed(newSpeed);

            if (newSpeed == speed)
            {
                return;
            }

            speed = newSpeed;
            SpeedChanged?.Invoke(speed);
        }

        public SimulationClockState CaptureState()
        {
            return new SimulationClockState(
                totalGameSeconds,
                fractionalGameSecond,
                speed);
        }

        public void RestoreState(
            SimulationClockState state)
        {
            ValidateState(state);

            int previousDay =
                CurrentTime.DayNumber;
            SimulationSpeed previousSpeed =
                speed;

            totalGameSeconds =
                state.TotalGameSeconds;
            fractionalGameSecond =
                state.FractionalGameSecond;
            speed = state.Speed;

            SimulationDateTime restoredTime =
                CurrentTime;

            if (restoredTime.DayNumber != previousDay)
            {
                DayChanged?.Invoke(restoredTime);
            }

            TimeChanged?.Invoke(restoredTime);

            if (speed != previousSpeed)
            {
                SpeedChanged?.Invoke(speed);
            }
        }


        private static void ValidateState(
            SimulationClockState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(
                    nameof(state));
            }

            if (state.TotalGameSeconds < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(state),
                    "Saved simulation time cannot be negative.");
            }

            if (double.IsNaN(state.FractionalGameSecond)
                || double.IsInfinity(state.FractionalGameSecond)
                || state.FractionalGameSecond < 0d
                || state.FractionalGameSecond >= 1d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(state),
                    "Saved fractional time must be between zero and one.");
            }

            ValidateSpeed(state.Speed);
        }

        private static void ValidateSpeed(
            SimulationSpeed candidate)
        {
            if (candidate != SimulationSpeed.Paused
                && candidate != SimulationSpeed.OneTimes
                && candidate != SimulationSpeed.TwoTimes
                && candidate != SimulationSpeed.FourTimes)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(candidate),
                    "Unsupported simulation speed.");
            }
        }
    }
}
