using System;
using BigRetail.Map.Fixtures;
using BigRetail.Merchandise.Domain;

namespace BigRetail.Work.Domain
{
    public enum PutAwayDeliveryWorkPhase
    {
        Queued = 0,
        TravelingToReceiving = 1,
        PickingUpCase = 2,
        TravelingToRack = 3,
        PlacingCase = 4,
        Completed = 5,
        Blocked = 6,
        Cancelled = 7
    }


    /// <summary>
    /// Engine-free state for one employee-compatible Receiving put-away job.
    /// A runner performs movement and commits each supplier case only at the
    /// visible rack-placement beat.
    /// </summary>
    public sealed class PutAwayDeliveryWorkOrder
    {
        public long OrderNumber { get; }

        public ProductId ProductId { get; private set; }

        public FixtureInstanceId TargetRackId { get; private set; }

        public PutAwayDeliveryWorkPhase Phase { get; private set; }

        public int PendingUnitCount { get; private set; }

        public int CarriedUnitCount { get; private set; }

        public int PlacedCaseCount { get; private set; }

        public int PlacedUnitCount { get; private set; }

        public string StatusMessage { get; private set; }

        public bool IsTerminal =>
            Phase == PutAwayDeliveryWorkPhase.Completed
            || Phase == PutAwayDeliveryWorkPhase.Blocked
            || Phase == PutAwayDeliveryWorkPhase.Cancelled;


        public PutAwayDeliveryWorkOrder(long orderNumber)
        {
            if (orderNumber <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(orderNumber));
            }

            OrderNumber = orderNumber;
            Phase = PutAwayDeliveryWorkPhase.Queued;
            StatusMessage = "Receiving put-away queued";
        }


        public void BeginCaseTrip(
            ProductId productId,
            int unitCount,
            FixtureInstanceId targetRackId)
        {
            RequirePhase(PutAwayDeliveryWorkPhase.Queued);

            if (!productId.IsValid)
            {
                throw new ArgumentException(
                    "Put-away work requires a valid product.",
                    nameof(productId));
            }

            if (unitCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(unitCount));
            }

            if (!targetRackId.IsValid)
            {
                throw new ArgumentException(
                    "Put-away work requires a valid destination rack.",
                    nameof(targetRackId));
            }

            if (PendingUnitCount != 0
                || CarriedUnitCount != 0)
            {
                throw new InvalidOperationException(
                    "A worker cannot begin another trip while carrying stock.");
            }

            ProductId = productId;
            TargetRackId = targetRackId;
            PendingUnitCount = unitCount;
            Phase = PutAwayDeliveryWorkPhase.TravelingToReceiving;
            StatusMessage = "Founder is walking to Receiving";
        }


        public void BeginPickup()
        {
            RequirePhase(PutAwayDeliveryWorkPhase.TravelingToReceiving);
            Phase = PutAwayDeliveryWorkPhase.PickingUpCase;
            StatusMessage = "Founder is taking a supplier case";
        }


        public void RecordCasePickedUp()
        {
            RequirePhase(PutAwayDeliveryWorkPhase.PickingUpCase);

            if (PendingUnitCount <= 0
                || CarriedUnitCount != 0)
            {
                throw new InvalidOperationException(
                    "A put-away trip has no supplier case to carry.");
            }

            CarriedUnitCount = PendingUnitCount;
            PendingUnitCount = 0;
            Phase = PutAwayDeliveryWorkPhase.TravelingToRack;
            StatusMessage =
                $"Founder is carrying a {CarriedUnitCount}-item case";
        }


        public void BeginPlacement()
        {
            RequirePhase(PutAwayDeliveryWorkPhase.TravelingToRack);
            Phase = PutAwayDeliveryWorkPhase.PlacingCase;
            StatusMessage = "Founder is placing the case on a storage rack";
        }


        public void RecordCasePlaced(int receivedUnitCount)
        {
            RequirePhase(PutAwayDeliveryWorkPhase.PlacingCase);

            if (receivedUnitCount <= 0
                || receivedUnitCount != CarriedUnitCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(receivedUnitCount));
            }

            PlacedCaseCount = checked(PlacedCaseCount + 1);
            PlacedUnitCount = checked(
                PlacedUnitCount + receivedUnitCount);
            CarriedUnitCount = 0;
            Phase = PutAwayDeliveryWorkPhase.Queued;
            StatusMessage =
                $"Founder stored {PlacedCaseCount} case"
                + (PlacedCaseCount == 1 ? string.Empty : "s");
        }


        public void Complete()
        {
            if (IsTerminal)
            {
                return;
            }

            if (PendingUnitCount != 0
                || CarriedUnitCount != 0)
            {
                throw new InvalidOperationException(
                    "Put-away work cannot complete while stock is carried.");
            }

            Phase = PutAwayDeliveryWorkPhase.Completed;
            StatusMessage =
                $"Founder finished putting away {PlacedCaseCount} case"
                + (PlacedCaseCount == 1 ? string.Empty : "s");
        }


        public void Block(string reason)
        {
            if (IsTerminal)
            {
                return;
            }

            PendingUnitCount = 0;
            CarriedUnitCount = 0;
            Phase = PutAwayDeliveryWorkPhase.Blocked;
            StatusMessage = string.IsNullOrWhiteSpace(reason)
                ? "Receiving put-away is blocked"
                : reason.Trim();
        }


        public void Cancel(string reason = null)
        {
            if (IsTerminal)
            {
                return;
            }

            PendingUnitCount = 0;
            CarriedUnitCount = 0;
            Phase = PutAwayDeliveryWorkPhase.Cancelled;
            StatusMessage = string.IsNullOrWhiteSpace(reason)
                ? "Receiving put-away cancelled"
                : reason.Trim();
        }


        private void RequirePhase(PutAwayDeliveryWorkPhase required)
        {
            if (Phase != required)
            {
                throw new InvalidOperationException(
                    $"Put-away work cannot perform that transition from {Phase}.");
            }
        }
    }
}
