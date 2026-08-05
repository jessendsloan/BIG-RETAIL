using System.Collections.Generic;
using BigRetail.Map.Domain;

namespace BigRetail.Map.Walls
{
    /// <summary>
    /// Describes the result of ensuring that a requested collection
    /// of wall edges exists.
    ///
    /// Existing walls count as already satisfied.
    /// Invalid edges are skipped.
    /// Missing legal walls are added together.
    /// </summary>
    public readonly struct WallEnsureResult
    {
        public bool Succeeded { get; }

        public int RequestedCount { get; }

        public int UniqueCount { get; }

        /// <summary>
        /// Exact edit containing only newly added wall edges.
        /// </summary>
        public WallEdit Edit { get; }

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

        public WallChangeFailure Failure { get; }

        public CellEdge FailedEdge { get; }


        private WallEnsureResult(
            bool succeeded,
            int requestedCount,
            int uniqueCount,
            WallEdit edit,
            int alreadyExistingCount,
            int skippedOutsideMapCount,
            int skippedOutsideConstructionAreaCount,
            int skippedMissingFoundationCount,
            WallChangeFailure failure,
            CellEdge failedEdge)
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
            FailedEdge = failedEdge;
        }


        public static WallEnsureResult Success(
            int requestedCount,
            int uniqueCount,
            IReadOnlyList<CellEdge> changedEdges,
            int alreadyExistingCount,
            int skippedOutsideMapCount,
            int skippedOutsideConstructionAreaCount,
            int skippedMissingFoundationCount)
        {
            return new WallEnsureResult(
                true,
                requestedCount,
                uniqueCount,
                WallEdit.AddWalls(changedEdges),
                alreadyExistingCount,
                skippedOutsideMapCount,
                skippedOutsideConstructionAreaCount,
                skippedMissingFoundationCount,
                WallChangeFailure.None,
                default);
        }


        public static WallEnsureResult Rejected(
            int requestedCount,
            int uniqueCount,
            CellEdge failedEdge,
            WallChangeFailure failure)
        {
            return new WallEnsureResult(
                false,
                requestedCount,
                uniqueCount,
                default,
                0,
                0,
                0,
                0,
                failure,
                failedEdge);
        }


        public override string ToString()
        {
            if (!Succeeded)
            {
                return
                    $"Ensure-wall request failed: {Failure}. " +
                    $"Failed edge: {FailedEdge}.";
            }

            return
                $"Ensure-wall request processed. " +
                $"Requested: {RequestedCount}. " +
                $"Unique: {UniqueCount}. " +
                $"Created: {ChangedCount}. " +
                $"Already existing: {AlreadyExistingCount}. " +
                $"Skipped: {SkippedCount}.";
        }
    }
}
