using BigRetail.Map.Domain;

namespace BigRetail.Receiving.Domain
{
    public readonly struct ReceivingAreaChangeResult
    {
        private ReceivingAreaChangeResult(
            bool succeeded,
            int changedCellCount,
            ReceivingAreaChangeFailure failure,
            GridPosition failedCell)
        {
            Succeeded = succeeded;
            ChangedCellCount = changedCellCount;
            Failure = failure;
            FailedCell = failedCell;
        }


        public bool Succeeded { get; }

        public int ChangedCellCount { get; }

        public ReceivingAreaChangeFailure Failure { get; }

        public GridPosition FailedCell { get; }


        public static ReceivingAreaChangeResult Success(
            int changedCellCount)
        {
            return new ReceivingAreaChangeResult(
                true,
                changedCellCount,
                ReceivingAreaChangeFailure.None,
                default);
        }

        public static ReceivingAreaChangeResult Rejected(
            ReceivingAreaChangeFailure failure,
            GridPosition failedCell = default)
        {
            return new ReceivingAreaChangeResult(
                false,
                0,
                failure,
                failedCell);
        }
    }
}
