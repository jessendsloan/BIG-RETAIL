using BigRetail.Map.Domain;

namespace BigRetail.Map.Walls
{
    /// <summary>
    /// Reports the outcome of one wall-face finish command.
    /// </summary>
    public readonly struct WallFinishChangeResult
    {
        public bool Succeeded =>
            Failure == WallFinishChangeFailure.None;

        public bool Changed { get; }
        public WallFinishChangeFailure Failure { get; }
        public CellEdge Edge { get; }
        public GridPosition FacingCell { get; }
        public WallFinishId EffectiveFinishId { get; }


        private WallFinishChangeResult(
            bool changed,
            WallFinishChangeFailure failure,
            CellEdge edge,
            GridPosition facingCell,
            WallFinishId effectiveFinishId)
        {
            Changed = changed;
            Failure = failure;
            Edge = edge;
            FacingCell = facingCell;
            EffectiveFinishId = effectiveFinishId;
        }


        internal static WallFinishChangeResult Success(
            CellEdge edge,
            GridPosition facingCell,
            WallFinishId effectiveFinishId,
            bool changed)
        {
            return new WallFinishChangeResult(
                changed,
                WallFinishChangeFailure.None,
                edge,
                facingCell,
                effectiveFinishId);
        }

        internal static WallFinishChangeResult Failed(
            CellEdge edge,
            GridPosition facingCell,
            WallFinishChangeFailure failure)
        {
            return new WallFinishChangeResult(
                false,
                failure,
                edge,
                facingCell,
                default);
        }
    }
}
