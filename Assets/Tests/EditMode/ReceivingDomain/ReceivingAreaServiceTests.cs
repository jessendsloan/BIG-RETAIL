using System.Collections.Generic;
using BigRetail.Map.Construction;
using BigRetail.Map.Domain;
using BigRetail.Receiving.Domain;
using NUnit.Framework;

namespace BigRetail.Receiving.Domain.Tests
{
    public sealed class ReceivingAreaServiceTests
    {
        private static readonly GridPosition FirstCell =
            new GridPosition(1, 1, 0);
        private static readonly GridPosition SecondCell =
            new GridPosition(2, 1, 0);
        private static readonly GridPosition ThirdCell =
            new GridPosition(3, 1, 0);

        private ReceivingAreaState state;
        private ReceivingAreaService service;
        private MutableEligibility eligibility;
        private MutableSurface surface;


        [SetUp]
        public void SetUp()
        {
            GridMapDefinition map = new GridMapDefinition(
                "receiving.test",
                new[]
                {
                    FirstCell,
                    SecondCell,
                    ThirdCell
                });
            eligibility = new MutableEligibility();
            surface = new MutableSurface();

            eligibility.Add(FirstCell);
            eligibility.Add(SecondCell);
            eligibility.Add(ThirdCell);
            surface.AddFloor(FirstCell);
            surface.AddFloor(SecondCell);
            surface.AddFloor(ThirdCell);

            state = new ReceivingAreaState();
            service = new ReceivingAreaService(
                map,
                eligibility,
                surface,
                state);
        }


        [Test]
        public void AddArea_DesignatesEveryValidFloorCell()
        {
            ReceivingAreaChangeResult result = service.TryAddArea(
                new[]
                {
                    FirstCell,
                    SecondCell
                });

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.ChangedCellCount, Is.EqualTo(2));
            Assert.That(state.CellCount, Is.EqualTo(2));
            Assert.That(state.Contains(FirstCell), Is.True);
        }

        [Test]
        public void AddArea_RejectsMissingFloorWithoutPartialMutation()
        {
            surface.RemoveFloor(SecondCell);

            ReceivingAreaChangeResult result = service.TryAddArea(
                new[]
                {
                    FirstCell,
                    SecondCell
                });

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.Failure,
                Is.EqualTo(ReceivingAreaChangeFailure.MissingFloor));
            Assert.That(state.CellCount, Is.Zero);
        }

        [Test]
        public void AddArea_RejectsFixtureObstruction()
        {
            surface.Obstruct(FirstCell);

            ReceivingAreaChangeResult result = service.TryAddArea(
                new[] { FirstCell });

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.Failure,
                Is.EqualTo(ReceivingAreaChangeFailure.Obstructed));
        }

        [Test]
        public void RemoveArea_RejectsAnOccupiedDeliveryCell()
        {
            service.TryAddArea(new[] { FirstCell });
            ReceivingAreaReservationService reservations =
                new ReceivingAreaReservationService(
                    state,
                    cell => true);
            reservations.Synchronize(new long[] { 1001 });

            ReceivingAreaChangeResult result = service.TryRemoveArea(
                new[] { FirstCell });

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.Failure,
                Is.EqualTo(
                    ReceivingAreaChangeFailure.OccupiedByDelivery));
            Assert.That(state.Contains(FirstCell), Is.True);
        }

        [Test]
        public void Reservations_PreserveStableSlotsAndLeaveOverflowWaiting()
        {
            service.TryAddArea(
                new[]
                {
                    SecondCell,
                    FirstCell
                });
            ReceivingAreaReservationService reservations =
                new ReceivingAreaReservationService(
                    state,
                    cell => true);

            int stagedCount = reservations.Synchronize(
                new long[] { 1001, 1002, 1003 });

            Assert.That(stagedCount, Is.EqualTo(2));
            Assert.That(
                state.TryGetReservation(1001, out GridPosition firstSlot),
                Is.True);
            Assert.That(firstSlot, Is.EqualTo(FirstCell));
            Assert.That(
                state.TryGetReservation(1002, out GridPosition secondSlot),
                Is.True);
            Assert.That(secondSlot, Is.EqualTo(SecondCell));
            Assert.That(state.TryGetReservation(1003, out _), Is.False);

            service.TryAddArea(new[] { ThirdCell });
            reservations.Synchronize(new long[] { 1001, 1002, 1003 });

            Assert.That(
                state.TryGetReservation(1001, out GridPosition stableSlot),
                Is.True);
            Assert.That(stableSlot, Is.EqualTo(FirstCell));
            Assert.That(state.TryGetReservation(1003, out _), Is.True);
        }

        [Test]
        public void Reservations_ReleaseOrdersThatAreNoLongerReady()
        {
            service.TryAddArea(new[] { FirstCell });
            ReceivingAreaReservationService reservations =
                new ReceivingAreaReservationService(
                    state,
                    cell => true);
            reservations.Synchronize(new long[] { 1001 });

            reservations.Synchronize(new long[] { 1002 });

            Assert.That(state.TryGetReservation(1001, out _), Is.False);
            Assert.That(state.TryGetReservation(1002, out _), Is.True);
        }

        [Test]
        public void Reservations_DistinguishSupplierAndEquipmentSequences()
        {
            service.TryAddArea(new[] { FirstCell, SecondCell });
            ReceivingAreaReservationService reservations =
                new ReceivingAreaReservationService(
                    state,
                    cell => true);
            ReceivingLoadId supplier =
                ReceivingLoadId.SupplierOrder(1);
            ReceivingLoadId equipment =
                ReceivingLoadId.EquipmentOrder(1);

            int staged = reservations.Synchronize(
                new[] { supplier, equipment });

            Assert.That(staged, Is.EqualTo(2));
            Assert.That(
                state.TryGetReservation(supplier, out GridPosition first),
                Is.True);
            Assert.That(
                state.TryGetReservation(equipment, out GridPosition second),
                Is.True);
            Assert.That(first, Is.Not.EqualTo(second));
        }


        private sealed class MutableEligibility :
            IConstructionCellEligibility
        {
            private readonly HashSet<GridPosition> cells =
                new HashSet<GridPosition>();

            public void Add(GridPosition cell)
            {
                cells.Add(cell);
            }

            public bool IsEligible(GridPosition position)
            {
                return cells.Contains(position);
            }
        }

        private sealed class MutableSurface :
            IReceivingAreaSurfaceQuery
        {
            private readonly HashSet<GridPosition> floors =
                new HashSet<GridPosition>();
            private readonly HashSet<GridPosition> obstructions =
                new HashSet<GridPosition>();

            public void AddFloor(GridPosition cell)
            {
                floors.Add(cell);
            }

            public void RemoveFloor(GridPosition cell)
            {
                floors.Remove(cell);
            }

            public void Obstruct(GridPosition cell)
            {
                obstructions.Add(cell);
            }

            public bool HasFloor(GridPosition cell)
            {
                return floors.Contains(cell);
            }

            public bool IsObstructed(GridPosition cell)
            {
                return obstructions.Contains(cell);
            }
        }
    }
}
