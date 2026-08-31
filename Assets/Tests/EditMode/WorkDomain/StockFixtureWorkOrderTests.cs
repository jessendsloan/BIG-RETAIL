using System;
using BigRetail.Map.Fixtures;
using BigRetail.Merchandise.Domain;
using NUnit.Framework;

namespace BigRetail.Work.Domain.Tests
{
    public sealed class StockFixtureWorkOrderTests
    {
        private static readonly FixtureInstanceId TargetFixtureId =
            new FixtureInstanceId("CHIP-FIXTURE");

        private static readonly FixtureInstanceId RackFixtureId =
            new FixtureInstanceId("CHIP-RACK");

        private static readonly ProductId ChipProductId =
            new ProductId("RIDGEWAY-ORIGINAL");


        [Test]
        public void TwoCaseStockingRun_TracksPhysicalWorkUntilComplete()
        {
            StockFixtureWorkOrder work = CreateWork();

            PickUpCase(work, unitCount: 12);
            work.RecordUnitsStocked(12);

            Assert.That(work.CarriedUnitCount, Is.Zero);
            Assert.That(work.StockedUnitCount, Is.EqualTo(12));

            PickUpCase(work, unitCount: 12);
            work.RecordUnitsStocked(3);
            work.BeginReturn();

            Assert.That(
                work.Phase,
                Is.EqualTo(StockFixtureWorkPhase.ReturningRemainder));
            Assert.That(work.CarriedUnitCount, Is.EqualTo(9));

            work.RecordRemainderReturned();
            work.Complete();

            Assert.That(work.IsTerminal, Is.True);
            Assert.That(
                work.Phase,
                Is.EqualTo(StockFixtureWorkPhase.Completed));
            Assert.That(work.StockedUnitCount, Is.EqualTo(15));
            Assert.That(work.CarriedUnitCount, Is.Zero);
        }

        [Test]
        public void Complete_WhileCarryingCase_IsRejected()
        {
            StockFixtureWorkOrder work = CreateWork();
            PickUpCase(work, unitCount: 12);

            Assert.Throws<InvalidOperationException>(work.Complete);
        }

        [Test]
        public void Block_PreservesReasonAndStopsFurtherTransitions()
        {
            StockFixtureWorkOrder work = CreateWork();

            work.Block("No route to storage");

            Assert.That(work.IsTerminal, Is.True);
            Assert.That(
                work.Phase,
                Is.EqualTo(StockFixtureWorkPhase.Blocked));
            Assert.That(work.StatusMessage, Is.EqualTo("No route to storage"));
            Assert.Throws<InvalidOperationException>(
                () => work.BeginBackstockTrip(RackFixtureId));
        }


        private static StockFixtureWorkOrder CreateWork()
        {
            return new StockFixtureWorkOrder(
                TargetFixtureId,
                ChipProductId);
        }

        private static void PickUpCase(
            StockFixtureWorkOrder work,
            int unitCount)
        {
            work.BeginBackstockTrip(RackFixtureId);
            work.BeginPickup();
            work.RecordCasePickedUp(unitCount);
            work.BeginStocking();
        }
    }
}
