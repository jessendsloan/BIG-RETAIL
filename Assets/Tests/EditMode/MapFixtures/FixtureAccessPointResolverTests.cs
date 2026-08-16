using System;
using System.Collections.Generic;
using BigRetail.Map.Domain;
using BigRetail.Map.Fixtures;
using NUnit.Framework;

namespace BigRetail.Map.Fixtures.Tests
{
    public sealed class FixtureAccessPointResolverTests
    {
        private const FixtureAccessMode SalesFloorAccess =
            FixtureAccessMode.CustomerBrowse
            | FixtureAccessMode.EmployeeStock;


        [Test]
        public void Resolve_NorthDoubleSidedShelf_UsesBothLongSides()
        {
            FixtureDefinition definition =
                CreateDoubleSidedShelfDefinition();

            FixtureFootprint footprint =
                FixtureFootprintResolver.Resolve(
                    definition,
                    new GridPosition(5, 7, 2),
                    FixtureOrientation.North);

            IReadOnlyList<FixtureAccessPoint> points =
                FixtureAccessPointResolver.Resolve(
                    definition,
                    footprint);

            Assert.That(points.Count, Is.EqualTo(4));
            AssertPoint(
                points,
                new GridPosition(5, 8, 2),
                FixtureSide.North);
            AssertPoint(
                points,
                new GridPosition(6, 8, 2),
                FixtureSide.North);
            AssertPoint(
                points,
                new GridPosition(5, 6, 2),
                FixtureSide.South);
            AssertPoint(
                points,
                new GridPosition(6, 6, 2),
                FixtureSide.South);
        }


        [Test]
        public void Resolve_EastDoubleSidedShelf_RotatesAccessToWorldEastWest()
        {
            FixtureDefinition definition =
                CreateDoubleSidedShelfDefinition();

            FixtureFootprint footprint =
                FixtureFootprintResolver.Resolve(
                    definition,
                    new GridPosition(3, 4),
                    FixtureOrientation.East);

            IReadOnlyList<FixtureAccessPoint> points =
                FixtureAccessPointResolver.Resolve(
                    definition,
                    footprint);

            Assert.That(footprint.WidthInCells, Is.EqualTo(1));
            Assert.That(footprint.DepthInCells, Is.EqualTo(2));
            Assert.That(points.Count, Is.EqualTo(4));
            AssertPoint(
                points,
                new GridPosition(4, 4),
                FixtureSide.East);
            AssertPoint(
                points,
                new GridPosition(4, 5),
                FixtureSide.East);
            AssertPoint(
                points,
                new GridPosition(2, 4),
                FixtureSide.West);
            AssertPoint(
                points,
                new GridPosition(2, 5),
                FixtureSide.West);
        }


        [TestCase(
            FixtureOrientation.North,
            FixtureSide.North,
            8,
            10,
            9,
            10)]
        [TestCase(
            FixtureOrientation.East,
            FixtureSide.West,
            7,
            9,
            7,
            10)]
        [TestCase(
            FixtureOrientation.South,
            FixtureSide.South,
            8,
            8,
            9,
            8)]
        [TestCase(
            FixtureOrientation.West,
            FixtureSide.East,
            9,
            9,
            9,
            10)]
        public void Resolve_LocalNorthSide_RotatesToExpectedWorldEdge(
            FixtureOrientation orientation,
            FixtureSide expectedSide,
            int firstX,
            int firstY,
            int secondX,
            int secondY)
        {
            FixtureDefinition definition =
                new FixtureDefinition(
                    new FixtureDefinitionId("wall-display"),
                    "Wall Display",
                    2,
                    1,
                    new FixtureAccessProfile(
                        FixtureAccessMode.CustomerBrowse,
                        FixtureAccessMode.None,
                        FixtureAccessMode.None,
                        FixtureAccessMode.None));

            FixtureFootprint footprint =
                FixtureFootprintResolver.Resolve(
                    definition,
                    new GridPosition(8, 9),
                    orientation);

            IReadOnlyList<FixtureAccessPoint> points =
                FixtureAccessPointResolver.Resolve(
                    definition,
                    footprint);

            Assert.That(points.Count, Is.EqualTo(2));
            Assert.That(
                points,
                Does.Contain(
                    new FixtureAccessPoint(
                        new GridPosition(firstX, firstY),
                        expectedSide,
                        FixtureAccessMode.CustomerBrowse)));
            Assert.That(
                points,
                Does.Contain(
                    new FixtureAccessPoint(
                        new GridPosition(secondX, secondY),
                        expectedSide,
                        FixtureAccessMode.CustomerBrowse)));
        }


