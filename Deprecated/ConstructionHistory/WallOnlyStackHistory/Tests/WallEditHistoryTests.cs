using System.Collections.Generic;
using BigRetail.Map.Construction;
using BigRetail.Map.Domain;
using BigRetail.Map.Walls;
using NUnit.Framework;

namespace BigRetail.Map.Walls.Tests
{
    public sealed class WallEditHistoryTests
    {
        private WallState wallState;
        private WallConstructionService service;
        private WallEditHistory history;


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
                    "history.test.map",
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

            history =
                new WallEditHistory(
                    service);
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
                result.Edit.Kind,
                Is.EqualTo(
                    WallEditKind.AddWalls));

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
                result.Edit.Kind,
                Is.EqualTo(
                    WallEditKind.RemoveWalls));

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
        public void BuildEdit_CanBeUndoneAndRedone()
        {
            CellEdge[] run =
                CreateRun(
                    2,
                    1,
                    3);

            WallEnsureResult buildResult =
                service.TryEnsureWalls(run);

            history.Record(
                buildResult.Edit);

            Assert.That(
                wallState.WallCount,
                Is.EqualTo(3));

            Assert.That(
                history.CanUndo,
                Is.True);

            bool undoSucceeded =
                history.TryUndo(
                    out WallHistoryResult undoResult);

            Assert.That(
                undoSucceeded,
                Is.True);

            Assert.That(
                undoResult.Succeeded,
                Is.True);

            Assert.That(
                wallState.WallCount,
                Is.EqualTo(0));

            Assert.That(
                history.CanRedo,
                Is.True);

            bool redoSucceeded =
                history.TryRedo(
                    out WallHistoryResult redoResult);

            Assert.That(
                redoSucceeded,
                Is.True);

            Assert.That(
                redoResult.Succeeded,
                Is.True);

            Assert.That(
                wallState.WallCount,
                Is.EqualTo(3));
        }


        [Test]
        public void DemolitionEdit_CanBeUndoneAndRedone()
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

            history.Record(
                clearResult.Edit);

            Assert.That(
                wallState.WallCount,
                Is.EqualTo(0));

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
        public void RecordAfterUndo_ClearsRedoStack()
        {
            CellEdge first =
                CreateEdge(
                    1,
                    1);

            CellEdge second =
                CreateEdge(
                    2,
                    1);

            CellEdge third =
                CreateEdge(
                    3,
                    1);

            WallEnsureResult firstResult =
                service.TryEnsureWalls(
                    new[] { first });

            history.Record(
                firstResult.Edit);

            WallEnsureResult secondResult =
                service.TryEnsureWalls(
                    new[] { second });

            history.Record(
                secondResult.Edit);

            Assert.That(
                history.TryUndo(out _),
                Is.True);

            Assert.That(
                history.CanRedo,
                Is.True);

            WallEnsureResult thirdResult =
                service.TryEnsureWalls(
                    new[] { third });

            history.Record(
                thirdResult.Edit);

            Assert.That(
                history.CanRedo,
                Is.False);

            Assert.That(
                history.UndoCount,
                Is.EqualTo(2));
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

            history.Record(
                buildResult.Edit);

            // Simulate an unrelated external mutation that causes
            // the recorded inverse edit to no longer match reality.
            service.TryClearWalls(
                new[] { run[0] });

            bool undoSucceeded =
                history.TryUndo(
                    out WallHistoryResult result);

            Assert.That(
                undoSucceeded,
                Is.False);

            Assert.That(
                result.Failure,
                Is.EqualTo(
                    WallHistoryFailure.EditCouldNotBeApplied));

            Assert.That(
                result.ApplyFailure,
                Is.EqualTo(
                    WallChangeFailure.NotFound));

            Assert.That(
                history.CanUndo,
                Is.True);

            Assert.That(
                history.CanRedo,
                Is.False);

            // The remaining wall was not partially removed.
            Assert.That(
                wallState.HasWall(run[1]),
                Is.True);
        }


        [Test]
        public void EmptyEdit_IsNotRecorded()
        {
            history.Record(
                default);

            Assert.That(
                history.CanUndo,
                Is.False);

            Assert.That(
                history.CanRedo,
                Is.False);

            Assert.That(
                history.UndoCount,
                Is.EqualTo(0));
        }


        [Test]
        public void Clear_RemovesUndoAndRedoEntries()
        {
            CellEdge edge =
                CreateEdge(
                    2,
                    1);

            WallEnsureResult result =
                service.TryEnsureWalls(
                    new[] { edge });

            history.Record(
                result.Edit);

            Assert.That(
                history.TryUndo(out _),
                Is.True);

            Assert.That(
                history.CanRedo,
                Is.True);

            history.Clear();

            Assert.That(
                history.CanUndo,
                Is.False);

            Assert.That(
                history.CanRedo,
                Is.False);
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
                    CreateEdge(
                        x,
                        startingY + index);
            }

            return edges;
        }


        private static CellEdge CreateEdge(
            int x,
            int y)
        {
            return new CellEdge(
                new GridPosition(
                    x,
                    y,
                    0),
                CellEdgeDirection.NorthEast);
        }
    }
}
