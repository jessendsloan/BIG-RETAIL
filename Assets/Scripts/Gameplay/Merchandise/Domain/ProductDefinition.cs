using System;

namespace BigRetail.Merchandise.Domain
{
    /// <summary>
    /// Describes one distinct product recognized by the store.
    ///
    /// This is shared merchandise data. Runtime stock quantities and storage
    /// locations belong to the inventory domain built on top of this catalog.
    /// </summary>
    public sealed class ProductDefinition
    {
        public ProductId Id { get; }
        public string DisplayName { get; }
        public ProductCategoryId CategoryId { get; }
        public StockUnit StockUnit { get; }
        public long WholesaleCaseCostCents { get; }
        public long RetailUnitPriceCents { get; }


        public ProductDefinition(
            ProductId id,
            string displayName,
            ProductCategoryId categoryId,
            StockUnit stockUnit)
            : this(
                id,
                displayName,
                categoryId,
                stockUnit,
                wholesaleCaseCostCents: 0,
                retailUnitPriceCents: 0)
        {
        }

        public ProductDefinition(
            ProductId id,
            string displayName,
            ProductCategoryId categoryId,
            StockUnit stockUnit,
            long wholesaleCaseCostCents)
            : this(
                id,
                displayName,
                categoryId,
                stockUnit,
                wholesaleCaseCostCents,
                retailUnitPriceCents: 0)
        {
        }

        public ProductDefinition(
            ProductId id,
            string displayName,
            ProductCategoryId categoryId,
            StockUnit stockUnit,
            long wholesaleCaseCostCents,
            long retailUnitPriceCents)
        {
            if (!id.IsValid)
            {
                throw new ArgumentException(
                    "A product definition requires a valid product identifier.",
                    nameof(id));
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException(
                    "A product definition requires a display name.",
                    nameof(displayName));
            }

            if (!categoryId.IsValid)
            {
                throw new ArgumentException(
                    "A product definition requires a valid category identifier.",
                    nameof(categoryId));
            }

            if (!Enum.IsDefined(
                    typeof(StockUnit),
                    stockUnit))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(stockUnit),
                    stockUnit,
                    "The stock unit is not supported.");
            }

            if (wholesaleCaseCostCents < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(wholesaleCaseCostCents),
                    wholesaleCaseCostCents,
                    "A wholesale case cost cannot be negative.");
            }

            if (retailUnitPriceCents < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(retailUnitPriceCents),
                    retailUnitPriceCents,
                    "A retail unit price cannot be negative.");
            }

            Id = id;
            DisplayName = displayName.Trim();
            CategoryId = categoryId;
            StockUnit = stockUnit;
            WholesaleCaseCostCents = wholesaleCaseCostCents;
            RetailUnitPriceCents = retailUnitPriceCents;
        }
    }
}
