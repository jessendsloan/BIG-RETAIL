using BigRetail.Map.Domain;

namespace BigRetail.Map.Floors
{
    /// <summary>
    /// Describes the evaluation of one floor cell.
    /// </summary>
    public readonly struct FloorChangeResult
    {
        public bool Succeeded { get; }

        public GridPosition Cell { get; }

        public FloorChangeFailure Failure { get; }


        private FloorChangeResult(
            bool succeeded,
            GridPosition cell,
            FloorChangeFailure failure)
        {
            Succeeded = succeeded;
            Cell = cell;
            Failure = failure;
        }


        public static FloorChangeResult Success(
            GridPosition cell)
        {
            return new FloorChangeResult(
                true,
                cell,
                FloorChangeFailure.None);
        }


        public static FloorChangeResult Rejected(
            GridPosition cell,
            FloorChangeFailure failure)
        {
            return new FloorChangeResult(
                false,
                cell,
                failure);
        }


        public override string ToString()
        {
            return Succeeded
                ? $"Floor change accepted at {Cell}."
                : $"Floor change rejected at {Cell}: {Failure}.";
        }
    }
}