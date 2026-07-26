using System;
using BigRetail.Map.Domain;

namespace BigRetail.Map.View
{
    /// <summary>
    /// Describes the rectangular logical envelope used to project one
    /// map level between the four isometric view orientations.
    ///
    /// The actual map may be irregular inside this envelope. Rotation
    /// changes presentation coordinates only and never fills invalid
    /// logical cells.
    /// </summary>
    public readonly struct IsometricMapFootprint :
        IEquatable<IsometricMapFootprint>
    {
        public int MinimumX { get; }
        public int MinimumY { get; }
        public int MaximumX { get; }
        public int MaximumY { get; }
        public int LogicalLevel { get; }

        public int Width =>
            MaximumX - MinimumX + 1;

        public int Height =>
            MaximumY - MinimumY + 1;

        public IsometricMapFootprint(
            int minimumX,
            int minimumY,
            int maximumX,
            int maximumY,
            int logicalLevel = 0)
        {
            if (maximumX < minimumX)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumX),
                    maximumX,
                    "Maximum X cannot be less than minimum X.");
            }

            if (maximumY < minimumY)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumY),
                    maximumY,
                    "Maximum Y cannot be less than minimum Y.");
            }

            MinimumX = minimumX;
            MinimumY = minimumY;
            MaximumX = maximumX;
            MaximumY = maximumY;
            LogicalLevel = logicalLevel;
        }

        public static IsometricMapFootprint FromMapDefinition(
            GridMapDefinition mapDefinition,
            int logicalLevel)
        {
            if (mapDefinition == null)
            {
                throw new ArgumentNullException(
                    nameof(mapDefinition));
            }

            bool foundCell = false;

            int minimumX = int.MaxValue;
            int minimumY = int.MaxValue;
            int maximumX = int.MinValue;
            int maximumY = int.MinValue;

            foreach (
                GridPosition cell
                in mapDefinition.EnumerateValidCells())
            {
                if (cell.Level != logicalLevel)
                {
                    continue;
                }

                foundCell = true;

                minimumX =
                    Math.Min(
                        minimumX,
                        cell.X);

                minimumY =
                    Math.Min(
                        minimumY,
                        cell.Y);

                maximumX =
                    Math.Max(
                        maximumX,
                        cell.X);

                maximumY =
                    Math.Max(
                        maximumY,
                        cell.Y);
            }

            if (!foundCell)
            {
                throw new InvalidOperationException(
                    $"Map '{mapDefinition.MapId}' contains no cells " +
                    $"on logical level {logicalLevel}.");
            }

            return new IsometricMapFootprint(
                minimumX,
                minimumY,
                maximumX,
                maximumY,
                logicalLevel);
        }

        public bool Equals(
            IsometricMapFootprint other)
        {
            return MinimumX == other.MinimumX
                && MinimumY == other.MinimumY
                && MaximumX == other.MaximumX
                && MaximumY == other.MaximumY
                && LogicalLevel == other.LogicalLevel;
        }

        public override bool Equals(
            object obj)
        {
            return obj is IsometricMapFootprint other
                && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;

                hash = (hash * 31) + MinimumX;
                hash = (hash * 31) + MinimumY;
                hash = (hash * 31) + MaximumX;
                hash = (hash * 31) + MaximumY;
                hash = (hash * 31) + LogicalLevel;

                return hash;
            }
        }
    }
}
