using System;
using System.Collections.Generic;
using BigRetail.Map.Domain;

namespace BigRetail.Map.Foundations
{
    /// <summary>
    /// Records an exact foundation-state mutation.
    ///
    /// This contains only cells that genuinely changed, not the player's
    /// complete requested area.
    /// </summary>
    public readonly struct FoundationEdit
    {
        private readonly GridPosition[] cells;


        public FoundationEditKind Kind { get; }

        public IReadOnlyList<GridPosition> Cells =>
            cells ?? Array.Empty<GridPosition>();

        public int Count =>
            cells?.Length ?? 0;

        public bool IsEmpty =>
            Count == 0;


        private FoundationEdit(
            FoundationEditKind kind,
            GridPosition[] cells)
        {
            Kind = kind;
            this.cells = cells;
        }


        public static FoundationEdit AddFoundations(
            IReadOnlyList<GridPosition> cells)
        {
            return Create(
                FoundationEditKind.AddFoundations,
                cells);
        }


        public static FoundationEdit RemoveFoundations(
            IReadOnlyList<GridPosition> cells)
        {
            return Create(
                FoundationEditKind.RemoveFoundations,
                cells);
        }


        public FoundationEdit Inverse()
        {
            if (IsEmpty)
            {
                return default;
            }

            FoundationEditKind inverseKind =
                Kind == FoundationEditKind.AddFoundations
                    ? FoundationEditKind.RemoveFoundations
                    : FoundationEditKind.AddFoundations;

            return new FoundationEdit(
                inverseKind,
                cells);
        }


        private static FoundationEdit Create(
            FoundationEditKind kind,
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
                        $"A FoundationEdit cannot contain duplicate cell {cell}.",
                        nameof(sourceCells));
                }

                copiedCells[index] =
                    cell;
            }

            return new FoundationEdit(
                kind,
                copiedCells);
        }


        public override string ToString()
        {
            return IsEmpty
                ? "Empty foundation edit."
                : $"{Kind}: {Count} foundation cell(s).";
        }
    }
}
