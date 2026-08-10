using System;
using System.Collections.Generic;
using BigRetail.Map.Domain;

namespace BigRetail.Map.Fixtures
{
    /// <summary>
    /// One resolved rectangular fixture footprint in world-grid space.
    /// </summary>
    public sealed class FixtureFootprint
    {
        private readonly GridPosition[] cells;


        public GridPosition AnchorCell { get; }

        public FixtureOrientation Orientation { get; }

        public int WidthInCells { get; }

        public int DepthInCells { get; }

        public int CellCount =>
            cells.Length;

        public IReadOnlyList<GridPosition> Cells =>
            cells;


        internal FixtureFootprint(
            GridPosition anchorCell,
            FixtureOrientation orientation,
            int widthInCells,
            int depthInCells,
            IReadOnlyList<GridPosition> cells)
        {
            if (!orientation.IsSupported())
            {
                throw new ArgumentOutOfRangeException(
                    nameof(orientation),
                    orientation,
                    "The fixture orientation is not supported.");
            }

            if (widthInCells <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(widthInCells));
            }

            if (depthInCells <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(depthInCells));
            }

            if (cells == null)
            {
                throw new ArgumentNullException(
                    nameof(cells));
            }

            if (cells.Count != widthInCells * depthInCells)
            {
                throw new ArgumentException(
                    "Fixture footprint cell count does not match its bounds.",
                    nameof(cells));
            }

            this.cells =
                new GridPosition[cells.Count];

            for (int index = 0;
                 index < cells.Count;
                 index++)
            {
                this.cells[index] = cells[index];
            }

            AnchorCell = anchorCell;
            Orientation = orientation;
            WidthInCells = widthInCells;
            DepthInCells = depthInCells;
        }


        public GridPosition GetCell(
            int index)
        {
            if (index < 0
                || index >= cells.Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index));
            }

            return cells[index];
        }

        public bool ContainsCell(
            GridPosition cell)
        {
            for (int index = 0;
                 index < cells.Length;
                 index++)
            {
                if (cells[index] == cell)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
