using System;
using BigRetail.Map.Domain;

namespace BigRetail.Map.Fixtures
{
    /// <summary>
    /// Free spatial intent for a fixture that has not yet been installed.
    /// </summary>
    public sealed class FixtureEquipmentPlan
    {
        public FixtureInstanceId Id { get; }

        public FixtureDefinitionId FixtureDefinitionId { get; }

        public FixtureFootprint Footprint { get; }

        public GridPosition AnchorCell => Footprint.AnchorCell;

        public FixtureOrientation Orientation => Footprint.Orientation;


        internal FixtureEquipmentPlan(
            FixtureInstanceId id,
            FixtureDefinitionId fixtureDefinitionId,
            FixtureFootprint footprint)
        {
            if (!id.IsValid)
            {
                throw new ArgumentException(
                    "An equipment plan requires an instance ID.",
                    nameof(id));
            }

            if (!fixtureDefinitionId.IsValid)
            {
                throw new ArgumentException(
                    "An equipment plan requires a fixture definition.",
                    nameof(fixtureDefinitionId));
            }

            Id = id;
            FixtureDefinitionId = fixtureDefinitionId;
            Footprint = footprint
                ?? throw new ArgumentNullException(nameof(footprint));
        }
    }
}
