using System;
using System.Collections.Generic;
using BigRetail.Map.Domain;

namespace BigRetail.Map.Sidewalks
{
    public readonly struct SidewalkEdit
    {
        private readonly GridPosition[] cells;


        public SidewalkEditKind Kind { get; }

        public IReadOnlyList<GridPosition> Cells =>
            cells ?? Array.Empty<GridPosition>();

        public int Count =>
            cells?.Length ?? 0;

        public bool IsEmpty =>
            Count == 0;


        private SidewalkEdit(
            SidewalkEditKind kind,
            GridPosition[] cells)
        {
            Kind = kind;
            this.cells = cells;
        }


        public static SidewalkEdit AddSidewalks(
            IReadOnlyList<GridPosition> cells)
        {
            return Create(
                SidewalkEditKind.AddSidewalks,
                cells);
        }


        public static SidewalkEdit RemoveSidewalks(
            IReadOnlyList<GridPosition> cells)
        {
            return Create(
                SidewalkEditKind.RemoveSidewalks,
                cells);
        }


        public SidewalkEdit Inverse()
        {
            if (IsEmpty)
            {
                return default;
            }

            return new SidewalkEdit(
                Kind == SidewalkEditKind.AddSidewalks
                    ? SidewalkEditKind.RemoveSidewalks
                    : SidewalkEditKind.AddSidewalks,
                cells);
        }


        private static SidewalkEdit Create(
            SidewalkEditKind kind,
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

            GridPosition[] copy =
                new GridPosition[sourceCells.Count];

            HashSet<GridPosition> uniqueCells =
                new HashSet<GridPosition>();

            for (int index = 0;
                 index < sourceCells.Count;
                 index++)
            {
                GridPosition cell = sourceCells[index];

                if (!uniqueCells.Add(cell))
                {
                    throw new ArgumentException(
                        $"A SidewalkEdit cannot contain duplicate cell {cell}.",
                        nameof(sourceCells));
                }

                copy[index] = cell;
            }

            return new SidewalkEdit(kind, copy);
        }
    }
}
