using System.Collections.Generic;
using BigRetail.Map.Construction;
using BigRetail.Map.Domain;
using BigRetail.Map.Floors;
using NUnit.Framework;

namespace BigRetail.Map.Floors.Tests
{
    public sealed class FloorConstructionServiceTests
    {
        private FloorState floorState;
        private FloorConstructionService service;
        private MutableFoundationSupport foundationSupport;


        [SetUp]
        public void SetUp()
        {
            List<GridPosition> validCells =
                new List<GridPosition>();

            List<GridPosition> eligibleCells =
                new List<GridPosition>();

            for (int x = 0; x <= 5; x++)
            {
                for (int y = 0; y <= 5; y++)
                {
                    GridPosition cell =
                        CreateCell(
                            x,
                            y);

                    validCells.Add(cell);

                    // The two eastern columns exist in the map
                    // but are not construction eligible.
                    if (x <= 3)
                    {
                        eligibleCells.Add(cell);
                    }
                }
            }

            GridMapDefinition map =
                new GridMapDefinition(
                    "floor.test.map",
                    validCells);

            ConstructionAreaDefinition area =
                new ConstructionAreaDefinition(
                    map,
                    eligibleCells);

            floorState =
                new FloorState();

            foundationSupport =
                new MutableFoundationSupport(
                    eligibleCells);

            service =
                new FloorConstructionService(
                    map,
                    area,
                    floorState,
                    foundationSupport);
        }


        [Test]
        public void EvaluatePlacement_ValidCell_DoesNotModifyState()
        {
            GridPosition cell =
                CreateCell(
                    2,
                    2);

            FloorChangeResult result =
                service.EvaluatePlacement(cell);

            Assert.That(
                result.Succeeded,
                Is.True);

            Assert.That(
                result.Failure,
                Is.EqualTo(
                    FloorChangeFailure.None));

            Assert.That(
                floorState.FloorCount,
                Is.EqualTo(0));
        }


        [Test]
        public void EvaluatePlacement_CellWithoutFoundation_IsRejected()
        {
            GridPosition cell =
                CreateCell(
                    2,
                    2);

            foundationSupport.Remove(cell);

            FloorChangeResult result =
                service.EvaluatePlacement(cell);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.Failure,
                Is.EqualTo(
                    FloorChangeFailure.MissingFoundation));
            Assert.That(floorState.HasFloor(cell), Is.False);
        }


