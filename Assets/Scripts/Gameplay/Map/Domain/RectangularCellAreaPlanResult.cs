using System;
using System.Collections.Generic;

namespace BigRetail.Map.Domain
{
    /// <summary>
    /// Describes the inclusive rectangular area between two
    /// grid positions on the same logical floor.
    ///
    /// Cells are stored in deterministic order:
    /// - Y increases first by row.
    /// - X increases within each row.
    /// </summary>
    public readonly struct RectangularCellAreaPlanResult
    {
        private readonly GridPosition[] cells;


        public bool Succeeded { get; }

        public RectangularCellAreaPlanFailure Failure { get; }

        public GridPosition StartCell { get; }

        public GridPosition EndCell { get; }


        public int MinimumX { get; }

        public int MaximumX { get; }

        public int MinimumY { get; }

        public int MaximumY { get; }

        public int Level { get; }


        public int Width =>
            Succeeded
                ? MaximumX - MinimumX + 1
                : 0;

        public int Height =>
            Succeeded
                ? MaximumY - MinimumY + 1
                : 0;

        public int CellCount =>
            cells?.Length ?? 0;

        public IReadOnlyList<GridPosition> Cells =>
            cells ?? Array.Empty<GridPosition>();


        private RectangularCellAreaPlanResult(
            bool succeeded,
            GridPosition startCell,
            GridPosition endCell,
            RectangularCellAreaPlanFailure failure,
            int minimumX,
            int maximumX,
            int minimumY,
            int maximumY,
            int level,
            GridPosition[] cells)
        {
            Succeeded = succeeded;
            StartCell = startCell;
            EndCell = endCell;
            Failure = failure;

            MinimumX = minimumX;
            MaximumX = maximumX;
            MinimumY = minimumY;
            MaximumY = maximumY;
            Level = level;

            this.cells =
                cells ?? Array.Empty<GridPosition>();
        }


        public static RectangularCellAreaPlanResult Success(
            GridPosition startCell,
            GridPosition endCell,
            int minimumX,
            int maximumX,
            int minimumY,
            int maximumY,
            int level,
            GridPosition[] cells)
        {
            if (cells == null)
            {
                throw new ArgumentNullException(
                    nameof(cells));
            }

            if (cells.Length == 0)
            {
                throw new ArgumentException(
                    "A successful rectangular cell-area plan " +
                    "must contain at least one cell.",
                    nameof(cells));
            }

            return new RectangularCellAreaPlanResult(
                true,
                startCell,
                endCell,
                RectangularCellAreaPlanFailure.None,
                minimumX,
                maximumX,
                minimumY,
                maximumY,
                level,
                cells);
        }


        public static RectangularCellAreaPlanResult Rejected(
            GridPosition startCell,
            GridPosition endCell,
            RectangularCellAreaPlanFailure failure)
        {
            if (failure
                == RectangularCellAreaPlanFailure.None)
            {
                throw new ArgumentException(
                    "A rejected rectangular cell-area plan " +
                    "requires a failure reason.",
                    nameof(failure));
            }

            return new RectangularCellAreaPlanResult(
                false,
                startCell,
                endCell,
                failure,
                0,
                0,
                0,
                0,
                startCell.Level,
                Array.Empty<GridPosition>());
        }


        public override string ToString()
        {
            if (!Succeeded)
            {
                return
                    $"Rectangular cell-area plan rejected: " +
                    $"{Failure}. Start: {StartCell}. " +
                    $"End: {EndCell}.";
            }

            return
                $"Rectangular cell area contains " +
                $"{CellCount} cell(s). " +
                $"Width: {Width}. Height: {Height}. " +
                $"Level: {Level}.";
        }
    }
}