using System;
using System.Collections.Generic;
using BigRetail.Map.Construction;
using BigRetail.Map.Domain;
using NUnit.Framework;

namespace BigRetail.Map.Foundations.Tests
{
    public sealed class ReversibleFoundationEditActionTests
    {
        private FoundationState foundationState;
        private FoundationConstructionService service;


        [SetUp]
        public void SetUp()
        {
            List<GridPosition> cells =
                new List<GridPosition>();

            for (int x = 0; x <= 3; x++)
            {
                for (int y = 0; y <= 3; y++)
                {
                    cells.Add(
                        new GridPosition(x, y));
                }
            }

            GridMapDefinition map =
                new GridMapDefinition(
                    "foundation.history.test.map",
                    cells);

            ConstructionAreaDefinition area =
                new ConstructionAreaDefinition(
                    map,
                    cells);

            foundationState =
                new FoundationState();

            service =
                new FoundationConstructionService(
                    map,
                    area,
                    foundationState,
                    UnrestrictedFoundationRemovalValidator.Instance);
        }


        [Test]
        public void Constructor_NullService_Throws()
        {
            FoundationEdit edit =
                FoundationEdit.AddFoundations(
                    new[] { new GridPosition(1, 1) });

            Assert.Throws<ArgumentNullException>(
                () => new ReversibleFoundationEditAction(
                    null,
                    edit));
        }


        [Test]
        public void Constructor_EmptyEdit_Throws()
        {
            Assert.Throws<ArgumentException>(
                () => new ReversibleFoundationEditAction(
                    service,
                    default));
        }


        [Test]
        public void TryUndo_AddEdit_RemovesFoundation()
        {
            GridPosition cell =
                new GridPosition(1, 1);

            FoundationEnsureResult placement =
                service.TryEnsureFoundations(
                    new[] { cell });

            ReversibleFoundationEditAction action =
                new ReversibleFoundationEditAction(
                    service,
                    placement.Edit);

            ConstructionActionResult result =
                action.TryUndo();

            Assert.That(result.Succeeded, Is.True);
            Assert.That(
                foundationState.HasFoundation(cell),
                Is.False);
        }


        [Test]
        public void TryRedo_AddEdit_RestoresFoundation()
        {
            GridPosition cell =
                new GridPosition(1, 1);

            FoundationEnsureResult placement =
                service.TryEnsureFoundations(
                    new[] { cell });

            ReversibleFoundationEditAction action =
                new ReversibleFoundationEditAction(
                    service,
                    placement.Edit);

            Assert.That(action.TryUndo().Succeeded, Is.True);

            ConstructionActionResult result =
                action.TryRedo();

            Assert.That(result.Succeeded, Is.True);
            Assert.That(
                foundationState.HasFoundation(cell),
                Is.True);
        }


        [Test]
        public void TryUndo_DivergedState_IsRejectedAtomically()
        {
            GridPosition first =
                new GridPosition(1, 1);

            GridPosition second =
                new GridPosition(2, 1);

            FoundationEnsureResult placement =
                service.TryEnsureFoundations(
                    new[] { first, second });

            ReversibleFoundationEditAction action =
                new ReversibleFoundationEditAction(
                    service,
                    placement.Edit);

            Assert.That(
                service.TryClearFoundations(
                    new[] { second }).Succeeded,
                Is.True);

            ConstructionActionResult result =
                action.TryUndo();

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                foundationState.HasFoundation(first),
                Is.True);
            Assert.That(
                foundationState.HasFoundation(second),
                Is.False);
            Assert.That(
                foundationState.FoundationCount,
                Is.EqualTo(1));
        }
    }
}
