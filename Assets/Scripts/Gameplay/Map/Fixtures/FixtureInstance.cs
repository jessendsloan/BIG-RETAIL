using System;
using System.Collections.Generic;
using BigRetail.Map.Domain;

namespace BigRetail.Map.Fixtures
{
    /// <summary>
    /// One immutable fixture placement. FixtureState owns whether this
    /// placement currently exists.
    /// </summary>
    public sealed class FixtureInstance
    {
        public FixtureInstanceId Id { get; }

        public FixtureDefinition Definition { get; }

        public FixtureDefinitionId DefinitionId =>
            Definition.Id;

        public FixtureFootprint Footprint { get; }

        public GridPosition AnchorCell =>
            Footprint.AnchorCell;

        public FixtureOrientation Orientation =>
            Footprint.Orientation;

        public int OccupiedCellCount =>
            Footprint.CellCount;

        public IReadOnlyList<GridPosition> OccupiedCells =>
            Footprint.Cells;

        /// <summary>
        /// Access points that were usable when this fixture was placed and
        /// are therefore protected from later construction.
        /// </summary>
        public IReadOnlyList<FixtureAccessPoint> ReservedAccessPoints { get; }


        internal FixtureInstance(
            FixtureInstanceId id,
            FixtureDefinition definition,
            FixtureFootprint footprint,
            IReadOnlyList<FixtureAccessPoint> reservedAccessPoints)
        {
            if (!id.IsValid)
            {
                throw new ArgumentException(
                    "A fixture instance requires a valid ID.",
                    nameof(id));
            }

            Definition =
                definition
                ?? throw new ArgumentNullException(
                    nameof(definition));

            Footprint =
                footprint
                ?? throw new ArgumentNullException(
                    nameof(footprint));

            if (reservedAccessPoints == null)
            {
                throw new ArgumentNullException(
                    nameof(reservedAccessPoints));
            }

            if (footprint.CellCount
                != definition.OccupiedCellCount)
            {
                throw new ArgumentException(
                    "The footprint does not match the fixture definition.",
                    nameof(footprint));
            }

            Id = id;
            FixtureAccessPoint[] accessPointSnapshot =
                new FixtureAccessPoint[reservedAccessPoints.Count];

            for (int index = 0;
                 index < reservedAccessPoints.Count;
                 index++)
            {
                accessPointSnapshot[index] = reservedAccessPoints[index];
            }

            ReservedAccessPoints = accessPointSnapshot;
        }


        public GridPosition GetOccupiedCell(
            int index)
        {
            return Footprint.GetCell(index);
        }

        public bool OccupiesCell(
            GridPosition cell)
        {
            return Footprint.ContainsCell(cell);
        }

        public bool HasSamePlacementAs(
            FixtureInstance other)
        {
            return other != null
                && Id == other.Id
                && DefinitionId == other.DefinitionId
                && AnchorCell == other.AnchorCell
                && Orientation == other.Orientation;
        }
    }
}
