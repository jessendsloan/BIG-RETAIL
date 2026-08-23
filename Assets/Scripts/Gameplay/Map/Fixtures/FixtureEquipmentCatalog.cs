using System;
using System.Collections.Generic;

namespace BigRetail.Map.Fixtures
{
    /// <summary>
    /// Complete fixture-equipment offer catalog for one store session.
    /// </summary>
    public sealed class FixtureEquipmentCatalog
    {
        private readonly Dictionary<FixtureDefinitionId,
            FixtureEquipmentDefinition> definitions =
                new Dictionary<FixtureDefinitionId,
                    FixtureEquipmentDefinition>();


        public int Count => definitions.Count;


        public FixtureEquipmentCatalog(
            FixtureDefinitionCatalog fixtureDefinitions,
            IEnumerable<FixtureEquipmentDefinition> equipmentDefinitions)
        {
            if (fixtureDefinitions == null)
            {
                throw new ArgumentNullException(nameof(fixtureDefinitions));
            }

            if (equipmentDefinitions == null)
            {
                throw new ArgumentNullException(nameof(equipmentDefinitions));
            }

            foreach (FixtureEquipmentDefinition definition
                     in equipmentDefinitions)
            {
                if (definition == null)
                {
                    throw new ArgumentException(
                        "An equipment catalog cannot contain a null entry.",
                        nameof(equipmentDefinitions));
                }

                if (!fixtureDefinitions.Contains(
                        definition.FixtureDefinitionId))
                {
                    throw new ArgumentException(
                        $"Equipment entry '{definition.FixtureDefinitionId}' has no fixture definition.",
                        nameof(equipmentDefinitions));
                }

                if (!definitions.TryAdd(
                        definition.FixtureDefinitionId,
                        definition))
                {
                    throw new ArgumentException(
                        $"Equipment entry '{definition.FixtureDefinitionId}' is duplicated.",
                        nameof(equipmentDefinitions));
                }
            }

            foreach (FixtureDefinition fixture
                     in fixtureDefinitions.EnumerateDefinitions())
            {
                if (!definitions.ContainsKey(fixture.Id))
                {
                    throw new ArgumentException(
                        $"Placeable fixture '{fixture.Id}' has no equipment terms.",
                        nameof(equipmentDefinitions));
                }
            }
        }


        public bool TryGet(
            FixtureDefinitionId fixtureDefinitionId,
            out FixtureEquipmentDefinition definition)
        {
            return definitions.TryGetValue(
                fixtureDefinitionId,
                out definition);
        }

        public FixtureEquipmentDefinition GetRequired(
            FixtureDefinitionId fixtureDefinitionId)
        {
            if (TryGet(fixtureDefinitionId, out FixtureEquipmentDefinition value))
            {
                return value;
            }

            throw new KeyNotFoundException(
                $"No equipment terms exist for '{fixtureDefinitionId}'.");
        }

        public IEnumerable<FixtureEquipmentDefinition> EnumerateDefinitions()
        {
            foreach (FixtureEquipmentDefinition definition
                     in definitions.Values)
            {
                yield return definition;
            }
        }
    }
}
