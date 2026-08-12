using System;

namespace BigRetail.Map.Fixtures
{
    /// <summary>
    /// Immutable storage capacity contributed by one placed fixture.
    /// Inventory remains authoritative for stock quantities; storage fixtures
    /// make the store's shared backstock pool physically accessible.
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
