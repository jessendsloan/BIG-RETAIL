using BigRetail.Map.Domain;
using BigRetail.Map.Fixtures;
using NUnit.Framework;

namespace BigRetail.Map.Fixtures.Tests
{
    public sealed class FixtureFootprintResolverTests
    {
        private static readonly FixtureDefinition Definition =
            new FixtureDefinition(
                new FixtureDefinitionId("large-fixture"),
                "Large Fixture",
                2,
                3);


        [Test]
        public void Resolve_North_UsesAuthoredDimensionsAndLevel()
        {
            GridPosition anchor =
                new GridPosition(4, 6, 2);

            FixtureFootprint footprint =
                FixtureFootprintResolver.Resolve(
                    Definition,
                    anchor,
                    FixtureOrientation.North);

            Assert.That(footprint.AnchorCell, Is.EqualTo(anchor));
            Assert.That(footprint.WidthInCells, Is.EqualTo(2));
            Assert.That(footprint.DepthInCells, Is.EqualTo(3));
            Assert.That(footprint.CellCount, Is.EqualTo(6));

            Assert.That(
                footprint.ContainsCell(
                    new GridPosition(5, 8, 2)),
                Is.True);

            Assert.That(
                footprint.ContainsCell(
                    new GridPosition(5, 8, 1)),
                Is.False);
        }


        [TestCase(FixtureOrientation.East)]
        [TestCase(FixtureOrientation.West)]
        public void Resolve_QuarterTurn_SwapsWidthAndDepth(
            FixtureOrientation orientation)
        {
            FixtureFootprint footprint =
                FixtureFootprintResolver.Resolve(
                    Definition,
                    new GridPosition(2, 3),
                    orientation);

            Assert.That(footprint.WidthInCells, Is.EqualTo(3));
            Assert.That(footprint.DepthInCells, Is.EqualTo(2));
            Assert.That(footprint.CellCount, Is.EqualTo(6));
            Assert.That(
                footprint.ContainsCell(
                    new GridPosition(4, 4)),
                Is.True);
        }


        [TestCase(FixtureOrientation.North)]
        [TestCase(FixtureOrientation.South)]
        public void Resolve_OppositeAuthoredAxis_UsesSameBounds(
            FixtureOrientation orientation)
        {
            FixtureFootprint footprint =
                FixtureFootprintResolver.Resolve(
                    Definition,
                    new GridPosition(2, 3),
                    orientation);

            Assert.That(footprint.WidthInCells, Is.EqualTo(2));
            Assert.That(footprint.DepthInCells, Is.EqualTo(3));
        }
    }
}
