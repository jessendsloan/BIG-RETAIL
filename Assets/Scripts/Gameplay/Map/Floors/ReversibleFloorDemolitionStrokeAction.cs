using System;
using System.Collections.Generic;
using BigRetail.Map.Construction;
using BigRetail.Map.Domain;

namespace BigRetail.Map.Floors
{
    /// <summary>
    /// Replays one Floor-demolition stroke with each removed Floor's exact
    /// effective finish.
    /// </summary>
    public sealed class ReversibleFloorDemolitionStrokeAction :
        IReversibleConstructionAction
    {
        private readonly FloorConstructionService floorConstruction;
        private readonly FloorFinishService floorFinishes;
        private readonly GridPosition[] removedCells;


        public FloorDemolitionStrokeEdit Edit { get; }

        public string Description =>
            $"Floor demolition stroke: {Edit.Count} Floor(s)";

        public int ChangeCount =>
            Edit.Count;


        public ReversibleFloorDemolitionStrokeAction(
            FloorConstructionService floorConstruction,
            FloorFinishService floorFinishes,
            FloorDemolitionStrokeEdit edit)
        {
            this.floorConstruction =
                floorConstruction
                ?? throw new ArgumentNullException(
                    nameof(floorConstruction));

            this.floorFinishes =
                floorFinishes
                ?? throw new ArgumentNullException(
                    nameof(floorFinishes));

            Edit =
                edit
                ?? throw new ArgumentNullException(
                    nameof(edit));

            if (Edit.IsEmpty)
            {
                throw new ArgumentException(
                    "A reversible Floor-demolition action requires a non-empty edit.",
                    nameof(edit));
            }

            removedCells =
                new GridPosition[Edit.Count];

            for (int index = 0;
                 index < Edit.Count;
                 index++)
            {
                removedCells[index] =
                    Edit.RemovedFloors[index].Cell;
            }
        }


        public ConstructionActionResult TryUndo()
        {
            for (int index = 0;
                 index < removedCells.Length;
                 index++)
            {
                if (floorConstruction.HasFloor(
                    removedCells[index]))
                {
                    return ConstructionActionResult.Rejected(
                        $"Floor demolition undo expected empty cell "
                        + $"'{removedCells[index]}'.");
                }
            }

            FloorBatchChangeResult addition =
                floorConstruction.TryApplyEdit(
                    FloorEdit.AddFloors(
                        removedCells));

            if (!addition.Succeeded)
            {
                return ConstructionActionResult.Rejected(
                    $"Floor demolition undo could not restore Floor "
                    + $"{addition.FailedCell}: {addition.Failure}.");
            }

            for (int index = 0;
                 index < Edit.Count;
                 index++)
            {
                FloorCellSnapshot snapshot =
                    Edit.RemovedFloors[index];

                FloorFinishChangeResult finishResult =
                    floorFinishes.TrySetFinish(
                        snapshot.Cell,
                        snapshot.FinishId);

                if (!finishResult.Succeeded)
                {
                    floorConstruction.TryApplyEdit(
                        FloorEdit.RemoveFloors(
                            removedCells));

                    return ConstructionActionResult.Rejected(
                        $"Floor demolition undo could not restore finish at "
                        + $"{snapshot.Cell}: {finishResult.Failure}.");
                }
            }

            return ConstructionActionResult.Success();
        }


        public ConstructionActionResult TryRedo()
        {
            for (int index = 0;
                 index < Edit.Count;
                 index++)
            {
                FloorCellSnapshot snapshot =
                    Edit.RemovedFloors[index];

                if (!floorConstruction.HasFloor(
                    snapshot.Cell))
                {
                    return ConstructionActionResult.Rejected(
                        $"Floor demolition redo expected Floor "
                        + $"'{snapshot.Cell}'.");
                }

                FloorFinishId currentFinish =
                    floorFinishes.GetEffectiveFinish(
                        snapshot.Cell);

                if (currentFinish != snapshot.FinishId)
                {
                    return ConstructionActionResult.Rejected(
                        $"Floor demolition redo expected "
                        + $"'{snapshot.FinishId}' at {snapshot.Cell}, "
                        + $"but found '{currentFinish}'.");
                }
            }

            FloorBatchChangeResult removal =
                floorConstruction.TryApplyEdit(
                    FloorEdit.RemoveFloors(
                        removedCells));

            if (!removal.Succeeded)
            {
                return ConstructionActionResult.Rejected(
                    $"Floor demolition redo could not remove Floor "
                    + $"{removal.FailedCell}: {removal.Failure}.");
            }

            return ConstructionActionResult.Success();
        }
    }
}
