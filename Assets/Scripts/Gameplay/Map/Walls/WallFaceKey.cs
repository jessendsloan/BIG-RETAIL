using System;
using BigRetail.Map.Domain;

namespace BigRetail.Map.Walls
{
    /// <summary>
    /// Identifies one independently finishable side of a structural wall.
    /// </summary>
    public readonly struct WallFaceKey :
        IEquatable<WallFaceKey>
    {
        public CellEdge Edge { get; }
        public GridPosition FacingCell { get; }


        public WallFaceKey(
            CellEdge edge,
            GridPosition facingCell)
        {
            if (!edge.TouchesCell(facingCell))
            {
                throw new ArgumentException(
                    $"Cell {facingCell} does not touch wall edge {edge}.",
                    nameof(facingCell));
            }

            Edge = edge;
            FacingCell = facingCell;
        }


        public bool Equals(
            WallFaceKey other)
        {
            return Edge.Equals(other.Edge)
                && FacingCell.Equals(other.FacingCell);
        }

        public override bool Equals(
            object obj)
        {
            return obj is WallFaceKey other
                && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;

                hash =
                    (hash * 31)
                    + Edge.GetHashCode();

                hash =
                    (hash * 31)
                    + FacingCell.GetHashCode();

                return hash;
            }
        }

        public override string ToString()
        {
            return $"{Edge} facing {FacingCell}";
        }


        public static bool operator ==(
            WallFaceKey left,
            WallFaceKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            WallFaceKey left,
            WallFaceKey right)
        {
            return !left.Equals(right);
        }
    }
}
