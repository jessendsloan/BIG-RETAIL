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


        public ProductDefinition(
            ProductId id,
            string displayName,
            ProductCategoryId categoryId,
            StockUnit stockUnit)
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

            Id = id;
            DisplayName = displayName.Trim();
            CategoryId = categoryId;
            StockUnit = stockUnit;
        }
    }
}
