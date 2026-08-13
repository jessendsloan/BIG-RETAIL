using System;

namespace BigRetail.Map.Fixtures
{
    /// <summary>
    /// Immutable storage capacity owned by one placed fixture. Inventory
    /// remains authoritative for the product quantities held by that rack.
    /// </summary>
    public sealed class FixtureStorageProfile
    {
        public static FixtureStorageProfile None { get; } =
            new FixtureStorageProfile(0);


        public int BackstockCapacityUnits { get; }

        public bool ProvidesBackstockStorage =>
            BackstockCapacityUnits > 0;


        public FixtureStorageProfile(
            int backstockCapacityUnits)
        {
            if (backstockCapacityUnits < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(backstockCapacityUnits),
                    backstockCapacityUnits,
                    "Fixture backstock capacity cannot be negative.");
            }

            BackstockCapacityUnits = backstockCapacityUnits;
        }
    }
}
