using System;
using BigRetail.Map.Domain;

namespace BigRetail.Construction.Unity.Walls
{
    /// <summary>
    /// Describes one wall edge currently selected by the
    /// construction pointer.
    ///
    /// RequestedCell and RequestedDirection preserve the player's
    /// point of view.
    ///
    /// Edge contains the normalized identity used by WallState.
    /// </summary>
    public readonly struct WallTarget :
        IEquatable<WallTarget>
    {
        public GridPosition RequestedCell { get; }

        public CellEdgeDirection RequestedDirection { get; }

        public CellEdge Edge { get; }


        public WallTarget(
            GridPosition requestedCell,
            CellEdgeDirection requestedDirection)
        {
            RequestedCell =
                requestedCell;

            RequestedDirection =
                requestedDirection;

            Edge =
                new CellEdge(
                    requestedCell,
                    requestedDirection);
        }


        public bool Equals(
            WallTarget other)
        {
            return RequestedCell
                    == other.RequestedCell
                && RequestedDirection
                    == other.RequestedDirection
                && Edge
                    == other.Edge;
        }


        public override bool Equals(
            object obj)
        {
            return obj is WallTarget other
                && Equals(other);
        }


        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;

                hash =
                    (hash * 31)
                    + RequestedCell.GetHashCode();

                hash =
                    (hash * 31)
                    + (int)RequestedDirection;

                hash =
                    (hash * 31)
                    + Edge.GetHashCode();

                return hash;
            }
        }


        public override string ToString()
        {
            return
                $"Requested {RequestedCell}, " +
                $"{RequestedDirection}. " +
                $"Canonical: {Edge}.";
        }


        public static bool operator ==(
            WallTarget left,
            WallTarget right)
        {
            return left.Equals(right);
        }


        public static bool operator !=(
            WallTarget left,
            WallTarget right)
        {
            return !left.Equals(right);
        }
    }
}