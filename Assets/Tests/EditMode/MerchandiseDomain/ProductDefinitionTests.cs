using System;
using NUnit.Framework;

namespace BigRetail.Merchandise.Domain.Tests
{
    /// <summary>
    /// Locks down the minimum shared data required to describe one product.
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
                    new ProductCategoryId("produce"),
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
                definition.CategoryId,
                Is.EqualTo(
                    new ProductCategoryId("PRODUCE")));

            Assert.That(
                definition.StockUnit,
                Is.EqualTo(StockUnit.Each));

            Assert.That(
                definition.WholesaleCaseCostCents,
                Is.EqualTo(4200));

            Assert.That(
                definition.RetailUnitPriceCents,
                Is.EqualTo(299));
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
