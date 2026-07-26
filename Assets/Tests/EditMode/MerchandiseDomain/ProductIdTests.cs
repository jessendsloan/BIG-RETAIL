using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace BigRetail.Merchandise.Domain.Tests
{
    /// <summary>
    /// Locks down canonical product identity behavior used across every future
    /// merchandise, inventory, shelf, basket, and checkout system.
    /// </summary>
    public sealed class ProductIdTests
    {
        [Test]
        public void Constructor_NormalizesWhitespaceAndCase()
        {
            ProductId productId =
                new ProductId(
                    "  choco-bar-155  ");

            Assert.That(
                productId.Value,
                Is.EqualTo("CHOCO-BAR-155"));
        }

        [Test]
        public void Constructor_RejectsEmptyValue()
        {
            Assert.Throws<ArgumentException>(
                () => new ProductId("   "));
        }

        [Test]
        public void EquivalentText_CreatesEqualIdentifiers()
        {
            ProductId first =
                new ProductId("ROMA-TOMATO");

            ProductId second =
                new ProductId("roma-tomato");

            Assert.That(
                second,
                Is.EqualTo(first));
        }

        [Test]
        public void EquivalentText_CreatesOneDictionaryEntry()
        {
            ProductId first =
                new ProductId("COLA-12OZ-CAN");

            ProductId second =
                new ProductId("cola-12oz-can");

            Dictionary<ProductId, int> quantities =
                new Dictionary<ProductId, int>
                {
                    [first] = 12
                };

            quantities[second] = 24;

            Assert.That(
                quantities.Count,
                Is.EqualTo(1));

            Assert.That(
                quantities[first],
                Is.EqualTo(24));
        }
    }
}
