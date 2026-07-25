using BigRetail.Map.Domain;

namespace BigRetail.Map.Walls
{
    /// <summary>
    /// Describes the result of one model-owned batch wall operation.
    ///
    /// A rejected result always reports zero changed walls.
    /// A successful result reports the complete number changed.
    /// </summary>
    public readonly struct WallBatchChangeResult
    {
        public bool Succeeded { get; }

        public int RequestedCount { get; }

        public int ChangedCount { get; }

        public WallChangeFailure Failure { get; }

        public CellEdge FailedEdge { get; }


        private WallBatchChangeResult(
            bool succeeded,
            int requestedCount,
            int changedCount,
            WallChangeFailure failure,
            CellEdge failedEdge)
        {
            Succeeded = succeeded;
            RequestedCount = requestedCount;
            ChangedCount = changedCount;
            Failure = failure;
            FailedEdge = failedEdge;
        }


        public static WallBatchChangeResult Success(
            int changedCount)
        {
            return new WallBatchChangeResult(
                true,
                changedCount,
                changedCount,
                WallChangeFailure.None,
                default);
        }


        public static WallBatchChangeResult Rejected(
            int requestedCount,
            CellEdge failedEdge,
            WallChangeFailure failure)
        {
            return new WallBatchChangeResult(
                false,
                requestedCount,
                0,
                failure,
                failedEdge);
        }


        public override string ToString()
        {
            if (Succeeded)
            {
                return
                    $"Wall batch succeeded. " +
                    $"Changed {ChangedCount} wall(s).";
            }

            return
                $"Wall batch rejected: {Failure}. " +
                $"Failed edge: {FailedEdge}. " +
                $"Changed 0 of {RequestedCount} requested wall(s).";
        }
    }
}