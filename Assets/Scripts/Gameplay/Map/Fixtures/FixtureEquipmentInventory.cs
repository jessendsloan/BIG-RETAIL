using System;
using System.Collections.Generic;

namespace BigRetail.Map.Fixtures
{
    /// <summary>
    /// Physical fixture modules owned by the store but not installed.
    /// </summary>
    public sealed class FixtureEquipmentInventory
    {
        private readonly FixtureEquipmentCatalog catalog;
        private readonly Dictionary<FixtureDefinitionId, int> quantities =
            new Dictionary<FixtureDefinitionId, int>();


        public FixtureEquipmentInventory(FixtureEquipmentCatalog catalog)
        {
            this.catalog = catalog
                ?? throw new ArgumentNullException(nameof(catalog));

            foreach (FixtureEquipmentDefinition definition
                     in catalog.EnumerateDefinitions())
            {
                if (definition.StartingOwnedQuantity > 0)
                {
                    quantities.Add(
                        definition.FixtureDefinitionId,
                        definition.StartingOwnedQuantity);
                }
            }
        }


        public event Action<FixtureDefinitionId> QuantityChanged;


        public int GetQuantity(FixtureDefinitionId fixtureDefinitionId)
        {
            return quantities.TryGetValue(
                    fixtureDefinitionId,
                    out int quantity)
                ? quantity
                : 0;
        }

        public bool TryConsume(
            FixtureDefinitionId fixtureDefinitionId,
            int quantity = 1)
        {
            if (quantity <= 0
                || !catalog.TryGet(fixtureDefinitionId, out _))
            {
                return false;
            }

            int current = GetQuantity(fixtureDefinitionId);

            if (current < quantity)
            {
                return false;
            }

            int next = current - quantity;

            if (next == 0)
            {
                quantities.Remove(fixtureDefinitionId);
            }
            else
            {
                quantities[fixtureDefinitionId] = next;
            }

            QuantityChanged?.Invoke(fixtureDefinitionId);
            return true;
        }

        public void Add(
            FixtureDefinitionId fixtureDefinitionId,
            int quantity = 1)
        {
            if (!catalog.TryGet(fixtureDefinitionId, out _))
            {
                throw new ArgumentException(
                    $"Unknown fixture equipment '{fixtureDefinitionId}'.",
                    nameof(fixtureDefinitionId));
            }

            if (quantity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(quantity));
            }

            int current = GetQuantity(fixtureDefinitionId);
            quantities[fixtureDefinitionId] = checked(current + quantity);
            QuantityChanged?.Invoke(fixtureDefinitionId);
        }
    }
}
