namespace BigRetail.StoreLayouts.Unity
{
    public enum StoreScenarioLoadFailure
    {
        None = 0,
        RuntimeUnavailable = 1,
        ValidationFailed = 2,
        UnsupportedContent = 3,
        ApplyFailed = 4,
        RollbackFailed = 5
    }


    /// <summary>
    /// Exact outcome of one scenario transaction. Failed preflight never
    /// mutates runtime state, while apply failures report rollback status.
    /// </summary>
    public sealed class StoreScenarioLoadResult
    {
        public bool Succeeded { get; }

        public StoreScenarioLoadFailure Failure { get; }

        public string Message { get; }

        public StoreDataValidationResult Validation { get; }

        public bool PreviousStateRestored { get; }


        private StoreScenarioLoadResult(
            bool succeeded,
            StoreScenarioLoadFailure failure,
            string message,
            StoreDataValidationResult validation,
            bool previousStateRestored)
        {
            Succeeded = succeeded;
            Failure = failure;
            Message = message ?? string.Empty;
            Validation = validation;
            PreviousStateRestored = previousStateRestored;
        }


        public static StoreScenarioLoadResult Success(
            string scenarioId)
        {
            return new StoreScenarioLoadResult(
                true,
                StoreScenarioLoadFailure.None,
                $"Loaded store scenario '{scenarioId}'.",
                null,
                true);
        }

        public static StoreScenarioLoadResult Rejected(
            StoreScenarioLoadFailure failure,
            string message,
            StoreDataValidationResult validation = null,
            bool previousStateRestored = true)
        {
            return new StoreScenarioLoadResult(
                false,
                failure,
                message,
                validation,
                previousStateRestored);
        }
    }
}
