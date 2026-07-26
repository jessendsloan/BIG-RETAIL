using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace BigRetail.Inventory.Domain.Tests
{
    public sealed class StorageLocationIdTests
    {
        [Test]
        public void Constructor_NormalizesWhitespaceAndCase()
        {
            StorageLocationId locationId =
                new StorageLocationId(
                    "  backroom-rack-a  ");

            Assert.That(
                locationId.Value,
                Is.EqualTo("BACKROOM-RACK-A"));
        }

        [Test]
        public void EquivalentText_CreatesOneHashSetEntry()
        {
            HashSet<StorageLocationId> locations =
                new HashSet<StorageLocationId>
                {
                    new StorageLocationId("sales-floor-01"),
                    new StorageLocationId(" SALES-FLOOR-01 ")
                };

            Assert.That(
                locations.Count,
                Is.EqualTo(1));
        }

        [Test]
        public void EmptyText_Throws()
        {
            Assert.Throws<ArgumentException>(
                () => new StorageLocationId("   "));
        }
    }
}
