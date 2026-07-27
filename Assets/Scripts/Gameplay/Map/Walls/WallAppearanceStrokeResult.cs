using BigRetail.Map.Domain;

namespace BigRetail.Map.Walls
{
    /// <summary>
    /// Describes one permissive wall-tool stroke across existing, missing,
    /// and invalid wall edges.
    /// </summary>
    public readonly struct WallAppearanceStrokeResult
    {
        public bool Succeeded { get; }

        public int RequestedCount { get; }

        public int CreatedWallCount { get; }

        public int ExistingWallCount { get; }

        public int SkippedWallCount { get; }

        public int ChangedFinishCount { get; }

        public int UnchangedFinishCount { get; }

        public WallAppearanceStrokeEdit Edit { get; }

        public WallAppearanceStrokeFailure Failure { get; }

        public CellEdge FailedEdge { get; }

        public WallFinishChangeFailure FinishFailure { get; }

        public bool HasChanges =>
            Edit != null
            && !Edit.IsEmpty;


        private WallAppearanceStrokeResult(
            bool succeeded,
            int requestedCount,
            int createdWallCount,
            int existingWallCount,
            int skippedWallCount,
            int changedFinishCount,
            int unchangedFinishCount,
            WallAppearanceStrokeEdit edit,
            WallAppearanceStrokeFailure failure,
            CellEdge failedEdge,
            WallFinishChangeFailure finishFailure)
        {
            Succeeded = succeeded;
            RequestedCount = requestedCount;
            CreatedWallCount = createdWallCount;
            ExistingWallCount = existingWallCount;
            SkippedWallCount = skippedWallCount;
            ChangedFinishCount = changedFinishCount;
            UnchangedFinishCount = unchangedFinishCount;
            Edit = edit;
            Failure = failure;
            FailedEdge = failedEdge;
            FinishFailure = finishFailure;
        }


        public static WallAppearanceStrokeResult Success(
            int requestedCount,
            int createdWallCount,
            int existingWallCount,
            int skippedWallCount,
            int changedFinishCount,
            int unchangedFinishCount,
            WallAppearanceStrokeEdit edit)
        {
            return new WallAppearanceStrokeResult(
                true,
                requestedCount,
                createdWallCount,
                existingWallCount,
                skippedWallCount,
                changedFinishCount,
                unchangedFinishCount,
                edit,
                WallAppearanceStrokeFailure.None,
                default,
                WallFinishChangeFailure.None);
        }


        public static WallAppearanceStrokeResult Rejected(
            int requestedCount,
            WallAppearanceStrokeFailure failure,
            CellEdge failedEdge = default,
            WallFinishChangeFailure finishFailure =
                WallFinishChangeFailure.None)
        {
            return new WallAppearanceStrokeResult(
                false,
                requestedCount,
                0,
                0,
                0,
                0,
                0,
                null,
                failure,
                failedEdge,
                finishFailure);
        }
    }
}
