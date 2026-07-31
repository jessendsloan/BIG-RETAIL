using System;
using System.Collections.Generic;
using BigRetail.Map.Domain;

namespace BigRetail.Map.Floors
{
    /// <summary>
    /// Applies one player-facing Floor-tool stroke.
    ///
    /// Missing supported Floors are created, existing Floors are preserved,
    /// invalid cells are skipped, and the requested finish is applied to every
    /// resulting Floor cell.
    /// </summary>
    public sealed class FloorAppearanceStrokeService
    {
        private readonly FloorConstructionService floorConstruction;
        private readonly FloorFinishService floorFinishes;
        private readonly FloorFinishCatalog finishCatalog;


        public FloorAppearanceStrokeService(
            FloorConstructionService floorConstruction,
            FloorFinishService floorFinishes,
            FloorFinishCatalog finishCatalog)
        {
            this.floorConstruction =
                floorConstruction
                ?? throw new ArgumentNullException(
                    nameof(floorConstruction));

            this.floorFinishes =
                floorFinishes
                ?? throw new ArgumentNullException(
                    nameof(floorFinishes));

            this.finishCatalog =
                finishCatalog
                ?? throw new ArgumentNullException(
                    nameof(finishCatalog));
        }


        public FloorAppearanceStrokeResult TryApply(
            IReadOnlyList<GridPosition> cells,
            FloorFinishId finishId)
        {
            if (cells == null)
            {
                throw new ArgumentNullException(
                    nameof(cells));
            }

            if (cells.Count == 0)
            {
                return FloorAppearanceStrokeResult.Rejected(
                    0,
                    FloorAppearanceStrokeFailure.EmptyRequest);
            }

            if (!finishCatalog.Contains(finishId))
            {
                return FloorAppearanceStrokeResult.Rejected(
                    cells.Count,
                    FloorAppearanceStrokeFailure.UnknownFinish);
            }

            List<GridPosition> eligibleCells =
                new List<GridPosition>(cells.Count);

            List<FloorFinishId> previousFinishes =
                new List<FloorFinishId>(cells.Count);

            HashSet<GridPosition> uniqueCells =
                new HashSet<GridPosition>();

            int existingFloorCount = 0;
            int skippedCellCount = 0;

            for (int index = 0;
                 index < cells.Count;
                 index++)
            {
                GridPosition cell =
                    cells[index];

                if (!uniqueCells.Add(cell))
                {
                    continue;
                }

                if (floorConstruction.HasFloor(cell))
                {
                    eligibleCells.Add(cell);
                    previousFinishes.Add(
                        floorFinishes.GetEffectiveFinish(cell));
                    existingFloorCount++;
                    continue;
                }

                FloorChangeResult placement =
                    floorConstruction.EvaluatePlacement(cell);

                if (!placement.Succeeded)
                {
                    skippedCellCount++;
                    continue;
                }

                eligibleCells.Add(cell);
                previousFinishes.Add(
                    finishCatalog.DefaultFinishId);
            }

            if (eligibleCells.Count == 0)
            {
                return FloorAppearanceStrokeResult.Success(
                    cells.Count,
                    0,
                    0,
                    skippedCellCount,
                    0,
                    0,
                    new FloorAppearanceStrokeEdit(
                        Array.Empty<GridPosition>(),
                        Array.Empty<FloorCellFinishEdit>()));
            }

            FloorEnsureResult ensureResult =
                floorConstruction.TryEnsureFloors(
                    eligibleCells);

            if (!ensureResult.Succeeded)
            {
                return FloorAppearanceStrokeResult.Rejected(
                    cells.Count,
                    FloorAppearanceStrokeFailure.FloorEnsureRejected,
                    ensureResult.FailedCell);
            }

            List<FloorCellFinishEdit> finishEdits =
                new List<FloorCellFinishEdit>();

            int unchangedFinishCount = 0;

            for (int index = 0;
                 index < eligibleCells.Count;
                 index++)
            {
                GridPosition cell =
                    eligibleCells[index];

                FloorFinishId previousFinish =
                    previousFinishes[index];

                FloorFinishChangeResult finishResult =
                    floorFinishes.TrySetFinish(
                        cell,
                        finishId);

                if (!finishResult.Succeeded)
                {
                    bool rolledBack =
                        TryRollback(
                            ensureResult.Edit,
                            finishEdits);

                    return FloorAppearanceStrokeResult.Rejected(
                        cells.Count,
                        rolledBack
                            ? FloorAppearanceStrokeFailure
                                .FinishChangeRejected
                            : FloorAppearanceStrokeFailure
                                .RollbackFailed,
                        cell);
                }

                if (!finishResult.Changed)
                {
                    unchangedFinishCount++;
                    continue;
                }

                finishEdits.Add(
                    new FloorCellFinishEdit(
                        cell,
                        previousFinish,
                        finishId));
            }

            FloorAppearanceStrokeEdit edit =
                new FloorAppearanceStrokeEdit(
                    ensureResult.Edit.Cells,
                    finishEdits);

            return FloorAppearanceStrokeResult.Success(
                cells.Count,
                ensureResult.ChangedCount,
                existingFloorCount,
                skippedCellCount,
                finishEdits.Count,
                unchangedFinishCount,
                edit);
        }


        private bool TryRollback(
            FloorEdit createdFloors,
            IReadOnlyList<FloorCellFinishEdit> finishEdits)
        {
            bool succeeded = true;

            for (int index = finishEdits.Count - 1;
                 index >= 0;
                 index--)
            {
                FloorCellFinishEdit edit =
                    finishEdits[index];

                FloorFinishChangeResult result =
                    floorFinishes.TrySetFinish(
                        edit.Cell,
                        edit.BeforeFinishId);

                succeeded &= result.Succeeded;
            }

            if (!createdFloors.IsEmpty)
            {
                FloorBatchChangeResult removal =
                    floorConstruction.TryApplyEdit(
                        createdFloors.Inverse());

                succeeded &= removal.Succeeded;
            }

            return succeeded;
        }
    }
}
