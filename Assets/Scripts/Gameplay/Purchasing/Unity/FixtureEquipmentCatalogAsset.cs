using System;
using System.Collections.Generic;
using BigRetail.Map.Fixtures;
using BigRetail.Map.Unity.Fixtures;
using UnityEngine;

namespace BigRetail.Purchasing.Unity
{
    [Serializable]
    public sealed class FixtureEquipmentCatalogEntryAsset
    {
        [SerializeField]
        private FixtureDefinitionAsset fixtureDefinition;

        [SerializeField]
        [Min(1)]
        private long unitPriceCents = 10000;

        [SerializeField]
        [Min(0)]
        private int deliveryLeadTimeGameMinutes = 120;

        [SerializeField]
        [Min(0)]
        private int startingOwnedQuantity;

        [SerializeField]
        private string categoryName = "General";


        public FixtureDefinitionAsset FixtureDefinition => fixtureDefinition;
        public long UnitPriceCents => unitPriceCents;
        public int DeliveryLeadTimeGameMinutes =>
            deliveryLeadTimeGameMinutes;
        public int StartingOwnedQuantity => startingOwnedQuantity;
        public string CategoryName =>
            string.IsNullOrWhiteSpace(categoryName)
                ? "General"
                : categoryName.Trim();


        internal FixtureEquipmentDefinition CreateDomainDefinition()
        {
            if (fixtureDefinition == null)
            {
                throw new InvalidOperationException(
                    "An equipment entry has no fixture definition.");
            }

            return new FixtureEquipmentDefinition(
                fixtureDefinition.Id,
                fixtureDefinition.DisplayName,
                unitPriceCents,
                checked((long)deliveryLeadTimeGameMinutes * 60L),
                startingOwnedQuantity);
        }
    }


    /// <summary>
    /// Authored fixture prices, delivery lead times, and opening ownership.
    /// </summary>
    [CreateAssetMenu(
        menuName = "Big Retail/Fixtures/Equipment Catalog",
        fileName = "FixtureEquipmentCatalog")]
    public sealed class FixtureEquipmentCatalogAsset : ScriptableObject
    {
        [SerializeField]
        private FixtureEquipmentCatalogEntryAsset[] entries =
            Array.Empty<FixtureEquipmentCatalogEntryAsset>();


        public int Count => entries?.Length ?? 0;


        public IEnumerable<FixtureEquipmentCatalogEntryAsset>
            EnumerateEntries()
        {
            if (entries == null)
            {
                yield break;
            }

            for (int index = 0; index < entries.Length; index++)
            {
                if (entries[index] != null)
                {
                    yield return entries[index];
                }
            }
        }

        public bool TryGetEntry(
            FixtureDefinitionId fixtureDefinitionId,
            out FixtureEquipmentCatalogEntryAsset entry)
        {
            if (entries != null)
            {
                for (int index = 0; index < entries.Length; index++)
                {
                    FixtureEquipmentCatalogEntryAsset candidate =
                        entries[index];

                    if (candidate?.FixtureDefinition != null
                        && candidate.FixtureDefinition.Id
                        == fixtureDefinitionId)
                    {
                        entry = candidate;
                        return true;
                    }
                }
            }

            entry = null;
            return false;
        }


        public FixtureEquipmentCatalog CreateDomainCatalog(
            FixtureDefinitionCatalog fixtureDefinitions)
        {
            if (entries == null)
            {
                throw new InvalidOperationException(
                    $"Equipment catalog '{name}' has a null entry list.");
            }

            List<FixtureEquipmentDefinition> definitions =
                new List<FixtureEquipmentDefinition>(entries.Length);

            for (int index = 0; index < entries.Length; index++)
            {
                FixtureEquipmentCatalogEntryAsset entry =
                    entries[index]
                    ?? throw new InvalidOperationException(
                        $"Equipment catalog '{name}' has an empty entry at index {index}.");
                definitions.Add(entry.CreateDomainDefinition());
            }

            return new FixtureEquipmentCatalog(
                fixtureDefinitions,
                definitions);
        }
    }
}
