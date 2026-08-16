using System;
using System.Collections.Generic;
using BigRetail.Map.Construction;
using BigRetail.Map.Domain;
using BigRetail.Map.Fixtures;
using NUnit.Framework;

namespace BigRetail.Map.Fixtures.Tests
{
    public sealed class FixtureAccessQueryServiceTests
    {
        private static readonly FixtureDefinitionId ShelfDefinitionId =
            new FixtureDefinitionId("standard-shelf");

        private static readonly FixtureDefinitionId BlockerDefinitionId =
            new FixtureDefinitionId("blocker");

        private static readonly FixtureAccessMode SalesFloorAccess =
            FixtureAccessMode.CustomerBrowse
            | FixtureAccessMode.EmployeeStock;


        [Test]
        public void GetAvailableAccessPoints_FiltersUnavailableSurfaceCells()
        {
            TestContext context = CreateContext();

            FixtureInstanceId shelfId =
                new FixtureInstanceId("shelf-1");

            Assert.That(
                context.Placement.TryPlaceFixture(
                        shelfId,
                        ShelfDefinitionId,
                        new GridPosition(2, 2),
                        FixtureOrientation.North)
                    .Succeeded,
                Is.True);

            context.Surface.SetUnavailable(
                new GridPosition(2, 1));

            IReadOnlyList<FixtureAccessPoint> points =
                context.Access.GetAvailableAccessPoints(
                    shelfId,
                    FixtureAccessMode.CustomerBrowse);

            Assert.That(points.Count, Is.EqualTo(3));
            Assert.That(
                points,
                Does.Contain(
                    new FixtureAccessPoint(
                        new GridPosition(2, 3),
                        FixtureSide.North,
                        SalesFloorAccess)));
            Assert.That(
                points,
                Does.Contain(
                    new FixtureAccessPoint(
                        new GridPosition(3, 3),
                        FixtureSide.North,
                        SalesFloorAccess)));
            Assert.That(
                points,
                Does.Contain(
                    new FixtureAccessPoint(
                        new GridPosition(3, 1),
                        FixtureSide.South,
                        SalesFloorAccess)));
        }


        [Test]
        public void GetAvailableAccessPoints_FiltersWallAcrossFixtureFace()
        {
            TestContext context = CreateContext();

            FixtureInstanceId shelfId =
                new FixtureInstanceId("shelf-1");

            Assert.That(
                context.Placement.TryPlaceFixture(
                        shelfId,
                        ShelfDefinitionId,
                        new GridPosition(2, 2),
                        FixtureOrientation.North)
                    .Succeeded,
                Is.True);

            context.Surface.SetWall(
                new CellEdge(
                    new GridPosition(2, 2),
                    CellEdgeDirection.NorthWest));

            IReadOnlyList<FixtureAccessPoint> points =
                context.Access.GetAvailableAccessPoints(
                    shelfId,
                    FixtureAccessMode.CustomerBrowse);

            FixtureAccessPoint blockedPoint =
                new FixtureAccessPoint(
                    new GridPosition(2, 3),
                    FixtureSide.North,
                    SalesFloorAccess);

            bool containsBlockedPoint = false;

            for (int index = 0;
                 index < points.Count;
                 index++)
            {
                containsBlockedPoint |=
                    points[index] == blockedPoint;
            }

            Assert.That(points.Count, Is.EqualTo(3));
            Assert.That(containsBlockedPoint, Is.False);
        }


        [Test]
        public void GetAvailableAccessPoints_RequiredMode_FiltersOtherInteractions()
        {
            TestContext context =
                CreateContext(
                    new FixtureAccessProfile(
                        FixtureAccessMode.CustomerBrowse,
                        FixtureAccessMode.None,
                        FixtureAccessMode.EmployeeStock,
                        FixtureAccessMode.None));

            FixtureInstanceId shelfId =
                new FixtureInstanceId("shelf-1");

            Assert.That(
                context.Placement.TryPlaceFixture(
                        shelfId,
                        ShelfDefinitionId,
                        new GridPosition(2, 2),
                        FixtureOrientation.North)
                    .Succeeded,
                Is.True);

            IReadOnlyList<FixtureAccessPoint> browsePoints =
                context.Access.GetAvailableAccessPoints(
                    shelfId,
                    FixtureAccessMode.CustomerBrowse);

            IReadOnlyList<FixtureAccessPoint> stockPoints =
                context.Access.GetAvailableAccessPoints(
                    shelfId,
                    FixtureAccessMode.EmployeeStock);

            IReadOnlyList<FixtureAccessPoint> combinedPoints =
                context.Access.GetAvailableAccessPoints(
                    shelfId,
                    SalesFloorAccess);

            Assert.That(browsePoints.Count, Is.EqualTo(2));
            Assert.That(
                browsePoints[0].Side,
                Is.EqualTo(FixtureSide.North));
            Assert.That(stockPoints.Count, Is.EqualTo(2));
            Assert.That(
                stockPoints[0].Side,
                Is.EqualTo(FixtureSide.South));
            Assert.That(combinedPoints, Is.Empty);
        }


