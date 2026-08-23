using System;

namespace BigRetail.Map.Fixtures
{
    /// <summary>
    /// Commercial terms for one physical fixture model.
    /// </summary>
    public sealed class FixtureEquipmentDefinition
    {
        public FixtureDefinitionId FixtureDefinitionId { get; }

        public string DisplayName { get; }

        public long UnitPriceCents { get; }

        public long DeliveryLeadTimeSeconds { get; }

        public int StartingOwnedQuantity { get; }


        public FixtureEquipmentDefinition(
            FixtureDefinitionId fixtureDefinitionId,
            string displayName,
            long unitPriceCents,
            long deliveryLeadTimeSeconds,
            int startingOwnedQuantity = 0)
        {
            if (!fixtureDefinitionId.IsValid)
            {
                throw new ArgumentException(
                    "Fixture equipment requires a fixture definition.",
                    nameof(fixtureDefinitionId));
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException(
                    "Fixture equipment requires a display name.",
                    nameof(displayName));
            }

            if (unitPriceCents <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(unitPriceCents),
                    unitPriceCents,
                    "Fixture equipment must have a positive price.");
            }

            if (deliveryLeadTimeSeconds < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(deliveryLeadTimeSeconds));
            }

            if (startingOwnedQuantity < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(startingOwnedQuantity));
            }

            FixtureDefinitionId = fixtureDefinitionId;
            DisplayName = displayName.Trim();
            UnitPriceCents = unitPriceCents;
            DeliveryLeadTimeSeconds = deliveryLeadTimeSeconds;
            StartingOwnedQuantity = startingOwnedQuantity;
        }
    }
}
