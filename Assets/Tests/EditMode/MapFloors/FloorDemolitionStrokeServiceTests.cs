using System;
using System.Collections.Generic;
using BigRetail.Map.Construction;
using BigRetail.Map.Domain;
using NUnit.Framework;

namespace BigRetail.Map.Floors.Tests
{
    public sealed class FloorDemolitionStrokeServiceTests
    {
        private static readonly FloorFinishId DefaultFinish =
            new FloorFinishId("DEFAULT");

        private static readonly FloorFinishId BrickFinish =
            new FloorFinishId("BRICK");


        [Test]
        public void TryApply_MixedArea_RemovesExistingFloorsAndCapturesFinishes()
        {
            TestContext context =
                CreateContext();

            GridPosition defaultCell =
                new GridPosition(1, 1, 0);

            GridPosition brickCell =
                new GridPosition(2, 1, 0);

            GridPosition emptyCell =
                new GridPosition(3, 1, 0);

            EnsureFloors(
                context,
                defaultCell,
                brickCell);

            Assert.That(
                context.FloorFinishes.TrySetFinish(
                    brickCell,
                    BrickFinish).Succeeded,
                Is.True);

            FloorDemolitionStrokeResult result =
                context.Demolition.TryApply(
                    new[]
                    {
                        defaultCell,
                        brickCell,
                        emptyCell
                    });

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.RequestedCount, Is.EqualTo(3));
            Assert.That(result.UniqueCount, Is.EqualTo(3));
            Assert.That(result.RemovedCount, Is.EqualTo(2));
            Assert.That(result.AlreadyEmptyCount, Is.EqualTo(1));
            Assert.That(
                context.Floors.HasFloor(defaultCell),
                Is.False);
            Assert.That(
                context.Floors.HasFloor(brickCell),
                Is.False);
            Assert.That(
                result.Edit.RemovedFloors[0].FinishId,
                Is.EqualTo(DefaultFinish));
            Assert.That(
                result.Edit.RemovedFloors[1].FinishId,
                Is.EqualTo(BrickFinish));
        }


