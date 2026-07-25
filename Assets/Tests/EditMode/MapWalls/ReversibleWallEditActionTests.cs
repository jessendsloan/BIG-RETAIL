using System.Collections.Generic;
using BigRetail.Map.Construction;
using BigRetail.Map.Domain;
using NUnit.Framework;

namespace BigRetail.Map.Walls.Tests
{
    public sealed class ReversibleWallEditActionTests
    {
        private WallState wallState;
        private WallConstructionService service;


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
                    "wall.action.test.map",
                    cells);

            ConstructionAreaDefinition area =
                new ConstructionAreaDefinition(
                    map,
                    cells);

            wallState =
                new WallState();

            service =
                new WallConstructionService(
                    map,
                    area,
                    wallState);
        }


        [Test]
        public void EnsureResult_EditContainsOnlyNewWalls()
        {
            CellEdge[] run =
                CreateRun(
                    2,
                    1,
                    3);

            Assert.That(
                service.TryPlaceWall(run[1]).Succeeded,
                Is.True);

            WallEnsureResult result =
                service.TryEnsureWalls(run);

            Assert.That(
                result.Succeeded,
                Is.True);

            Assert.That(
                result.ChangedCount,
                Is.EqualTo(2));

            Assert.That(
                result.Edit.Edges,
                Has.Member(run[0]));

            Assert.That(
                result.Edit.Edges,
                Has.No.Member(run[1]));

            Assert.That(
                result.Edit.Edges,
                Has.Member(run[2]));
        }


        [Test]
        public void ClearResult_EditContainsOnlyRemovedWalls()
        {
            CellEdge[] run =
                CreateRun(
                    2,
                    1,
                    3);

            Assert.That(
                service.TryPlaceWall(run[0]).Succeeded,
                Is.True);

            Assert.That(
                service.TryPlaceWall(run[2]).Succeeded,
                Is.True);

            WallClearResult result =
                service.TryClearWalls(run);

            Assert.That(
                result.Succeeded,
                Is.True);

            Assert.That(
                result.RemovedCount,
                Is.EqualTo(2));

            Assert.That(
                result.Edit.Edges,
                Has.Member(run[0]));

            Assert.That(
                result.Edit.Edges,
                Has.No.Member(run[1]));

            Assert.That(
                result.Edit.Edges,
                Has.Member(run[2]));
        }


        [Test]
        public void BuildAction_CanBeUndoneAndRedone()
        {
            CellEdge[] run =
                CreateRun(
                    2,
                    1,
                    3);

            WallEnsureResult buildResult =
                service.TryEnsureWalls(run);

            ConstructionHistory history =
                new ConstructionHistory();

            history.Record(
                new ReversibleWallEditAction(
                    service,
                    buildResult.Edit));

            Assert.That(
                history.TryUndo(out _),
                Is.True);

            Assert.That(
                wallState.WallCount,
                Is.EqualTo(0));

            Assert.That(
                history.TryRedo(out _),
                Is.True);

            Assert.That(
                wallState.WallCount,
                Is.EqualTo(3));
        }


        [Test]
        public void DemolitionAction_CanBeUndoneAndRedone()
        {
            CellEdge[] run =
                CreateRun(
                    2,
                    1,
                    3);

            Assert.That(
                service.TryPlaceWalls(run).Succeeded,
                Is.True);

            WallClearResult clearResult =
                service.TryClearWalls(run);

            ConstructionHistory history =
                new ConstructionHistory();

            history.Record(
                new ReversibleWallEditAction(
                    service,
                    clearResult.Edit));

            Assert.That(
                history.TryUndo(out _),
                Is.True);

            Assert.That(
                wallState.WallCount,
                Is.EqualTo(3));

            Assert.That(
                history.TryRedo(out _),
                Is.True);

            Assert.That(
                wallState.WallCount,
                Is.EqualTo(0));
        }


        [Test]
        public void FailedUndo_PreservesHistoryAndCurrentState()
        {
            CellEdge[] run =
                CreateRun(
                    2,
                    1,
                    2);

            WallEnsureResult buildResult =
                service.TryEnsureWalls(run);

            ConstructionHistory history =
                new ConstructionHistory();

            history.Record(
                new ReversibleWallEditAction(
                    service,
                    buildResult.Edit));

            // Simulate an external mutation that makes the recorded
            // inverse no longer match authoritative state.
            service.TryClearWalls(
                new[] { run[0] });

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
                wallState.HasWall(run[1]),
                Is.True);
        }


        private static CellEdge[] CreateRun(
            int x,
            int startingY,
            int count)
        {
            CellEdge[] edges =
                new CellEdge[count];

            for (int index = 0;
                 index < count;
                 index++)
            {
                edges[index] =
                    new CellEdge(
                        new GridPosition(
                            x,
                            startingY + index,
                            0),
                        CellEdgeDirection.NorthEast);
            }

            return edges;
        }
    }
}
