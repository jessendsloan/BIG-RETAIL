using BigRetail.Map.Domain;

namespace BigRetail.Map.Floors
{
    /// <summary>
    /// Reports the outcome of one floor-finish command.
    /// </summary>
    public readonly struct FloorFinishChangeResult
    {
        public bool Succeeded =>
            Failure == FloorFinishChangeFailure.None;

        public bool Changed { get; }

        public FloorFinishChangeFailure Failure { get; }

        public GridPosition Cell { get; }

        public FloorFinishId EffectiveFinishId { get; }


        private FloorFinishChangeResult(
            bool changed,
            FloorFinishChangeFailure failure,
            GridPosition cell,
            FloorFinishId effectiveFinishId)
        {
            Changed = changed;
            Failure = failure;
            Cell = cell;
            EffectiveFinishId = effectiveFinishId;
        }


        internal static FloorFinishChangeResult Success(
            GridPosition cell,
            FloorFinishId effectiveFinishId,
            bool changed)
        {
            return new FloorFinishChangeResult(
                changed,
                FloorFinishChangeFailure.None,
                cell,
                effectiveFinishId);
        }

        internal static FloorFinishChangeResult Failed(
            GridPosition cell,
            FloorFinishChangeFailure failure)
        {
            return new FloorFinishChangeResult(
                false,
                failure,
                cell,
                default);
        }
    }
}
