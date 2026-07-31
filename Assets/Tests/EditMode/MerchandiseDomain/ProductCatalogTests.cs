using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace BigRetail.Merchandise.Domain.Tests
{
    /// <summary>
    /// Locks down the immutable lookup contract shared by future retail
    /// systems.
    /// </summary>
    public sealed class ProductCatalogTests
    {
        [Test]
        public void Constructor_BuildsLookupByProductId()
        {
            ProductDefinition candyBar =
                CreateProduct(
                    "CHOCO-BAR-155",
                    "Chocolate Bar",
                    "CANDY");

            ProductDefinition tomato =
                CreateProduct(
                    "ROMA-TOMATO",
                    "Roma Tomato",
                    "PRODUCE");

            ProductCatalog catalog =
                new ProductCatalog(
                    new[]
                    {
                        candyBar,
                        tomato
                    });

            bool found =
                catalog.TryGet(
                    new ProductId("roma-tomato"),
                    out ProductDefinition resolved);

            Assert.That(
                found,
                Is.True);

            Assert.That(
                resolved,
                Is.SameAs(tomato));

            Assert.That(
                catalog.Count,
                Is.EqualTo(2));
        }

        [Test]
        public void Constructor_RejectsDuplicateProductIds()
        {
            ProductDefinition first =
                CreateProduct(
                    "CHOCO-BAR-155",
                    "Chocolate Bar",
                    "CANDY");

            ProductDefinition duplicate =
                CreateProduct(
                    "choco-bar-155",
                    "Chocolate Bar Alternate Name",
                    "CANDY");

            Assert.Throws<ArgumentException>(
                () =>
                    new ProductCatalog(
                        new[]
                        {
                            first,
                            duplicate
                        }));
        }

        [Test]
        public void Constructor_RejectsNullDefinition()
        {
            Assert.Throws<ArgumentException>(
                () =>
                    new ProductCatalog(
                        new ProductDefinition[]
                        {
                            null
                        }));
        }

        [Test]
        public void GetRequired_ReturnsKnownProduct()
        {
            ProductDefinition candyBar =
                CreateProduct(
                    "CHOCO-BAR-155",
                    "Chocolate Bar",
                    "CANDY");

            ProductCatalog catalog =
                new ProductCatalog(
                    new[]
                    {
                        candyBar
                    });

            ProductDefinition resolved =
                catalog.GetRequired(
                    new ProductId("CHOCO-BAR-155"));

            Assert.That(
                resolved,
                Is.SameAs(candyBar));
        }

        [Test]
        public void GetRequired_ThrowsForUnknownProduct()
        {
            ProductCatalog catalog =
                new ProductCatalog(
                    Array.Empty<ProductDefinition>());

            Assert.Throws<KeyNotFoundException>(
                () =>
                    catalog.GetRequired(
                        new ProductId("UNKNOWN")));
        }


        private static ProductDefinition CreateProduct(
            string productId,
            string displayName,
            string categoryId)
        {
            return new ProductDefinition(
                new ProductId(productId),
                displayName,
                new ProductCategoryId(categoryId),
                StockUnit.Each);
        }
    }
}
