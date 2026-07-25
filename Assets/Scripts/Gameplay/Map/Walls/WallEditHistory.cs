using System;
using System.Collections.Generic;

namespace BigRetail.Map.Walls
{
    /// <summary>
    /// Stores successfully committed wall edits and coordinates
    /// undo and redo through WallConstructionService.
    ///
    /// A new edit recorded after an undo clears the redo stack.
    /// Failed undo or redo attempts leave history unchanged.
    /// </summary>
    public sealed class WallEditHistory
    {
        private readonly WallConstructionService wallService;

        private readonly Stack<WallEdit> undoStack =
            new Stack<WallEdit>();

        private readonly Stack<WallEdit> redoStack =
            new Stack<WallEdit>();


        public bool CanUndo =>
            undoStack.Count > 0;

        public bool CanRedo =>
            redoStack.Count > 0;

        public int UndoCount =>
            undoStack.Count;

        public int RedoCount =>
            redoStack.Count;


        public event Action HistoryChanged;


        public WallEditHistory(
            WallConstructionService wallService)
        {
            this.wallService =
                wallService
                ?? throw new ArgumentNullException(
                    nameof(wallService));
        }


        /// <summary>
        /// Records an exact, successfully committed wall edit.
        ///
        /// Empty edits are harmless no-ops and are not recorded.
        /// Recording a new edit creates a new timeline branch,
        /// clearing all redo entries.
        /// </summary>
        public void Record(
            WallEdit edit)
        {
            if (edit.IsEmpty)
            {
                return;
            }

            undoStack.Push(edit);
            redoStack.Clear();

            HistoryChanged?.Invoke();
        }


        public bool TryUndo(
            out WallHistoryResult result)
        {
            if (!CanUndo)
            {
                result =
                    WallHistoryResult.Rejected(
                        WallHistoryFailure.NothingToUndo);

                return false;
            }

            WallEdit originalEdit =
                undoStack.Peek();

            WallEdit inverseEdit =
                originalEdit.Inverse();

            WallBatchChangeResult applyResult =
                wallService.TryApplyEdit(
                    inverseEdit);

            if (!applyResult.Succeeded)
            {
                result =
                    WallHistoryResult.Rejected(
                        WallHistoryFailure.EditCouldNotBeApplied,
                        inverseEdit,
                        applyResult.Failure,
                        applyResult.FailedEdge);

                return false;
            }

            undoStack.Pop();
            redoStack.Push(originalEdit);

            result =
                WallHistoryResult.Success(
                    inverseEdit);

            HistoryChanged?.Invoke();

            return true;
        }


        public bool TryRedo(
            out WallHistoryResult result)
        {
            if (!CanRedo)
            {
                result =
                    WallHistoryResult.Rejected(
                        WallHistoryFailure.NothingToRedo);

                return false;
            }

            WallEdit originalEdit =
                redoStack.Peek();

            WallBatchChangeResult applyResult =
                wallService.TryApplyEdit(
                    originalEdit);

            if (!applyResult.Succeeded)
            {
                result =
                    WallHistoryResult.Rejected(
                        WallHistoryFailure.EditCouldNotBeApplied,
                        originalEdit,
                        applyResult.Failure,
                        applyResult.FailedEdge);

                return false;
            }

            redoStack.Pop();
            undoStack.Push(originalEdit);

            result =
                WallHistoryResult.Success(
                    originalEdit);

            HistoryChanged?.Invoke();

            return true;
        }


        public void Clear()
        {
            if (!CanUndo && !CanRedo)
            {
                return;
            }

            undoStack.Clear();
            redoStack.Clear();

            HistoryChanged?.Invoke();
        }
    }
}