        [TestCase(
            FixtureOrientation.North,
            FixtureSide.South,
            8,
            8,
            9,
            8)]
        [TestCase(
            FixtureOrientation.East,
            FixtureSide.East,
            9,
            9,
            9,
            10)]
        [TestCase(
            FixtureOrientation.South,
            FixtureSide.North,
            8,
            10,
            9,
            10)]
        [TestCase(
            FixtureOrientation.West,
            FixtureSide.West,
            7,
            9,
            7,
            10)]
        public void Resolve_HalfShelf_ReservesBothFrontCellsAfterRotation(
            FixtureOrientation orientation,
            FixtureSide expectedSide,
            int firstX,
            int firstY,
            int secondX,
            int secondY)
        {
            FixtureDefinition definition =
                new FixtureDefinition(
                    new FixtureDefinitionId("half-shelf"),
                    "Half Shelf",
                    2,
                    1,
                    new FixtureAccessProfile(
                        FixtureAccessMode.None,
                        FixtureAccessMode.None,
                        SalesFloorAccess,
                        FixtureAccessMode.None));

            FixtureFootprint footprint =
                FixtureFootprintResolver.Resolve(
                    definition,
                    new GridPosition(8, 9),
                    orientation);

            IReadOnlyList<FixtureAccessPoint> points =
                FixtureAccessPointResolver.Resolve(
                    definition,
                    footprint);

            Assert.That(points.Count, Is.EqualTo(2));
            AssertPoint(
                points,
                new GridPosition(firstX, firstY),
                expectedSide);
            AssertPoint(
                points,
                new GridPosition(secondX, secondY),
                expectedSide);
        }


        [Test]
        public void AccessProfile_UnsupportedMode_IsRejected()
        {
            Assert.That(
                () => new FixtureAccessProfile(
                    (FixtureAccessMode)16,
                    FixtureAccessMode.None,
                    FixtureAccessMode.None,
                    FixtureAccessMode.None),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }


        [TestCase(
            FixtureSide.North,
            CellEdgeDirection.NorthWest,
            5,
            5)]
        [TestCase(
            FixtureSide.East,
            CellEdgeDirection.NorthEast,
            4,
            6)]
        [TestCase(
            FixtureSide.South,
            CellEdgeDirection.NorthWest,
            5,
            6)]
        [TestCase(
            FixtureSide.West,
            CellEdgeDirection.NorthEast,
            5,
            6)]
        public void AccessPoint_BoundaryEdge_SeparatesStandCellFromFace(
            FixtureSide side,
            CellEdgeDirection expectedDirection,
            int expectedAnchorX,
            int expectedAnchorY)
        {
            FixtureAccessPoint point =
                new FixtureAccessPoint(
                    new GridPosition(5, 6),
                    side,
                    SalesFloorAccess);

            Assert.That(
                point.BoundaryEdge,
                Is.EqualTo(
                    new CellEdge(
                        new GridPosition(
                            expectedAnchorX,
                            expectedAnchorY),
                        expectedDirection)));
        }


        private static FixtureDefinition
            CreateDoubleSidedShelfDefinition()
        {
            return new FixtureDefinition(
                new FixtureDefinitionId("gondola-shelf"),
                "Gondola Shelf",
                2,
                1,
                new FixtureAccessProfile(
                    SalesFloorAccess,
                    FixtureAccessMode.None,
                    SalesFloorAccess,
                    FixtureAccessMode.None));
        }


        private static void AssertPoint(
            IReadOnlyList<FixtureAccessPoint> points,
            GridPosition expectedCell,
            FixtureSide expectedSide)
        {
            FixtureAccessPoint expected =
                new FixtureAccessPoint(
                    expectedCell,
                    expectedSide,
                    SalesFloorAccess);

            Assert.That(points, Does.Contain(expected));
        }
    }
}
