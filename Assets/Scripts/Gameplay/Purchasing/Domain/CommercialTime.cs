using System;

namespace BigRetail.Purchasing.Domain
{
    /// <summary>
    /// A deterministic point on the continuous campaign calendar. Day zero is
    /// Monday; the campaign clock can translate into this value at integration.
    /// </summary>
    public readonly struct CommercialTime :
        IEquatable<CommercialTime>,
        IComparable<CommercialTime>
    {
        public const int MinutesPerHour = 60;
        public const int MinutesPerDay = 24 * MinutesPerHour;

        private readonly long totalMinutes;


        public long TotalMinutes =>
            totalMinutes;

        public int DayIndex =>
            checked((int)(totalMinutes / MinutesPerDay));

        public int MinuteOfDay =>
            checked((int)(totalMinutes % MinutesPerDay));

        public int Hour =>
            MinuteOfDay / MinutesPerHour;

        public int Minute =>
            MinuteOfDay % MinutesPerHour;

        public SupplierWeekday Weekday =>
            GetWeekdayForDay(DayIndex);


        public CommercialTime(int dayIndex, int hour, int minute)
        {
            if (dayIndex < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(dayIndex),
                    dayIndex,
                    "A commercial day cannot be negative.");
            }

            if (hour < 0 || hour >= 24)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(hour),
                    hour,
                    "An hour must be between 0 and 23.");
            }

            if (minute < 0 || minute >= MinutesPerHour)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(minute),
                    minute,
                    "A minute must be between 0 and 59.");
            }

            totalMinutes = checked(
                ((long)dayIndex * MinutesPerDay)
                + (hour * MinutesPerHour)
                + minute);
        }

        private CommercialTime(long totalMinutes)
        {
            if (totalMinutes < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(totalMinutes),
                    totalMinutes,
                    "Commercial time cannot precede campaign day zero.");
            }

            this.totalMinutes = totalMinutes;
        }


        public CommercialTime AddHours(int hours)
        {
            return new CommercialTime(
                checked(totalMinutes + ((long)hours * MinutesPerHour)));
        }

        public CommercialTime AddDays(int days)
        {
            return new CommercialTime(
                checked(totalMinutes + ((long)days * MinutesPerDay)));
        }

        public int CompareTo(CommercialTime other)
        {
            return totalMinutes.CompareTo(other.totalMinutes);
        }

        public bool Equals(CommercialTime other)
        {
            return totalMinutes == other.totalMinutes;
        }

        public override bool Equals(object obj)
        {
            return obj is CommercialTime other && Equals(other);
        }

        public override int GetHashCode()
        {
            return totalMinutes.GetHashCode();
        }

        public static SupplierWeekday GetWeekdayForDay(int dayIndex)
        {
            if (dayIndex < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(dayIndex),
                    dayIndex,
                    "A commercial day cannot be negative.");
            }

            return (SupplierWeekday)(1 << (dayIndex % 7));
        }

        public static bool operator ==(
            CommercialTime left,
            CommercialTime right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            CommercialTime left,
            CommercialTime right)
        {
            return !left.Equals(right);
        }
    }
}
