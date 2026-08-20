using System;
using System.Collections.Generic;

namespace BigRetail.Purchasing.Domain
{
    public enum SupplierDeliveryKind
    {
        SameDay = 0,
        NextDay = 1,
        WeeklyRoute = 2
    }


    [Flags]
    public enum SupplierWeekday
    {
        None = 0,
        Monday = 1 << 0,
        Tuesday = 1 << 1,
        Wednesday = 1 << 2,
        Thursday = 1 << 3,
        Friday = 1 << 4,
        Saturday = 1 << 5,
        Sunday = 1 << 6
    }


    /// <summary>
    /// Supplier-wide opening delivery promise. It calculates a deterministic
    /// estimate from a campaign-provided commercial time.
    /// </summary>
    public sealed class SupplierDeliveryRule
    {
        public SupplierDeliveryKind Kind { get; }

        public int SameDayLeadHours { get; }

        public SupplierWeekday RouteDays { get; }


        private SupplierDeliveryRule(
            SupplierDeliveryKind kind,
            int sameDayLeadHours,
            SupplierWeekday routeDays)
        {
            Kind = kind;
            SameDayLeadHours = sameDayLeadHours;
            RouteDays = routeDays;
        }


        public static SupplierDeliveryRule SameDay(int leadHours)
        {
            if (leadHours <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(leadHours),
                    leadHours,
                    "Same-day delivery requires a positive lead time.");
            }

            return new SupplierDeliveryRule(
                SupplierDeliveryKind.SameDay,
                leadHours,
                SupplierWeekday.None);
        }

        public static SupplierDeliveryRule NextDay()
        {
            return new SupplierDeliveryRule(
                SupplierDeliveryKind.NextDay,
                0,
                SupplierWeekday.None);
        }

        public static SupplierDeliveryRule WeeklyRoute(
            SupplierWeekday routeDays)
        {
            const SupplierWeekday supportedDays =
                SupplierWeekday.Monday
                | SupplierWeekday.Tuesday
                | SupplierWeekday.Wednesday
                | SupplierWeekday.Thursday
                | SupplierWeekday.Friday
                | SupplierWeekday.Saturday
                | SupplierWeekday.Sunday;

            if (routeDays == SupplierWeekday.None
                || (routeDays & ~supportedDays) != SupplierWeekday.None)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(routeDays),
                    routeDays,
                    "A weekly route requires at least one supported weekday.");
            }

            return new SupplierDeliveryRule(
                SupplierDeliveryKind.WeeklyRoute,
                0,
                routeDays);
        }


        public string GetPlayerFacingSummary()
        {
            switch (Kind)
            {
                case SupplierDeliveryKind.SameDay:
                    return SameDayLeadHours == 1
                        ? "Within 1 hour"
                        : $"Within {SameDayLeadHours} hours";

                case SupplierDeliveryKind.NextDay:
                    return "Next day";

                case SupplierDeliveryKind.WeeklyRoute:
                    return $"{FormatRouteDays()} route";

                default:
                    throw new InvalidOperationException(
                        $"Delivery kind '{Kind}' is not supported.");
            }
        }

        public SupplierDeliveryEstimate EstimateDelivery(
            CommercialTime orderedAt)
        {
            switch (Kind)
            {
                case SupplierDeliveryKind.SameDay:
                    return SupplierDeliveryEstimate.Exact(
                        orderedAt.AddHours(SameDayLeadHours));

                case SupplierDeliveryKind.NextDay:
                    return SupplierDeliveryEstimate.Exact(
                        orderedAt.AddDays(1));

                case SupplierDeliveryKind.WeeklyRoute:
                    // A route-day order is conservatively assigned to the next
                    // route until an explicit cutoff is authored.
                    for (int dayOffset = 1; dayOffset <= 7; dayOffset++)
                    {
                        int candidateDay = orderedAt.DayIndex + dayOffset;
                        SupplierWeekday candidateWeekday =
                            CommercialTime.GetWeekdayForDay(candidateDay);

                        if ((RouteDays & candidateWeekday)
                            != SupplierWeekday.None)
                        {
                            return SupplierDeliveryEstimate.RouteDay(
                                candidateDay);
                        }
                    }

                    throw new InvalidOperationException(
                        "A weekly route has no reachable delivery day.");

                default:
                    throw new InvalidOperationException(
                        $"Delivery kind '{Kind}' is not supported.");
            }
        }


        private string FormatRouteDays()
        {
            List<string> names = new List<string>();
            AddDayName(names, SupplierWeekday.Monday, "Mon");
            AddDayName(names, SupplierWeekday.Tuesday, "Tue");
            AddDayName(names, SupplierWeekday.Wednesday, "Wed");
            AddDayName(names, SupplierWeekday.Thursday, "Thu");
            AddDayName(names, SupplierWeekday.Friday, "Fri");
            AddDayName(names, SupplierWeekday.Saturday, "Sat");
            AddDayName(names, SupplierWeekday.Sunday, "Sun");
            return string.Join(" / ", names);
        }

        private void AddDayName(
            ICollection<string> names,
            SupplierWeekday day,
            string displayName)
        {
            if ((RouteDays & day) != SupplierWeekday.None)
            {
                names.Add(displayName);
            }
        }
    }
}
