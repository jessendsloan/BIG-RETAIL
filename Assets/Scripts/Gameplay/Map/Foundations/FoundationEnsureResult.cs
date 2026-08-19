using System.Collections.Generic;
using BigRetail.Map.Domain;

namespace BigRetail.Map.Foundations
{
    /// <summary>
    /// Describes the result of ensuring that requested cells contain
    /// constructed foundations.
    /// </summary>
    public readonly struct FoundationEnsureResult
    {
        public bool Succeeded { get; }

        public int RequestedCount { get; }

        public int UniqueCount { get; }

        public FoundationEdit Edit { get; }

        public int ChangedCount =>
            Edit.Count;

        public int AlreadyExistingCount { get; }

        public int SkippedOutsideMapCount { get; }

        public int SkippedOutsideConstructionAreaCount { get; }

        public int SkippedSidewalkCount { get; }

        public int SkippedCount =>
            SkippedOutsideMapCount
            + SkippedOutsideConstructionAreaCount
            + SkippedSidewalkCount;

        public int SatisfiedCount =>
            ChangedCount
            + AlreadyExistingCount;

        public FoundationChangeFailure Failure { get; }

        public GridPosition FailedCell { get; }


        private FoundationEnsureResult(
            bool succeeded,
            int requestedCount,
            int uniqueCount,
            FoundationEdit edit,
            int alreadyExistingCount,
            int skippedOutsideMapCount,
            int skippedOutsideConstructionAreaCount,
            int skippedSidewalkCount,
            FoundationChangeFailure failure,
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
            SkippedSidewalkCount = skippedSidewalkCount;
            Failure = failure;
            FailedCell = failedCell;
        }


        public static FoundationEnsureResult Success(
            int requestedCount,
            int uniqueCount,
            IReadOnlyList<GridPosition> changedCells,
            int alreadyExistingCount,
            int skippedOutsideMapCount,
            int skippedOutsideConstructionAreaCount,
            int skippedSidewalkCount = 0)
        {
            return new FoundationEnsureResult(
                true,
                requestedCount,
                uniqueCount,
                FoundationEdit.AddFoundations(changedCells),
                alreadyExistingCount,
                skippedOutsideMapCount,
                skippedOutsideConstructionAreaCount,
                skippedSidewalkCount,
                FoundationChangeFailure.None,
                default);
        }


        public static FoundationEnsureResult Rejected(
            int requestedCount,
            int uniqueCount,
            GridPosition failedCell,
            FoundationChangeFailure failure)
        {
            return new FoundationEnsureResult(
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
                    $"Ensure-foundation request failed: {Failure}. " +
                    $"Failed cell: {FailedCell}.";
            }

            return
                $"Ensure-foundation request processed. " +
                $"Requested: {RequestedCount}. " +
                $"Unique: {UniqueCount}. " +
                $"Created: {ChangedCount}. " +
                $"Already existing: {AlreadyExistingCount}. " +
                $"Skipped: {SkippedCount}.";
        }
    }
}
