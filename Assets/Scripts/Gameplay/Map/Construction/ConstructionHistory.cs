using System;
using System.Collections.Generic;

namespace BigRetail.Map.Construction
{
    /// <summary>
    /// Stores reversible construction transactions without knowing
    /// which construction domain produced them.
    ///
    /// Standard mode retains one transaction. Unlimited mode retains
    /// the complete session timeline. Failed replay leaves both the
    /// authoritative model and the history position unchanged.
    /// </summary>
    public sealed class ConstructionHistory
    {
        private readonly Stack<IReversibleConstructionAction>
            undoStack =
                new Stack<IReversibleConstructionAction>();

        private readonly Stack<IReversibleConstructionAction>
            redoStack =
                new Stack<IReversibleConstructionAction>();


        public ConstructionHistoryMode Mode { get; }

        public bool CanUndo =>
            undoStack.Count > 0;

        public bool CanRedo =>
            redoStack.Count > 0;

        public int UndoCount =>
            undoStack.Count;

        public int RedoCount =>
            redoStack.Count;


        public event Action HistoryChanged;


        public ConstructionHistory(
            ConstructionHistoryMode mode =
                ConstructionHistoryMode.Standard)
        {
            if (mode != ConstructionHistoryMode.Standard
                && mode != ConstructionHistoryMode.Unlimited)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(mode),
                    mode,
                    "Unsupported construction history mode.");
            }

            Mode = mode;
        }


        /// <summary>
        /// Records one successful, non-empty construction transaction.
        ///
        /// Recording creates a new timeline branch and clears Redo.
        /// In Standard mode it also replaces the previous Undo entry.
        /// </summary>
        public void Record(
            IReversibleConstructionAction action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(
                    nameof(action));
            }

            if (action.ChangeCount <= 0)
            {
                throw new ArgumentException(
                    "A construction history action must represent at " +
                    "least one state change.",
                    nameof(action));
            }

            if (Mode == ConstructionHistoryMode.Standard)
            {
                undoStack.Clear();
            }

            undoStack.Push(action);
            redoStack.Clear();

            HistoryChanged?.Invoke();
        }


        public bool TryUndo(
            out ConstructionHistoryResult result)
        {
            if (!CanUndo)
            {
                result =
                    ConstructionHistoryResult.Rejected(
                        ConstructionHistoryFailure.NothingToUndo);

                return false;
            }

            IReversibleConstructionAction action =
                undoStack.Peek();

            ConstructionActionResult actionResult =
                action.TryUndo();

            if (!actionResult.Succeeded)
            {
                result =
                    ConstructionHistoryResult.Rejected(
                        ConstructionHistoryFailure
                            .ActionCouldNotBeApplied,
                        action,
                        actionResult.FailureReason);

                return false;
            }

            undoStack.Pop();
            redoStack.Push(action);

            result =
                ConstructionHistoryResult.Success(
                    action);

            HistoryChanged?.Invoke();

            return true;
        }


        public bool TryRedo(
            out ConstructionHistoryResult result)
        {
            if (!CanRedo)
            {
                result =
                    ConstructionHistoryResult.Rejected(
                        ConstructionHistoryFailure.NothingToRedo);

                return false;
            }

            IReversibleConstructionAction action =
                redoStack.Peek();

            ConstructionActionResult actionResult =
                action.TryRedo();

            if (!actionResult.Succeeded)
            {
                result =
                    ConstructionHistoryResult.Rejected(
                        ConstructionHistoryFailure
                            .ActionCouldNotBeApplied,
                        action,
                        actionResult.FailureReason);

                return false;
            }

            redoStack.Pop();
            undoStack.Push(action);

            result =
                ConstructionHistoryResult.Success(
                    action);

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
