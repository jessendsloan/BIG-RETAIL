using System;
using System.Collections.Generic;

namespace BigRetail.Merchandise.Domain
{
    /// <summary>
    /// Immutable lookup of the consumer brands recognized by one game.
    /// </summary>
    public sealed class BrandCatalog
    {
        private readonly Dictionary<BrandId, BrandDefinition> definitions;
        private readonly List<BrandDefinition> orderedDefinitions;


        public int Count =>
            orderedDefinitions.Count;


        public BrandCatalog(IEnumerable<BrandDefinition> definitions)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException(nameof(definitions));
            }

            this.definitions =
                new Dictionary<BrandId, BrandDefinition>();
            orderedDefinitions = new List<BrandDefinition>();

            foreach (BrandDefinition definition in definitions)
            {
                if (definition == null)
                {
                    throw new ArgumentException(
                        "A brand catalog cannot contain a null definition.",
                        nameof(definitions));
                }

                if (this.definitions.ContainsKey(definition.Id))
                {
                    throw new ArgumentException(
                        $"The brand identifier '{definition.Id}' is duplicated.",
                        nameof(definitions));
                }

                this.definitions.Add(definition.Id, definition);
                orderedDefinitions.Add(definition);
            }
        }


        public bool Contains(BrandId brandId)
        {
            return definitions.ContainsKey(brandId);
        }

        public bool TryGet(
            BrandId brandId,
            out BrandDefinition definition)
        {
            return definitions.TryGetValue(brandId, out definition);
        }

        public BrandDefinition GetRequired(BrandId brandId)
        {
            if (definitions.TryGetValue(
                    brandId,
                    out BrandDefinition definition))
            {
                return definition;
            }

            throw new KeyNotFoundException(
                $"Brand '{brandId}' does not exist in the catalog.");
        }

        public IEnumerable<BrandDefinition> EnumerateDefinitions()
        {
            for (int index = 0;
                 index < orderedDefinitions.Count;
                 index++)
            {
                yield return orderedDefinitions[index];
            }
        }
    }
}
