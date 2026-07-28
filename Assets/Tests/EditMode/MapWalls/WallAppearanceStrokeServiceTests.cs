using BigRetail.Map.Construction;
using BigRetail.Map.Domain;
using NUnit.Framework;

namespace BigRetail.Map.Walls.Tests
{
    public sealed class WallAppearanceStrokeServiceTests
    {
        private static readonly WallFinishId DefaultFinish =
            new WallFinishId("DEFAULT");

        private static readonly WallFinishId BrickFinish =
            new WallFinishId("BRICK");

        private static readonly CellEdge ExistingWall =
            new CellEdge(
                new GridPosition(0, 0),
                CellEdgeDirection.NorthEast);

        private static readonly CellEdge MissingWall =
            new CellEdge(
                new GridPosition(0, 1),
                CellEdgeDirection.NorthEast);

        private static readonly CellEdge InvalidWall =
            new CellEdge(
                new GridPosition(20, 20),
                CellEdgeDirection.NorthEast);


        private WallState wallState;
        private WallFinishState finishState;
        private WallConstructionService wallConstruction;
        private WallFinishService wallFinishes;
        private WallAppearanceStrokeService strokeService;


        [SetUp]
        public void SetUp()
        {
            GridPosition[] validCells =
            {
                new GridPosition(0, 0),
                new GridPosition(1, 0),
                new GridPosition(0, 1),
                new GridPosition(1, 1),
                new GridPosition(0, 2),
                new GridPosition(1, 2)
            };

            GridMapDefinition mapDefinition =
                new GridMapDefinition(
                    "test.map.wall_appearance_strokes",
                    validCells);

            ConstructionAreaDefinition constructionArea =
                new ConstructionAreaDefinition(
                    mapDefinition,
                    validCells);

            wallState =
                new WallState(
                    new[]
                    {
                        ExistingWall
                    });

            finishState =
                new WallFinishState();

            WallFinishCatalog finishCatalog =
                new WallFinishCatalog(
                    DefaultFinish,
                    new[]
                    {
                        DefaultFinish,
                        BrickFinish
                    });

            wallConstruction =
                new WallConstructionService(
                    mapDefinition,
                    constructionArea,
                    wallState);

            wallFinishes =
                new WallFinishService(
                    wallState,
                    finishCatalog,
                    finishState);

            strokeService =
                new WallAppearanceStrokeService(
                    wallConstruction,
                    wallFinishes,
                    finishCatalog);
        }


        [TearDown]
        public void TearDown()
        {
            wallFinishes?.Dispose();
        }


        [Test]
        public void Apply_MissingWall_CreatesWallAndFinishesOnlyRequestedFace()
        {
            WallFaceKey visibleFace =
                new WallFaceKey(
                    MissingWall,
                    MissingWall.FirstCell);

            WallAppearanceStrokeResult result =
                strokeService.TryApply(
                    new[]
                    {
                        visibleFace
                    },
                    BrickFinish);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.CreatedWallCount, Is.EqualTo(1));
            Assert.That(result.ExistingWallCount, Is.EqualTo(0));
            Assert.That(result.ChangedFinishCount, Is.EqualTo(1));
            Assert.That(result.Edit.ChangeCount, Is.EqualTo(2));
            Assert.That(wallState.HasWall(MissingWall), Is.True);
            Assert.That(
                wallFinishes.GetEffectiveFinish(
                    MissingWall,
                    MissingWall.FirstCell),
                Is.EqualTo(BrickFinish));
            Assert.That(
                wallFinishes.GetEffectiveFinish(
                    MissingWall,
                    MissingWall.SecondCell),
                Is.EqualTo(DefaultFinish));
        }


