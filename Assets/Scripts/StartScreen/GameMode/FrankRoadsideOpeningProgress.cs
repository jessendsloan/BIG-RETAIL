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
    }

    public enum FrankRoadsideOpeningBeat
    {
        WakeUp = 0,
        CoverTheStore = 1,
        MoveReceivingToStockroom = 2,
        Complete = 3
    }
}
