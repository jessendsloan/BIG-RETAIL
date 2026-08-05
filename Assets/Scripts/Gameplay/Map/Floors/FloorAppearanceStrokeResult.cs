using System;
using BigRetail.Map.Domain;

namespace BigRetail.Map.Floors
{
    public readonly struct FloorAppearanceStrokeResult
    {
        public bool Succeeded =>
            Failure == FloorAppearanceStrokeFailure.None;

        public int RequestedCount { get; }

        public int CreatedFloorCount { get; }

        public int ExistingFloorCount { get; }

        public int SkippedCellCount { get; }

        public int FinishChangeCount { get; }

        public int UnchangedFinishCount { get; }

        public FloorAppearanceStrokeFailure Failure { get; }

        public GridPosition FailedCell { get; }

        public FloorAppearanceStrokeEdit Edit { get; }


        private FloorAppearanceStrokeResult(
            int requestedCount,
            int createdFloorCount,
            int existingFloorCount,
            int skippedCellCount,
            int finishChangeCount,
            int unchangedFinishCount,
            FloorAppearanceStrokeFailure failure,
            GridPosition failedCell,
            FloorAppearanceStrokeEdit edit)
        {
            RequestedCount = requestedCount;
            CreatedFloorCount = createdFloorCount;
            ExistingFloorCount = existingFloorCount;
            SkippedCellCount = skippedCellCount;
            FinishChangeCount = finishChangeCount;
            UnchangedFinishCount = unchangedFinishCount;
            Failure = failure;
            FailedCell = failedCell;
            Edit = edit;
        }


        internal static FloorAppearanceStrokeResult Success(
            int requestedCount,
            int createdFloorCount,
            int existingFloorCount,
            int skippedCellCount,
            int finishChangeCount,
            int unchangedFinishCount,
            FloorAppearanceStrokeEdit edit)
        {
            return new FloorAppearanceStrokeResult(
                requestedCount,
                createdFloorCount,
                existingFloorCount,
                skippedCellCount,
                finishChangeCount,
                unchangedFinishCount,
                FloorAppearanceStrokeFailure.None,
                default,
                edit);
        }

        internal static FloorAppearanceStrokeResult Rejected(
            int requestedCount,
            FloorAppearanceStrokeFailure failure,
            GridPosition failedCell = default)
        {
            return new FloorAppearanceStrokeResult(
                requestedCount,
                0,
                0,
                0,
                0,
                0,
                failure,
                failedCell,
                new FloorAppearanceStrokeEdit(
                    Array.Empty<GridPosition>(),
                    Array.Empty<FloorCellFinishEdit>()));
        }
    }
}
