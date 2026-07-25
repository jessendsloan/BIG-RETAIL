using System;
using System.Collections.Generic;
using BigRetail.Map.Domain;
using BigRetail.Map.Floors;
using BigRetail.Map.Walls;
using NUnit.Framework;

namespace BigRetail.Map.Construction.Tests
{
    public sealed class ConstructionHistoryTests
    {
        [Test]
        public void Standard_RecordSecondAction_ReplacesFirstAction()
        {
            ConstructionHistory history =
                new ConstructionHistory(
                    ConstructionHistoryMode.Standard);

            FakeAction first =
                new FakeAction(
                    "First");

            FakeAction second =
                new FakeAction(
                    "Second");

            history.Record(first);
            history.Record(second);

            Assert.That(
                history.UndoCount,
                Is.EqualTo(1));

            Assert.That(
                history.TryUndo(out _),
                Is.True);

            Assert.That(
                first.IsApplied,
                Is.True);

            Assert.That(
                second.IsApplied,
                Is.False);

            Assert.That(
                history.TryUndo(out _),
                Is.False);
        }


        [Test]
        public void Standard_OneAction_CanAlternateUndoAndRedo()
        {
            ConstructionHistory history =
                new ConstructionHistory(
                    ConstructionHistoryMode.Standard);

            FakeAction action =
                new FakeAction(
                    "Alternating");

            history.Record(action);

            Assert.That(
                history.TryUndo(out _),
                Is.True);

            Assert.That(
                action.IsApplied,
                Is.False);

            Assert.That(
                history.TryRedo(out _),
                Is.True);

            Assert.That(
                action.IsApplied,
                Is.True);

            Assert.That(
                history.TryUndo(out _),
                Is.True);

            Assert.That(
                action.IsApplied,
                Is.False);
        }


        [Test]
        public void Standard_RecordAfterUndo_ClearsRedoAndBranches()
        {
            ConstructionHistory history =
                new ConstructionHistory(
                    ConstructionHistoryMode.Standard);

            FakeAction first =
                new FakeAction(
                    "First");

            FakeAction branch =
                new FakeAction(
                    "Branch");

            history.Record(first);

            Assert.That(
                history.TryUndo(out _),
                Is.True);

            Assert.That(
                history.CanRedo,
                Is.True);

            history.Record(branch);

            Assert.That(
                history.CanRedo,
                Is.False);

            Assert.That(
                history.UndoCount,
                Is.EqualTo(1));

            Assert.That(
                history.TryUndo(out _),
                Is.True);

            Assert.That(
                branch.IsApplied,
                Is.False);

            Assert.That(
                first.IsApplied,
                Is.False);
        }


        [Test]
        public void Unlimited_UndoesAndRedoesChronologically()
        {
            ConstructionHistory history =
                new ConstructionHistory(
                    ConstructionHistoryMode.Unlimited);

            FakeAction first =
                new FakeAction(
                    "First");

            FakeAction second =
                new FakeAction(
                    "Second");

            FakeAction third =
                new FakeAction(
                    "Third");

            history.Record(first);
            history.Record(second);
            history.Record(third);

            Assert.That(
                history.UndoCount,
                Is.EqualTo(3));

            Assert.That(
                history.TryUndo(out _),
                Is.True);

            Assert.That(
                third.IsApplied,
                Is.False);

            Assert.That(
                history.TryUndo(out _),
                Is.True);

            Assert.That(
                second.IsApplied,
                Is.False);

            Assert.That(
                history.TryUndo(out _),
                Is.True);

            Assert.That(
                first.IsApplied,
                Is.False);

            Assert.That(
                history.TryRedo(out _),
                Is.True);

            Assert.That(
                first.IsApplied,
                Is.True);

            Assert.That(
                history.TryRedo(out _),
                Is.True);

            Assert.That(
                second.IsApplied,
                Is.True);

            Assert.That(
                history.TryRedo(out _),
                Is.True);

            Assert.That(
                third.IsApplied,
                Is.True);
        }


        [Test]
        public void Unlimited_RecordAfterUndo_ClearsRedoBranch()
        {
            ConstructionHistory history =
                new ConstructionHistory(
                    ConstructionHistoryMode.Unlimited);

            history.Record(
                new FakeAction(
                    "First"));

            history.Record(
                new FakeAction(
                    "Second"));

            Assert.That(
                history.TryUndo(out _),
                Is.True);

            history.Record(
                new FakeAction(
                    "Branch"));

            Assert.That(
                history.CanRedo,
                Is.False);

            Assert.That(
                history.UndoCount,
                Is.EqualTo(2));
        }


        [Test]
        public void FailedUndo_PreservesHistoryPositionAndActionState()
        {
            ConstructionHistory history =
                new ConstructionHistory();

            FakeAction action =
                new FakeAction(
                    "Blocked")
                {
                    RejectUndo = true
                };

            history.Record(action);

            bool succeeded =
                history.TryUndo(
                    out ConstructionHistoryResult result);

            Assert.That(
                succeeded,
                Is.False);

            Assert.That(
                result.Failure,
                Is.EqualTo(
                    ConstructionHistoryFailure
                        .ActionCouldNotBeApplied));

            Assert.That(
                action.IsApplied,
                Is.True);

            Assert.That(
                history.UndoCount,
                Is.EqualTo(1));

            Assert.That(
                history.RedoCount,
                Is.EqualTo(0));
        }


