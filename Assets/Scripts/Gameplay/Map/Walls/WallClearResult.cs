using System.Collections.Generic;
using BigRetail.Map.Domain;

namespace BigRetail.Map.Walls
{
    /// <summary>
    /// Describes the result of ensuring that a requested collection
    /// of edges contains no walls.
    ///
    /// Existing walls are removed.
    /// Already-empty edges count as already satisfied.
    /// </summary>
    public readonly struct WallClearResult
    {
        public bool Succeeded { get; }

        public int RequestedCount { get; }

        public int UniqueCount { get; }

        /// <summary>
        /// Exact edit containing only the walls genuinely removed.
        /// </summary>
        public WallEdit Edit { get; }

        public int RemovedCount =>
            Edit.Count;

        public int AlreadyEmptyCount { get; }

        public WallChangeFailure Failure { get; }

        public CellEdge FailedEdge { get; }


        private WallClearResult(
            bool succeeded,
            int requestedCount,
            int uniqueCount,
            WallEdit edit,
            int alreadyEmptyCount,
            WallChangeFailure failure,
            CellEdge failedEdge)
        {
            Succeeded = succeeded;
            RequestedCount = requestedCount;
            UniqueCount = uniqueCount;
            Edit = edit;
            AlreadyEmptyCount = alreadyEmptyCount;
            Failure = failure;
            FailedEdge = failedEdge;
        }


        public static WallClearResult Success(
            int requestedCount,
            int uniqueCount,
            IReadOnlyList<CellEdge> removedEdges,
            int alreadyEmptyCount)
        {
            return new WallClearResult(
                true,
                requestedCount,
                uniqueCount,
                WallEdit.RemoveWalls(removedEdges),
                alreadyEmptyCount,
                WallChangeFailure.None,
                default);
        }


        public static WallClearResult Rejected(
            int requestedCount,
            int uniqueCount,
            CellEdge failedEdge,
            WallChangeFailure failure)
        {
            return new WallClearResult(
                false,
                requestedCount,
                uniqueCount,
                default,
                0,
                failure,
                failedEdge);
        }


        public override string ToString()
        {
            if (!Succeeded)
            {
                return
                    $"Clear-wall request failed: {Failure}. " +
                    $"Failed edge: {FailedEdge}.";
            }

            return
                $"Clear-wall request processed. " +
                $"Requested: {RequestedCount}. " +
                $"Unique: {UniqueCount}. " +
                $"Removed: {RemovedCount}. " +
                $"Already empty: {AlreadyEmptyCount}.";
        }
    }
}