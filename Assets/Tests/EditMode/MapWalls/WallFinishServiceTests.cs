using System;
using BigRetail.Map.Construction;
using BigRetail.Map.Domain;
using NUnit.Framework;

namespace BigRetail.Map.Walls.Tests
{
    public sealed class WallFinishServiceTests
    {
        private static readonly WallFinishId DefaultFinish =
            new WallFinishId("DEFAULT");

        private static readonly WallFinishId BrickFinish =
            new WallFinishId("BRICK");

        private static readonly WallFinishId TileFinish =
            new WallFinishId("TILE");

        private static readonly CellEdge FirstWall =
            new CellEdge(
                new GridPosition(0, 0),
                CellEdgeDirection.NorthEast);

        private static readonly CellEdge SecondWall =
            new CellEdge(
                new GridPosition(0, 1),
                CellEdgeDirection.NorthEast);


        private WallState wallState;
        private WallFinishState finishState;
        private WallFinishService finishService;
        private WallConstructionService wallConstructionService;


        [SetUp]
        public void SetUp()
        {
            GridPosition[] validCells =
            {
                new GridPosition(0, 0),
                new GridPosition(1, 0),
                new GridPosition(0, 1),
                new GridPosition(1, 1)
            };

            GridMapDefinition mapDefinition =
                new GridMapDefinition(
                    "test.map.wall_finishes",
                    validCells);

            ConstructionAreaDefinition constructionArea =
                new ConstructionAreaDefinition(
                    mapDefinition,
                    validCells);

            wallState =
                new WallState(
                    new[]
                    {
                        FirstWall,
                        SecondWall
                    });

            finishState =
                new WallFinishState();

            WallFinishCatalog finishCatalog =
                new WallFinishCatalog(
                    DefaultFinish,
                    new[]
                    {
                        DefaultFinish,
                        BrickFinish,
                        TileFinish
                    });

            finishService =
                new WallFinishService(
                    wallState,
                    finishCatalog,
                    finishState);

            wallConstructionService =
                new WallConstructionService(
                    mapDefinition,
                    constructionArea,
                    wallState,
                    UnrestrictedFoundationSupportQuery.Instance);
        }

        [TearDown]
        public void TearDown()
        {
            finishService?.Dispose();
        }


        [Test]
        public void WallFinishId_NormalizesWhitespaceAndCase()
        {
            WallFinishId normalized =
                new WallFinishId("  brick  ");

            Assert.That(normalized, Is.EqualTo(BrickFinish));
            Assert.That(normalized.Value, Is.EqualTo("BRICK"));
        }

        [Test]
        public void Catalog_DefaultFinishMustBeIncluded()
        {
            Assert.Throws<ArgumentException>(
                () => new WallFinishCatalog(
                    DefaultFinish,
                    new[]
                    {
                        BrickFinish,
                        TileFinish
                    }));
        }

        [Test]
        public void NewWallFaces_UseDefaultWithoutStoringOverrides()
        {
            Assert.That(
                finishService.GetEffectiveFinish(
                    FirstWall,
                    FirstWall.FirstCell),
                Is.EqualTo(DefaultFinish));

            Assert.That(
                finishService.GetEffectiveFinish(
                    FirstWall,
                    FirstWall.SecondCell),
                Is.EqualTo(DefaultFinish));

            Assert.That(finishState.OverrideCount, Is.EqualTo(0));
        }

        [Test]
        public void SetFinish_ChangesOnlyRequestedFace()
        {
            WallFinishChangeResult result =
                finishService.TrySetFinish(
                    FirstWall,
                    FirstWall.FirstCell,
                    BrickFinish);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Changed, Is.True);
            Assert.That(result.EffectiveFinishId, Is.EqualTo(BrickFinish));

            Assert.That(
                finishService.GetEffectiveFinish(
                    FirstWall,
                    FirstWall.FirstCell),
                Is.EqualTo(BrickFinish));

            Assert.That(
                finishService.GetEffectiveFinish(
                    FirstWall,
                    FirstWall.SecondCell),
                Is.EqualTo(DefaultFinish));

            Assert.That(finishState.OverrideCount, Is.EqualTo(1));
        }

        [Test]
        public void SetFinish_ChangesOnlyRequestedWall()
        {
            finishService.TrySetFinish(
                FirstWall,
                FirstWall.FirstCell,
                BrickFinish);

            Assert.That(
                finishService.GetEffectiveFinish(
                    SecondWall,
                    SecondWall.FirstCell),
                Is.EqualTo(DefaultFinish));

            Assert.That(
                finishService.GetEffectiveFinish(
                    SecondWall,
                    SecondWall.SecondCell),
                Is.EqualTo(DefaultFinish));
        }

        [Test]
        public void SetSameFinishTwice_SecondRequestIsSuccessfulNoOp()
        {
            WallFinishChangeResult first =
                finishService.TrySetFinish(
                    FirstWall,
                    FirstWall.FirstCell,
                    BrickFinish);

            WallFinishChangeResult second =
                finishService.TrySetFinish(
                    FirstWall,
                    FirstWall.FirstCell,
                    BrickFinish);

            Assert.That(first.Changed, Is.True);
            Assert.That(second.Succeeded, Is.True);
            Assert.That(second.Changed, Is.False);
            Assert.That(finishState.OverrideCount, Is.EqualTo(1));
        }

