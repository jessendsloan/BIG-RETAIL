using System;
using System.Collections.Generic;
using BigRetail.Merchandise.Domain;

namespace BigRetail.Purchasing.Domain
{
    public sealed class SupplierOfferCatalog
    {
        private readonly Dictionary<SupplierOfferId, SupplierOfferDefinition>
            definitions;
        private readonly List<SupplierOfferDefinition> orderedDefinitions;


        public int Count =>
            orderedDefinitions.Count;


        public SupplierOfferCatalog(
            IEnumerable<SupplierOfferDefinition> definitions)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException(nameof(definitions));
            }

            this.definitions =
                new Dictionary<SupplierOfferId, SupplierOfferDefinition>();
            orderedDefinitions = new List<SupplierOfferDefinition>();

            foreach (SupplierOfferDefinition definition in definitions)
            {
                if (definition == null)
                {
                    throw new ArgumentException(
                        "A supplier offer catalog cannot contain a null definition.",
                        nameof(definitions));
                }

                if (this.definitions.ContainsKey(definition.Id))
                {
                    throw new ArgumentException(
                        $"The supplier offer identifier '{definition.Id}' is duplicated.",
                        nameof(definitions));
                }

                this.definitions.Add(definition.Id, definition);
                orderedDefinitions.Add(definition);
            }
        }


        public SupplierOfferDefinition GetRequired(SupplierOfferId offerId)
        {
            if (definitions.TryGetValue(
                    offerId,
                    out SupplierOfferDefinition definition))
            {
                return definition;
            }

            throw new KeyNotFoundException(
                $"Supplier offer '{offerId}' does not exist in the catalog.");
        }

        public IEnumerable<SupplierOfferDefinition> EnumerateDefinitions()
        {
            for (int index = 0;
                 index < orderedDefinitions.Count;
                 index++)
            {
                yield return orderedDefinitions[index];
            }
        }

        public IEnumerable<SupplierOfferDefinition> EnumerateForProduct(
            ProductId productId,
            bool availableOnly = true)
        {
            for (int index = 0;
                 index < orderedDefinitions.Count;
                 index++)
            {
                SupplierOfferDefinition offer = orderedDefinitions[index];

                if (offer.ProductId == productId
                    && (!availableOnly || offer.IsAvailable))
                {
                    yield return offer;
                }
            }
        }

        public IEnumerable<SupplierOfferDefinition> EnumerateForSupplier(
            SupplierId supplierId,
            bool availableOnly = true)
        {
            for (int index = 0;
                 index < orderedDefinitions.Count;
                 index++)
            {
                SupplierOfferDefinition offer = orderedDefinitions[index];

                if (offer.SupplierId == supplierId
                    && (!availableOnly || offer.IsAvailable))
                {
                    yield return offer;
                }
            }
        }
    }
}
