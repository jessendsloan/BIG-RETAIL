using System;
using BigRetail.Map.Fixtures;
using BigRetail.Merchandise.Domain;
using NUnit.Framework;

namespace BigRetail.Work.Domain.Tests
{
    public sealed class PutAwayDeliveryWorkOrderTests
    {
        private static readonly ProductId ChipProductId =
            new ProductId("RIDGEWAY-ORIGINAL");

        private static readonly FixtureInstanceId RackFixtureId =
            new FixtureInstanceId("CHIP-RACK");


        [Test]
        public void FourCaseRun_TracksEveryPhysicalTripUntilComplete()
        {
            PutAwayDeliveryWorkOrder work =
                new PutAwayDeliveryWorkOrder(orderNumber: 42);

            for (int index = 0; index < 4; index++)
            {
                PutAwayOneCase(work, unitCount: 12);
            }

            work.Complete();

            Assert.That(work.IsTerminal, Is.True);
            Assert.That(
                work.Phase,
                Is.EqualTo(PutAwayDeliveryWorkPhase.Completed));
            Assert.That(work.PlacedCaseCount, Is.EqualTo(4));
            Assert.That(work.PlacedUnitCount, Is.EqualTo(48));
            Assert.That(work.CarriedUnitCount, Is.Zero);
        }


        [Test]
        public void Complete_WhileCaseIsAssigned_IsRejected()
        {
            PutAwayDeliveryWorkOrder work =
                new PutAwayDeliveryWorkOrder(orderNumber: 42);
            work.BeginCaseTrip(
                ChipProductId,
                unitCount: 12,
                RackFixtureId);

            Assert.Throws<InvalidOperationException>(work.Complete);
        }


        [Test]
        public void Block_ReleasesCarriedCaseAndPreservesReason()
        {
            PutAwayDeliveryWorkOrder work =
                new PutAwayDeliveryWorkOrder(orderNumber: 42);
            work.BeginCaseTrip(
                ChipProductId,
                unitCount: 12,
                RackFixtureId);

            work.Block("No route to Receiving");

            Assert.That(work.IsTerminal, Is.True);
            Assert.That(work.PendingUnitCount, Is.Zero);
            Assert.That(work.CarriedUnitCount, Is.Zero);
            Assert.That(
                work.StatusMessage,
                Is.EqualTo("No route to Receiving"));
        }


        private static void PutAwayOneCase(
            PutAwayDeliveryWorkOrder work,
            int unitCount)
        {
            work.BeginCaseTrip(
                ChipProductId,
                unitCount,
                RackFixtureId);
            work.BeginPickup();
            work.RecordCasePickedUp();
            work.BeginPlacement();
            work.RecordCasePlaced(unitCount);
        }
    }
}
