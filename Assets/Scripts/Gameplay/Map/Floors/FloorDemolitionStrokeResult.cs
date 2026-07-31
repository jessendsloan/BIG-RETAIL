using BigRetail.Map.Domain;

namespace BigRetail.Map.Floors
{
    /// <summary>
    /// Describes one player-facing Floor-demolition stroke.
    /// </summary>
    public readonly struct FloorDemolitionStrokeResult
    {
        public bool Succeeded { get; }

        public int RequestedCount { get; }

        public int UniqueCount { get; }

        public int RemovedCount { get; }

        public int AlreadyEmptyCount { get; }

        public FloorDemolitionStrokeFailure Failure { get; }

        public GridPosition FailedCell { get; }

        public FloorDemolitionStrokeEdit Edit { get; }


        private FloorDemolitionStrokeResult(
            bool succeeded,
            int requestedCount,
            int uniqueCount,
            int removedCount,
            int alreadyEmptyCount,
            FloorDemolitionStrokeFailure failure,
            GridPosition failedCell,
            FloorDemolitionStrokeEdit edit)
        {
            Succeeded = succeeded;
            RequestedCount = requestedCount;
            UniqueCount = uniqueCount;
            RemovedCount = removedCount;
            AlreadyEmptyCount = alreadyEmptyCount;
            Failure = failure;
            FailedCell = failedCell;
            Edit = edit;
        }


        public static FloorDemolitionStrokeResult Success(
            int requestedCount,
            int uniqueCount,
            int alreadyEmptyCount,
            FloorDemolitionStrokeEdit edit)
        {
            return new FloorDemolitionStrokeResult(
                true,
                requestedCount,
                uniqueCount,
                edit.Count,
                alreadyEmptyCount,
                FloorDemolitionStrokeFailure.None,
                default,
                edit);
        }

        public static FloorDemolitionStrokeResult Rejected(
            int requestedCount,
            FloorDemolitionStrokeFailure failure,
            GridPosition failedCell = default)
        {
            return new FloorDemolitionStrokeResult(
                false,
                requestedCount,
                0,
                0,
                0,
                failure,
                failedCell,
                new FloorDemolitionStrokeEdit(
                    System.Array.Empty<FloorCellSnapshot>()));
        }
    }
}
