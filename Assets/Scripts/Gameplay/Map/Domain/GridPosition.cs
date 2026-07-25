using System;

namespace BigRetail.Map.Domain
{
    /// <summary>
    /// Identifies one logical cell in a grid map.
    ///
    /// X and Y identify the cell on a floor.
    /// Level identifies which floor the cell belongs to.
    /// </summary>
    public readonly struct GridPosition : IEquatable<GridPosition>
    {
        public int X { get; }
        public int Y { get; }
        public int Level { get; }

        public GridPosition(
            int x,
            int y,
            int level = 0)
        {
            X = x;
            Y = y;
            Level = level;
        }

        /// <summary>
        /// Creates another grid position relative to this one.
        /// </summary>
        public GridPosition Offset(
            int xOffset,
            int yOffset,
            int levelOffset = 0)
        {
            return new GridPosition(
                X + xOffset,
                Y + yOffset,
                Level + levelOffset);
        }

        public bool Equals(GridPosition other)
        {
            return X == other.X
                && Y == other.Y
                && Level == other.Level;
        }

        public override bool Equals(object obj)
        {
            return obj is GridPosition other
                && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;

                hash = (hash * 31) + X;
                hash = (hash * 31) + Y;
                hash = (hash * 31) + Level;

                return hash;
            }
        }

        public override string ToString()
        {
            return $"({X}, {Y}, Level {Level})";
        }

        public static bool operator ==(
            GridPosition left,
            GridPosition right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            GridPosition left,
            GridPosition right)
        {
            return !left.Equals(right);
        }
    }
}