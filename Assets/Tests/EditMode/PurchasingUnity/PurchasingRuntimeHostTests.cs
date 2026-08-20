using BigRetail.Purchasing.Domain;
using BigRetail.Purchasing.Unity;
using BigRetail.Simulation.Time.Domain;
using NUnit.Framework;

namespace BigRetail.Purchasing.Unity.Tests
{
    public sealed class PurchasingRuntimeHostTests
    {
        [Test]
        public void ToCommercialTime_UsesDayOneAsMondayEpoch()
        {
            CommercialTime result =
                PurchasingRuntimeHost.ToCommercialTime(
                    SimulationDateTime.FromCalendar(
                        dayNumber: 3,
                        hour: 14,
                        minute: 25));

            Assert.That(result.DayIndex, Is.EqualTo(2));
            Assert.That(result.Hour, Is.EqualTo(14));
            Assert.That(result.Minute, Is.EqualTo(25));
            Assert.That(result.Weekday, Is.EqualTo(SupplierWeekday.Wednesday));
        }
    }
}
