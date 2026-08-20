using System;
using System.Collections.Generic;

namespace BigRetail.Purchasing.Domain
{
    public sealed class SupplierCatalog
    {
        private readonly Dictionary<SupplierId, SupplierDefinition> definitions;
        private readonly List<SupplierDefinition> orderedDefinitions;


        public int Count =>
            orderedDefinitions.Count;


        public SupplierCatalog(IEnumerable<SupplierDefinition> definitions)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException(nameof(definitions));
            }

            this.definitions =
                new Dictionary<SupplierId, SupplierDefinition>();
            orderedDefinitions = new List<SupplierDefinition>();

            foreach (SupplierDefinition definition in definitions)
            {
                if (definition == null)
                {
                    throw new ArgumentException(
                        "A supplier catalog cannot contain a null definition.",
                        nameof(definitions));
                }

                if (this.definitions.ContainsKey(definition.Id))
                {
                    throw new ArgumentException(
                        $"The supplier identifier '{definition.Id}' is duplicated.",
                        nameof(definitions));
                }

                this.definitions.Add(definition.Id, definition);
                orderedDefinitions.Add(definition);
            }
        }


        public bool Contains(SupplierId supplierId)
        {
            return definitions.ContainsKey(supplierId);
        }

        public bool TryGet(
            SupplierId supplierId,
            out SupplierDefinition definition)
        {
            return definitions.TryGetValue(supplierId, out definition);
        }

        public SupplierDefinition GetRequired(SupplierId supplierId)
        {
            if (definitions.TryGetValue(
                    supplierId,
                    out SupplierDefinition definition))
            {
                return definition;
            }

            throw new KeyNotFoundException(
                $"Supplier '{supplierId}' does not exist in the catalog.");
        }

        public IEnumerable<SupplierDefinition> EnumerateDefinitions()
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
