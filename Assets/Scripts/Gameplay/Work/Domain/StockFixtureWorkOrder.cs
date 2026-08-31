using System;
using BigRetail.Map.Fixtures;
using BigRetail.Merchandise.Domain;

namespace BigRetail.Work.Domain
{
    public enum StockFixtureWorkPhase
    {
        Queued = 0,
        TravelingToBackstock = 1,
        PickingUpCase = 2,
        TravelingToFixture = 3,
        StockingFixture = 4,
        ReturningRemainder = 5,
        Completed = 6,
        Blocked = 7,
        Cancelled = 8
    }


    /// <summary>
    /// Engine-free state for one employee-compatible fixture stocking job.
    /// The Unity runner performs movement and inventory transactions, then
    /// records those completed physical beats here.
    /// </summary>
    public sealed class StockFixtureWorkOrder
    {
        public FixtureInstanceId TargetFixtureId { get; }

        public ProductId ProductId { get; }

        public FixtureInstanceId SourceRackId { get; private set; }

        public StockFixtureWorkPhase Phase { get; private set; }

        public int CarriedUnitCount { get; private set; }

        public int StockedUnitCount { get; private set; }

        public string StatusMessage { get; private set; }

        public bool IsTerminal =>
            Phase == StockFixtureWorkPhase.Completed
            || Phase == StockFixtureWorkPhase.Blocked
            || Phase == StockFixtureWorkPhase.Cancelled;


        public StockFixtureWorkOrder(
            FixtureInstanceId targetFixtureId,
            ProductId productId)
        {
            if (!targetFixtureId.IsValid)
            {
                throw new ArgumentException(
                    "Stocking work requires a valid target fixture.",
                    nameof(targetFixtureId));
            }

            if (!productId.IsValid)
            {
                throw new ArgumentException(
                    "Stocking work requires a valid product.",
                    nameof(productId));
            }

            TargetFixtureId = targetFixtureId;
            ProductId = productId;
            Phase = StockFixtureWorkPhase.Queued;
            StatusMessage = "Queued";
        }


        public void BeginBackstockTrip(FixtureInstanceId sourceRackId)
        {
            RequirePhase(
                StockFixtureWorkPhase.Queued,
                StockFixtureWorkPhase.ReturningRemainder,
                StockFixtureWorkPhase.StockingFixture);

            if (!sourceRackId.IsValid)
            {
                throw new ArgumentException(
                    "A stock trip requires a valid source rack.",
                    nameof(sourceRackId));
            }

            if (CarriedUnitCount != 0)
            {
                throw new InvalidOperationException(
                    "A worker cannot collect another case while carrying stock.");
            }

            SourceRackId = sourceRackId;
            Phase = StockFixtureWorkPhase.TravelingToBackstock;
            StatusMessage = "Founder is walking to storage";
        }


        public void BeginPickup()
        {
            RequirePhase(StockFixtureWorkPhase.TravelingToBackstock);
            Phase = StockFixtureWorkPhase.PickingUpCase;
            StatusMessage = "Founder is picking up a case";
        }


        public void RecordCasePickedUp(int unitCount)
        {
            RequirePhase(StockFixtureWorkPhase.PickingUpCase);

            if (unitCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(unitCount));
            }

            CarriedUnitCount = unitCount;
            Phase = StockFixtureWorkPhase.TravelingToFixture;
            StatusMessage = $"Founder is carrying {unitCount} items";
        }


        public void BeginStocking()
        {
            RequirePhase(StockFixtureWorkPhase.TravelingToFixture);

            if (CarriedUnitCount <= 0)
            {
                throw new InvalidOperationException(
                    "A worker cannot stock an empty case.");
            }

            Phase = StockFixtureWorkPhase.StockingFixture;
            StatusMessage = "Founder is stocking the fixture";
        }


        public void RecordUnitsStocked(int unitCount)
        {
            RequirePhase(StockFixtureWorkPhase.StockingFixture);

            if (unitCount <= 0 || unitCount > CarriedUnitCount)
            {
                throw new ArgumentOutOfRangeException(nameof(unitCount));
            }

            CarriedUnitCount -= unitCount;
            StockedUnitCount = checked(StockedUnitCount + unitCount);
            StatusMessage =
                $"Founder stocked {StockedUnitCount} item"
                + (StockedUnitCount == 1 ? string.Empty : "s");
        }


        public void BeginReturn()
        {
            RequirePhase(
                StockFixtureWorkPhase.StockingFixture,
                StockFixtureWorkPhase.TravelingToFixture);

            if (CarriedUnitCount <= 0)
            {
                throw new InvalidOperationException(
                    "There is no case remainder to return.");
            }

            Phase = StockFixtureWorkPhase.ReturningRemainder;
            StatusMessage = "Founder is returning the open case";
        }


        public void RecordRemainderReturned()
        {
            RequirePhase(StockFixtureWorkPhase.ReturningRemainder);
            CarriedUnitCount = 0;
            Phase = StockFixtureWorkPhase.StockingFixture;
            StatusMessage = "Open case returned to storage";
        }


        public void Complete()
        {
            if (IsTerminal)
            {
                return;
            }

            if (CarriedUnitCount != 0)
            {
                throw new InvalidOperationException(
                    "Stocking work cannot complete while stock is carried.");
            }

            Phase = StockFixtureWorkPhase.Completed;
            StatusMessage =
                $"Founder finished stocking {StockedUnitCount} item"
                + (StockedUnitCount == 1 ? string.Empty : "s");
        }


        public void Block(string reason)
        {
            if (IsTerminal)
            {
                return;
            }

            Phase = StockFixtureWorkPhase.Blocked;
            StatusMessage = string.IsNullOrWhiteSpace(reason)
                ? "Stocking work is blocked"
                : reason.Trim();
        }


        public void Cancel(string reason = null)
        {
            if (IsTerminal)
            {
                return;
            }

            Phase = StockFixtureWorkPhase.Cancelled;
            StatusMessage = string.IsNullOrWhiteSpace(reason)
                ? "Stocking work cancelled"
                : reason.Trim();
        }


        private void RequirePhase(
            StockFixtureWorkPhase required,
            params StockFixtureWorkPhase[] alternatives)
        {
            if (Phase == required)
            {
                return;
            }

            for (int index = 0;
                 index < alternatives.Length;
                 index++)
            {
                if (Phase == alternatives[index])
                {
                    return;
                }
            }

            throw new InvalidOperationException(
                $"Stocking work cannot perform that transition from {Phase}.");
        }
    }
}
