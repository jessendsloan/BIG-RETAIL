using System;

namespace BigRetail.Simulation.Time.Domain
{
    public enum SimulationDayOfWeek
    {
        Monday = 0,
        Tuesday = 1,
        Wednesday = 2,
        Thursday = 3,
        Friday = 4,
        Saturday = 5,
        Sunday = 6
    }


    /// <summary>
    /// Read-only calendar projection of the clock's elapsed whole seconds.
    /// Day one is always a Monday, giving every system a stable epoch.
    /// </summary>
    public readonly struct SimulationDateTime : IEquatable<SimulationDateTime>
    {
        public const int SecondsPerMinute = 60;
        public const int SecondsPerHour = 60 * SecondsPerMinute;
        public const int SecondsPerDay = 24 * SecondsPerHour;

        public long TotalGameSeconds { get; }

        public int DayNumber { get; }

        public int WeekNumber { get; }

        public SimulationDayOfWeek DayOfWeek { get; }

        public int Hour { get; }

        public int Minute { get; }

        public int Second { get; }


        private SimulationDateTime(
            long totalGameSeconds)
        {
            if (totalGameSeconds < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(totalGameSeconds),
                    "Simulation time cannot precede day one.");
            }

            TotalGameSeconds = totalGameSeconds;

            long elapsedDays =
                totalGameSeconds / SecondsPerDay;

            long secondsOfDay =
                totalGameSeconds % SecondsPerDay;

            DayNumber = checked((int)(elapsedDays + 1));
            WeekNumber = checked((int)(elapsedDays / 7 + 1));
            DayOfWeek =
                (SimulationDayOfWeek)(elapsedDays % 7);
            Hour =
                (int)(secondsOfDay / SecondsPerHour);
            Minute =
                (int)(secondsOfDay % SecondsPerHour)
                / SecondsPerMinute;
            Second =
                (int)(secondsOfDay % SecondsPerMinute);
        }


        public static SimulationDateTime FromTotalGameSeconds(
            long totalGameSeconds)
        {
            return new SimulationDateTime(
                totalGameSeconds);
        }

        public static SimulationDateTime FromCalendar(
            int dayNumber,
            int hour,
            int minute,
            int second = 0)
        {
            if (dayNumber < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(dayNumber),
                    "The first simulation day is day one.");
            }

            if (hour < 0 || hour >= 24)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(hour));
            }

            if (minute < 0 || minute >= 60)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(minute));
            }

            if (second < 0 || second >= 60)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(second));
            }

            long totalGameSeconds = checked(
                ((long)dayNumber - 1) * SecondsPerDay
                + hour * SecondsPerHour
                + minute * SecondsPerMinute
                + second);

            return FromTotalGameSeconds(
                totalGameSeconds);
        }

        public bool Equals(
            SimulationDateTime other)
        {
            return TotalGameSeconds ==
                other.TotalGameSeconds;
        }

        public override bool Equals(
            object obj)
        {
            return obj is SimulationDateTime other
                && Equals(other);
        }

        public override int GetHashCode()
        {
            return TotalGameSeconds.GetHashCode();
        }

        public static bool operator ==(
            SimulationDateTime left,
            SimulationDateTime right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            SimulationDateTime left,
            SimulationDateTime right)
        {
            return !left.Equals(right);
        }
    }
}
