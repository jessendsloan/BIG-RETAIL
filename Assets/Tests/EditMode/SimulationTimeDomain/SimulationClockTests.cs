using System;
using System.Collections.Generic;
using BigRetail.Simulation.Time.Domain;
using NUnit.Framework;

namespace BigRetail.Simulation.Time.Domain.Tests
{
    public sealed class SimulationClockTests
    {
        [Test]
        public void Constructor_ProjectsStableStartingCalendar()
        {
            SimulationClock clock =
                CreateClock(
                    day: 1,
                    hour: 8,
                    minute: 15);

            Assert.That(clock.CurrentTime.DayNumber, Is.EqualTo(1));
            Assert.That(clock.CurrentTime.DayOfWeek, Is.EqualTo(SimulationDayOfWeek.Monday));
            Assert.That(clock.CurrentTime.WeekNumber, Is.EqualTo(1));
            Assert.That(clock.CurrentTime.Hour, Is.EqualTo(8));
            Assert.That(clock.CurrentTime.Minute, Is.EqualTo(15));
        }

        [Test]
        public void Advance_UsesConfiguredGameTimeRate()
        {
            SimulationClock clock =
                CreateClock();

            clock.Advance(2d);

            Assert.That(clock.CurrentTime.Hour, Is.EqualTo(8));
            Assert.That(clock.CurrentTime.Minute, Is.EqualTo(2));
        }

        [Test]
        public void PausedClock_DoesNotAdvance()
        {
            SimulationClock clock =
                CreateClock();

            clock.SetSpeed(SimulationSpeed.Paused);
            clock.Advance(30d);

            Assert.That(clock.CurrentTime.Hour, Is.EqualTo(8));
            Assert.That(clock.CurrentTime.Minute, Is.Zero);
        }

        [TestCase(SimulationSpeed.TwoTimes, 2)]
        [TestCase(SimulationSpeed.FourTimes, 4)]
        public void FasterSpeeds_ApplyExactMultiplier(
            SimulationSpeed speed,
            int expectedMinutes)
        {
            SimulationClock clock =
                CreateClock();

            clock.SetSpeed(speed);
            clock.Advance(1d);

            Assert.That(
                clock.CurrentTime.Minute,
                Is.EqualTo(expectedMinutes));
        }

        [Test]
        public void CrossingMidnight_RaisesDayBoundaryAndChangesWeekday()
        {
            SimulationClock clock =
                CreateClock(
                    day: 1,
                    hour: 23,
                    minute: 59);

            List<SimulationDateTime> dayChanges =
                new List<SimulationDateTime>();

            clock.DayChanged += dayChanges.Add;
            clock.Advance(1d);

            Assert.That(clock.CurrentTime.DayNumber, Is.EqualTo(2));
            Assert.That(clock.CurrentTime.DayOfWeek, Is.EqualTo(SimulationDayOfWeek.Tuesday));
            Assert.That(clock.CurrentTime.Hour, Is.Zero);
            Assert.That(dayChanges, Has.Count.EqualTo(1));
            Assert.That(dayChanges[0].DayNumber, Is.EqualTo(2));
        }

        [Test]
        public void LargeAdvance_RaisesEveryCrossedDayBoundary()
        {
            SimulationClock clock =
                new SimulationClock(
                    1,
                    0,
                    0,
                    SimulationSpeed.OneTimes,
                    SimulationDateTime.SecondsPerDay);

            List<SimulationDateTime> dayChanges =
                new List<SimulationDateTime>();

            clock.DayChanged += dayChanges.Add;
            clock.Advance(3d);

            Assert.That(clock.CurrentTime.DayNumber, Is.EqualTo(4));
            Assert.That(dayChanges, Has.Count.EqualTo(3));
            Assert.That(dayChanges[2].DayNumber, Is.EqualTo(4));
        }

        [Test]
        public void CaptureAndRestore_PreservesFractionAndSpeed()
        {
            SimulationClock source =
                new SimulationClock(
                    1,
                    8,
                    0,
                    SimulationSpeed.TwoTimes,
                    1d);

            source.Advance(0.75d);
            SimulationClockState snapshot =
                source.CaptureState();

            SimulationClock restored =
                CreateClock();

            restored.RestoreState(snapshot);
            restored.Advance(0.25d);

            Assert.That(restored.Speed, Is.EqualTo(SimulationSpeed.TwoTimes));
            Assert.That(restored.CurrentTime.Second, Is.EqualTo(2));
            Assert.That(
                restored.CaptureState().FractionalGameSecond,
                Is.EqualTo(0d).Within(0.000001d));
        }

        [Test]
        public void Restore_InvalidSnapshotIsRejected()
        {
            SimulationClock clock =
                CreateClock();

            SimulationClockState invalid =
                new SimulationClockState(
                    -1,
                    0d,
                    SimulationSpeed.OneTimes);

            Assert.Throws<ArgumentOutOfRangeException>(
                () => clock.RestoreState(invalid));
        }

        [Test]
        public void Calendar_RepeatsMondayAtStartOfSecondWeek()
        {
            SimulationDateTime dateTime =
                SimulationDateTime.FromCalendar(
                    8,
                    12,
                    0);

            Assert.That(dateTime.WeekNumber, Is.EqualTo(2));
            Assert.That(dateTime.DayOfWeek, Is.EqualTo(SimulationDayOfWeek.Monday));
        }


        private static SimulationClock CreateClock(
            int day = 1,
            int hour = 8,
            int minute = 0)
        {
            return new SimulationClock(
                day,
                hour,
                minute,
                SimulationSpeed.OneTimes,
                SimulationDateTime.SecondsPerMinute);
        }
    }
}
