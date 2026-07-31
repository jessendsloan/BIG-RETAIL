using System;
using System.Collections.Generic;
using BigRetail.Map.Domain;

namespace BigRetail.Map.Floors
{
    /// <summary>
    /// Removes Floors while preserving their effective finishes for exact
    /// construction-history replay.
    /// </summary>
    public sealed class FloorDemolitionStrokeService
    {
        private readonly FloorConstructionService floorConstruction;
        private readonly FloorFinishService floorFinishes;


        public FloorDemolitionStrokeService(
            FloorConstructionService floorConstruction,
            FloorFinishService floorFinishes)
        {
            this.floorConstruction =
                floorConstruction
                ?? throw new ArgumentNullException(
                    nameof(floorConstruction));

            this.floorFinishes =
                floorFinishes
                ?? throw new ArgumentNullException(
                    nameof(floorFinishes));
        }


        public FloorDemolitionStrokeResult TryApply(
            IReadOnlyList<GridPosition> cells)
        {
            if (cells == null)
            {
                throw new ArgumentNullException(
                    nameof(cells));
            }

            if (cells.Count == 0)
            {
                return FloorDemolitionStrokeResult.Rejected(
                    0,
                    FloorDemolitionStrokeFailure.EmptyRequest);
            }

            HashSet<GridPosition> uniqueCells =
                new HashSet<GridPosition>();

            List<FloorCellSnapshot> removedFloors =
                new List<FloorCellSnapshot>();

            int alreadyEmptyCount = 0;

            for (int index = 0;
                 index < cells.Count;
                 index++)
            {
                GridPosition cell =
                    cells[index];

                if (!uniqueCells.Add(cell))
                {
                    continue;
                }

                if (!floorConstruction.HasFloor(cell))
                {
                    alreadyEmptyCount++;
                    continue;
                }

                removedFloors.Add(
                    new FloorCellSnapshot(
                        cell,
                        floorFinishes.GetEffectiveFinish(cell)));
            }

            if (removedFloors.Count == 0)
            {
                return FloorDemolitionStrokeResult.Success(
                    cells.Count,
                    uniqueCells.Count,
                    alreadyEmptyCount,
                    new FloorDemolitionStrokeEdit(
                        removedFloors));
            }

            GridPosition[] removedCells =
                new GridPosition[removedFloors.Count];

            for (int index = 0;
                 index < removedFloors.Count;
                 index++)
            {
                removedCells[index] =
                    removedFloors[index].Cell;
            }

            FloorClearResult clearResult =
                floorConstruction.TryClearFloors(
                    removedCells);

            if (!clearResult.Succeeded)
            {
                return FloorDemolitionStrokeResult.Rejected(
                    cells.Count,
                    FloorDemolitionStrokeFailure
                        .FloorClearRejected,
                    clearResult.FailedCell);
            }

            return FloorDemolitionStrokeResult.Success(
                cells.Count,
                uniqueCells.Count,
                alreadyEmptyCount,
                new FloorDemolitionStrokeEdit(
                    removedFloors));
        }
    }
}
