using BigRetail.Map.Domain;

namespace BigRetail.Map.Foundations
{
    /// <summary>
    /// Describes the result of one strict batch foundation mutation.
    /// A rejected result always reports zero changed foundations.
    /// </summary>
    public readonly struct FoundationBatchChangeResult
    {
        public bool Succeeded { get; }

        public int RequestedCount { get; }

        public int ChangedCount { get; }

        public FoundationChangeFailure Failure { get; }

        public GridPosition FailedCell { get; }


        private FoundationBatchChangeResult(
            bool succeeded,
            int requestedCount,
            int changedCount,
            FoundationChangeFailure failure,
            GridPosition failedCell)
        {
            Succeeded = succeeded;
            RequestedCount = requestedCount;
            ChangedCount = changedCount;
            Failure = failure;
            FailedCell = failedCell;
        }


        public static FoundationBatchChangeResult Success(
            int changedCount)
        {
            return new FoundationBatchChangeResult(
                true,
                changedCount,
                changedCount,
                FoundationChangeFailure.None,
                default);
        }


        public static FoundationBatchChangeResult Rejected(
            int requestedCount,
            GridPosition failedCell,
            FoundationChangeFailure failure)
        {
            return new FoundationBatchChangeResult(
                false,
                requestedCount,
                0,
                failure,
                failedCell);
        }


        public override string ToString()
        {
            if (Succeeded)
            {
                return
                    $"Foundation batch succeeded. " +
                    $"Changed {ChangedCount} foundation(s).";
            }

            return
                $"Foundation batch rejected: {Failure}. " +
                $"Failed cell: {FailedCell}. " +
                $"Changed 0 of {RequestedCount} requested foundation(s).";
        }
    }
}
