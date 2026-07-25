using System;
using System.Collections.Generic;
using BigRetail.Map.Domain;

namespace BigRetail.Map.Floors
{
    /// <summary>
    /// Records an exact floor-state mutation.
    ///
    /// This is not the player's complete requested area.
    /// It contains only cells that genuinely changed.
    /// </summary>
    public readonly struct FloorEdit
    {
        private readonly GridPosition[] cells;


        public FloorEditKind Kind { get; }

        public IReadOnlyList<GridPosition> Cells =>
            cells ?? Array.Empty<GridPosition>();

        public int Count =>
            cells?.Length ?? 0;

        public bool IsEmpty =>
            Count == 0;


        private FloorEdit(
            FloorEditKind kind,
            GridPosition[] cells)
        {
            Kind = kind;
            this.cells = cells;
        }


        public static FloorEdit AddFloors(
            IReadOnlyList<GridPosition> cells)
        {
            return Create(
                FloorEditKind.AddFloors,
                cells);
        }


        public static FloorEdit RemoveFloors(
            IReadOnlyList<GridPosition> cells)
        {
            return Create(
                FloorEditKind.RemoveFloors,
                cells);
        }


        public FloorEdit Inverse()
        {
            if (IsEmpty)
            {
                return default;
            }

            FloorEditKind inverseKind =
                Kind == FloorEditKind.AddFloors
                    ? FloorEditKind.RemoveFloors
                    : FloorEditKind.AddFloors;

            // The private array is treated as immutable, so the
            // inverse can safely share the same cell collection.
            return new FloorEdit(
                inverseKind,
                cells);
        }


        private static FloorEdit Create(
            FloorEditKind kind,
            IReadOnlyList<GridPosition> sourceCells)
        {
            if (sourceCells == null)
            {
                throw new ArgumentNullException(
                    nameof(sourceCells));
            }

            if (sourceCells.Count == 0)
            {
                return default;
            }

            GridPosition[] copiedCells =
                new GridPosition[sourceCells.Count];

            HashSet<GridPosition> uniqueCells =
                new HashSet<GridPosition>();

            for (int index = 0;
                 index < sourceCells.Count;
                 index++)
            {
                GridPosition cell =
                    sourceCells[index];

                if (!uniqueCells.Add(cell))
                {
                    throw new ArgumentException(
                        $"A FloorEdit cannot contain duplicate cell " +
                        $"{cell}.",
                        nameof(sourceCells));
                }

                copiedCells[index] =
                    cell;
            }

            return new FloorEdit(
                kind,
                copiedCells);
        }


        public override string ToString()
        {
            return IsEmpty
                ? "Empty floor edit."
                : $"{Kind}: {Count} floor cell(s).";
        }
    }
}