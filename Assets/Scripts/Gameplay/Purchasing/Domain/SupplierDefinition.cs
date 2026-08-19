using System;

namespace BigRetail.Purchasing.Domain
{
    /// <summary>
    /// Static commercial identity and supplier-wide ordering terms.
    /// Product-specific packs and prices belong to SupplierOfferDefinition.
    /// </summary>
    public sealed class SupplierDefinition
    {
        public SupplierId Id { get; }

        public string DisplayName { get; }

        public string Specialty { get; }

        public long MinimumOrderCents { get; }

        public SupplierDeliveryRule DeliveryRule { get; }


        public SupplierDefinition(
            SupplierId id,
            string displayName,
            string specialty,
            long minimumOrderCents,
            SupplierDeliveryRule deliveryRule)
        {
            if (!id.IsValid)
            {
                throw new ArgumentException(
                    "A supplier definition requires a valid identifier.",
                    nameof(id));
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException(
                    "A supplier definition requires a display name.",
                    nameof(displayName));
            }

            if (string.IsNullOrWhiteSpace(specialty))
            {
                throw new ArgumentException(
                    "A supplier definition requires a commercial specialty.",
                    nameof(specialty));
            }

            if (minimumOrderCents < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(minimumOrderCents),
                    minimumOrderCents,
                    "A minimum order cannot be negative.");
            }

            Id = id;
            DisplayName = displayName.Trim();
            Specialty = specialty.Trim();
            MinimumOrderCents = minimumOrderCents;
            DeliveryRule = deliveryRule
                ?? throw new ArgumentNullException(nameof(deliveryRule));
        }
    }
}
