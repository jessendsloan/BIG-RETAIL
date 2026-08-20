using System;
using NUnit.Framework;

namespace BigRetail.Merchandise.Domain.Tests
{
    /// <summary>
    /// Locks down the shared data required to describe one product while the
    /// existing fixture graybox transitions to supplier-backed purchasing.
    /// </summary>
    public sealed class ProductDefinitionTests
    {
        [Test]
        public void Constructor_PreservesCanonicalMerchandiseData()
        {
            ProductDefinition definition =
                new ProductDefinition(
                    new ProductId("ROMA-TOMATO"),
                    "Roma Tomato",
                    new BrandId("HOMESTEAD"),
                    "Tomato",
                    new ProductCategoryId("produce"),
                    MarketPosition.Standard,
                    "Single Tomato",
                    StockUnit.Each,
                    wholesaleCaseCostCents: 4200,
                    retailUnitPriceCents: 299);

            Assert.That(
                definition.Id,
                Is.EqualTo(
                    new ProductId("roma-tomato")));

            Assert.That(
                definition.DisplayName,
                Is.EqualTo("Roma Tomato"));

            Assert.That(
                definition.BrandId,
                Is.EqualTo(new BrandId("homestead")));

            Assert.That(
                definition.ProductLine,
                Is.EqualTo("Tomato"));

            Assert.That(
                definition.CategoryId,
                Is.EqualTo(
                    new ProductCategoryId("PRODUCE")));

            Assert.That(
                definition.StockUnit,
                Is.EqualTo(StockUnit.Each));

            Assert.That(
                definition.MarketPosition,
                Is.EqualTo(MarketPosition.Standard));

            Assert.That(
                definition.PackageForm,
                Is.EqualTo("Single Tomato"));

            Assert.That(
                definition.WholesaleCaseCostCents,
                Is.EqualTo(4200));

            Assert.That(
                definition.RetailUnitPriceCents,
                Is.EqualTo(299));
        }

        [Test]
        public void LegacyConstructor_PreservesUnbrandedGrayboxCompatibility()
        {
            ProductDefinition definition =
                new ProductDefinition(
                    new ProductId("GRAYBOX-COLA"),
                    "Graybox Cola",
                    new ProductCategoryId("BEVERAGES"),
                    StockUnit.Each);

            Assert.That(definition.BrandId, Is.EqualTo(BrandId.Unbranded));
            Assert.That(definition.ProductLine, Is.EqualTo("Graybox Cola"));
            Assert.That(definition.PackageForm, Is.EqualTo("Each"));
            Assert.That(definition.WholesaleCaseCostCents, Is.Zero);
            Assert.That(definition.RetailUnitPriceCents, Is.Zero);
        }

        [Test]
        public void GrayboxCostConstructor_PreservesTemporaryGameplayValues()
        {
            ProductDefinition definition =
                new ProductDefinition(
                    new ProductId("GRAYBOX-CEREAL"),
                    "Graybox Cereal",
                    new ProductCategoryId("GROCERY"),
                    StockUnit.Each,
                    wholesaleCaseCostCents: 3600,
                    retailUnitPriceCents: 249);

            Assert.That(definition.WholesaleCaseCostCents, Is.EqualTo(3600));
            Assert.That(definition.RetailUnitPriceCents, Is.EqualTo(249));
            Assert.That(definition.BrandId, Is.EqualTo(BrandId.Unbranded));
        }

        [Test]
        public void Constructor_RejectsEmptyDisplayName()
        {
            Assert.Throws<ArgumentException>(
                () =>
                    new ProductDefinition(
                        new ProductId("CHOCOLATE-BAR"),
                        " ",
                        new ProductCategoryId("CANDY"),
                        StockUnit.Each));
        }

        [Test]
        public void Constructor_RejectsUnsupportedStockUnit()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () =>
                    new ProductDefinition(
                        new ProductId("CHOCOLATE-BAR"),
                        "Chocolate Bar",
                        new ProductCategoryId("CANDY"),
                        (StockUnit)999));
        }
    }
}
