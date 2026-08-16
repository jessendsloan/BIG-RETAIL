using BigRetail.Map.Domain;

namespace BigRetail.Map.Sidewalks
{
    public readonly struct SidewalkBatchChangeResult
    {
        public bool Succeeded { get; }

        public int RequestedCount { get; }

        public int ChangedCount { get; }

        public SidewalkChangeFailure Failure { get; }

        public GridPosition FailedCell { get; }


        private SidewalkBatchChangeResult(
            bool succeeded,
            int requestedCount,
            int changedCount,
            SidewalkChangeFailure failure,
            GridPosition failedCell)
        {
            Succeeded = succeeded;
            RequestedCount = requestedCount;
            ChangedCount = changedCount;
            Failure = failure;
            FailedCell = failedCell;
        }


        public static SidewalkBatchChangeResult Success(int changedCount)
        {
            return new SidewalkBatchChangeResult(
                true,
                changedCount,
                changedCount,
                SidewalkChangeFailure.None,
                default);
        }


        public static SidewalkBatchChangeResult Rejected(
            int requestedCount,
            GridPosition failedCell,
            SidewalkChangeFailure failure)
        {
            return new SidewalkBatchChangeResult(
                false,
                requestedCount,
                0,
                failure,
                failedCell);
        }
    }
}
