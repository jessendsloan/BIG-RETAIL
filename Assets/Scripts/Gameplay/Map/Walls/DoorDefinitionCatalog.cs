using System;
using System.Collections.Generic;

namespace BigRetail.Map.Walls
{
    /// <summary>
    /// Defines the door models available to one store simulation.
    /// Unity assets can later map these definitions to layered artwork.
    /// </summary>
    public sealed class DoorDefinitionCatalog
    {
        private readonly Dictionary<DoorDefinitionId, DoorDefinition>
            definitions =
                new Dictionary<DoorDefinitionId, DoorDefinition>();


        public int Count =>
            definitions.Count;


        public DoorDefinitionCatalog(
            IEnumerable<DoorDefinition> definitions)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException(
                    nameof(definitions));
            }

            foreach (DoorDefinition definition in definitions)
            {
                if (definition == null)
                {
                    throw new ArgumentException(
                        "A door catalog cannot contain a null definition.",
                        nameof(definitions));
                }

                if (!this.definitions.TryAdd(
                        definition.Id,
                        definition))
                {
                    throw new ArgumentException(
                        $"Door definition '{definition.Id}' is duplicated.",
                        nameof(definitions));
                }
            }
        }


        public bool TryGetDefinition(
            DoorDefinitionId definitionId,
            out DoorDefinition definition)
        {
            return definitions.TryGetValue(
                definitionId,
                out definition);
        }

        public bool Contains(
            DoorDefinitionId definitionId)
        {
            return definitions.ContainsKey(
                definitionId);
        }

        public IEnumerable<DoorDefinition> EnumerateDefinitions()
        {
            foreach (DoorDefinition definition in definitions.Values)
            {
                yield return definition;
            }
        }
    }
}
