namespace BigRetail.Core.Session
{
    /// <summary>
    /// Session-owned progress for the short wake-up that opens Frank's
    /// Roadside. Later prologue beats remain separate from this first moment.
    /// </summary>
    public sealed class FrankRoadsideOpeningProgress
    {
        public FrankRoadsideOpeningBeat CurrentBeat { get; private set; } =
            FrankRoadsideOpeningBeat.WakeUp;

        public bool IsComplete =>
            CurrentBeat == FrankRoadsideOpeningBeat.Complete;


        public void Advance()
        {
            if (IsComplete)
            {
                return;
            }

            CurrentBeat++;
        }

        public void Skip()
        {
            CurrentBeat = FrankRoadsideOpeningBeat.Complete;
        }


        public static FrankRoadsideOpeningObjective ResolveStockingObjective(
            int backstockUnitCount,
            int displayedUnitCount,
            int requiredUnitCount)
        {
            return ResolveStockingObjective(
                backstockUnitCount,
                displayedUnitCount,
                requiredUnitCount,
                requiredUnitCount);
        }


        public static FrankRoadsideOpeningObjective ResolveStockingObjective(
            int backstockUnitCount,
            int displayedUnitCount,
            int requiredReceivedUnitCount,
            int requiredDisplayedUnitCount)
        {
            if (requiredReceivedUnitCount <= 0)
            {
                throw new System.ArgumentOutOfRangeException(
                    nameof(requiredReceivedUnitCount),
                    requiredReceivedUnitCount,
                    "Frank's stocking objective requires at least one unit.");
            }

            if (requiredDisplayedUnitCount <= 0
                || requiredDisplayedUnitCount
                    > requiredReceivedUnitCount)
            {
                throw new System.ArgumentOutOfRangeException(
                    nameof(requiredDisplayedUnitCount),
                    requiredDisplayedUnitCount,
                    "Frank's display target must fit within received stock.");
            }

            int safeBackstockUnitCount =
                System.Math.Max(0, backstockUnitCount);
            int safeDisplayedUnitCount =
                System.Math.Max(0, displayedUnitCount);

            long receivedUnitCount =
                (long)safeBackstockUnitCount
                + safeDisplayedUnitCount;

            if (safeDisplayedUnitCount >= requiredDisplayedUnitCount
                && receivedUnitCount >= requiredReceivedUnitCount)
            {
                return FrankRoadsideOpeningObjective.Complete;
            }

            return receivedUnitCount >= requiredReceivedUnitCount
                ? FrankRoadsideOpeningObjective.StockSalesFloor
                : FrankRoadsideOpeningObjective.MoveReceivingToStockroom;
        }
    }

    public enum FrankRoadsideOpeningBeat
    {
        WakeUp = 0,
        CoverTheStore = 1,
        MoveReceivingToStockroom = 2,
        Complete = 3
    }

    public enum FrankRoadsideOpeningObjective
    {
        MoveReceivingToStockroom = 0,
        StockSalesFloor = 1,
        Complete = 2
    }
}
