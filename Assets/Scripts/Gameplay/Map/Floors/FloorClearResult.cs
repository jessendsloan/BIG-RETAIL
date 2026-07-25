using System.Collections.Generic;
using BigRetail.Map.Domain;

namespace BigRetail.Map.Floors
{
    /// <summary>
    /// Describes the result of ensuring that a requested collection
    /// of cells contains no constructed floors.
    ///
    /// Existing floors are removed.
    /// Empty cells count as already satisfied.
    /// </summary>
    public readonly struct FloorClearResult
    {
        public bool Succeeded { get; }

        public int RequestedCount { get; }

        public int UniqueCount { get; }

        /// <summary>
        /// Exact edit containing only genuinely removed floors.
        /// </summary>
        public FloorEdit Edit { get; }

        public int RemovedCount =>
            Edit.Count;

        public int AlreadyEmptyCount { get; }

        public FloorChangeFailure Failure { get; }

        public GridPosition FailedCell { get; }


        private FloorClearResult(
            bool succeeded,
            int requestedCount,
            int uniqueCount,
            FloorEdit edit,
            int alreadyEmptyCount,
            FloorChangeFailure failure,
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


        public static FloorClearResult Success(
            int requestedCount,
            int uniqueCount,
            IReadOnlyList<GridPosition> removedCells,
            int alreadyEmptyCount)
        {
            return new FloorClearResult(
                true,
                requestedCount,
                uniqueCount,
                FloorEdit.RemoveFloors(removedCells),
                alreadyEmptyCount,
                FloorChangeFailure.None,
                default);
        }


        public static FloorClearResult Rejected(
            int requestedCount,
            int uniqueCount,
            GridPosition failedCell,
            FloorChangeFailure failure)
        {
            return new FloorClearResult(
                false,
                requestedCount,
                uniqueCount,
                default,
                0,
                failure,
                failedCell);
        }


        public override string ToString()
        {
            if (!Succeeded)
            {
                return
                    $"Clear-floor request failed: {Failure}. " +
                    $"Failed cell: {FailedCell}.";
            }

            return
                $"Clear-floor request processed. " +
                $"Requested: {RequestedCount}. " +
                $"Unique: {UniqueCount}. " +
                $"Removed: {RemovedCount}. " +
                $"Already empty: {AlreadyEmptyCount}.";
        }
    }
}