        [Test]
        public void TryEnsureFloors_MissingFoundation_IsSkipped()
        {
            GridPosition supported =
                CreateCell(1, 1);

            GridPosition unsupported =
                CreateCell(2, 1);

            foundationSupport.Remove(unsupported);

            FloorEnsureResult result =
                service.TryEnsureFloors(
                    new[]
                    {
                        supported,
                        unsupported
                    });

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.ChangedCount, Is.EqualTo(1));
            Assert.That(
                result.SkippedMissingFoundationCount,
                Is.EqualTo(1));
            Assert.That(floorState.HasFloor(supported), Is.True);
            Assert.That(floorState.HasFloor(unsupported), Is.False);
        }


        [Test]
        public void TryApplyEdit_AddWithoutFoundation_IsRejected()
        {
            GridPosition cell =
                CreateCell(2, 2);

            foundationSupport.Remove(cell);

            FloorBatchChangeResult result =
                service.TryApplyEdit(
                    FloorEdit.AddFloors(
                        new[] { cell }));

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.Failure,
                Is.EqualTo(
                    FloorChangeFailure.MissingFoundation));
            Assert.That(floorState.HasFloor(cell), Is.False);
        }


        [Test]
        public void EvaluatePlacement_ExistingFloor_IsRejected()
        {
            GridPosition cell =
                CreateCell(
                    2,
                    2);

            Assert.That(
                service.TryEnsureFloors(
                    new[] { cell }).Succeeded,
                Is.True);

            FloorChangeResult result =
                service.EvaluatePlacement(cell);

            Assert.That(
                result.Succeeded,
                Is.False);

            Assert.That(
                result.Failure,
                Is.EqualTo(
                    FloorChangeFailure.AlreadyExists));

            Assert.That(
                floorState.FloorCount,
                Is.EqualTo(1));
        }


        [Test]
        public void EvaluatePlacement_OutsideMap_IsRejected()
        {
            GridPosition cell =
                CreateCell(
                    20,
                    20);

            FloorChangeResult result =
                service.EvaluatePlacement(cell);

            Assert.That(
                result.Succeeded,
                Is.False);

            Assert.That(
                result.Failure,
                Is.EqualTo(
                    FloorChangeFailure.OutsideMap));

            Assert.That(
                floorState.FloorCount,
                Is.EqualTo(0));
        }


        [Test]
        public void EvaluatePlacement_OutsideConstructionArea_IsRejected()
        {
            GridPosition cell =
                CreateCell(
                    5,
                    2);

            FloorChangeResult result =
                service.EvaluatePlacement(cell);

            Assert.That(
                result.Succeeded,
                Is.False);

            Assert.That(
                result.Failure,
                Is.EqualTo(
                    FloorChangeFailure
                        .OutsideConstructionArea));

            Assert.That(
                floorState.FloorCount,
                Is.EqualTo(0));
        }


        [Test]
        public void TryEnsureFloors_ValidCells_AddsEveryMissingFloor()
        {
            GridPosition[] cells =
            {
                CreateCell(1, 1),
                CreateCell(2, 1),
                CreateCell(3, 1)
            };

            FloorEnsureResult result =
                service.TryEnsureFloors(cells);

            Assert.That(
                result.Succeeded,
                Is.True);

            Assert.That(
                result.ChangedCount,
                Is.EqualTo(3));

            Assert.That(
                result.AlreadyExistingCount,
                Is.EqualTo(0));

            Assert.That(
                result.SkippedCount,
                Is.EqualTo(0));

            Assert.That(
                floorState.FloorCount,
                Is.EqualTo(3));
        }


        [Test]
        public void TryEnsureFloors_ExistingCells_AddsOnlyMissingAndRecordsExactEdit()
        {
            GridPosition first =
                CreateCell(
                    1,
                    1);

            GridPosition existing =
                CreateCell(
                    2,
                    1);

            GridPosition third =
                CreateCell(
                    3,
                    1);

            Assert.That(
                service.TryEnsureFloors(
                    new[] { existing }).Succeeded,
                Is.True);

            FloorEnsureResult result =
                service.TryEnsureFloors(
                    new[]
                    {
                        first,
                        existing,
                        third
                    });

            HashSet<GridPosition> editedCells =
                new HashSet<GridPosition>(
                    result.Edit.Cells);

            Assert.That(
                result.Succeeded,
                Is.True);

            Assert.That(
                result.ChangedCount,
                Is.EqualTo(2));

            Assert.That(
                result.AlreadyExistingCount,
                Is.EqualTo(1));

            Assert.That(
                result.Edit.Kind,
                Is.EqualTo(
                    FloorEditKind.AddFloors));

            Assert.That(
                editedCells.Contains(first),
                Is.True);

            Assert.That(
                editedCells.Contains(existing),
                Is.False);

            Assert.That(
                editedCells.Contains(third),
                Is.True);

            Assert.That(
                floorState.FloorCount,
                Is.EqualTo(3));
        }


        [Test]
        public void TryEnsureFloors_InvalidCells_DoNotBlockValidCells()
        {
            GridPosition validStart =
                CreateCell(
                    1,
                    1);

            GridPosition outsideConstructionArea =
                CreateCell(
                    5,
                    2);

            GridPosition outsideMap =
                CreateCell(
                    20,
                    20);

            GridPosition validEnd =
                CreateCell(
                    3,
                    3);

            FloorEnsureResult result =
                service.TryEnsureFloors(
                    new[]
                    {
                        validStart,
                        outsideConstructionArea,
                        outsideMap,
                        validEnd
                    });

            Assert.That(
                result.Succeeded,
                Is.True);

            Assert.That(
                result.ChangedCount,
                Is.EqualTo(2));

            Assert.That(
                result.SkippedOutsideMapCount,
                Is.EqualTo(1));

            Assert.That(
                result.SkippedOutsideConstructionAreaCount,
                Is.EqualTo(1));

            Assert.That(
                floorState.HasFloor(validStart),
                Is.True);

            Assert.That(
                floorState.HasFloor(validEnd),
                Is.True);

            Assert.That(
                floorState.FloorCount,
                Is.EqualTo(2));
        }


        [Test]
        public void TryEnsureFloors_DuplicateCells_AreCollapsed()
        {
            GridPosition cell =
                CreateCell(
                    2,
                    2);

            FloorEnsureResult result =
                service.TryEnsureFloors(
                    new[]
                    {
                        cell,
                        cell,
                        cell
                    });

            Assert.That(
                result.Succeeded,
                Is.True);

            Assert.That(
                result.RequestedCount,
                Is.EqualTo(3));

            Assert.That(
                result.UniqueCount,
                Is.EqualTo(1));

            Assert.That(
                result.ChangedCount,
                Is.EqualTo(1));

            Assert.That(
                floorState.FloorCount,
                Is.EqualTo(1));
        }


        [Test]
        public void TryEnsureFloors_AllExisting_IsSuccessfulNoOp()
        {
            GridPosition[] cells =
            {
                CreateCell(1, 1),
                CreateCell(2, 1),
                CreateCell(3, 1)
            };

            Assert.That(
                service.TryEnsureFloors(cells).Succeeded,
                Is.True);

            FloorEnsureResult result =
                service.TryEnsureFloors(cells);

            Assert.That(
                result.Succeeded,
                Is.True);

            Assert.That(
                result.ChangedCount,
                Is.EqualTo(0));

            Assert.That(
                result.AlreadyExistingCount,
                Is.EqualTo(3));

            Assert.That(
                result.Edit.IsEmpty,
                Is.True);

            Assert.That(
                floorState.FloorCount,
                Is.EqualTo(3));
        }


        [Test]
        public void TryEnsureFloors_PublishesAfterAcceptedSubsetIsComplete()
        {
            GridPosition[] cells =
            {
                CreateCell(1, 1),
                CreateCell(2, 1),
                CreateCell(3, 1)
            };

            int eventCount = 0;
            int countDuringFirstEvent = -1;

            floorState.FloorAdded +=
                cell =>
                {
                    eventCount++;

                    if (eventCount == 1)
                    {
                        countDuringFirstEvent =
                            floorState.FloorCount;
                    }
                };

            FloorEnsureResult result =
                service.TryEnsureFloors(cells);

            Assert.That(
                result.Succeeded,
                Is.True);

            Assert.That(
                eventCount,
                Is.EqualTo(3));

            Assert.That(
                countDuringFirstEvent,
                Is.EqualTo(3));
        }


        [Test]
        public void TryClearFloors_MixedSelection_RemovesOnlyExistingAndRecordsExactEdit()
        {
            GridPosition first =
                CreateCell(
                    1,
                    1);

            GridPosition empty =
                CreateCell(
                    2,
                    1);

            GridPosition third =
                CreateCell(
                    3,
                    1);

            Assert.That(
                service.TryEnsureFloors(
                    new[]
                    {
                        first,
                        third
                    }).Succeeded,
                Is.True);

            FloorClearResult result =
                service.TryClearFloors(
                    new[]
                    {
                        first,
                        empty,
                        third
                    });

            HashSet<GridPosition> editedCells =
                new HashSet<GridPosition>(
                    result.Edit.Cells);

            Assert.That(
                result.Succeeded,
                Is.True);

            Assert.That(
                result.RemovedCount,
                Is.EqualTo(2));

            Assert.That(
                result.AlreadyEmptyCount,
                Is.EqualTo(1));

            Assert.That(
                result.Edit.Kind,
                Is.EqualTo(
                    FloorEditKind.RemoveFloors));

            Assert.That(
                editedCells.Contains(first),
                Is.True);

            Assert.That(
                editedCells.Contains(empty),
                Is.False);

            Assert.That(
                editedCells.Contains(third),
                Is.True);

            Assert.That(
                floorState.FloorCount,
                Is.EqualTo(0));
        }


        [Test]
        public void TryClearFloors_DuplicateCells_AreCollapsed()
        {
            GridPosition cell =
                CreateCell(
                    2,
                    2);

            Assert.That(
                service.TryEnsureFloors(
                    new[] { cell }).Succeeded,
                Is.True);

            FloorClearResult result =
                service.TryClearFloors(
                    new[]
                    {
                        cell,
                        cell,
                        cell
                    });

            Assert.That(
                result.Succeeded,
                Is.True);

            Assert.That(
                result.RequestedCount,
                Is.EqualTo(3));

            Assert.That(
                result.UniqueCount,
                Is.EqualTo(1));

            Assert.That(
                result.RemovedCount,
                Is.EqualTo(1));

            Assert.That(
                floorState.FloorCount,
                Is.EqualTo(0));
        }


        [Test]
        public void TryClearFloors_AllEmpty_IsSuccessfulNoOp()
        {
            GridPosition[] cells =
            {
                CreateCell(1, 1),
                CreateCell(2, 1),
                CreateCell(3, 1)
            };

            FloorClearResult result =
                service.TryClearFloors(cells);

            Assert.That(
                result.Succeeded,
                Is.True);

            Assert.That(
                result.RemovedCount,
                Is.EqualTo(0));

            Assert.That(
                result.AlreadyEmptyCount,
                Is.EqualTo(3));

            Assert.That(
                result.Edit.IsEmpty,
                Is.True);

            Assert.That(
                floorState.FloorCount,
                Is.EqualTo(0));
        }


        [Test]
        public void TryClearFloors_PublishesAfterCompleteMutation()
        {
            GridPosition[] cells =
            {
                CreateCell(1, 1),
                CreateCell(2, 1),
                CreateCell(3, 1)
            };

            Assert.That(
                service.TryEnsureFloors(cells).Succeeded,
                Is.True);

            int eventCount = 0;
            int countDuringFirstEvent = -1;

            floorState.FloorRemoved +=
                cell =>
                {
                    eventCount++;

                    if (eventCount == 1)
                    {
                        countDuringFirstEvent =
                            floorState.FloorCount;
                    }
                };

            FloorClearResult result =
                service.TryClearFloors(cells);

            Assert.That(
                result.Succeeded,
                Is.True);

            Assert.That(
                eventCount,
                Is.EqualTo(3));

            Assert.That(
                countDuringFirstEvent,
                Is.EqualTo(0));
        }


        [Test]
        public void TryClearFloors_ExistingOutsideCurrentMap_RemovesIt()
        {
            GridPosition outsideCurrentMap =
                CreateCell(
                    20,
                    20);

            foundationSupport.Add(outsideCurrentMap);

            Assert.That(
                service.TryApplyEdit(
                    FloorEdit.AddFloors(
                        new[] { outsideCurrentMap }))
                    .Succeeded,
                Is.True);

            FloorClearResult result =
                service.TryClearFloors(
                    new[] { outsideCurrentMap });

            Assert.That(
                result.Succeeded,
                Is.True);

            Assert.That(
                result.RemovedCount,
                Is.EqualTo(1));

            Assert.That(
                floorState.HasFloor(outsideCurrentMap),
                Is.False);
        }


        [Test]
        public void EmptyRequests_AreRejected()
        {
            GridPosition[] empty =
                new GridPosition[0];

            FloorEnsureResult ensureResult =
                service.TryEnsureFloors(empty);

            FloorClearResult clearResult =
                service.TryClearFloors(empty);

            Assert.That(
                ensureResult.Succeeded,
                Is.False);

            Assert.That(
                ensureResult.Failure,
                Is.EqualTo(
                    FloorChangeFailure.EmptyRequest));

            Assert.That(
                clearResult.Succeeded,
                Is.False);

            Assert.That(
                clearResult.Failure,
                Is.EqualTo(
                    FloorChangeFailure.EmptyRequest));

            Assert.That(
                floorState.FloorCount,
                Is.EqualTo(0));
        }


        [Test]
        public void TryApplyEdit_AddAndInverseRemove_ReplayExactly()
        {
            GridPosition[] cells =
            {
                CreateCell(1, 1),
                CreateCell(2, 1),
                CreateCell(3, 1)
            };

            FloorEdit addEdit =
                FloorEdit.AddFloors(
                    cells);

            FloorBatchChangeResult addResult =
                service.TryApplyEdit(
                    addEdit);

            Assert.That(
                addResult.Succeeded,
                Is.True);

            Assert.That(
                floorState.FloorCount,
                Is.EqualTo(3));

            FloorBatchChangeResult removeResult =
                service.TryApplyEdit(
                    addEdit.Inverse());

            Assert.That(
                removeResult.Succeeded,
                Is.True);

            Assert.That(
                floorState.FloorCount,
                Is.EqualTo(0));
        }


        [Test]
        public void TryApplyEdit_AddConflict_ChangesNothing()
        {
            GridPosition existing =
                CreateCell(
                    1,
                    1);

            GridPosition missing =
                CreateCell(
                    2,
                    1);

            Assert.That(
                service.TryEnsureFloors(
                    new[] { existing }).Succeeded,
                Is.True);

            FloorBatchChangeResult result =
                service.TryApplyEdit(
                    FloorEdit.AddFloors(
                        new[]
                        {
                            existing,
                            missing
                        }));

            Assert.That(
                result.Succeeded,
                Is.False);

            Assert.That(
                result.Failure,
                Is.EqualTo(
                    FloorChangeFailure.AlreadyExists));

            Assert.That(
                floorState.HasFloor(existing),
                Is.True);

            Assert.That(
                floorState.HasFloor(missing),
                Is.False);
        }


        [Test]
        public void TryApplyEdit_RemoveConflict_ChangesNothing()
        {
            GridPosition existing =
                CreateCell(
                    1,
                    1);

            GridPosition missing =
                CreateCell(
                    2,
                    1);

            Assert.That(
                service.TryEnsureFloors(
                    new[] { existing }).Succeeded,
                Is.True);

            FloorBatchChangeResult result =
                service.TryApplyEdit(
                    FloorEdit.RemoveFloors(
                        new[]
                        {
                            existing,
                            missing
                        }));

            Assert.That(
                result.Succeeded,
                Is.False);

            Assert.That(
                result.Failure,
                Is.EqualTo(
                    FloorChangeFailure.NotFound));

            Assert.That(
                floorState.HasFloor(existing),
                Is.True);
        }


        [Test]
        public void TryApplyEdit_HistoryReplayBypassesCurrentAreaMask()
        {
            GridPosition outsideCurrentMap =
                CreateCell(
                    20,
                    20);

            foundationSupport.Add(outsideCurrentMap);

            FloorBatchChangeResult result =
                service.TryApplyEdit(
                    FloorEdit.AddFloors(
                        new[] { outsideCurrentMap }));

            Assert.That(
                result.Succeeded,
                Is.True);

            Assert.That(
                floorState.HasFloor(outsideCurrentMap),
                Is.True);
        }


        private static GridPosition CreateCell(
            int x,
            int y)
        {
            return new GridPosition(
                x,
                y,
                0);
        }


        private sealed class MutableFoundationSupport :
            IFoundationSupportQuery
        {
            private readonly HashSet<GridPosition> supportedCells;


            public MutableFoundationSupport(
                IEnumerable<GridPosition> cells)
            {
                supportedCells =
                    new HashSet<GridPosition>(cells);
            }


            public bool HasFoundation(
                GridPosition cell)
            {
                return supportedCells.Contains(cell);
            }


            public void Remove(
                GridPosition cell)
            {
                supportedCells.Remove(cell);
            }


            public void Add(
                GridPosition cell)
            {
                supportedCells.Add(cell);
            }
        }
    }
}
