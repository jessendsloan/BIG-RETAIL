using System;

namespace BigRetail.Map.Fixtures
{
    /// <summary>
    /// Engine-free footprint contract for one fixture model in its authored
    /// North orientation.
    /// </summary>
    public sealed class FixtureDefinition
    {
        public FixtureDefinitionId Id { get; }

        public string DisplayName { get; }

        public int WidthInCells { get; }

        public int DepthInCells { get; }

        public FixtureAccessProfile AccessProfile { get; }

        public FixtureMerchandisingProfile MerchandisingProfile { get; }

        public int OccupiedCellCount =>
            WidthInCells * DepthInCells;


        public FixtureDefinition(
            FixtureDefinitionId id,
            string displayName,
            int widthInCells,
            int depthInCells,
            FixtureAccessProfile accessProfile = null,
            FixtureMerchandisingProfile merchandisingProfile = null)
        {
            if (!id.IsValid)
            {
                throw new ArgumentException(
                    "A fixture definition requires a valid ID.",
                    nameof(id));
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException(
                    "A fixture definition requires a display name.",
                    nameof(displayName));
            }

            if (widthInCells <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(widthInCells),
                    widthInCells,
                    "Fixture width must be at least one cell.");
            }

            if (depthInCells <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(depthInCells),
                    depthInCells,
                    "Fixture depth must be at least one cell.");
            }

            Id = id;
            DisplayName = displayName.Trim();
            WidthInCells = widthInCells;
            DepthInCells = depthInCells;
            AccessProfile =
                accessProfile
                ?? FixtureAccessProfile.None;

            MerchandisingProfile =
                merchandisingProfile
                ?? FixtureMerchandisingProfile
                    .CreateForCustomerBrowseSides(AccessProfile);
        }
    }
}
