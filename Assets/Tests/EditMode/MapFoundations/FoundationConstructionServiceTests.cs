using System;
using System.Collections.Generic;
using BigRetail.Map.Construction;
using BigRetail.Map.Domain;
using NUnit.Framework;

namespace BigRetail.Map.Foundations.Tests
{
    public sealed class FoundationConstructionServiceTests
    {
        private FoundationState foundationState;
        private FoundationConstructionService service;
        private BlockingRemovalValidator removalValidator;


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
                        CreateCell(x, y);

                    validCells.Add(cell);

                    if (x <= 3)
                    {
                        eligibleCells.Add(cell);
                    }
                }
            }

            GridMapDefinition map =
                new GridMapDefinition(
                    "foundation.test.map",
                    validCells);

            ConstructionAreaDefinition area =
                new ConstructionAreaDefinition(
                    map,
                    eligibleCells);

            foundationState =
                new FoundationState();

            removalValidator =
                new BlockingRemovalValidator();

            service =
                new FoundationConstructionService(
                    map,
                    area,
                    foundationState,
                    removalValidator);
        }


        [Test]
        public void EvaluatePlacement_ValidCell_DoesNotModifyState()
        {
            GridPosition cell =
                CreateCell(2, 2);

            FoundationChangeResult result =
                service.EvaluatePlacement(cell);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(
                result.Failure,
                Is.EqualTo(FoundationChangeFailure.None));
            Assert.That(
                foundationState.FoundationCount,
                Is.EqualTo(0));
        }


        [Test]
        public void EvaluatePlacement_ExistingFoundation_IsRejected()
        {
            GridPosition cell =
                CreateCell(2, 2);

            Assert.That(
                service.TryEnsureFoundations(
                    new[] { cell }).Succeeded,
                Is.True);

            FoundationChangeResult result =
                service.EvaluatePlacement(cell);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.Failure,
                Is.EqualTo(
                    FoundationChangeFailure.AlreadyExists));
            Assert.That(
                foundationState.FoundationCount,
                Is.EqualTo(1));
        }


        [Test]
        public void EvaluatePlacement_OutsideMap_IsRejected()
        {
            FoundationChangeResult result =
                service.EvaluatePlacement(
                    CreateCell(20, 20));

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.Failure,
                Is.EqualTo(
                    FoundationChangeFailure.OutsideMap));
        }


        [Test]
        public void EvaluatePlacement_OutsideConstructionArea_IsRejected()
        {
            FoundationChangeResult result =
                service.EvaluatePlacement(
                    CreateCell(5, 2));

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.Failure,
                Is.EqualTo(
                    FoundationChangeFailure
                        .OutsideConstructionArea));
        }


        [Test]
        public void TryEnsureFoundations_ValidCells_AddsEveryMissingFoundation()
        {
            GridPosition[] cells =
            {
                CreateCell(1, 1),
                CreateCell(2, 1),
                CreateCell(3, 1)
            };

            FoundationEnsureResult result =
                service.TryEnsureFoundations(cells);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.ChangedCount, Is.EqualTo(3));
            Assert.That(result.AlreadyExistingCount, Is.EqualTo(0));
            Assert.That(result.SkippedCount, Is.EqualTo(0));
            Assert.That(
                foundationState.FoundationCount,
                Is.EqualTo(3));
        }


        [Test]
        public void TryEnsureFoundations_ExistingCells_RecordsOnlyNewEditCells()
        {
            GridPosition first =
                CreateCell(1, 1);

            GridPosition existing =
                CreateCell(2, 1);

            GridPosition third =
                CreateCell(3, 1);

            Assert.That(
                service.TryEnsureFoundations(
                    new[] { existing }).Succeeded,
                Is.True);

            FoundationEnsureResult result =
                service.TryEnsureFoundations(
                    new[]
                    {
                        first,
                        existing,
                        third
                    });

            HashSet<GridPosition> editedCells =
                new HashSet<GridPosition>(
                    result.Edit.Cells);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.ChangedCount, Is.EqualTo(2));
            Assert.That(result.AlreadyExistingCount, Is.EqualTo(1));
            Assert.That(
                result.Edit.Kind,
                Is.EqualTo(
                    FoundationEditKind.AddFoundations));
            Assert.That(editedCells.Contains(first), Is.True);
            Assert.That(editedCells.Contains(existing), Is.False);
            Assert.That(editedCells.Contains(third), Is.True);
        }


        [Test]
        public void TryEnsureFoundations_InvalidCells_DoNotBlockValidCells()
        {
            GridPosition validStart =
                CreateCell(1, 1);

            GridPosition outsideConstructionArea =
                CreateCell(5, 2);

            GridPosition outsideMap =
                CreateCell(20, 20);

            GridPosition validEnd =
                CreateCell(3, 3);

            FoundationEnsureResult result =
                service.TryEnsureFoundations(
                    new[]
                    {
                        validStart,
                        outsideConstructionArea,
                        outsideMap,
                        validEnd
                    });

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.ChangedCount, Is.EqualTo(2));
            Assert.That(
                result.SkippedOutsideMapCount,
                Is.EqualTo(1));
            Assert.That(
                result.SkippedOutsideConstructionAreaCount,
                Is.EqualTo(1));
            Assert.That(
                foundationState.HasFoundation(validStart),
                Is.True);
            Assert.That(
                foundationState.HasFoundation(validEnd),
                Is.True);
        }


        [Test]
        public void TryEnsureFoundations_DuplicateCells_AreCollapsed()
        {
            GridPosition cell =
                CreateCell(2, 2);

            FoundationEnsureResult result =
                service.TryEnsureFoundations(
                    new[] { cell, cell, cell });

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.RequestedCount, Is.EqualTo(3));
            Assert.That(result.UniqueCount, Is.EqualTo(1));
            Assert.That(result.ChangedCount, Is.EqualTo(1));
            Assert.That(
                foundationState.FoundationCount,
                Is.EqualTo(1));
        }


        [Test]
        public void TryEnsureFoundations_AllExisting_IsSuccessfulNoOp()
        {
            GridPosition[] cells =
            {
                CreateCell(1, 1),
                CreateCell(2, 1),
                CreateCell(3, 1)
            };

            Assert.That(
                service.TryEnsureFoundations(cells).Succeeded,
                Is.True);

            FoundationEnsureResult result =
                service.TryEnsureFoundations(cells);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.ChangedCount, Is.EqualTo(0));
            Assert.That(result.AlreadyExistingCount, Is.EqualTo(3));
            Assert.That(result.Edit.IsEmpty, Is.True);
        }


        [Test]
        public void TryEnsureFoundations_PublishesAfterAcceptedSubsetIsComplete()
        {
            GridPosition[] cells =
            {
                CreateCell(1, 1),
                CreateCell(2, 1),
                CreateCell(3, 1)
            };

            int eventCount = 0;
            int countDuringFirstEvent = -1;

            foundationState.FoundationAdded +=
                cell =>
                {
                    eventCount++;

                    if (eventCount == 1)
                    {
                        countDuringFirstEvent =
                            foundationState.FoundationCount;
                    }
                };

            FoundationEnsureResult result =
                service.TryEnsureFoundations(cells);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(eventCount, Is.EqualTo(3));
            Assert.That(countDuringFirstEvent, Is.EqualTo(3));
        }


        [Test]
        public void TryClearFoundations_MixedSelection_RemovesOnlyExisting()
        {
            GridPosition first =
                CreateCell(1, 1);

            GridPosition empty =
                CreateCell(2, 1);

            GridPosition third =
                CreateCell(3, 1);

            Assert.That(
                service.TryEnsureFoundations(
                    new[] { first, third }).Succeeded,
                Is.True);

            FoundationClearResult result =
                service.TryClearFoundations(
                    new[] { first, empty, third });

            HashSet<GridPosition> editedCells =
                new HashSet<GridPosition>(
                    result.Edit.Cells);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.RemovedCount, Is.EqualTo(2));
            Assert.That(result.AlreadyEmptyCount, Is.EqualTo(1));
            Assert.That(
                result.Edit.Kind,
                Is.EqualTo(
                    FoundationEditKind.RemoveFoundations));
            Assert.That(editedCells.Contains(first), Is.True);
            Assert.That(editedCells.Contains(empty), Is.False);
            Assert.That(editedCells.Contains(third), Is.True);
            Assert.That(
                foundationState.FoundationCount,
                Is.EqualTo(0));
        }


        [Test]
        public void TryClearFoundations_AllEmpty_IsSuccessfulNoOp()
        {
            FoundationClearResult result =
                service.TryClearFoundations(
                    new[]
                    {
                        CreateCell(1, 1),
                        CreateCell(2, 1)
                    });

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.RemovedCount, Is.EqualTo(0));
            Assert.That(result.AlreadyEmptyCount, Is.EqualTo(2));
            Assert.That(result.Edit.IsEmpty, Is.True);
        }


        [Test]
        public void TryClearFoundations_SupportedConstruction_IsAtomic()
        {
            GridPosition first =
                CreateCell(1, 1);

            GridPosition blocked =
                CreateCell(2, 1);

            Assert.That(
                service.TryEnsureFoundations(
                    new[] { first, blocked }).Succeeded,
                Is.True);

            removalValidator.BlockedCell = blocked;

            FoundationClearResult result =
                service.TryClearFoundations(
                    new[] { first, blocked });

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.Failure,
                Is.EqualTo(
                    FoundationChangeFailure
                        .SupportsConstruction));
            Assert.That(result.FailedCell, Is.EqualTo(blocked));
            Assert.That(foundationState.HasFoundation(first), Is.True);
            Assert.That(foundationState.HasFoundation(blocked), Is.True);
        }


        [Test]
        public void EvaluateRemoval_SupportedConstruction_IsRejected()
        {
            GridPosition cell =
                CreateCell(2, 2);

            Assert.That(
                service.TryEnsureFoundations(
                    new[] { cell }).Succeeded,
                Is.True);

            removalValidator.BlockedCell = cell;

            FoundationChangeResult result =
                service.EvaluateRemoval(cell);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.Failure,
                Is.EqualTo(
                    FoundationChangeFailure
                        .SupportsConstruction));
            Assert.That(foundationState.HasFoundation(cell), Is.True);
        }


        [Test]
        public void TryApplyEdit_RemoveSupportedFoundation_IsRejected()
        {
            GridPosition cell =
                CreateCell(2, 2);

            Assert.That(
                service.TryEnsureFoundations(
                    new[] { cell }).Succeeded,
                Is.True);

            removalValidator.BlockedCell = cell;

            FoundationBatchChangeResult result =
                service.TryApplyEdit(
                    FoundationEdit.RemoveFoundations(
                        new[] { cell }));

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.Failure,
                Is.EqualTo(
                    FoundationChangeFailure
                        .SupportsConstruction));
            Assert.That(foundationState.HasFoundation(cell), Is.True);
        }


        [Test]
        public void TryApplyEdit_AddWithExistingCell_IsAtomic()
        {
            GridPosition existing =
                CreateCell(1, 1);

            GridPosition missing =
                CreateCell(2, 1);

            Assert.That(
                service.TryEnsureFoundations(
                    new[] { existing }).Succeeded,
                Is.True);

            FoundationEdit edit =
                FoundationEdit.AddFoundations(
                    new[] { missing, existing });

            FoundationBatchChangeResult result =
                service.TryApplyEdit(edit);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.Failure,
                Is.EqualTo(
                    FoundationChangeFailure.AlreadyExists));
            Assert.That(
                foundationState.HasFoundation(missing),
                Is.False);
            Assert.That(
                foundationState.FoundationCount,
                Is.EqualTo(1));
        }


        [Test]
        public void TryApplyEdit_RemoveWithMissingCell_IsAtomic()
        {
            GridPosition existing =
                CreateCell(1, 1);

            GridPosition missing =
                CreateCell(2, 1);

            Assert.That(
                service.TryEnsureFoundations(
                    new[] { existing }).Succeeded,
                Is.True);

            FoundationEdit edit =
                FoundationEdit.RemoveFoundations(
                    new[] { existing, missing });

            FoundationBatchChangeResult result =
                service.TryApplyEdit(edit);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.Failure,
                Is.EqualTo(
                    FoundationChangeFailure.NotFound));
            Assert.That(
                foundationState.HasFoundation(existing),
                Is.True);
            Assert.That(
                foundationState.FoundationCount,
                Is.EqualTo(1));
        }


        [Test]
        public void FoundationEdit_DuplicateCells_Throws()
        {
            GridPosition cell =
                CreateCell(1, 1);

            Assert.Throws<ArgumentException>(
                () => FoundationEdit.AddFoundations(
                    new[] { cell, cell }));
        }


        [Test]
        public void FoundationEdit_Inverse_PreservesExactCells()
        {
            GridPosition[] cells =
            {
                CreateCell(1, 1),
                CreateCell(2, 2)
            };

            FoundationEdit inverse =
                FoundationEdit.AddFoundations(cells)
                    .Inverse();

            Assert.That(
                inverse.Kind,
                Is.EqualTo(
                    FoundationEditKind.RemoveFoundations));
            Assert.That(inverse.Count, Is.EqualTo(2));
            Assert.That(inverse.Cells[0], Is.EqualTo(cells[0]));
            Assert.That(inverse.Cells[1], Is.EqualTo(cells[1]));
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


        private sealed class BlockingRemovalValidator :
            IFoundationRemovalValidator
        {
            public GridPosition? BlockedCell
            {
                get;
                set;
            }


            public FoundationRemovalValidation ValidateRemoval(
                IReadOnlyList<GridPosition> cells)
            {
                return BlockedCell.HasValue
                    ? FoundationRemovalValidation.Blocked(
                        BlockedCell.Value)
                    : FoundationRemovalValidation.Allowed();
            }
        }
    }
}
