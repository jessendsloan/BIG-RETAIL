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
        public const int DefaultDisplayUnitsPerFrontageUnit = 6;

        public ProductId Id { get; }
        public string DisplayName { get; }
        public BrandId BrandId { get; }
        public string ProductLine { get; }
        public ProductCategoryId CategoryId { get; }
        public MarketPosition MarketPosition { get; }
        public string PackageForm { get; }
        public StockUnit StockUnit { get; }

        /// <summary>
        /// Physical sellable units represented by one planogram frontage.
        /// Package-specific presentation can map each unit count to authored
        /// shelf artwork without imposing a fixture-wide inventory limit.
        /// </summary>
        public int DisplayUnitsPerFrontageUnit { get; }

        /// <summary>
        /// Temporary graybox purchasing value retained for the existing
        /// fixture workflow. Supplier purchasing prices live on Supplier Offers.
        /// </summary>
        public long WholesaleCaseCostCents { get; }

        /// <summary>
        /// Temporary graybox shelf price retained for the opening gameplay loop.
        /// </summary>
        public long RetailUnitPriceCents { get; }


        public ProductDefinition(
            ProductId id,
            string displayName,
            ProductCategoryId categoryId,
            StockUnit stockUnit)
            : this(
                id,
                displayName,
                BrandId.Unbranded,
                displayName,
                categoryId,
                MarketPosition.Standard,
                stockUnit.ToString(),
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
                BrandId.Unbranded,
                displayName,
                categoryId,
                MarketPosition.Standard,
                stockUnit.ToString(),
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
            long retailUnitPriceCents,
            int displayUnitsPerFrontageUnit =
                DefaultDisplayUnitsPerFrontageUnit)
            : this(
                id,
                displayName,
                BrandId.Unbranded,
                displayName,
                categoryId,
                MarketPosition.Standard,
                stockUnit.ToString(),
                stockUnit,
                wholesaleCaseCostCents,
                retailUnitPriceCents,
                displayUnitsPerFrontageUnit)
        {
        }

        public ProductDefinition(
            ProductId id,
            string displayName,
            BrandId brandId,
            string productLine,
            ProductCategoryId categoryId,
            MarketPosition marketPosition,
            string packageForm,
            StockUnit stockUnit)
            : this(
                id,
                displayName,
                brandId,
                productLine,
                categoryId,
                marketPosition,
                packageForm,
                stockUnit,
                wholesaleCaseCostCents: 0,
                retailUnitPriceCents: 0)
        {
        }

        public ProductDefinition(
            ProductId id,
            string displayName,
            BrandId brandId,
            string productLine,
            ProductCategoryId categoryId,
            MarketPosition marketPosition,
            string packageForm,
            StockUnit stockUnit,
            long wholesaleCaseCostCents,
            long retailUnitPriceCents,
            int displayUnitsPerFrontageUnit =
                DefaultDisplayUnitsPerFrontageUnit)
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

            if (!brandId.IsValid)
            {
                throw new ArgumentException(
                    "A product definition requires a valid brand identifier.",
                    nameof(brandId));
            }

            if (string.IsNullOrWhiteSpace(productLine))
            {
                throw new ArgumentException(
                    "A product definition requires a product line.",
                    nameof(productLine));
            }

            if (!Enum.IsDefined(
                    typeof(MarketPosition),
                    marketPosition))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(marketPosition),
                    marketPosition,
                    "The market position is not supported.");
            }

            if (string.IsNullOrWhiteSpace(packageForm))
            {
                throw new ArgumentException(
                    "A product definition requires a customer package or form.",
                    nameof(packageForm));
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

            if (displayUnitsPerFrontageUnit <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(displayUnitsPerFrontageUnit),
                    displayUnitsPerFrontageUnit,
                    "A display frontage must hold at least one sellable unit.");
            }

            Id = id;
            DisplayName = displayName.Trim();
            BrandId = brandId;
            ProductLine = productLine.Trim();
            CategoryId = categoryId;
            MarketPosition = marketPosition;
            PackageForm = packageForm.Trim();
            StockUnit = stockUnit;
            DisplayUnitsPerFrontageUnit =
                displayUnitsPerFrontageUnit;
            WholesaleCaseCostCents = wholesaleCaseCostCents;
            RetailUnitPriceCents = retailUnitPriceCents;
        }
    }
}
