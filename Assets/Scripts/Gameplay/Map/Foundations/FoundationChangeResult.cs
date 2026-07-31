using BigRetail.Map.Domain;

namespace BigRetail.Map.Foundations
{
    /// <summary>
    /// Describes the evaluation of one foundation cell.
    /// </summary>
    public readonly struct FoundationChangeResult
    {
        public bool Succeeded { get; }

        public GridPosition Cell { get; }

        public FoundationChangeFailure Failure { get; }


        private FoundationChangeResult(
            bool succeeded,
            GridPosition cell,
            FoundationChangeFailure failure)
        {
            Succeeded = succeeded;
            Cell = cell;
            Failure = failure;
        }


        public static FoundationChangeResult Success(
            GridPosition cell)
        {
            return new FoundationChangeResult(
                true,
                cell,
                FoundationChangeFailure.None);
        }


        public static FoundationChangeResult Rejected(
            GridPosition cell,
            FoundationChangeFailure failure)
        {
            return new FoundationChangeResult(
                false,
                cell,
                failure);
        }


        public override string ToString()
        {
            return Succeeded
                ? $"Foundation change accepted at {Cell}."
                : $"Foundation change rejected at {Cell}: {Failure}.";
        }
    }
}
