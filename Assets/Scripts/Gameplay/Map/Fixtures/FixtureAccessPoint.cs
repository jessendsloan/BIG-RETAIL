using System;
using BigRetail.Map.Domain;

namespace BigRetail.Map.Fixtures
{
    /// <summary>
    /// One world-grid cell from which an actor may interact with one side of
    /// a fixture. Reachability remains a navigation concern.
    /// </summary>
    public readonly struct FixtureAccessPoint :
        IEquatable<FixtureAccessPoint>
    {
        public GridPosition Cell { get; }

        public FixtureSide Side { get; }

        public FixtureAccessMode Mode { get; }

        /// <summary>
        /// The wall edge separating this stand cell from the fixture face it
        /// serves.
        /// </summary>
        public CellEdge BoundaryEdge
        {
            get
            {
                return Side switch
                {
                    FixtureSide.North =>
                        new CellEdge(
                            Cell,
                            CellEdgeDirection.SouthEast),

                    FixtureSide.East =>
                        new CellEdge(
                            Cell,
                            CellEdgeDirection.SouthWest),

                    FixtureSide.South =>
                        new CellEdge(
                            Cell,
                            CellEdgeDirection.NorthWest),

                    FixtureSide.West =>
                        new CellEdge(
                            Cell,
                            CellEdgeDirection.NorthEast),

                    _ => throw new InvalidOperationException(
                        "A fixture access point must use a supported side.")
                };
            }
        }


        public FixtureAccessPoint(
            GridPosition cell,
            FixtureSide side,
            FixtureAccessMode mode)
        {
            if (!side.IsSupported())
            {
                throw new ArgumentOutOfRangeException(
                    nameof(side),
                    side,
                    "The fixture side is not supported.");
            }

            if (mode == FixtureAccessMode.None
                || !mode.IsSupported())
            {
                throw new ArgumentOutOfRangeException(
                    nameof(mode),
                    mode,
                    "An access point requires a supported interaction mode.");
            }

            Cell = cell;
            Side = side;
            Mode = mode;
        }


        public bool Equals(FixtureAccessPoint other)
        {
            return Cell == other.Cell
                && Side == other.Side
                && Mode == other.Mode;
        }


        public override bool Equals(object obj)
        {
            return obj is FixtureAccessPoint other
                && Equals(other);
        }


        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 31) + Cell.GetHashCode();
                hash = (hash * 31) + Side.GetHashCode();
                hash = (hash * 31) + Mode.GetHashCode();
                return hash;
            }
        }


        public static bool operator ==(
            FixtureAccessPoint left,
            FixtureAccessPoint right)
        {
            return left.Equals(right);
        }


        public static bool operator !=(
            FixtureAccessPoint left,
            FixtureAccessPoint right)
        {
            return !left.Equals(right);
        }
    }
}
