using System;

namespace BigRetail.Map.Domain
{
    /// <summary>
    /// Identifies one unique edge shared by two neighboring map cells.
    ///
    /// Opposite descriptions of the same physical edge are normalized
    /// into one consistent value.
    ///
    /// For example:
    ///
    /// Cell A, NorthEast
    /// and
    /// Cell B, SouthWest
    ///
    /// represent the same CellEdge.
    /// </summary>
    public readonly struct CellEdge : IEquatable<CellEdge>
    {
        /// <summary>
        /// The canonical cell used to identify and store this edge.
        /// </summary>
        public GridPosition AnchorCell { get; }

        /// <summary>
        /// The normalized direction used to identify and store this edge.
        ///
        /// A constructed CellEdge will always normalize to either:
        /// - NorthEast
        /// - NorthWest
        /// </summary>
        public CellEdgeDirection CanonicalDirection { get; }

        /// <summary>
        /// The first logical cell touching this edge.
        /// </summary>
        public GridPosition FirstCell => AnchorCell;

        /// <summary>
        /// The neighboring logical cell on the opposite side
        /// of this edge.
        /// </summary>
        public GridPosition SecondCell
        {
            get
            {
                switch (CanonicalDirection)
                {
                    case CellEdgeDirection.NorthEast:
                        return AnchorCell.Offset(1, 0);

                    case CellEdgeDirection.NorthWest:
                        return AnchorCell.Offset(0, 1);

                    default:
                        throw new InvalidOperationException(
                            "A normalized CellEdge must use " +
                            "NorthEast or NorthWest.");
                }
            }
        }

        /// <summary>
        /// One endpoint of this edge in the logical vertex lattice.
        /// </summary>
        public GridVertex FirstVertex
        {
            get
            {
                switch (CanonicalDirection)
                {
                    case CellEdgeDirection.NorthEast:
                        return new GridVertex(
                            AnchorCell.X,
                            AnchorCell.Y - 1,
                            AnchorCell.Level);

                    case CellEdgeDirection.NorthWest:
                        return new GridVertex(
                            AnchorCell.X - 1,
                            AnchorCell.Y,
                            AnchorCell.Level);

                    default:
                        throw new InvalidOperationException(
                            "A normalized CellEdge must use " +
                            "NorthEast or NorthWest.");
                }
            }
        }

        /// <summary>
        /// The other endpoint of this edge in the logical vertex lattice.
        /// </summary>
        public GridVertex SecondVertex =>
            new GridVertex(
                AnchorCell.X,
                AnchorCell.Y,
                AnchorCell.Level);


        public CellEdge(
            GridPosition cell,
            CellEdgeDirection direction)
        {
            switch (direction)
            {
                case CellEdgeDirection.NorthEast:
                    AnchorCell = cell;
                    CanonicalDirection =
                        CellEdgeDirection.NorthEast;
                    break;

                case CellEdgeDirection.NorthWest:
                    AnchorCell = cell;
                    CanonicalDirection =
                        CellEdgeDirection.NorthWest;
                    break;

                case CellEdgeDirection.SouthEast:
                    // The SouthEast edge of this cell is the
                    // NorthWest edge of the neighboring cell.
                    AnchorCell = cell.Offset(0, -1);
                    CanonicalDirection =
                        CellEdgeDirection.NorthWest;
                    break;

                case CellEdgeDirection.SouthWest:
                    // The SouthWest edge of this cell is the
                    // NorthEast edge of the neighboring cell.
                    AnchorCell = cell.Offset(-1, 0);
                    CanonicalDirection =
                        CellEdgeDirection.NorthEast;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(direction),
                        direction,
                        "Unsupported cell-edge direction.");
            }
        }

        /// <summary>
        /// Creates the unique edge between two adjacent, axis-aligned vertices.
        /// Vertex order does not affect the canonical result.
        /// </summary>
        public CellEdge(
            GridVertex firstVertex,
            GridVertex secondVertex)
        {
            if (firstVertex.Level != secondVertex.Level)
            {
                throw new ArgumentException(
                    "A CellEdge cannot span logical levels.",
                    nameof(secondVertex));
            }

            int xDifference =
                secondVertex.X - firstVertex.X;

            int yDifference =
                secondVertex.Y - firstVertex.Y;

            if (xDifference == 0
                && Math.Abs(yDifference) == 1)
            {
                AnchorCell =
                    new GridPosition(
                        firstVertex.X,
                        Math.Max(
                            firstVertex.Y,
                            secondVertex.Y),
                        firstVertex.Level);

                CanonicalDirection =
                    CellEdgeDirection.NorthEast;

                return;
            }

            if (yDifference == 0
                && Math.Abs(xDifference) == 1)
            {
                AnchorCell =
                    new GridPosition(
                        Math.Max(
                            firstVertex.X,
                            secondVertex.X),
                        firstVertex.Y,
                        firstVertex.Level);

                CanonicalDirection =
                    CellEdgeDirection.NorthWest;

                return;
            }

            throw new ArgumentException(
                "A CellEdge requires two adjacent, axis-aligned vertices.",
                nameof(secondVertex));
        }

        /// <summary>
        /// Returns true when the supplied cell touches this edge.
        /// </summary>
        public bool TouchesCell(GridPosition position)
        {
            return FirstCell == position
                || SecondCell == position;
        }

        /// <summary>
        /// Returns true when the supplied vertex is an endpoint of this edge.
        /// </summary>
        public bool TouchesVertex(GridVertex vertex)
        {
            return FirstVertex == vertex
                || SecondVertex == vertex;
        }

        public bool Equals(CellEdge other)
        {
            return AnchorCell.Equals(other.AnchorCell)
                && CanonicalDirection
                    == other.CanonicalDirection;
        }

        public override bool Equals(object obj)
        {
            return obj is CellEdge other
                && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;

                hash =
                    (hash * 31)
                    + AnchorCell.GetHashCode();

                hash =
                    (hash * 31)
                    + (int)CanonicalDirection;

                return hash;
            }
        }

        public override string ToString()
        {
            return
                $"{AnchorCell} — " +
                $"{CanonicalDirection} Edge";
        }

        public static bool operator ==(
            CellEdge left,
            CellEdge right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            CellEdge left,
            CellEdge right)
        {
            return !left.Equals(right);
        }
    }
}
