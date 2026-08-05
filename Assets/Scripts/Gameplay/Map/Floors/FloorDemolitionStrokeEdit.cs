using System;
using System.Collections.Generic;

namespace BigRetail.Map.Floors
{
    /// <summary>
    /// Records the exact Floor cells and finishes removed by one
    /// player-facing demolition stroke.
    /// </summary>
    public sealed class FloorDemolitionStrokeEdit
    {
        private readonly FloorCellSnapshot[] removedFloors;


        public IReadOnlyList<FloorCellSnapshot> RemovedFloors =>
            removedFloors;

        public int Count =>
            removedFloors.Length;

        public bool IsEmpty =>
            Count == 0;


        public FloorDemolitionStrokeEdit(
            IReadOnlyList<FloorCellSnapshot> removedFloors)
        {
            if (removedFloors == null)
            {
                throw new ArgumentNullException(
                    nameof(removedFloors));
            }

            this.removedFloors =
                new FloorCellSnapshot[removedFloors.Count];

            for (int index = 0;
                 index < removedFloors.Count;
                 index++)
            {
                this.removedFloors[index] =
                    removedFloors[index];
            }
        }
    }
}
