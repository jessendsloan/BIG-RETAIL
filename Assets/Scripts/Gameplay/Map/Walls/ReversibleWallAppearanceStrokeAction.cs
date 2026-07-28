using System;
using System.Collections.Generic;
using BigRetail.Map.Construction;
using BigRetail.Map.Domain;

namespace BigRetail.Map.Walls
{
    /// <summary>
    /// Replays one combined structural-wall and wall-face-finish stroke as a
    /// single construction-history action.
    /// </summary>
    public sealed class ReversibleWallAppearanceStrokeAction :
        IReversibleConstructionAction
    {
        private readonly WallConstructionService wallConstruction;
        private readonly WallFinishService wallFinishes;
        private readonly HashSet<CellEdge> createdWallSet;


        public WallAppearanceStrokeEdit Edit { get; }

        public string Description =>
            $"Wall appearance stroke: {Edit.CreatedWallCount} wall(s), "
            + $"{Edit.FinishChangeCount} finish change(s)";

        public int ChangeCount =>
            Edit.ChangeCount;


        public ReversibleWallAppearanceStrokeAction(
            WallConstructionService wallConstruction,
            WallFinishService wallFinishes,
            WallAppearanceStrokeEdit edit)
        {
            this.wallConstruction =
                wallConstruction
                ?? throw new ArgumentNullException(
                    nameof(wallConstruction));

            this.wallFinishes =
                wallFinishes
                ?? throw new ArgumentNullException(
                    nameof(wallFinishes));

            Edit =
                edit
                ?? throw new ArgumentNullException(
                    nameof(edit));

            if (Edit.IsEmpty)
            {
                throw new ArgumentException(
                    "A reversible wall appearance action requires a non-empty edit.",
                    nameof(edit));
            }

            createdWallSet =
                new HashSet<CellEdge>(
                    Edit.CreatedWalls);
        }


        public ConstructionActionResult TryUndo()
        {
            ConstructionActionResult validation =
                ValidateUndoState();

            if (!validation.Succeeded)
            {
                return validation;
            }

            List<WallFaceFinishEdit> restoredFinishes =
                new List<WallFaceFinishEdit>();

            for (int index = Edit.FinishEdits.Count - 1;
                 index >= 0;
                 index--)
            {
                WallFaceFinishEdit edit =
                    Edit.FinishEdits[index];

                WallFinishChangeResult result =
                    wallFinishes.TrySetFinish(
                        edit.Face.Edge,
                        edit.Face.FacingCell,
                        edit.BeforeFinishId);

                if (!result.Succeeded)
                {
                    RestoreAfterFinishes(restoredFinishes);

                    return ConstructionActionResult.Rejected(
                        $"Wall appearance undo could not restore {edit.Face}: "
                        + $"{result.Failure}.");
                }

                restoredFinishes.Add(edit);
            }

            if (Edit.CreatedWallCount > 0)
            {
                WallBatchChangeResult removal =
                    wallConstruction.TryApplyEdit(
                        WallEdit.RemoveWalls(
                            Edit.CreatedWalls));

                if (!removal.Succeeded)
                {
                    RestoreAfterFinishes(restoredFinishes);

                    return ConstructionActionResult.Rejected(
                        $"Wall appearance undo could not remove created wall "
                        + $"{removal.FailedEdge}: {removal.Failure}.");
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

            if (Edit.CreatedWallCount > 0)
            {
                WallBatchChangeResult addition =
                    wallConstruction.TryApplyEdit(
                        WallEdit.AddWalls(
                            Edit.CreatedWalls));

                if (!addition.Succeeded)
                {
                    return ConstructionActionResult.Rejected(
                        $"Wall appearance redo could not restore created wall "
                        + $"{addition.FailedEdge}: {addition.Failure}.");
                }
            }

            List<WallFaceFinishEdit> appliedFinishes =
                new List<WallFaceFinishEdit>();

            for (int index = 0;
                 index < Edit.FinishEdits.Count;
                 index++)
            {
                WallFaceFinishEdit edit =
                    Edit.FinishEdits[index];

                WallFinishChangeResult result =
                    wallFinishes.TrySetFinish(
                        edit.Face.Edge,
                        edit.Face.FacingCell,
                        edit.AfterFinishId);

                if (!result.Succeeded)
                {
                    RestoreBeforeFinishes(appliedFinishes);
                    RemoveRedoCreatedWalls();

                    return ConstructionActionResult.Rejected(
                        $"Wall appearance redo could not apply {edit.Face}: "
                        + $"{result.Failure}.");
                }

                appliedFinishes.Add(edit);
            }

            return ConstructionActionResult.Success();
        }


        private ConstructionActionResult ValidateUndoState()
        {
            for (int index = 0;
                 index < Edit.CreatedWalls.Count;
                 index++)
            {
                CellEdge wall =
                    Edit.CreatedWalls[index];

                if (!wallConstruction.HasWall(wall))
                {
                    return ConstructionActionResult.Rejected(
                        $"Wall appearance undo expected created wall '{wall}'.");
                }
            }

            for (int index = 0;
                 index < Edit.FinishEdits.Count;
                 index++)
            {
                WallFaceFinishEdit edit =
                    Edit.FinishEdits[index];

                if (!wallConstruction.HasWall(edit.Face.Edge))
                {
                    return ConstructionActionResult.Rejected(
                        $"Wall appearance undo expected wall '{edit.Face.Edge}'.");
                }

                WallFinishId current =
                    wallFinishes.GetEffectiveFinish(
                        edit.Face.Edge,
                        edit.Face.FacingCell);

                if (current != edit.AfterFinishId)
                {
                    return ConstructionActionResult.Rejected(
                        $"Wall appearance undo expected '{edit.AfterFinishId}' "
                        + $"on {edit.Face}, but found '{current}'.");
                }
            }

            return ConstructionActionResult.Success();
        }


        private ConstructionActionResult ValidateRedoState()
        {
            for (int index = 0;
                 index < Edit.CreatedWalls.Count;
                 index++)
            {
                CellEdge wall =
                    Edit.CreatedWalls[index];

                if (wallConstruction.HasWall(wall))
                {
                    return ConstructionActionResult.Rejected(
                        $"Wall appearance redo expected empty edge '{wall}'.");
                }
            }

            for (int index = 0;
                 index < Edit.FinishEdits.Count;
                 index++)
            {
                WallFaceFinishEdit edit =
                    Edit.FinishEdits[index];

                if (createdWallSet.Contains(edit.Face.Edge))
                {
                    continue;
                }

                if (!wallConstruction.HasWall(edit.Face.Edge))
                {
                    return ConstructionActionResult.Rejected(
                        $"Wall appearance redo expected wall '{edit.Face.Edge}'.");
                }

                WallFinishId current =
                    wallFinishes.GetEffectiveFinish(
                        edit.Face.Edge,
                        edit.Face.FacingCell);

                if (current != edit.BeforeFinishId)
                {
                    return ConstructionActionResult.Rejected(
                        $"Wall appearance redo expected '{edit.BeforeFinishId}' "
                        + $"on {edit.Face}, but found '{current}'.");
                }
            }

            return ConstructionActionResult.Success();
        }


        private void RestoreAfterFinishes(
            IReadOnlyList<WallFaceFinishEdit> edits)
        {
            for (int index = edits.Count - 1;
                 index >= 0;
                 index--)
            {
                WallFaceFinishEdit edit =
                    edits[index];

                wallFinishes.TrySetFinish(
                    edit.Face.Edge,
                    edit.Face.FacingCell,
                    edit.AfterFinishId);
            }
        }


        private void RestoreBeforeFinishes(
            IReadOnlyList<WallFaceFinishEdit> edits)
        {
            for (int index = edits.Count - 1;
                 index >= 0;
                 index--)
            {
                WallFaceFinishEdit edit =
                    edits[index];

                wallFinishes.TrySetFinish(
                    edit.Face.Edge,
                    edit.Face.FacingCell,
                    edit.BeforeFinishId);
            }
        }


        private void RemoveRedoCreatedWalls()
        {
            if (Edit.CreatedWallCount == 0)
            {
                return;
            }

            wallConstruction.TryApplyEdit(
                WallEdit.RemoveWalls(
                    Edit.CreatedWalls));
        }
    }
}
