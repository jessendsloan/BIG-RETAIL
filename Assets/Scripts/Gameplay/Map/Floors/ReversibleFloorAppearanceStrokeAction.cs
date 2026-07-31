using System;
using System.Collections.Generic;
using BigRetail.Map.Construction;
using BigRetail.Map.Domain;

namespace BigRetail.Map.Floors
{
    /// <summary>
    /// Replays one combined structural-Floor and finish stroke as a single
    /// construction-history action.
    /// </summary>
    public sealed class ReversibleFloorAppearanceStrokeAction :
        IReversibleConstructionAction
    {
        private readonly FloorConstructionService floorConstruction;
        private readonly FloorFinishService floorFinishes;
        private readonly HashSet<GridPosition> createdFloorSet;


        public FloorAppearanceStrokeEdit Edit { get; }

        public string Description =>
            $"Floor appearance stroke: {Edit.CreatedFloorCount} Floor(s), "
            + $"{Edit.FinishChangeCount} finish change(s)";

        public int ChangeCount =>
            Edit.ChangeCount;


        public ReversibleFloorAppearanceStrokeAction(
            FloorConstructionService floorConstruction,
            FloorFinishService floorFinishes,
            FloorAppearanceStrokeEdit edit)
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
                    "A reversible Floor appearance action requires a non-empty edit.",
                    nameof(edit));
            }

            createdFloorSet =
                new HashSet<GridPosition>(
                    Edit.CreatedFloors);
        }


        public ConstructionActionResult TryUndo()
        {
            ConstructionActionResult validation =
                ValidateUndoState();

            if (!validation.Succeeded)
            {
                return validation;
            }

            List<FloorCellFinishEdit> restoredFinishes =
                new List<FloorCellFinishEdit>();

            for (int index = Edit.FinishEdits.Count - 1;
                 index >= 0;
                 index--)
            {
                FloorCellFinishEdit edit =
                    Edit.FinishEdits[index];

                FloorFinishChangeResult result =
                    floorFinishes.TrySetFinish(
                        edit.Cell,
                        edit.BeforeFinishId);

                if (!result.Succeeded)
                {
                    RestoreAfterFinishes(restoredFinishes);

                    return ConstructionActionResult.Rejected(
                        $"Floor appearance undo could not restore {edit.Cell}: "
                        + $"{result.Failure}.");
                }

                restoredFinishes.Add(edit);
            }

            if (Edit.CreatedFloorCount > 0)
            {
                FloorBatchChangeResult removal =
                    floorConstruction.TryApplyEdit(
                        FloorEdit.RemoveFloors(
                            Edit.CreatedFloors));

                if (!removal.Succeeded)
                {
                    RestoreAfterFinishes(restoredFinishes);

                    return ConstructionActionResult.Rejected(
                        $"Floor appearance undo could not remove created Floor "
                        + $"{removal.FailedCell}: {removal.Failure}.");
                }
            }

            return ConstructionActionResult.Success();
        }

        public ConstructionActionResult TryRedo()
        {
            ConstructionActionResult validation =
                ValidateRedoState();

            if (!validation.Succeeded)
            {
                return validation;
            }

            if (Edit.CreatedFloorCount > 0)
            {
                FloorBatchChangeResult addition =
                    floorConstruction.TryApplyEdit(
                        FloorEdit.AddFloors(
                            Edit.CreatedFloors));

                if (!addition.Succeeded)
                {
                    return ConstructionActionResult.Rejected(
                        $"Floor appearance redo could not restore created Floor "
                        + $"{addition.FailedCell}: {addition.Failure}.");
                }
            }

            List<FloorCellFinishEdit> appliedFinishes =
                new List<FloorCellFinishEdit>();

            for (int index = 0;
                 index < Edit.FinishEdits.Count;
                 index++)
            {
                FloorCellFinishEdit edit =
                    Edit.FinishEdits[index];

                FloorFinishChangeResult result =
                    floorFinishes.TrySetFinish(
                        edit.Cell,
                        edit.AfterFinishId);

                if (!result.Succeeded)
                {
                    RestoreBeforeFinishes(appliedFinishes);
                    RemoveRedoCreatedFloors();

                    return ConstructionActionResult.Rejected(
                        $"Floor appearance redo could not apply {edit.Cell}: "
                        + $"{result.Failure}.");
                }

                appliedFinishes.Add(edit);
            }

            return ConstructionActionResult.Success();
        }


        private ConstructionActionResult ValidateUndoState()
        {
            for (int index = 0;
                 index < Edit.CreatedFloors.Count;
                 index++)
            {
                GridPosition cell =
                    Edit.CreatedFloors[index];

                if (!floorConstruction.HasFloor(cell))
                {
                    return ConstructionActionResult.Rejected(
                        $"Floor appearance undo expected created Floor '{cell}'.");
                }
            }

            for (int index = 0;
                 index < Edit.FinishEdits.Count;
                 index++)
            {
                FloorCellFinishEdit edit =
                    Edit.FinishEdits[index];

                if (!floorConstruction.HasFloor(edit.Cell))
                {
                    return ConstructionActionResult.Rejected(
                        $"Floor appearance undo expected Floor '{edit.Cell}'.");
                }

                FloorFinishId current =
                    floorFinishes.GetEffectiveFinish(edit.Cell);

                if (current != edit.AfterFinishId)
                {
                    return ConstructionActionResult.Rejected(
                        $"Floor appearance undo expected '{edit.AfterFinishId}' "
                        + $"at {edit.Cell}, but found '{current}'.");
                }
            }

            return ConstructionActionResult.Success();
        }

        private ConstructionActionResult ValidateRedoState()
        {
            for (int index = 0;
                 index < Edit.CreatedFloors.Count;
                 index++)
            {
                GridPosition cell =
                    Edit.CreatedFloors[index];

                if (floorConstruction.HasFloor(cell))
                {
                    return ConstructionActionResult.Rejected(
                        $"Floor appearance redo expected empty cell '{cell}'.");
                }
            }

            for (int index = 0;
                 index < Edit.FinishEdits.Count;
                 index++)
            {
                FloorCellFinishEdit edit =
                    Edit.FinishEdits[index];

                if (createdFloorSet.Contains(edit.Cell))
                {
                    continue;
                }

                if (!floorConstruction.HasFloor(edit.Cell))
                {
                    return ConstructionActionResult.Rejected(
                        $"Floor appearance redo expected Floor '{edit.Cell}'.");
                }

                FloorFinishId current =
                    floorFinishes.GetEffectiveFinish(edit.Cell);

                if (current != edit.BeforeFinishId)
                {
                    return ConstructionActionResult.Rejected(
                        $"Floor appearance redo expected '{edit.BeforeFinishId}' "
                        + $"at {edit.Cell}, but found '{current}'.");
                }
            }

            return ConstructionActionResult.Success();
        }


        private void RestoreAfterFinishes(
            IReadOnlyList<FloorCellFinishEdit> edits)
        {
            for (int index = edits.Count - 1;
                 index >= 0;
                 index--)
            {
                FloorCellFinishEdit edit =
                    edits[index];

                floorFinishes.TrySetFinish(
                    edit.Cell,
                    edit.AfterFinishId);
            }
        }


        private void RestoreBeforeFinishes(
            IReadOnlyList<FloorCellFinishEdit> edits)
        {
            for (int index = edits.Count - 1;
                 index >= 0;
                 index--)
            {
                FloorCellFinishEdit edit =
                    edits[index];

                floorFinishes.TrySetFinish(
                    edit.Cell,
                    edit.BeforeFinishId);
            }
        }


        private void RemoveRedoCreatedFloors()
        {
            if (Edit.CreatedFloorCount == 0)
            {
                return;
            }

            floorConstruction.TryApplyEdit(
                FloorEdit.RemoveFloors(
                    Edit.CreatedFloors));
        }
    }
}
