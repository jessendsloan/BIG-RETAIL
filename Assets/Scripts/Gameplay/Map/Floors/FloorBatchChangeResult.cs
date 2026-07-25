using BigRetail.Map.Domain;

namespace BigRetail.Map.Floors
{
    /// <summary>
    /// Describes the result of one strict batch floor mutation.
    ///
    /// A rejected result always reports zero changed floors.
    /// </summary>
    public readonly struct FloorBatchChangeResult
    {
        public bool Succeeded { get; }

        public int RequestedCount { get; }

        public int ChangedCount { get; }

        public FloorChangeFailure Failure { get; }

        public GridPosition FailedCell { get; }


        private FloorBatchChangeResult(
            bool succeeded,
            int requestedCount,
            int changedCount,
            FloorChangeFailure failure,
            GridPosition failedCell)
        {
            Succeeded = succeeded;
            RequestedCount = requestedCount;
            ChangedCount = changedCount;
            Failure = failure;
            FailedCell = failedCell;
        }


        public static FloorBatchChangeResult Success(
            int changedCount)
        {
            return new FloorBatchChangeResult(
                true,
                changedCount,
                changedCount,
                FloorChangeFailure.None,
                default);
        }


        public static FloorBatchChangeResult Rejected(
            int requestedCount,
            GridPosition failedCell,
            FloorChangeFailure failure)
        {
            return new FloorBatchChangeResult(
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
                    $"Floor batch succeeded. " +
                    $"Changed {ChangedCount} floor(s).";
            }

            return
                $"Floor batch rejected: {Failure}. " +
                $"Failed cell: {FailedCell}. " +
                $"Changed 0 of {RequestedCount} requested floor(s).";
        }
    }
}
