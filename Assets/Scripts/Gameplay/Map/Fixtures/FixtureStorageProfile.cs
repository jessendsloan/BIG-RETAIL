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

        /// <summary>
        /// Maximum number of tracked physical supplier cases that can occupy
        /// this fixture. Units stored are derived from the cases occupying
        /// these slots rather than constrained by a separate rack limit.
        /// </summary>
        public int BackstockCaseSlotCapacity { get; }

        public bool ProvidesBackstockStorage =>
            BackstockCaseSlotCapacity > 0;


        public FixtureStorageProfile(
            int backstockCaseSlotCapacity)
        {
            if (backstockCaseSlotCapacity < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(backstockCaseSlotCapacity),
                    backstockCaseSlotCapacity,
                    "Fixture backstock case-slot capacity cannot be negative.");
            }

            BackstockCaseSlotCapacity = backstockCaseSlotCapacity;
        }
    }
}
