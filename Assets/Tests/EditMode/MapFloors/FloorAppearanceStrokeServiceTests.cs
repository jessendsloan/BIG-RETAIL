using System.Collections.Generic;
using BigRetail.Map.Construction;
using BigRetail.Map.Domain;
using BigRetail.Map.Floors;
using NUnit.Framework;

namespace BigRetail.Map.Floors.Tests
{
    public sealed class FloorAppearanceStrokeServiceTests
    {
        private static readonly FloorFinishId DefaultFinish =
            new FloorFinishId("concrete");

        private static readonly FloorFinishId WoodFinish =
            new FloorFinishId("wood");

        private FloorState floorState;
        private FloorConstructionService floorConstruction;
        private FloorFinishService floorFinishes;
        private FloorAppearanceStrokeService appearanceService;


        [SetUp]
        public void SetUp()
        {
            List<GridPosition> cells =
                new List<GridPosition>();

            for (int x = 0; x < 4; x++)
            {
                for (int y = 0; y < 4; y++)
                {
                    cells.Add(
                        Cell(x, y));
                }
            }

            GridMapDefinition map =
                new GridMapDefinition(
                    "floor.appearance.test",
                    cells);

            floorState =
                new FloorState();

            floorConstruction =
                new FloorConstructionService(
                    map,
                    new ConstructionAreaDefinition(
                        map,
                        cells),
                    floorState,
                    UnrestrictedFoundationSupportQuery.Instance);

            FloorFinishCatalog catalog =
                new FloorFinishCatalog(
                    DefaultFinish,
                    new[]
                    {
                        DefaultFinish,
                        WoodFinish
                    });

            floorFinishes =
                new FloorFinishService(
                    floorState,
                    catalog,
                    new FloorFinishState());

            appearanceService =
                new FloorAppearanceStrokeService(
                    floorConstruction,
                    floorFinishes,
                    catalog);
        }

        [TearDown]
        public void TearDown()
        {
            floorFinishes.Dispose();
        }


        [Test]
        public void ApplyDefault_CreatesMissingFloorWithoutOverride()
        {
            GridPosition cell =
                Cell(1, 1);

            FloorAppearanceStrokeResult result =
                appearanceService.TryApply(
                    new[]
                    {
                        cell
                    },
                    DefaultFinish);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.CreatedFloorCount, Is.EqualTo(1));
            Assert.That(result.FinishChangeCount, Is.Zero);
            Assert.That(floorState.HasFloor(cell), Is.True);
            Assert.That(
                floorFinishes.GetEffectiveFinish(cell),
                Is.EqualTo(DefaultFinish));
        }

