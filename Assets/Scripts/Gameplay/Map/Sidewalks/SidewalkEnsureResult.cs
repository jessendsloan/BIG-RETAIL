using System.Collections.Generic;
using BigRetail.Map.Domain;

namespace BigRetail.Map.Sidewalks
{
    public readonly struct SidewalkEnsureResult
    {
        public bool Succeeded { get; }

        public int RequestedCount { get; }

        public int UniqueCount { get; }

        public SidewalkEdit Edit { get; }

        public int ChangedCount => Edit.Count;

        public int SatisfiedCount =>
            ChangedCount + AlreadyExistingCount;

        public int AlreadyExistingCount { get; }

        public int SkippedOutsideMapCount { get; }

        public int SkippedOutsideConstructionAreaCount { get; }

        public int SkippedFoundationCount { get; }

        public int SkippedCount =>
            SkippedOutsideMapCount
            + SkippedOutsideConstructionAreaCount
            + SkippedFoundationCount;

        public SidewalkChangeFailure Failure { get; }

        public GridPosition FailedCell { get; }


        private SidewalkEnsureResult(
            bool succeeded,
            int requestedCount,
            int uniqueCount,
            SidewalkEdit edit,
            int alreadyExistingCount,
            int skippedOutsideMapCount,
            int skippedOutsideConstructionAreaCount,
            int skippedFoundationCount,
            SidewalkChangeFailure failure,
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
            SkippedFoundationCount = skippedFoundationCount;
            Failure = failure;
            FailedCell = failedCell;
        }


        public static SidewalkEnsureResult Success(
            int requestedCount,
            int uniqueCount,
            IReadOnlyList<GridPosition> changedCells,
            int alreadyExistingCount,
            int skippedOutsideMapCount,
            int skippedOutsideConstructionAreaCount,
            int skippedFoundationCount)
        {
            return new SidewalkEnsureResult(
                true,
                requestedCount,
                uniqueCount,
                SidewalkEdit.AddSidewalks(changedCells),
                alreadyExistingCount,
                skippedOutsideMapCount,
                skippedOutsideConstructionAreaCount,
                skippedFoundationCount,
                SidewalkChangeFailure.None,
                default);
        }


        public static SidewalkEnsureResult Rejected(
            int requestedCount,
            int uniqueCount,
            GridPosition failedCell,
            SidewalkChangeFailure failure)
        {
            return new SidewalkEnsureResult(
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
    }
}
