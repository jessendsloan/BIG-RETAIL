namespace BigRetail.StoreLayouts.Unity
{
    public enum StoreLayoutLoadFailure
    {
        None = 0,
        RuntimeUnavailable = 1,
        ValidationFailed = 2,
        ActiveDeliveries = 3,
        ApplyFailed = 4,
        RollbackFailed = 5
    }


    /// <summary>
    /// Exact outcome of one layout transaction. A failed preflight never
    /// mutates state; an apply failure reports whether the prior store was
    /// restored successfully.
    /// </summary>
    public sealed class StoreLayoutLoadResult
    {
        public bool Succeeded { get; }

        public StoreLayoutLoadFailure Failure { get; }

        public string Message { get; }

        public StoreDataValidationResult Validation { get; }

        public bool PreviousStateRestored { get; }


        private StoreLayoutLoadResult(
            bool succeeded,
            StoreLayoutLoadFailure failure,
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


        public static StoreLayoutLoadResult Success(
            string layoutId)
        {
            return new StoreLayoutLoadResult(
                true,
                StoreLayoutLoadFailure.None,
                $"Loaded store layout '{layoutId}'.",
                null,
                true);
        }


        public static StoreLayoutLoadResult Rejected(
            StoreLayoutLoadFailure failure,
            string message,
            StoreDataValidationResult validation = null,
            bool previousStateRestored = true)
        {
            return new StoreLayoutLoadResult(
                false,
                failure,
                message,
                validation,
                previousStateRestored);
        }
    }
}