        [Test]
        public void Apply_ExistingWall_ChangesOnlyRequestedFace()
        {
            WallAppearanceStrokeResult result =
                strokeService.TryApply(
                    new[]
                    {
                        new WallFaceKey(
                            ExistingWall,
                            ExistingWall.SecondCell)
                    },
                    BrickFinish);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.CreatedWallCount, Is.EqualTo(0));
            Assert.That(result.ExistingWallCount, Is.EqualTo(1));
            Assert.That(result.ChangedFinishCount, Is.EqualTo(1));
            Assert.That(result.Edit.ChangeCount, Is.EqualTo(1));
            Assert.That(wallState.WallCount, Is.EqualTo(1));
            Assert.That(
                wallFinishes.GetEffectiveFinish(
                    ExistingWall,
                    ExistingWall.FirstCell),
                Is.EqualTo(DefaultFinish));
            Assert.That(
                wallFinishes.GetEffectiveFinish(
                    ExistingWall,
                    ExistingWall.SecondCell),
                Is.EqualTo(BrickFinish));
        }


        [Test]
        public void Apply_MixedStroke_CreatesAndRepaintsTogether()
        {
            WallAppearanceStrokeResult result =
                strokeService.TryApply(
                    new[]
                    {
                        new WallFaceKey(
                            ExistingWall,
                            ExistingWall.FirstCell),
                        new WallFaceKey(
                            MissingWall,
                            MissingWall.FirstCell)
                    },
                    BrickFinish);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.CreatedWallCount, Is.EqualTo(1));
            Assert.That(result.ExistingWallCount, Is.EqualTo(1));
            Assert.That(result.ChangedFinishCount, Is.EqualTo(2));
            Assert.That(result.Edit.ChangeCount, Is.EqualTo(3));
            Assert.That(wallState.WallCount, Is.EqualTo(2));
        }


        [Test]
        public void Apply_InvalidEdge_IsSkippedWithoutMutation()
        {
            WallAppearanceStrokeResult result =
                strokeService.TryApply(
                    new[]
                    {
                        new WallFaceKey(
                            InvalidWall,
                            InvalidWall.FirstCell)
                    },
                    BrickFinish);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.SkippedWallCount, Is.EqualTo(1));
            Assert.That(result.HasChanges, Is.False);
            Assert.That(wallState.WallCount, Is.EqualTo(1));
            Assert.That(finishState.OverrideCount, Is.EqualTo(0));
        }


        [Test]
        public void Apply_SameFinish_IsSuccessfulNoOp()
        {
            wallFinishes.TrySetFinish(
                ExistingWall,
                ExistingWall.FirstCell,
                BrickFinish);

            WallAppearanceStrokeResult result =
                strokeService.TryApply(
                    new[]
                    {
                        new WallFaceKey(
                            ExistingWall,
                            ExistingWall.FirstCell)
                    },
                    BrickFinish);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.ChangedFinishCount, Is.EqualTo(0));
            Assert.That(result.UnchangedFinishCount, Is.EqualTo(1));
            Assert.That(result.HasChanges, Is.False);
            Assert.That(finishState.OverrideCount, Is.EqualTo(1));
        }


        [Test]
        public void ReversibleAction_UndoAndRedo_RestoreMixedStroke()
        {
            WallAppearanceStrokeResult result =
                strokeService.TryApply(
                    new[]
                    {
                        new WallFaceKey(
                            ExistingWall,
                            ExistingWall.FirstCell),
                        new WallFaceKey(
                            MissingWall,
                            MissingWall.FirstCell)
                    },
                    BrickFinish);

            ReversibleWallAppearanceStrokeAction action =
                new ReversibleWallAppearanceStrokeAction(
                    wallConstruction,
                    wallFinishes,
                    result.Edit);

            ConstructionActionResult undo =
                action.TryUndo();

            Assert.That(undo.Succeeded, Is.True);
            Assert.That(wallState.HasWall(MissingWall), Is.False);
            Assert.That(
                wallFinishes.GetEffectiveFinish(
                    ExistingWall,
                    ExistingWall.FirstCell),
                Is.EqualTo(DefaultFinish));

            ConstructionActionResult redo =
                action.TryRedo();

            Assert.That(redo.Succeeded, Is.True);
            Assert.That(wallState.HasWall(MissingWall), Is.True);
            Assert.That(
                wallFinishes.GetEffectiveFinish(
                    ExistingWall,
                    ExistingWall.FirstCell),
                Is.EqualTo(BrickFinish));
            Assert.That(
                wallFinishes.GetEffectiveFinish(
                    MissingWall,
                    MissingWall.FirstCell),
                Is.EqualTo(BrickFinish));
            Assert.That(
                wallFinishes.GetEffectiveFinish(
                    MissingWall,
                    MissingWall.SecondCell),
                Is.EqualTo(DefaultFinish));
        }


        [Test]
        public void ReversibleAction_RejectsUndoAfterExternalFaceChange()
        {
            WallAppearanceStrokeResult result =
                strokeService.TryApply(
                    new[]
                    {
                        new WallFaceKey(
                            ExistingWall,
                            ExistingWall.FirstCell)
                    },
                    BrickFinish);

            ReversibleWallAppearanceStrokeAction action =
                new ReversibleWallAppearanceStrokeAction(
                    wallConstruction,
                    wallFinishes,
                    result.Edit);

            wallFinishes.TrySetFinish(
                ExistingWall,
                ExistingWall.FirstCell,
                DefaultFinish);

            ConstructionActionResult undo =
                action.TryUndo();

            Assert.That(undo.Succeeded, Is.False);
            Assert.That(wallState.HasWall(ExistingWall), Is.True);
            Assert.That(
                wallFinishes.GetEffectiveFinish(
                    ExistingWall,
                    ExistingWall.FirstCell),
                Is.EqualTo(DefaultFinish));
        }
    }
}