        [Test]
        public void TryApply_DuplicateCells_AreCollapsed()
        {
            TestContext context =
                CreateContext();

            GridPosition cell =
                new GridPosition(1, 1, 0);

            EnsureFloors(context, cell);

            FloorDemolitionStrokeResult result =
                context.Demolition.TryApply(
                    new[]
                    {
                        cell,
                        cell,
                        cell
                    });

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.RequestedCount, Is.EqualTo(3));
            Assert.That(result.UniqueCount, Is.EqualTo(1));
            Assert.That(result.RemovedCount, Is.EqualTo(1));
            Assert.That(result.AlreadyEmptyCount, Is.Zero);
        }


        [Test]
        public void TryApply_AllEmpty_IsSuccessfulNoOp()
        {
            TestContext context =
                CreateContext();

            FloorDemolitionStrokeResult result =
                context.Demolition.TryApply(
                    new[]
                    {
                        new GridPosition(1, 1, 0),
                        new GridPosition(2, 1, 0)
                    });

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.RemovedCount, Is.Zero);
            Assert.That(result.AlreadyEmptyCount, Is.EqualTo(2));
            Assert.That(result.Edit.IsEmpty, Is.True);
        }


        [Test]
        public void TryApply_EmptyRequest_IsRejected()
        {
            TestContext context =
                CreateContext();

            FloorDemolitionStrokeResult result =
                context.Demolition.TryApply(
                    Array.Empty<GridPosition>());

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.Failure,
                Is.EqualTo(
                    FloorDemolitionStrokeFailure.EmptyRequest));
        }


        [Test]
        public void ReversibleAction_UndoAndRedo_RestoreExactFinish()
        {
            TestContext context =
                CreateContext();

            GridPosition cell =
                new GridPosition(1, 1, 0);

            EnsureFloors(context, cell);

            context.FloorFinishes.TrySetFinish(
                cell,
                BrickFinish);

            FloorDemolitionStrokeResult demolition =
                context.Demolition.TryApply(
                    new[] { cell });

            ReversibleFloorDemolitionStrokeAction action =
                new ReversibleFloorDemolitionStrokeAction(
                    context.Floors,
                    context.FloorFinishes,
                    demolition.Edit);

            ConstructionActionResult undo =
                action.TryUndo();

            Assert.That(undo.Succeeded, Is.True);
            Assert.That(
                context.Floors.HasFloor(cell),
                Is.True);
            Assert.That(
                context.FloorFinishes.GetEffectiveFinish(cell),
                Is.EqualTo(BrickFinish));

            ConstructionActionResult redo =
                action.TryRedo();

            Assert.That(redo.Succeeded, Is.True);
            Assert.That(
                context.Floors.HasFloor(cell),
                Is.False);
        }


        [Test]
        public void ReversibleAction_UndoWithoutFoundation_IsAtomic()
        {
            TestContext context =
                CreateContext();

            GridPosition first =
                new GridPosition(1, 1, 0);

            GridPosition second =
                new GridPosition(2, 1, 0);

            EnsureFloors(
                context,
                first,
                second);

            FloorDemolitionStrokeResult demolition =
                context.Demolition.TryApply(
                    new[]
                    {
                        first,
                        second
                    });

            Assert.That(
                context.FoundationSupport.Remove(second),
                Is.True);

            ReversibleFloorDemolitionStrokeAction action =
                new ReversibleFloorDemolitionStrokeAction(
                    context.Floors,
                    context.FloorFinishes,
                    demolition.Edit);

            ConstructionActionResult undo =
                action.TryUndo();

            Assert.That(undo.Succeeded, Is.False);
            Assert.That(
                context.Floors.HasFloor(first),
                Is.False);
            Assert.That(
                context.Floors.HasFloor(second),
                Is.False);
        }


        [Test]
        public void ReversibleAction_UndoRejectsOccupiedCellWithoutMutation()
        {
            TestContext context =
                CreateContext();

            GridPosition cell =
                new GridPosition(1, 1, 0);

            EnsureFloors(context, cell);

            FloorDemolitionStrokeResult demolition =
                context.Demolition.TryApply(
                    new[] { cell });

            Assert.That(
                context.Floors.TryEnsureFloors(
                    new[] { cell }).Succeeded,
                Is.True);

            ReversibleFloorDemolitionStrokeAction action =
                new ReversibleFloorDemolitionStrokeAction(
                    context.Floors,
                    context.FloorFinishes,
                    demolition.Edit);

            ConstructionActionResult undo =
                action.TryUndo();

            Assert.That(undo.Succeeded, Is.False);
            Assert.That(
                context.Floors.HasFloor(cell),
                Is.True);
        }


        [Test]
        public void ReversibleAction_RedoRejectsChangedFinishWithoutMutation()
        {
            TestContext context =
                CreateContext();

            GridPosition cell =
                new GridPosition(1, 1, 0);

            EnsureFloors(context, cell);

            FloorDemolitionStrokeResult demolition =
                context.Demolition.TryApply(
                    new[] { cell });

            ReversibleFloorDemolitionStrokeAction action =
                new ReversibleFloorDemolitionStrokeAction(
                    context.Floors,
                    context.FloorFinishes,
                    demolition.Edit);

            Assert.That(
                action.TryUndo().Succeeded,
                Is.True);

            Assert.That(
                context.FloorFinishes.TrySetFinish(
                    cell,
                    BrickFinish).Succeeded,
                Is.True);

            ConstructionActionResult redo =
                action.TryRedo();

            Assert.That(redo.Succeeded, Is.False);
            Assert.That(
                context.Floors.HasFloor(cell),
                Is.True);
            Assert.That(
                context.FloorFinishes.GetEffectiveFinish(cell),
                Is.EqualTo(BrickFinish));
        }


        private static TestContext CreateContext()
        {
            List<GridPosition> cells =
                new List<GridPosition>();

            for (int x = 0; x < 6; x++)
            {
                for (int y = 0; y < 6; y++)
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
                    "map",
                    cells);

            ConstructionAreaDefinition area =
                new ConstructionAreaDefinition(
                    map,
                    cells);

            MutableFoundationSupport foundationSupport =
                new MutableFoundationSupport();

            FloorState floorState =
                new FloorState();

            FloorConstructionService floors =
                new FloorConstructionService(
                    map,
                    area,
                    floorState,
                    foundationSupport);

            FloorFinishCatalog catalog =
                new FloorFinishCatalog(
                    DefaultFinish,
                    new[]
                    {
                        DefaultFinish,
                        BrickFinish
                    });

            FloorFinishState finishState =
                new FloorFinishState();

            FloorFinishService finishes =
                new FloorFinishService(
                    floorState,
                    catalog,
                    finishState);

            return new TestContext(
                foundationSupport,
                floors,
                finishes,
                new FloorDemolitionStrokeService(
                    floors,
                    finishes));
        }


        private static void EnsureFloors(
            TestContext context,
            params GridPosition[] cells)
        {
            for (int index = 0;
                index < cells.Length;
                 index++)
            {
                Assert.That(
                    context.FoundationSupport.Add(
                        cells[index]),
                    Is.True);
            }

            Assert.That(
                context.Floors.TryEnsureFloors(cells).Succeeded,
                Is.True);
        }


        private sealed class TestContext
        {
            public MutableFoundationSupport FoundationSupport { get; }

            public FloorConstructionService Floors { get; }

            public FloorFinishService FloorFinishes { get; }

            public FloorDemolitionStrokeService Demolition { get; }


            public TestContext(
                MutableFoundationSupport foundationSupport,
                FloorConstructionService floors,
                FloorFinishService floorFinishes,
                FloorDemolitionStrokeService demolition)
            {
                FoundationSupport = foundationSupport;
                Floors = floors;
                FloorFinishes = floorFinishes;
                Demolition = demolition;
            }
        }


        private sealed class MutableFoundationSupport :
            IFoundationSupportQuery
        {
            private readonly HashSet<GridPosition> cells =
                new HashSet<GridPosition>();


            public bool HasFoundation(
                GridPosition cell)
            {
                return cells.Contains(cell);
            }

            public bool Add(
                GridPosition cell)
            {
                return cells.Add(cell);
            }

            public bool Remove(
                GridPosition cell)
            {
                return cells.Remove(cell);
            }
        }
    }
}