        [Test]
        public void ApplyNonDefault_CreatesAndFinishesMissingFloor()
        {
            GridPosition cell =
                Cell(1, 1);

            FloorAppearanceStrokeResult result =
                appearanceService.TryApply(
                    new[]
                    {
                        cell
                    },
                    WoodFinish);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.CreatedFloorCount, Is.EqualTo(1));
            Assert.That(result.FinishChangeCount, Is.EqualTo(1));
            Assert.That(
                floorFinishes.GetEffectiveFinish(cell),
                Is.EqualTo(WoodFinish));
        }

        [Test]
        public void ApplyToExistingFloor_ResurfacesWithoutCreating()
        {
            GridPosition cell =
                Cell(1, 1);

            AddFloor(cell);

            FloorAppearanceStrokeResult result =
                appearanceService.TryApply(
                    new[]
                    {
                        cell
                    },
                    WoodFinish);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.CreatedFloorCount, Is.Zero);
            Assert.That(result.ExistingFloorCount, Is.EqualTo(1));
            Assert.That(result.FinishChangeCount, Is.EqualTo(1));
            Assert.That(
                floorFinishes.GetEffectiveFinish(cell),
                Is.EqualTo(WoodFinish));
        }

        [Test]
        public void ApplySameFinishToExistingFloor_IsNoOp()
        {
            GridPosition cell =
                Cell(1, 1);

            AddFloor(cell);

            FloorAppearanceStrokeResult result =
                appearanceService.TryApply(
                    new[]
                    {
                        cell
                    },
                    DefaultFinish);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Edit.IsEmpty, Is.True);
            Assert.That(result.UnchangedFinishCount, Is.EqualTo(1));
        }

        [Test]
        public void Apply_MixesCreatedAndExistingFloors()
        {
            GridPosition existing =
                Cell(1, 1);

            GridPosition created =
                Cell(2, 1);

            AddFloor(existing);

            FloorAppearanceStrokeResult result =
                appearanceService.TryApply(
                    new[]
                    {
                        existing,
                        created
                    },
                    WoodFinish);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.CreatedFloorCount, Is.EqualTo(1));
            Assert.That(result.ExistingFloorCount, Is.EqualTo(1));
            Assert.That(result.FinishChangeCount, Is.EqualTo(2));
            Assert.That(result.Edit.ChangeCount, Is.EqualTo(3));
        }

        [Test]
        public void Apply_SkipsInvalidCells()
        {
            GridPosition valid =
                Cell(1, 1);

            GridPosition outside =
                Cell(20, 20);

            FloorAppearanceStrokeResult result =
                appearanceService.TryApply(
                    new[]
                    {
                        valid,
                        outside
                    },
                    WoodFinish);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.CreatedFloorCount, Is.EqualTo(1));
            Assert.That(result.SkippedCellCount, Is.EqualTo(1));
            Assert.That(floorState.HasFloor(outside), Is.False);
        }

        [Test]
        public void Apply_RejectsUnknownFinishWithoutMutation()
        {
            FloorAppearanceStrokeResult result =
                appearanceService.TryApply(
                    new[]
                    {
                        Cell(1, 1)
                    },
                    new FloorFinishId("unknown"));

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.Failure,
                Is.EqualTo(
                    FloorAppearanceStrokeFailure.UnknownFinish));
            Assert.That(floorState.FloorCount, Is.Zero);
        }

        [Test]
        public void ReversibleAction_UndoesAndRedoesMixedStroke()
        {
            GridPosition existing =
                Cell(1, 1);

            GridPosition created =
                Cell(2, 1);

            AddFloor(existing);

            FloorAppearanceStrokeResult result =
                appearanceService.TryApply(
                    new[]
                    {
                        existing,
                        created
                    },
                    WoodFinish);

            ReversibleFloorAppearanceStrokeAction action =
                new ReversibleFloorAppearanceStrokeAction(
                    floorConstruction,
                    floorFinishes,
                    result.Edit);

            ConstructionActionResult undo =
                action.TryUndo();

            Assert.That(undo.Succeeded, Is.True);
            Assert.That(floorState.HasFloor(existing), Is.True);
            Assert.That(floorState.HasFloor(created), Is.False);
            Assert.That(
                floorFinishes.GetEffectiveFinish(existing),
                Is.EqualTo(DefaultFinish));

            ConstructionActionResult redo =
                action.TryRedo();

            Assert.That(redo.Succeeded, Is.True);
            Assert.That(floorState.HasFloor(created), Is.True);
            Assert.That(
                floorFinishes.GetEffectiveFinish(existing),
                Is.EqualTo(WoodFinish));
            Assert.That(
                floorFinishes.GetEffectiveFinish(created),
                Is.EqualTo(WoodFinish));
        }

        [Test]
        public void ReversibleAction_RejectsUndoAfterExternalFinishChange()
        {
            GridPosition cell =
                Cell(1, 1);

            AddFloor(cell);

            FloorAppearanceStrokeResult result =
                appearanceService.TryApply(
                    new[]
                    {
                        cell
                    },
                    WoodFinish);

            ReversibleFloorAppearanceStrokeAction action =
                new ReversibleFloorAppearanceStrokeAction(
                    floorConstruction,
                    floorFinishes,
                    result.Edit);

            floorFinishes.TrySetFinish(
                cell,
                DefaultFinish);

            ConstructionActionResult undo =
                action.TryUndo();

            Assert.That(undo.Succeeded, Is.False);
            Assert.That(
                floorFinishes.GetEffectiveFinish(cell),
                Is.EqualTo(DefaultFinish));
        }


        private void AddFloor(
            GridPosition cell)
        {
            FloorEnsureResult result =
                floorConstruction.TryEnsureFloors(
                    new[]
                    {
                        cell
                    });

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.ChangedCount, Is.EqualTo(1));
        }

        private static GridPosition Cell(
            int x,
            int y)
        {
            return new GridPosition(
                x,
                y,
                0);
        }
    }
}
