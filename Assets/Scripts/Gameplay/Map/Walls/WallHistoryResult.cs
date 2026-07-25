using BigRetail.Map.Domain;

namespace BigRetail.Map.Walls
{
    /// <summary>
    /// Describes an undo or redo attempt.
    /// </summary>
    public readonly struct WallHistoryResult
    {
        public bool Succeeded { get; }

        public WallHistoryFailure Failure { get; }

        /// <summary>
        /// The exact edit applied during this history operation.
        ///
        /// Undo reports the inverse edit.
        /// Redo reports the original edit.
        /// </summary>
        public WallEdit AppliedEdit { get; }

        public WallChangeFailure ApplyFailure { get; }

        public CellEdge FailedEdge { get; }


        private WallHistoryResult(
            bool succeeded,
            WallHistoryFailure failure,
            WallEdit appliedEdit,
            WallChangeFailure applyFailure,
            CellEdge failedEdge)
        {
            Succeeded = succeeded;
            Failure = failure;
            AppliedEdit = appliedEdit;
            ApplyFailure = applyFailure;
            FailedEdge = failedEdge;
        }


        public static WallHistoryResult Success(
            WallEdit appliedEdit)
        {
            return new WallHistoryResult(
                true,
                WallHistoryFailure.None,
                appliedEdit,
                WallChangeFailure.None,
                default);
        }


        public static WallHistoryResult Rejected(
            WallHistoryFailure failure,
            WallEdit attemptedEdit = default,
            WallChangeFailure applyFailure =
                WallChangeFailure.None,
            CellEdge failedEdge = default)
        {
            return new WallHistoryResult(
                false,
                failure,
                attemptedEdit,
                applyFailure,
                failedEdge);
        }


        public override string ToString()
        {
            if (Succeeded)
            {
                return
                    $"Wall history operation succeeded: " +
                    $"{AppliedEdit}.";
            }

            if (Failure
                == WallHistoryFailure.EditCouldNotBeApplied)
            {
                return
                    $"Wall history operation failed because the edit " +
                    $"could not be applied. Reason: {ApplyFailure}. " +
                    $"Edge: {FailedEdge}.";
            }

            return
                $"Wall history operation rejected: {Failure}.";
        }
    }
}