        [Test]
        public void InvalidAction_DoesNotReplaceValidStandardEntry()
        {
            ConstructionHistory history =
                new ConstructionHistory();

            FakeAction valid =
                new FakeAction(
                    "Valid");

            history.Record(valid);

            Assert.Throws<ArgumentException>(
                () =>
                    history.Record(
                        new FakeAction(
                            "Empty",
                            0)));

            Assert.That(
                history.UndoCount,
                Is.EqualTo(1));

            Assert.That(
                history.TryUndo(out _),
                Is.True);

            Assert.That(
                valid.IsApplied,
                Is.False);
        }


        [Test]
        public void Clear_RemovesUndoAndRedoEntries()
        {
            ConstructionHistory history =
                new ConstructionHistory();

            history.Record(
                new FakeAction(
                    "One"));

            Assert.That(
                history.TryUndo(out _),
                Is.True);

            history.Clear();

            Assert.That(
                history.CanUndo,
                Is.False);

            Assert.That(
                history.CanRedo,
                Is.False);
        }


        [Test]
        public void Unlimited_WallFloorWall_ReplaysOneTimeline()
        {
            List<GridPosition> cells =
                CreateCells();

            GridMapDefinition map =
                new GridMapDefinition(
                    "mixed.history.test.map",
                    cells);

            ConstructionAreaDefinition area =
                new ConstructionAreaDefinition(
                    map,
                    cells);

            WallState wallState =
                new WallState();

            FloorState floorState =
                new FloorState();

            WallConstructionService wallService =
                new WallConstructionService(
                    map,
                    area,
                    wallState);

            FloorConstructionService floorService =
                new FloorConstructionService(
                    map,
                    area,
                    floorState);

            CellEdge firstWall =
                new CellEdge(
                    new GridPosition(
                        1,
                        1,
                        0),
                    CellEdgeDirection.NorthEast);

            GridPosition floor =
                new GridPosition(
                    2,
                    2,
                    0);

            CellEdge secondWall =
                new CellEdge(
                    new GridPosition(
                        3,
                        1,
                        0),
                    CellEdgeDirection.NorthEast);

            ConstructionHistory history =
                new ConstructionHistory(
                    ConstructionHistoryMode.Unlimited);

            WallEnsureResult firstWallResult =
                wallService.TryEnsureWalls(
                    new[] { firstWall });

            history.Record(
                new ReversibleWallEditAction(
                    wallService,
                    firstWallResult.Edit));

            FloorEnsureResult floorResult =
                floorService.TryEnsureFloors(
                    new[] { floor });

            history.Record(
                new ReversibleFloorEditAction(
                    floorService,
                    floorResult.Edit));

            WallEnsureResult secondWallResult =
                wallService.TryEnsureWalls(
                    new[] { secondWall });

            history.Record(
                new ReversibleWallEditAction(
                    wallService,
                    secondWallResult.Edit));

            Assert.That(
                history.TryUndo(out _),
                Is.True);

            Assert.That(
                wallState.HasWall(secondWall),
                Is.False);

            Assert.That(
                history.TryUndo(out _),
                Is.True);

            Assert.That(
                floorState.HasFloor(floor),
                Is.False);

            Assert.That(
                history.TryUndo(out _),
                Is.True);

            Assert.That(
                wallState.HasWall(firstWall),
                Is.False);

            Assert.That(
                history.TryRedo(out _),
                Is.True);

            Assert.That(
                wallState.HasWall(firstWall),
                Is.True);

            Assert.That(
                history.TryRedo(out _),
                Is.True);

            Assert.That(
                floorState.HasFloor(floor),
                Is.True);

            Assert.That(
                history.TryRedo(out _),
                Is.True);

            Assert.That(
                wallState.HasWall(secondWall),
                Is.True);
        }


        [Test]
        public void DomainActions_RejectEmptyEdits()
        {
            List<GridPosition> cells =
                CreateCells();

            GridMapDefinition map =
                new GridMapDefinition(
                    "empty.action.test.map",
                    cells);

            ConstructionAreaDefinition area =
                new ConstructionAreaDefinition(
                    map,
                    cells);

            WallConstructionService wallService =
                new WallConstructionService(
                    map,
                    area,
                    new WallState());

            FloorConstructionService floorService =
                new FloorConstructionService(
                    map,
                    area,
                    new FloorState());

            Assert.Throws<ArgumentException>(
                () =>
                    new ReversibleWallEditAction(
                        wallService,
                        default));

            Assert.Throws<ArgumentException>(
                () =>
                    new ReversibleFloorEditAction(
                        floorService,
                        default));
        }


        private static List<GridPosition> CreateCells()
        {
            List<GridPosition> cells =
                new List<GridPosition>();

            for (int x = 0; x <= 4; x++)
            {
                for (int y = 0; y <= 4; y++)
                {
                    cells.Add(
                        new GridPosition(
                            x,
                            y,
                            0));
                }
            }

            return cells;
        }


        private sealed class FakeAction :
            IReversibleConstructionAction
        {
            public string Description { get; }

            public int ChangeCount { get; }

            public bool IsApplied { get; private set; } = true;

            public bool RejectUndo { get; set; }


            public FakeAction(
                string description,
                int changeCount = 1)
            {
                Description = description;
                ChangeCount = changeCount;
            }


            public ConstructionActionResult TryUndo()
            {
                if (RejectUndo)
                {
                    return ConstructionActionResult.Rejected(
                        "Undo was blocked for this test.");
                }

                if (!IsApplied)
                {
                    return ConstructionActionResult.Rejected(
                        "Action is already undone.");
                }

                IsApplied = false;

                return ConstructionActionResult.Success();
            }


            public ConstructionActionResult TryRedo()
            {
                if (IsApplied)
                {
                    return ConstructionActionResult.Rejected(
                        "Action is already applied.");
                }

                IsApplied = true;

                return ConstructionActionResult.Success();
            }
        }
    }
}
