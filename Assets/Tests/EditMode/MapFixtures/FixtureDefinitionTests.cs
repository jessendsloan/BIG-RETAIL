using System;
using BigRetail.Map.Fixtures;
using NUnit.Framework;

namespace BigRetail.Map.Fixtures.Tests
{
    public sealed class FixtureDefinitionTests
    {
        [Test]
        public void Constructor_ValidShelf_StoresAuthoredFootprint()
        {
            FixtureDefinition definition =
                new FixtureDefinition(
                    new FixtureDefinitionId("shelf-standard"),
                    "Standard Shelf",
                    1,
                    2);

            Assert.That(
                definition.Id,
                Is.EqualTo(
                    new FixtureDefinitionId("SHELF-STANDARD")));

            Assert.That(
                definition.DisplayName,
                Is.EqualTo("Standard Shelf"));

            Assert.That(definition.WidthInCells, Is.EqualTo(1));
            Assert.That(definition.DepthInCells, Is.EqualTo(2));
            Assert.That(definition.OccupiedCellCount, Is.EqualTo(2));
            Assert.That(definition.AccessProfile.HasAnyAccess, Is.False);
        }


        [Test]
        public void Constructor_AccessProfile_PreservesRetailRules()
        {
            FixtureAccessProfile accessProfile =
                new FixtureAccessProfile(
                    FixtureAccessMode.CustomerBrowse,
                    FixtureAccessMode.None,
                    FixtureAccessMode.EmployeeStock,
                    FixtureAccessMode.None);

            FixtureDefinition definition =
                new FixtureDefinition(
                    new FixtureDefinitionId("shelf-access"),
                    "Accessible Shelf",
                    2,
                    1,
                    accessProfile);

            Assert.That(
                definition.AccessProfile,
                Is.SameAs(accessProfile));
            Assert.That(
                definition.AccessProfile.North.Includes(
                    FixtureAccessMode.CustomerBrowse),
                Is.True);
            Assert.That(
                definition.AccessProfile.South.Includes(
                    FixtureAccessMode.EmployeeStock),
                Is.True);
        }


        [TestCase(0, 1)]
        [TestCase(1, 0)]
        [TestCase(-1, 1)]
        public void Constructor_NonPositiveDimension_IsRejected(
            int width,
            int depth)
        {
            Assert.That(
                () => new FixtureDefinition(
                    new FixtureDefinitionId("invalid"),
                    "Invalid",
                    width,
                    depth),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }


        [Test]
        public void Catalog_DuplicateDefinitionId_IsRejected()
        {
            FixtureDefinition first =
                new FixtureDefinition(
                    new FixtureDefinitionId("shelf"),
                    "Shelf",
                    1,
                    2);

            FixtureDefinition duplicate =
                new FixtureDefinition(
                    new FixtureDefinitionId(" SHELF "),
                    "Another Shelf",
                    2,
                    1);

            Assert.That(
                () => new FixtureDefinitionCatalog(
                    new[]
                    {
                        first,
                        duplicate
                    }),
                Throws.TypeOf<ArgumentException>());
        }


        [Test]
        public void OrientationRotation_FourSteps_ReturnsToNorth()
        {
            FixtureOrientation orientation =
                FixtureOrientation.North;

            for (int step = 0; step < 4; step++)
            {
                orientation =
                    orientation.RotateClockwise();
            }

            Assert.That(
                orientation,
                Is.EqualTo(FixtureOrientation.North));
        }
    }
}