        [Test]
        public void TryFindNearestAvailableAccessPoint_ReturnsClosestUsableCell()
        {
            TestContext context = CreateContext();

            FixtureInstanceId shelfId =
                new FixtureInstanceId("shelf-1");

            Assert.That(
                context.Placement.TryPlaceFixture(
                        shelfId,
                        ShelfDefinitionId,
                        new GridPosition(2, 2),
                        FixtureOrientation.North)
                    .Succeeded,
                Is.True);

            bool found =
                context.Access.TryFindNearestAvailableAccessPoint(
                    shelfId,
                    FixtureAccessMode.EmployeeStock,
                    new GridPosition(4, 3),
                    out FixtureAccessPoint point);

            Assert.That(found, Is.True);
            Assert.That(
                point.Cell,
                Is.EqualTo(new GridPosition(3, 3)));
            Assert.That(point.Side, Is.EqualTo(FixtureSide.North));
        }


        [Test]
        public void TryFindNearestAvailableAccessPoint_NoUsableCell_ReturnsFalse()
        {
            TestContext context = CreateContext();

            FixtureInstanceId shelfId =
                new FixtureInstanceId("shelf-1");

            Assert.That(
                context.Placement.TryPlaceFixture(
                        shelfId,
                        ShelfDefinitionId,
                        new GridPosition(2, 2),
                        FixtureOrientation.North)
                    .Succeeded,
                Is.True);

            context.Surface.ClearAvailableCells();

            Assert.That(
                context.Access.TryFindNearestAvailableAccessPoint(
                    shelfId,
                    FixtureAccessMode.CustomerBrowse,
                    new GridPosition(0, 0),
                    out _),
                Is.False);
        }


        [Test]
        public void Query_UnknownFixture_ReturnsNoAccess()
        {
            TestContext context = CreateContext();

            FixtureInstanceId unknown =
                new FixtureInstanceId("unknown");

            Assert.That(
                context.Access.GetAvailableAccessPoints(
                    unknown,
                    FixtureAccessMode.CustomerBrowse),
                Is.Empty);

            Assert.That(
                context.Access.TryFindNearestAvailableAccessPoint(
                    unknown,
                    FixtureAccessMode.CustomerBrowse,
                    new GridPosition(0, 0),
                    out _),
                Is.False);
        }


        [TestCase(FixtureAccessMode.None)]
        [TestCase((FixtureAccessMode)16)]
        public void Query_UnsupportedRequiredMode_IsRejected(
            FixtureAccessMode mode)
        {
            TestContext context = CreateContext();

            Assert.That(
                () => context.Access.GetAvailableAccessPoints(
                    new FixtureInstanceId("shelf-1"),
                    mode),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }


        private static TestContext CreateContext(
            FixtureAccessProfile shelfAccess = null)
        {
            HashSet<GridPosition> cells =
                new HashSet<GridPosition>();

            for (int x = 0; x <= 6; x++)
            {
                for (int y = 0; y <= 6; y++)
                {
                    cells.Add(new GridPosition(x, y));
                }
            }

            GridMapDefinition map =
                new GridMapDefinition(
                    "fixture-access-test",
                    cells);

            FixtureState state =
                new FixtureState();

            MutableSurfaceQuery surface =
                new MutableSurfaceQuery(cells);

            FixtureDefinition shelf =
                new FixtureDefinition(
                    ShelfDefinitionId,
                    "Standard Shelf",
                    2,
                    1,
                    shelfAccess
                    ?? new FixtureAccessProfile(
                        SalesFloorAccess,
                        FixtureAccessMode.None,
                        SalesFloorAccess,
                        FixtureAccessMode.None));

            FixtureDefinition blocker =
                new FixtureDefinition(
                    BlockerDefinitionId,
                    "Blocker",
                    1,
                    1);

            FixturePlacementService placement =
                new FixturePlacementService(
                    map,
                    new ConstructionAreaDefinition(
                        map,
                        cells),
                    new FixtureDefinitionCatalog(
                        new[]
                        {
                            shelf,
                            blocker
                        }),
                    state,
                    surface);

            return new TestContext(
                placement,
                new FixtureAccessQueryService(
                    state,
                    surface),
                surface);
        }


        private sealed class TestContext
        {
            public FixturePlacementService Placement { get; }

            public FixtureAccessQueryService Access { get; }

            public MutableSurfaceQuery Surface { get; }


            public TestContext(
                FixturePlacementService placement,
                FixtureAccessQueryService access,
                MutableSurfaceQuery surface)
            {
                Placement = placement;
                Access = access;
                Surface = surface;
            }
        }


        private sealed class MutableSurfaceQuery :
            IFixturePlacementSurfaceQuery,
            IFixtureAccessSurfaceQuery
        {
            private readonly HashSet<GridPosition> availableCells;

            private readonly HashSet<CellEdge> walls =
                new HashSet<CellEdge>();


            public MutableSurfaceQuery(
                IEnumerable<GridPosition> availableCells)
            {
                this.availableCells =
                    new HashSet<GridPosition>(availableCells);
            }


            public bool HasFloor(GridPosition cell)
            {
                return availableCells.Contains(cell);
            }


            public bool HasWall(
                CellEdge edge)
            {
                return walls.Contains(edge);
            }


            public bool IsReservedForDoorPassage(
                GridPosition cell)
            {
                return false;
            }


            public bool CanUseAccessPoint(
                FixtureAccessPoint accessPoint)
            {
                return availableCells.Contains(
                        accessPoint.Cell)
                    && !walls.Contains(
                        accessPoint.BoundaryEdge);
            }


            public void SetWall(CellEdge edge)
            {
                walls.Add(edge);
            }


            public void SetUnavailable(GridPosition cell)
            {
                availableCells.Remove(cell);
            }


            public void ClearAvailableCells()
            {
                availableCells.Clear();
            }
        }
    }
}
