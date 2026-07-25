namespace BigRetail.Map.Construction
{
    /// <summary>
    /// Describes one construction Undo or Redo request.
    /// </summary>
    public readonly struct ConstructionHistoryResult
    {
        public bool Succeeded { get; }

        public ConstructionHistoryFailure Failure { get; }

        public IReversibleConstructionAction Action { get; }

        public string ActionFailureReason { get; }


        private ConstructionHistoryResult(
            bool succeeded,
            ConstructionHistoryFailure failure,
            IReversibleConstructionAction action,
            string actionFailureReason)
        {
            Succeeded = succeeded;
            Failure = failure;
            Action = action;
            ActionFailureReason =
                actionFailureReason ?? string.Empty;
        }


        public static ConstructionHistoryResult Success(
            IReversibleConstructionAction action)
        {
            return new ConstructionHistoryResult(
                true,
                ConstructionHistoryFailure.None,
                action,
                string.Empty);
        }


        public static ConstructionHistoryResult Rejected(
            ConstructionHistoryFailure failure,
            IReversibleConstructionAction action = null,
            string actionFailureReason = null)
        {
            return new ConstructionHistoryResult(
                false,
                failure,
                action,
                actionFailureReason);
        }


        public override string ToString()
        {
            if (Succeeded)
            {
                return
                    $"Construction history operation succeeded: " +
                    $"{Action?.Description ?? "Unknown action"}.";
            }

            if (Failure
                == ConstructionHistoryFailure
                    .ActionCouldNotBeApplied)
            {
                return
                    $"Construction history operation failed because " +
                    $"the action could not be applied. " +
                    $"{ActionFailureReason}";
            }

            return
                $"Construction history operation rejected: " +
                $"{Failure}.";
        }
    }
}
