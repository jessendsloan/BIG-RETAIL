using BigRetail.Map.Domain;

namespace BigRetail.Map.Sidewalks
{
    public readonly struct SidewalkChangeResult
    {
        public bool Succeeded { get; }

        public GridPosition Cell { get; }

        public SidewalkChangeFailure Failure { get; }


        private SidewalkChangeResult(
            bool succeeded,
            GridPosition cell,
            SidewalkChangeFailure failure)
        {
            Succeeded = succeeded;
            Cell = cell;
            Failure = failure;
        }


        public static SidewalkChangeResult Success(GridPosition cell)
        {
            return new SidewalkChangeResult(
                true,
                cell,
                SidewalkChangeFailure.None);
        }


        public static SidewalkChangeResult Rejected(
            GridPosition cell,
            SidewalkChangeFailure failure)
        {
            return new SidewalkChangeResult(
                false,
                cell,
                failure);
        }
    }
}
