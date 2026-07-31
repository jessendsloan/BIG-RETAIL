using System;
using System.Collections.Generic;
using BigRetail.Map.Domain;

namespace BigRetail.Map.Floors
{
    /// <summary>
    /// Exact structural and finish changes committed by one Floor-tool stroke.
    /// </summary>
    public sealed class FloorAppearanceStrokeEdit
    {
        private readonly GridPosition[] createdFloors;
        private readonly FloorCellFinishEdit[] finishEdits;


        public IReadOnlyList<GridPosition> CreatedFloors =>
            createdFloors;

        public IReadOnlyList<FloorCellFinishEdit> FinishEdits =>
            finishEdits;

        public int CreatedFloorCount =>
            createdFloors.Length;

        public int FinishChangeCount =>
            finishEdits.Length;

        public int ChangeCount =>
            CreatedFloorCount + FinishChangeCount;

        public bool IsEmpty =>
            ChangeCount == 0;


        public FloorAppearanceStrokeEdit(
            IReadOnlyList<GridPosition> createdFloors,
            IReadOnlyList<FloorCellFinishEdit> finishEdits)
        {
            if (createdFloors == null)
            {
                throw new ArgumentNullException(
                    nameof(createdFloors));
            }

            if (finishEdits == null)
            {
                throw new ArgumentNullException(
                    nameof(finishEdits));
            }

            this.createdFloors =
                CopyUniqueCells(createdFloors);

            this.finishEdits =
                CopyUniqueFinishEdits(finishEdits);
        }


        private static GridPosition[] CopyUniqueCells(
            IReadOnlyList<GridPosition> cells)
        {
            GridPosition[] copy =
                new GridPosition[cells.Count];

            HashSet<GridPosition> uniqueCells =
                new HashSet<GridPosition>();

            for (int index = 0;
                 index < cells.Count;
                 index++)
            {
                GridPosition cell =
                    cells[index];

                if (!uniqueCells.Add(cell))
                {
                    throw new ArgumentException(
                        $"Floor appearance edit contains duplicate cell '{cell}'.",
                        nameof(cells));
                }

                copy[index] = cell;
            }

            return copy;
        }

        private static FloorCellFinishEdit[] CopyUniqueFinishEdits(
            IReadOnlyList<FloorCellFinishEdit> edits)
        {
            FloorCellFinishEdit[] copy =
                new FloorCellFinishEdit[edits.Count];

            HashSet<GridPosition> uniqueCells =
                new HashSet<GridPosition>();

            for (int index = 0;
                 index < edits.Count;
                 index++)
            {
                FloorCellFinishEdit edit =
                    edits[index];

                if (!uniqueCells.Add(edit.Cell))
                {
                    throw new ArgumentException(
                        $"Floor appearance edit contains duplicate finish cell '{edit.Cell}'.",
                        nameof(edits));
                }

                copy[index] = edit;
            }

            return copy;
        }
    }
}
