using System;
using NUnit.Framework;

namespace BigRetail.Inventory.Domain.Tests
{
    public sealed class StorageLocationDefinitionTests
    {
        [Test]
        public void Constructor_PreservesLogicalRole()
        {
            StorageLocationDefinition location =
                new StorageLocationDefinition(
                    new StorageLocationId("backroom-a"),
                    " Backroom A ",
                    StorageRole.Backroom);

            Assert.That(
                location.DisplayName,
                Is.EqualTo("Backroom A"));

            Assert.That(
                location.Role,
                Is.EqualTo(StorageRole.Backroom));
        }

        [Test]
        public void UnsupportedRole_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () =>
                    new StorageLocationDefinition(
                        new StorageLocationId("bad-role"),
                        "Bad Role",
                        (StorageRole)99));
        }
    }
}
