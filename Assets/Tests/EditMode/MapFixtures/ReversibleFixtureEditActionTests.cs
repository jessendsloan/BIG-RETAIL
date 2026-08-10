using System.Collections.Generic;
using BigRetail.Map.Construction;
using BigRetail.Map.Domain;
using BigRetail.Map.Fixtures;
using NUnit.Framework;

namespace BigRetail.Map.Fixtures.Tests
{
    public sealed class ReversibleFixtureEditActionTests
    {
        private static readonly FixtureDefinitionId ShelfDefinitionId =
            new FixtureDefinitionId("standard-shelf");

        private static readonly FixtureAccessMode SalesFloorAccess =
            FixtureAccessMode.CustomerBrowse
            | FixtureAccessMode.EmployeeStock;

        private static readonly FixtureAccessProfile LongFaceAccess =
            new FixtureAccessProfile(
                FixtureAccessMode.None,
                SalesFloorAccess,
                FixtureAccessMode.None,
                SalesFloorAccess);

        private FixtureState state;
        private FixturePlacementService service;


        [SetUp]
        public void SetUp()
        {
            HashSet<GridPosition> cells =
                new HashSet<GridPosition>();

            for (int x = 0; x <= 5; x++)
            {
                for (int y = 0; y <= 5; y++)
                {
                    cells.Add(
                        new GridPosition(x, y));
                }
            }

            GridMapDefinition map =
                new GridMapDefinition(
                    "fixture-history-test",
                    cells);

            state =
                new FixtureState();

            service =
                new FixturePlacementService(
                    map,
                    new ConstructionAreaDefinition(
                        map,
                        cells),
                    new FixtureDefinitionCatalog(
                        new[]
                        {
                            new FixtureDefinition(
                                ShelfDefinitionId,
                                "Standard Shelf",
                                1,
                                2,
                                LongFaceAccess)
                        }),
                    state,
                    new TestSurfaceQuery(cells));
        }


        [Test]
        public void PlacementAction_UndoAndRedo_PreserveExactPlacement()
        {
            FixturePlacementResult placement =
                service.TryPlaceFixture(
                    new FixtureInstanceId("shelf-1"),
                    ShelfDefinitionId,
                    new GridPosition(2, 2),
                    FixtureOrientation.East);

            ConstructionHistory history =
                new ConstructionHistory();

            history.Record(
                new ReversibleFixtureEditAction(
                    service,
                    placement.Edit));

            Assert.That(history.TryUndo(out _), Is.True);
            Assert.That(state.FixtureCount, Is.EqualTo(0));

            Assert.That(history.TryRedo(out _), Is.True);
            Assert.That(state.FixtureCount, Is.EqualTo(1));
            Assert.That(state.OccupiedCellCount, Is.EqualTo(2));

            Assert.That(
                state.TryGetFixture(
                    placement.InstanceId,
                    out FixtureInstance restored),
                Is.True);

            Assert.That(
                restored.AnchorCell,
                Is.EqualTo(new GridPosition(2, 2)));

            Assert.That(
                restored.Orientation,
                Is.EqualTo(FixtureOrientation.East));
        }


        [Test]
        public void RemovalAction_UndoAndRedo_RestoreAndRemoveFixture()
        {
            FixtureInstanceId instanceId =
                new FixtureInstanceId("shelf-1");

            Assert.That(
                service.TryPlaceFixture(
                        instanceId,
                        ShelfDefinitionId,
                        new GridPosition(2, 2),
                        FixtureOrientation.North)
                    .Succeeded,
                Is.True);

            FixturePlacementResult removal =
                service.TryRemoveFixture(instanceId);

            ReversibleFixtureEditAction action =
                new ReversibleFixtureEditAction(
                    service,
                    removal.Edit);

            Assert.That(action.TryUndo().Succeeded, Is.True);
            Assert.That(state.FixtureCount, Is.EqualTo(1));
            Assert.That(state.OccupiedCellCount, Is.EqualTo(2));
            Assert.That(state.ReservedAccessCellCount, Is.EqualTo(4));
            Assert.That(
                state.ReservedAccessBoundaryCount,
                Is.EqualTo(4));

            Assert.That(action.TryRedo().Succeeded, Is.True);
            Assert.That(state.FixtureCount, Is.EqualTo(0));
            Assert.That(state.OccupiedCellCount, Is.EqualTo(0));
            Assert.That(state.ReservedAccessCellCount, Is.EqualTo(0));
            Assert.That(
                state.ReservedAccessBoundaryCount,
                Is.EqualTo(0));
        }


        [Test]
        public void RemovalRedo_ReusedInstanceId_IsRejectedWithoutMutation()
        {
            FixtureInstanceId instanceId =
                new FixtureInstanceId("shelf-1");

            Assert.That(
                service.TryPlaceFixture(
                        instanceId,
                        ShelfDefinitionId,
                        new GridPosition(1, 1),
                        FixtureOrientation.North)
                    .Succeeded,
                Is.True);

            FixturePlacementResult removal =
                service.TryRemoveFixture(instanceId);

            ReversibleFixtureEditAction action =
                new ReversibleFixtureEditAction(
                    service,
                    removal.Edit);

            Assert.That(
                service.TryPlaceFixture(
                        instanceId,
                        ShelfDefinitionId,
                        new GridPosition(3, 3),
                        FixtureOrientation.East)
                    .Succeeded,
                Is.True);

            ConstructionActionResult result =
                action.TryRedo();

            Assert.That(result.Succeeded, Is.False);
            Assert.That(state.FixtureCount, Is.EqualTo(1));
            Assert.That(
                state.TryGetFixture(
                    instanceId,
                    out FixtureInstance replacement),
                Is.True);
            Assert.That(
                replacement.AnchorCell,
                Is.EqualTo(new GridPosition(3, 3)));
        }


        private sealed class TestSurfaceQuery :
            IFixturePlacementSurfaceQuery
        {
            private readonly HashSet<GridPosition> floors;


            public TestSurfaceQuery(
                IEnumerable<GridPosition> floors)
            {
                this.floors =
                    new HashSet<GridPosition>(floors);
            }


            public bool HasFloor(
                GridPosition cell)
            {
                return floors.Contains(cell);
            }


            public bool HasWall(
                CellEdge edge)
            {
                return false;
            }


            public bool IsReservedForDoorPassage(
                GridPosition cell)
            {
                return false;
            }
        }
    }
}
