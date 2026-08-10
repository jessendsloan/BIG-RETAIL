using System;
using BigRetail.Map.Domain;

namespace BigRetail.Map.Fixtures
{
    /// <summary>
    /// Resolves an authored fixture footprint into canonical world-grid
    /// cells. Opposite orientations share bounds while East and West swap
    /// the authored width and depth.
    /// </summary>
    public static class FixtureFootprintResolver
    {
        public static FixtureFootprint Resolve(
            FixtureDefinition definition,
            GridPosition anchorCell,
            FixtureOrientation orientation)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(
                    nameof(definition));
            }

            if (!orientation.IsSupported())
            {
                throw new ArgumentOutOfRangeException(
                    nameof(orientation),
                    orientation,
                    "The fixture orientation is not supported.");
            }

            bool swapsAxes =
                orientation == FixtureOrientation.East
                || orientation == FixtureOrientation.West;

            int widthInCells =
                swapsAxes
                    ? definition.DepthInCells
                    : definition.WidthInCells;

            int depthInCells =
                swapsAxes
                    ? definition.WidthInCells
                    : definition.DepthInCells;

            GridPosition[] cells =
                new GridPosition[widthInCells * depthInCells];

            int index = 0;

            for (int yOffset = 0;
                 yOffset < depthInCells;
                 yOffset++)
            {
                for (int xOffset = 0;
                     xOffset < widthInCells;
                     xOffset++)
                {
                    cells[index] =
                        anchorCell.Offset(
                            xOffset,
                            yOffset);

                    index++;
                }
            }

            return new FixtureFootprint(
                anchorCell,
                orientation,
                widthInCells,
                depthInCells,
                cells);
        }
    }
}
