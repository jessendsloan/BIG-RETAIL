using System;

namespace BigRetail.Map.Domain
{
    /// <summary>
    /// Produces an inclusive rectangular collection of grid cells
    /// between two positions on the same logical floor.
    ///
    /// Drag direction does not affect the resulting cell order.
    ///
    /// Returned ordering:
    /// - Minimum Y through maximum Y.
    /// - Within each Y row, minimum X through maximum X.
    /// </summary>
    public static class RectangularCellAreaPlanner
    {
        public static RectangularCellAreaPlanResult Plan(
            GridPosition startCell,
            GridPosition endCell)
        {
            if (startCell.Level != endCell.Level)
            {
                return RectangularCellAreaPlanResult.Rejected(
                    startCell,
                    endCell,
                    RectangularCellAreaPlanFailure.DifferentLevel);
            }

            int minimumX =
                Math.Min(
                    startCell.X,
                    endCell.X);

            int maximumX =
                Math.Max(
                    startCell.X,
                    endCell.X);

            int minimumY =
                Math.Min(
                    startCell.Y,
                    endCell.Y);

            int maximumY =
                Math.Max(
                    startCell.Y,
                    endCell.Y);

            int width =
                maximumX - minimumX + 1;

            int height =
                maximumY - minimumY + 1;

            int cellCount =
                checked(width * height);

            GridPosition[] cells =
                new GridPosition[cellCount];

            int outputIndex = 0;

            for (int y = minimumY;
                 y <= maximumY;
                 y++)
            {
                for (int x = minimumX;
                     x <= maximumX;
                     x++)
                {
                    cells[outputIndex] =
                        new GridPosition(
                            x,
                            y,
                            startCell.Level);

                    outputIndex++;
                }
            }

            return RectangularCellAreaPlanResult.Success(
                startCell,
                endCell,
                minimumX,
                maximumX,
                minimumY,
                maximumY,
                startCell.Level,
                cells);
        }
    }
}