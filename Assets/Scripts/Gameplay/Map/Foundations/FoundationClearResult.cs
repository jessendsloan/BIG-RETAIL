using System.Collections.Generic;
using BigRetail.Map.Domain;

namespace BigRetail.Map.Foundations
{
    /// <summary>
    /// Describes the result of ensuring that requested cells contain no
    /// constructed foundations.
    /// </summary>
    public readonly struct FoundationClearResult
    {
        public bool Succeeded { get; }

        public int RequestedCount { get; }

        public int UniqueCount { get; }

        public FoundationEdit Edit { get; }

        public int RemovedCount =>
            Edit.Count;

        public int AlreadyEmptyCount { get; }

        public FoundationChangeFailure Failure { get; }

        public GridPosition FailedCell { get; }


        private FoundationClearResult(
            bool succeeded,
            int requestedCount,
            int uniqueCount,
            FoundationEdit edit,
            int alreadyEmptyCount,
            FoundationChangeFailure failure,
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


        public static FoundationClearResult Success(
            int requestedCount,
            int uniqueCount,
            IReadOnlyList<GridPosition> removedCells,
            int alreadyEmptyCount)
        {
            return new FoundationClearResult(
                true,
                requestedCount,
                uniqueCount,
                FoundationEdit.RemoveFoundations(removedCells),
                alreadyEmptyCount,
                FoundationChangeFailure.None,
                default);
        }


        public static FoundationClearResult Rejected(
            int requestedCount,
            int uniqueCount,
            GridPosition failedCell,
            FoundationChangeFailure failure)
        {
            return new FoundationClearResult(
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
                    $"Clear-foundation request failed: {Failure}. " +
                    $"Failed cell: {FailedCell}.";
            }

            return
                $"Clear-foundation request processed. " +
                $"Requested: {RequestedCount}. " +
                $"Unique: {UniqueCount}. " +
                $"Removed: {RemovedCount}. " +
                $"Already empty: {AlreadyEmptyCount}.";
        }
    }
}
