using System.Collections.Generic;
using BigRetail.Map.Domain;

namespace BigRetail.Map.Sidewalks
{
    public readonly struct SidewalkClearResult
    {
        public bool Succeeded { get; }

        public int RequestedCount { get; }

        public int UniqueCount { get; }

        public SidewalkEdit Edit { get; }

        public int RemovedCount => Edit.Count;

        public int AlreadyEmptyCount { get; }

        public SidewalkChangeFailure Failure { get; }

        public GridPosition FailedCell { get; }


        private SidewalkClearResult(
            bool succeeded,
            int requestedCount,
            int uniqueCount,
            SidewalkEdit edit,
            int alreadyEmptyCount,
            SidewalkChangeFailure failure,
            GridPosition failedCell)
        {
            Succeeded = succeeded;
            RequestedCount = requestedCount;
            UniqueCount = uniqueCount;
            Edit = edit;
            AlreadyEmptyCount = alreadyEmptyCount;
            Failure = failure;
            FailedCell = failedCell;
        }


        public static SidewalkClearResult Success(
            int requestedCount,
            int uniqueCount,
            IReadOnlyList<GridPosition> removedCells,
            int alreadyEmptyCount)
        {
            return new SidewalkClearResult(
                true,
                requestedCount,
                uniqueCount,
                SidewalkEdit.RemoveSidewalks(removedCells),
                alreadyEmptyCount,
                SidewalkChangeFailure.None,
                default);
        }


        public static SidewalkClearResult Rejected(
            int requestedCount,
            int uniqueCount,
            GridPosition failedCell,
            SidewalkChangeFailure failure)
        {
            return new SidewalkClearResult(
                false,
                requestedCount,
                uniqueCount,
                default,
                0,
                failure,
                failedCell);
        }
    }
}
