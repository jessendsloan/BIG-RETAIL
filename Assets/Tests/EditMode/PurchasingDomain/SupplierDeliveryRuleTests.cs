using System;
using NUnit.Framework;

namespace BigRetail.Purchasing.Domain.Tests
{
    public sealed class SupplierDeliveryRuleTests
    {
        [Test]
        public void SameDay_DescribesLeadTime()
        {
            SupplierDeliveryRule rule = SupplierDeliveryRule.SameDay(3);

            Assert.That(rule.Kind, Is.EqualTo(SupplierDeliveryKind.SameDay));
            Assert.That(rule.GetPlayerFacingSummary(), Is.EqualTo("Within 3 hours"));
        }

        [Test]
        public void WeeklyRoute_DescribesSelectedWeekdays()
        {
            SupplierDeliveryRule rule =
                SupplierDeliveryRule.WeeklyRoute(
                    SupplierWeekday.Tuesday | SupplierWeekday.Friday);

            Assert.That(
                rule.GetPlayerFacingSummary(),
                Is.EqualTo("Tue / Fri route"));
        }

        [Test]
        public void WeeklyRoute_RejectsEmptySchedule()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => SupplierDeliveryRule.WeeklyRoute(SupplierWeekday.None));
        }

        [Test]
        public void EstimateDelivery_ResolvesCurrentTemporalRules()
        {
            CommercialTime mondayMorning =
                new CommercialTime(0, 9, 0);

            SupplierDeliveryEstimate sameDay =
                SupplierDeliveryRule.SameDay(3)
                    .EstimateDelivery(mondayMorning);
            SupplierDeliveryEstimate nextDay =
                SupplierDeliveryRule.NextDay()
                    .EstimateDelivery(mondayMorning);
            SupplierDeliveryEstimate route =
                SupplierDeliveryRule.WeeklyRoute(
                        SupplierWeekday.Tuesday | SupplierWeekday.Friday)
                    .EstimateDelivery(mondayMorning);

            Assert.That(sameDay.HasExactArrivalTime, Is.True);
            Assert.That(sameDay.EarliestArrival.DayIndex, Is.Zero);
            Assert.That(sameDay.EarliestArrival.Hour, Is.EqualTo(12));
            Assert.That(nextDay.HasExactArrivalTime, Is.True);
            Assert.That(nextDay.EarliestArrival.DayIndex, Is.EqualTo(1));
            Assert.That(nextDay.EarliestArrival.Hour, Is.EqualTo(9));
            Assert.That(route.HasExactArrivalTime, Is.False);
            Assert.That(route.EarliestArrival.DayIndex, Is.EqualTo(1));
            Assert.That(
                route.EarliestArrival.Weekday,
                Is.EqualTo(SupplierWeekday.Tuesday));
        }

        [Test]
        public void WeeklyRoute_OrderOnRouteDayUsesNextRoute()
        {
            SupplierDeliveryRule rule =
                SupplierDeliveryRule.WeeklyRoute(
                    SupplierWeekday.Tuesday | SupplierWeekday.Friday);

            SupplierDeliveryEstimate tuesdayOrder =
                rule.EstimateDelivery(new CommercialTime(1, 9, 0));
            SupplierDeliveryEstimate fridayOrder =
                rule.EstimateDelivery(new CommercialTime(4, 9, 0));

            Assert.That(
                tuesdayOrder.EarliestArrival.Weekday,
                Is.EqualTo(SupplierWeekday.Friday));
            Assert.That(
                fridayOrder.EarliestArrival.Weekday,
                Is.EqualTo(SupplierWeekday.Tuesday));
            Assert.That(fridayOrder.EarliestArrival.DayIndex, Is.EqualTo(8));
        }
    }
}
