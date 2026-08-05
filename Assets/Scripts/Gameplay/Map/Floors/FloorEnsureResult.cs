using System.Collections.Generic;
using BigRetail.Map.Domain;

namespace BigRetail.Map.Floors
{
    /// <summary>
    /// Describes the result of ensuring that a requested collection
    /// of cells contains constructed floors.
    ///
    /// Existing floors count as already satisfied.
    /// Invalid cells are skipped.
    /// </summary>
    public readonly struct FloorEnsureResult
    {
        public bool Succeeded { get; }

        public int RequestedCount { get; }

        public int UniqueCount { get; }

        /// <summary>
        /// Exact edit containing only newly constructed floors.
        /// </summary>
        public FloorEdit Edit { get; }

        public int ChangedCount =>
            Edit.Count;

        public int AlreadyExistingCount { get; }

        public int SkippedOutsideMapCount { get; }

        public int SkippedOutsideConstructionAreaCount { get; }

        public int SkippedMissingFoundationCount { get; }

        public int SkippedCount =>
            SkippedOutsideMapCount
            + SkippedOutsideConstructionAreaCount
            + SkippedMissingFoundationCount;

        public int SatisfiedCount =>
            ChangedCount
            + AlreadyExistingCount;

        public FloorChangeFailure Failure { get; }

        public GridPosition FailedCell { get; }


        private FloorEnsureResult(
            bool succeeded,
            int requestedCount,
            int uniqueCount,
            FloorEdit edit,
            int alreadyExistingCount,
            int skippedOutsideMapCount,
            int skippedOutsideConstructionAreaCount,
            int skippedMissingFoundationCount,
            FloorChangeFailure failure,
            GridPosition failedCell)
        {
            Succeeded = succeeded;
            RequestedCount = requestedCount;
            UniqueCount = uniqueCount;
            Edit = edit;
            AlreadyExistingCount = alreadyExistingCount;
            SkippedOutsideMapCount = skippedOutsideMapCount;
            SkippedOutsideConstructionAreaCount =
                skippedOutsideConstructionAreaCount;
            SkippedMissingFoundationCount =
                skippedMissingFoundationCount;
            Failure = failure;
            FailedCell = failedCell;
        }


        public static FloorEnsureResult Success(
            int requestedCount,
            int uniqueCount,
            IReadOnlyList<GridPosition> changedCells,
            int alreadyExistingCount,
            int skippedOutsideMapCount,
            int skippedOutsideConstructionAreaCount,
            int skippedMissingFoundationCount)
        {
            return new FloorEnsureResult(
                true,
                requestedCount,
                uniqueCount,
                FloorEdit.AddFloors(changedCells),
                alreadyExistingCount,
                skippedOutsideMapCount,
                skippedOutsideConstructionAreaCount,
                skippedMissingFoundationCount,
                FloorChangeFailure.None,
                default);
        }


        public static FloorEnsureResult Rejected(
            int requestedCount,
            int uniqueCount,
            GridPosition failedCell,
            FloorChangeFailure failure)
        {
            return new FloorEnsureResult(
                false,
                requestedCount,
                uniqueCount,
                default,
                0,
                0,
                0,
                0,
                failure,
                failedCell);
        }


        public override string ToString()
        {
            if (!Succeeded)
            {
                return
                    $"Ensure-floor request failed: {Failure}. " +
                    $"Failed cell: {FailedCell}.";
            }

            return
                $"Ensure-floor request processed. " +
                $"Requested: {RequestedCount}. " +
                $"Unique: {UniqueCount}. " +
                $"Created: {ChangedCount}. " +
                $"Already existing: {AlreadyExistingCount}. " +
                $"Skipped: {SkippedCount}.";
        }
    }
}
