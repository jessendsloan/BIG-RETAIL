namespace BigRetail.Map.Construction
{
    /// <summary>
    /// Describes whether one domain-owned construction action could
    /// apply its undo or redo operation.
    /// </summary>
    public readonly struct ConstructionActionResult
    {
        public bool Succeeded { get; }

        public string FailureReason { get; }


        private ConstructionActionResult(
            bool succeeded,
            string failureReason)
        {
            Succeeded = succeeded;
            FailureReason = failureReason;
        }


        public static ConstructionActionResult Success()
        {
            return new ConstructionActionResult(
                true,
                string.Empty);
        }


        public static ConstructionActionResult Rejected(
            string failureReason)
        {
            return new ConstructionActionResult(
                false,
                failureReason ?? string.Empty);
        }
    }
}
