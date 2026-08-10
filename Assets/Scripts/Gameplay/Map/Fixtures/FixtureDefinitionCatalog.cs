using System;
using System.Collections.Generic;

namespace BigRetail.Map.Fixtures
{
    /// <summary>
    /// Defines the fixture models available to one store simulation.
    /// Unity assets can later map these definitions to authored artwork.
    /// </summary>
    public sealed class FixtureDefinitionCatalog
    {
        private readonly Dictionary<
            FixtureDefinitionId,
            FixtureDefinition> definitions =
                new Dictionary<
                    FixtureDefinitionId,
                    FixtureDefinition>();


        public int Count =>
            definitions.Count;


        public FixtureDefinitionCatalog(
            IEnumerable<FixtureDefinition> definitions)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException(
                    nameof(definitions));
            }

            foreach (FixtureDefinition definition in definitions)
            {
                if (definition == null)
                {
                    throw new ArgumentException(
                        "A fixture catalog cannot contain a null definition.",
                        nameof(definitions));
                }

                if (!this.definitions.TryAdd(
                        definition.Id,
                        definition))
                {
                    throw new ArgumentException(
                        $"Fixture definition '{definition.Id}' is duplicated.",
                        nameof(definitions));
                }
            }
        }


        public bool TryGetDefinition(
            FixtureDefinitionId definitionId,
            out FixtureDefinition definition)
        {
            return definitions.TryGetValue(
                definitionId,
                out definition);
        }

        public bool Contains(
            FixtureDefinitionId definitionId)
        {
            return definitions.ContainsKey(
                definitionId);
        }

        public IEnumerable<FixtureDefinition> EnumerateDefinitions()
        {
            foreach (FixtureDefinition definition in definitions.Values)
            {
                yield return definition;
            }
        }
    }
}
