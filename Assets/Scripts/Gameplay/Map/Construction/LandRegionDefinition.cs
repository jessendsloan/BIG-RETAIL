using System.Collections.Generic;
using BigRetail.Map.Domain;

namespace BigRetail.Map.Construction
{
    /// <summary>
    /// Immutable geometry for one authored 32x32 Land Region.
    /// </summary>
    public sealed class LandRegionDefinition
    {
        public const int SideLength = 32;

        public const int CellCount = SideLength * SideLength;

        public LandRegionId Id { get; }

        public GridPosition MinimumCell { get; }

        public LandRegionDefinition(
            LandRegionId id,
            GridPosition minimumCell)
        {
            Id = id;
            MinimumCell = minimumCell;
        }

        public bool Contains(GridPosition position)
        {
            return position.Level == MinimumCell.Level
                && position.X >= MinimumCell.X
                && position.X < MinimumCell.X + SideLength
                && position.Y >= MinimumCell.Y
                && position.Y < MinimumCell.Y + SideLength;
        }

        public IEnumerable<GridPosition> EnumerateCells()
        {
            for (int yOffset = 0; yOffset < SideLength; yOffset++)
            {
                for (int xOffset = 0; xOffset < SideLength; xOffset++)
                {
                    yield return MinimumCell.Offset(xOffset, yOffset);
                }
            }
        }
    }
}
