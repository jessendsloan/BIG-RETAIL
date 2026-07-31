using System;
using System.Collections.Generic;
using BigRetail.Map.Construction;
using BigRetail.Map.Domain;
using BigRetail.Map.Floors;
using NUnit.Framework;

namespace BigRetail.Map.Floors.Tests
{
    public sealed class FloorFinishServiceTests
    {
        private static readonly GridPosition FloorCell =
            new GridPosition(4, 7, 0);

        private static readonly FloorFinishId DefaultFinish =
            new FloorFinishId("concrete");

        private static readonly FloorFinishId WoodFinish =
            new FloorFinishId("wood");

        private FloorState floorState;
        private FloorFinishState finishState;
        private FloorFinishService service;
        private FloorConstructionService floorConstruction;


        [SetUp]
        public void SetUp()
        {
            floorState =
                new FloorState(
                    new[]
                    {
                        FloorCell
                    });

            finishState =
                new FloorFinishState();

            GridMapDefinition map =
                new GridMapDefinition(
                    "floor.finish.test",
                    new[]
                    {
                        FloorCell
                    });

            floorConstruction =
                new FloorConstructionService(
                    map,
                    new ConstructionAreaDefinition(
                        map,
                        new[]
                        {
                            FloorCell
                        }),
                    floorState,
                    UnrestrictedFoundationSupportQuery.Instance);

            service =
                new FloorFinishService(
                    floorState,
                    new FloorFinishCatalog(
                        DefaultFinish,
                        new[]
                        {
                            DefaultFinish,
                            WoodFinish
                        }),
                    finishState);
        }

        [TearDown]
        public void TearDown()
        {
            service.Dispose();
        }


        [Test]
        public void FloorFinishId_NormalizesIdentity()
        {
            Assert.That(
                new FloorFinishId(" wood "),
                Is.EqualTo(
                    new FloorFinishId("WOOD")));
        }

        [Test]
        public void FloorFinishId_RejectsEmptyIdentity()
        {
            Assert.Throws<ArgumentException>(
                () => new FloorFinishId("  "));
        }

        [Test]
        public void Catalog_RequiresDefaultInRegisteredFinishes()
        {
            Assert.Throws<ArgumentException>(
                () => new FloorFinishCatalog(
                    DefaultFinish,
                    new[]
                    {
                        WoodFinish
                    }));
        }

        [Test]
        public void Catalog_RejectsDuplicateNormalizedIdentity()
        {
            Assert.Throws<ArgumentException>(
                () => new FloorFinishCatalog(
                    DefaultFinish,
                    new[]
                    {
                        DefaultFinish,
                        new FloorFinishId(" CONCRETE ")
                    }));
        }

        [Test]
        public void NewFloor_UsesCatalogDefault()
        {
            Assert.That(
                service.GetEffectiveFinish(FloorCell),
                Is.EqualTo(DefaultFinish));
        }

        [Test]
        public void SetFinish_StoresNonDefaultOverride()
        {
            FloorFinishChangeResult result =
                service.TrySetFinish(
                    FloorCell,
                    WoodFinish);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Changed, Is.True);
            Assert.That(
                service.GetEffectiveFinish(FloorCell),
                Is.EqualTo(WoodFinish));
            Assert.That(
                finishState.OverrideCount,
                Is.EqualTo(1));
        }

        [Test]
        public void SetSameFinish_IsSuccessfulNoOp()
        {
            service.TrySetFinish(
                FloorCell,
                WoodFinish);

            FloorFinishChangeResult result =
                service.TrySetFinish(
                    FloorCell,
                    WoodFinish);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Changed, Is.False);
            Assert.That(
                finishState.OverrideCount,
                Is.EqualTo(1));
        }

        [Test]
        public void ResetFinish_RemovesOverride()
        {
            service.TrySetFinish(
                FloorCell,
                WoodFinish);

            FloorFinishChangeResult result =
                service.TryResetFinish(FloorCell);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Changed, Is.True);
            Assert.That(
                service.GetEffectiveFinish(FloorCell),
                Is.EqualTo(DefaultFinish));
            Assert.That(
                finishState.OverrideCount,
                Is.Zero);
        }

        [Test]
        public void SetFinish_RejectsMissingFloorWithoutMutation()
        {
            GridPosition emptyCell =
                new GridPosition(8, 9, 0);

            FloorFinishChangeResult result =
                service.TrySetFinish(
                    emptyCell,
                    WoodFinish);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.Failure,
                Is.EqualTo(
                    FloorFinishChangeFailure.FloorNotFound));
            Assert.That(
                finishState.OverrideCount,
                Is.Zero);
        }

        [Test]
        public void SetFinish_RejectsUnknownFinishWithoutMutation()
        {
            FloorFinishChangeResult result =
                service.TrySetFinish(
                    FloorCell,
                    new FloorFinishId("unknown"));

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.Failure,
                Is.EqualTo(
                    FloorFinishChangeFailure.UnknownFinish));
            Assert.That(
                finishState.OverrideCount,
                Is.Zero);
        }

        [Test]
        public void FinishChange_PublishesEffectiveFinish()
        {
            var changes =
                new List<KeyValuePair<GridPosition, FloorFinishId>>();

            service.EffectiveFinishChanged +=
                (cell, finishId) =>
                    changes.Add(
                        new KeyValuePair<GridPosition, FloorFinishId>(
                            cell,
                            finishId));

            service.TrySetFinish(
                FloorCell,
                WoodFinish);

            Assert.That(changes, Has.Count.EqualTo(1));
            Assert.That(changes[0].Key, Is.EqualTo(FloorCell));
            Assert.That(changes[0].Value, Is.EqualTo(WoodFinish));
        }

        [Test]
        public void FloorRemoval_ClearsFinishOverride()
        {
            service.TrySetFinish(
                FloorCell,
                WoodFinish);

            FloorClearResult removal =
                floorConstruction.TryClearFloors(
                    new[]
                    {
                        FloorCell
                    });

            Assert.That(removal.Succeeded, Is.True);
            Assert.That(removal.RemovedCount, Is.EqualTo(1));
            Assert.That(
                finishState.OverrideCount,
                Is.Zero);
        }

        [Test]
        public void GetEffectiveFinish_RejectsMissingFloor()
        {
            Assert.Throws<KeyNotFoundException>(
                () => service.GetEffectiveFinish(
                    new GridPosition(8, 9, 0)));
        }

        [Test]
        public void DisposedService_RejectsFurtherQueries()
        {
            service.Dispose();

            Assert.Throws<ObjectDisposedException>(
                () => service.GetEffectiveFinish(FloorCell));
        }
    }
}