        [Test]
        public void SetDefaultFinish_RemovesStoredOverride()
        {
            finishService.TrySetFinish(
                FirstWall,
                FirstWall.FirstCell,
                BrickFinish);

            WallFinishChangeResult result =
                finishService.TrySetFinish(
                    FirstWall,
                    FirstWall.FirstCell,
                    DefaultFinish);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Changed, Is.True);
            Assert.That(result.EffectiveFinishId, Is.EqualTo(DefaultFinish));
            Assert.That(finishState.OverrideCount, Is.EqualTo(0));
        }

        [Test]
        public void ResetFinish_RemovesStoredOverride()
        {
            finishService.TrySetFinish(
                FirstWall,
                FirstWall.SecondCell,
                TileFinish);

            WallFinishChangeResult result =
                finishService.TryResetFinish(
                    FirstWall,
                    FirstWall.SecondCell);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Changed, Is.True);
            Assert.That(result.EffectiveFinishId, Is.EqualTo(DefaultFinish));
            Assert.That(finishState.OverrideCount, Is.EqualTo(0));
        }

        [Test]
        public void ResetDefaultFace_IsSuccessfulNoOp()
        {
            WallFinishChangeResult result =
                finishService.TryResetFinish(
                    FirstWall,
                    FirstWall.FirstCell);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Changed, Is.False);
            Assert.That(result.EffectiveFinishId, Is.EqualTo(DefaultFinish));
        }

        [Test]
        public void SetFinish_CellNotOnEdge_IsRejectedWithoutMutation()
        {
            WallFinishChangeResult result =
                finishService.TrySetFinish(
                    FirstWall,
                    new GridPosition(8, 8),
                    BrickFinish);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.Failure,
                Is.EqualTo(
                    WallFinishChangeFailure.FacingCellNotOnEdge));
            Assert.That(finishState.OverrideCount, Is.EqualTo(0));
        }

        [Test]
        public void SetFinish_UnknownFinish_IsRejectedWithoutMutation()
        {
            WallFinishChangeResult result =
                finishService.TrySetFinish(
                    FirstWall,
                    FirstWall.FirstCell,
                    new WallFinishId("MARBLE"));

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.Failure,
                Is.EqualTo(WallFinishChangeFailure.UnknownFinish));
            Assert.That(finishState.OverrideCount, Is.EqualTo(0));
        }

        [Test]
        public void SetFinish_MissingWall_IsRejectedWithoutMutation()
        {
            CellEdge missingWall =
                new CellEdge(
                    new GridPosition(0, 0),
                    CellEdgeDirection.NorthWest);

            WallFinishChangeResult result =
                finishService.TrySetFinish(
                    missingWall,
                    missingWall.FirstCell,
                    BrickFinish);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.Failure,
                Is.EqualTo(WallFinishChangeFailure.WallNotFound));
            Assert.That(finishState.OverrideCount, Is.EqualTo(0));
        }

        [Test]
        public void RemovingWall_ClearsBothFaceOverrides()
        {
            finishService.TrySetFinish(
                FirstWall,
                FirstWall.FirstCell,
                BrickFinish);

            finishService.TrySetFinish(
                FirstWall,
                FirstWall.SecondCell,
                TileFinish);

            Assert.That(finishState.OverrideCount, Is.EqualTo(2));

            WallChangeResult removal =
                wallConstructionService.TryRemoveWall(
                    FirstWall);

            Assert.That(removal.Succeeded, Is.True);
            Assert.That(finishState.OverrideCount, Is.EqualTo(0));
        }

        [Test]
        public void RemovingDifferentWall_PreservesUnrelatedOverrides()
        {
            finishService.TrySetFinish(
                FirstWall,
                FirstWall.FirstCell,
                BrickFinish);

            wallConstructionService.TryRemoveWall(
                SecondWall);

            Assert.That(finishState.OverrideCount, Is.EqualTo(1));
            Assert.That(
                finishService.GetEffectiveFinish(
                    FirstWall,
                    FirstWall.FirstCell),
                Is.EqualTo(BrickFinish));
        }

        [Test]
        public void EffectiveFinishChanged_RaisesOnlyForRealChanges()
        {
            int notificationCount = 0;
            WallFaceKey lastFace = default;
            WallFinishId lastFinish = default;

            finishService.EffectiveFinishChanged +=
                (face, finish) =>
                {
                    notificationCount++;
                    lastFace = face;
                    lastFinish = finish;
                };

            finishService.TrySetFinish(
                FirstWall,
                FirstWall.FirstCell,
                BrickFinish);

            finishService.TrySetFinish(
                FirstWall,
                FirstWall.FirstCell,
                BrickFinish);

            finishService.TryResetFinish(
                FirstWall,
                FirstWall.FirstCell);

            Assert.That(notificationCount, Is.EqualTo(2));
            Assert.That(lastFace.Edge, Is.EqualTo(FirstWall));
            Assert.That(
                lastFace.FacingCell,
                Is.EqualTo(FirstWall.FirstCell));
            Assert.That(lastFinish, Is.EqualTo(DefaultFinish));
        }
    }
}
