using System;
using NUnit.Framework;

namespace BigRetail.Merchandise.Domain.Tests
{
    public sealed class BrandCatalogTests
    {
        [Test]
        public void Constructor_PreservesAuthoredOrderAndLookup()
        {
            BrandDefinition bright =
                new BrandDefinition(
                    new BrandId("BRIGHT"),
                    "Bright Beverage Co.");
            BrandDefinition homestead =
                new BrandDefinition(
                    new BrandId("HOMESTEAD"),
                    "Homestead Foods");

            BrandCatalog catalog =
                new BrandCatalog(new[] { bright, homestead });

            Assert.That(catalog.Count, Is.EqualTo(2));
            Assert.That(
                catalog.GetRequired(new BrandId("bright")),
                Is.SameAs(bright));
            Assert.That(
                catalog.EnumerateDefinitions(),
                Is.EqualTo(new[] { bright, homestead }));
        }

        [Test]
        public void Constructor_RejectsDuplicateBrandIds()
        {
            Assert.Throws<ArgumentException>(
                () => new BrandCatalog(
                    new[]
                    {
                        new BrandDefinition(
                            new BrandId("BRIGHT"),
                            "Bright Beverage Co."),
                        new BrandDefinition(
                            new BrandId("bright"),
                            "Bright Alternate")
                    }));
        }
    }
}
