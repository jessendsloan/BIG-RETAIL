using System;

namespace BigRetail.Map.Construction
{
    /// <summary>
    /// Stable location of one Land Region within the property's 3x3 layout.
    /// </summary>
    public readonly struct LandRegionId : IEquatable<LandRegionId>
    {
        public int Column { get; }

        public int Row { get; }

        public LandRegionId(int column, int row)
        {
            Column = column;
            Row = row;
        }

        public bool Equals(LandRegionId other)
        {
            return Column == other.Column
                && Row == other.Row;
        }

        public override bool Equals(object obj)
        {
            return obj is LandRegionId other
                && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Column * 397) ^ Row;
            }
        }

        public override string ToString()
        {
            return $"Land Region ({Column}, {Row})";
        }

        public static bool operator ==(
            LandRegionId left,
            LandRegionId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            LandRegionId left,
            LandRegionId right)
        {
            return !left.Equals(right);
        }
    }
}
