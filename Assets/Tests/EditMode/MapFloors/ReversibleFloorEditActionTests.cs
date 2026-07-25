using System.Collections.Generic;
using BigRetail.Map.Construction;
using BigRetail.Map.Domain;
using NUnit.Framework;

namespace BigRetail.Map.Floors.Tests
{
    public sealed class ReversibleFloorEditActionTests
    {
        private FloorState floorState;
        private FloorConstructionService service;


        [SetUp]
        public void SetUp()
        {
            List<GridPosition> cells =
                new List<GridPosition>();

            for (int x = 0; x <= 5; x++)
            {
                for (int y = 0; y <= 5; y++)
                {
                    cells.Add(
                        new GridPosition(
                            x,
                            y,
                            0));
                }
            }

            GridMapDefinition map =
                new GridMapDefinition(
                    "floor.action.test.map",
                    cells);

            ConstructionAreaDefinition area =
                new ConstructionAreaDefinition(
                    map,
                    cells);

            floorState =
                new FloorState();

            service =
                new FloorConstructionService(
                    map,
                    area,
                    floorState);
        }


        [Test]
        public void BuildAction_CanBeUndoneAndRedone()
        {
            GridPosition[] area =
                CreateArea();

            FloorEnsureResult buildResult =
                service.TryEnsureFloors(area);

            ConstructionHistory history =
                new ConstructionHistory();

            history.Record(
                new ReversibleFloorEditAction(
                    service,
                    buildResult.Edit));

            Assert.That(
                history.TryUndo(out _),
                Is.True);

            Assert.That(
                floorState.FloorCount,
                Is.EqualTo(0));

            Assert.That(
                history.TryRedo(out _),
                Is.True);

            Assert.That(
                floorState.FloorCount,
                Is.EqualTo(4));
        }


        [Test]
        public void DemolitionAction_CanBeUndoneAndRedone()
        {
            GridPosition[] area =
                CreateArea();

            Assert.That(
                service.TryEnsureFloors(area).Succeeded,
                Is.True);

            FloorClearResult clearResult =
                service.TryClearFloors(area);

            ConstructionHistory history =
                new ConstructionHistory();

            history.Record(
                new ReversibleFloorEditAction(
                    service,
                    clearResult.Edit));

            Assert.That(
                history.TryUndo(out _),
                Is.True);

            Assert.That(
                floorState.FloorCount,
                Is.EqualTo(4));

            Assert.That(
                history.TryRedo(out _),
                Is.True);

            Assert.That(
                floorState.FloorCount,
                Is.EqualTo(0));
        }


        [Test]
        public void MixedDemolition_UndoRestoresOnlyRemovedFloors()
        {
            GridPosition[] area =
                CreateArea();

            Assert.That(
                service.TryEnsureFloors(
                    new[]
                    {
                        area[0],
                        area[2]
                    }).Succeeded,
                Is.True);

            FloorClearResult clearResult =
                service.TryClearFloors(area);

            ConstructionHistory history =
                new ConstructionHistory();

            history.Record(
                new ReversibleFloorEditAction(
                    service,
                    clearResult.Edit));

            Assert.That(
                clearResult.RemovedCount,
                Is.EqualTo(2));

            Assert.That(
                history.TryUndo(out _),
                Is.True);

            Assert.That(
                floorState.HasFloor(area[0]),
                Is.True);

            Assert.That(
                floorState.HasFloor(area[1]),
                Is.False);

            Assert.That(
                floorState.HasFloor(area[2]),
                Is.True);

            Assert.That(
                floorState.HasFloor(area[3]),
                Is.False);
        }


        [Test]
        public void FailedUndo_PreservesHistoryAndCurrentState()
        {
            GridPosition[] area =
                CreateArea();

            FloorEnsureResult buildResult =
                service.TryEnsureFloors(area);

            ConstructionHistory history =
                new ConstructionHistory();

            history.Record(
                new ReversibleFloorEditAction(
                    service,
                    buildResult.Edit));

            // Simulate an external mutation that makes the recorded
            // inverse no longer match authoritative state.
            service.TryClearFloors(
                new[] { area[0] });

            bool undoSucceeded =
                history.TryUndo(
                    out ConstructionHistoryResult result);

            Assert.That(
                undoSucceeded,
                Is.False);

            Assert.That(
                result.Failure,
                Is.EqualTo(
                    ConstructionHistoryFailure
                        .ActionCouldNotBeApplied));

            Assert.That(
                history.CanUndo,
                Is.True);

            Assert.That(
                history.CanRedo,
                Is.False);

            Assert.That(
                floorState.HasFloor(area[1]),
                Is.True);
        }


        private static GridPosition[] CreateArea()
        {
            return new[]
            {
                new GridPosition(1, 1, 0),
                new GridPosition(2, 1, 0),
                new GridPosition(1, 2, 0),
                new GridPosition(2, 2, 0)
            };
        }
    }
}
