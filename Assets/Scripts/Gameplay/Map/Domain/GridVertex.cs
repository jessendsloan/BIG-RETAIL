using System;

namespace BigRetail.Map.Domain
{
    /// <summary>
    /// Identifies one corner shared by neighboring logical grid cells.
    ///
    /// A cell at (X, Y) is bounded by these four vertices:
    /// - (X - 1, Y - 1)
    /// - (X,     Y - 1)
    /// - (X - 1, Y)
    /// - (X,     Y)
    ///
    /// Level identifies the logical building floor.
    /// </summary>
    public readonly struct GridVertex :
        IEquatable<GridVertex>
    {
        public int X { get; }
        public int Y { get; }
        public int Level { get; }


        public GridVertex(
            int x,
            int y,
            int level = 0)
        {
            X = x;
            Y = y;
            Level = level;
        }


        public GridVertex Offset(
            int xOffset,
            int yOffset,
            int levelOffset = 0)
        {
            return new GridVertex(
                X + xOffset,
                Y + yOffset,
                Level + levelOffset);
        }


        public bool Equals(
            GridVertex other)
        {
            return X == other.X
                && Y == other.Y
                && Level == other.Level;
        }


        public override bool Equals(
            object obj)
        {
            return obj is GridVertex other
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
            return $"Vertex ({X}, {Y}, Level {Level})";
        }


        public static bool operator ==(
            GridVertex left,
            GridVertex right)
        {
            return left.Equals(right);
        }


        public static bool operator !=(
            GridVertex left,
            GridVertex right)
        {
            return !left.Equals(right);
        }
    }
}
