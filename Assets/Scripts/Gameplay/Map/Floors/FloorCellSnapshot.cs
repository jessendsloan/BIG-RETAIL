using BigRetail.Map.Domain;

namespace BigRetail.Map.Floors
{
    /// <summary>
    /// Captures one constructed Floor and its effective finish.
    /// </summary>
    public readonly struct FloorCellSnapshot
    {
        public GridPosition Cell { get; }

        public FloorFinishId FinishId { get; }


        public FloorCellSnapshot(
            GridPosition cell,
            FloorFinishId finishId)
        {
            Cell = cell;
            FinishId = finishId;
        }
    }
}
