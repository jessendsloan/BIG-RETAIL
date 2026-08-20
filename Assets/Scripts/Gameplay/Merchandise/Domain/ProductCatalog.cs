using System;
using System.Collections.Generic;

namespace BigRetail.Merchandise.Domain
{
    /// <summary>
    /// Provides the authoritative lookup of products recognized by one game.
    ///
    /// The catalog is immutable after construction so product identity remains
    /// stable while runtime pricing, stock, and sales state change around it.
    /// </summary>
    public sealed class ProductCatalog
    {
        private readonly Dictionary<ProductId, ProductDefinition> definitions;
        private readonly List<ProductDefinition> orderedDefinitions;


        public int Count =>
            definitions.Count;


        public ProductCatalog(
            IEnumerable<ProductDefinition> definitions)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException(
                    nameof(definitions));
            }

            this.definitions =
                new Dictionary<ProductId, ProductDefinition>();
            orderedDefinitions = new List<ProductDefinition>();

            foreach (ProductDefinition definition in definitions)
            {
                if (definition == null)
                {
                    throw new ArgumentException(
                        "A product catalog cannot contain a null definition.",
                        nameof(definitions));
                }

                if (this.definitions.ContainsKey(
                        definition.Id))
                {
                    throw new ArgumentException(
                        $"The product identifier '{definition.Id}' is duplicated.",
                        nameof(definitions));
                }

                this.definitions.Add(
                    definition.Id,
                    definition);
                orderedDefinitions.Add(definition);
            }
        }


        public bool Contains(
            ProductId productId)
        {
            return definitions.ContainsKey(productId);
        }

        public bool TryGet(
            ProductId productId,
            out ProductDefinition definition)
        {
            return definitions.TryGetValue(
                productId,
                out definition);
        }

        public ProductDefinition GetRequired(
            ProductId productId)
        {
            if (definitions.TryGetValue(
                    productId,
                    out ProductDefinition definition))
            {
                return definition;
            }

            throw new KeyNotFoundException(
                $"Product '{productId}' does not exist in the catalog.");
        }

        public IEnumerable<ProductDefinition> EnumerateDefinitions()
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
