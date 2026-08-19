using System.Collections.Generic;
using BigRetail.Map.Construction;
using BigRetail.Map.Domain;
using BigRetail.Map.Fixtures;
using NUnit.Framework;

namespace BigRetail.Map.Fixtures.Tests
{
    public sealed class FixturePlacementServiceTests
    {
        private static readonly FixtureDefinitionId ShelfDefinitionId =
            new FixtureDefinitionId("standard-shelf");

        private static readonly FixtureDefinitionId BlockerDefinitionId =
            new FixtureDefinitionId("blocker");

        private static readonly FixtureAccessMode SalesFloorAccess =
            FixtureAccessMode.CustomerBrowse
            | FixtureAccessMode.EmployeeStock;

        private static readonly FixtureAccessProfile LongFaceAccess =
            new FixtureAccessProfile(
                FixtureAccessMode.None,
                SalesFloorAccess,
                FixtureAccessMode.None,
                SalesFloorAccess);

        private static readonly FixtureAccessProfile SingleFaceAccess =
            new FixtureAccessProfile(
                FixtureAccessMode.None,
                FixtureAccessMode.None,
                SalesFloorAccess,
                FixtureAccessMode.None);

        private static readonly FixtureAccessProfile FlexibleStorageAccess =
            new FixtureAccessProfile(
                FixtureAccessMode.EmployeeStock,
                FixtureAccessMode.None,
                FixtureAccessMode.EmployeeStock,
                FixtureAccessMode.None,
                FixtureAccessClearancePolicy.AtLeastOneCompleteSide);


        [Test]
        public void EvaluatePlacement_ValidFixture_DoesNotMutateState()
        {
            FixtureState state =
                new FixtureState();

            FixturePlacementResult result =
                CreateService(state)
                    .EvaluatePlacement(
                        new FixtureInstanceId("shelf-1"),
                        ShelfDefinitionId,
                        new GridPosition(2, 2),
                        FixtureOrientation.East);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Changed, Is.False);
            Assert.That(result.Fixture, Is.Null);
            Assert.That(result.Footprint, Is.Not.Null);
            Assert.That(result.OccupiedCellCount, Is.EqualTo(2));
            Assert.That(result.Footprint.WidthInCells, Is.EqualTo(2));
            Assert.That(result.Footprint.DepthInCells, Is.EqualTo(1));
            Assert.That(state.FixtureCount, Is.EqualTo(0));
            Assert.That(state.OccupiedCellCount, Is.EqualTo(0));
        }


        [Test]
        public void TryPlaceFixture_ValidFixture_OccupiesEveryCellBeforeEvent()
        {
            FixtureState state =
                new FixtureState();

            FixtureInstanceId instanceId =
                new FixtureInstanceId("shelf-1");

            bool eventObservedCompleteState = false;

            state.FixtureAdded += fixture =>
            {
                eventObservedCompleteState =
                    fixture.Id == instanceId
                    && fixture.Orientation == FixtureOrientation.East
                    && state.FixtureCount == 1
                    && state.OccupiedCellCount == 2
                    && state.ReservedAccessCellCount == 4
                    && state.ReservedAccessBoundaryCount == 4;
            };

            FixturePlacementResult result =
                CreateService(
                        state,
                        shelfAccess: LongFaceAccess)
                    .TryPlaceFixture(
                        instanceId,
                        ShelfDefinitionId,
                        new GridPosition(2, 2),
                        FixtureOrientation.East);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Changed, Is.True);
            Assert.That(result.Edit.IsEmpty, Is.False);
            Assert.That(
                result.Edit.Kind,
                Is.EqualTo(FixtureEditKind.AddFixture));
            Assert.That(eventObservedCompleteState, Is.True);

            Assert.That(
                state.TryGetFixtureAtCell(
                    new GridPosition(2, 2),
                    out FixtureInstance firstCellFixture),
                Is.True);

            Assert.That(
                state.TryGetFixtureAtCell(
                    new GridPosition(3, 2),
                    out FixtureInstance secondCellFixture),
                Is.True);

            Assert.That(firstCellFixture, Is.SameAs(result.Fixture));
            Assert.That(secondCellFixture, Is.SameAs(result.Fixture));
            Assert.That(
                state.ReservedAccessCellCount,
                Is.EqualTo(4));
            Assert.That(
                state.ReservedAccessBoundaryCount,
                Is.EqualTo(4));
        }


        [Test]
        public void TryPlaceFixture_MissingFloor_RejectsAtomically()
        {
            FixtureState state =
                new FixtureState();

            HashSet<GridPosition> floors =
                CreateCells();

            floors.Remove(
                new GridPosition(3, 2));

            FixturePlacementResult result =
                CreateService(
                        state,
                        floors)
                    .TryPlaceFixture(
                        new FixtureInstanceId("shelf-1"),
                        ShelfDefinitionId,
                        new GridPosition(2, 2),
                        FixtureOrientation.East);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.Failure,
                Is.EqualTo(FixturePlacementFailure.MissingFloor));
            Assert.That(
                result.FailedCell,
                Is.EqualTo(new GridPosition(3, 2)));
            Assert.That(result.Footprint, Is.Not.Null);
            Assert.That(state.FixtureCount, Is.EqualTo(0));
            Assert.That(state.OccupiedCellCount, Is.EqualTo(0));
        }


        [Test]
        public void TryPlaceFixture_PartiallyOutsideMap_RejectsAtomically()
        {
            FixtureState state =
                new FixtureState();

            FixturePlacementResult result =
                CreateService(state)
                    .TryPlaceFixture(
                        new FixtureInstanceId("shelf-edge"),
                        ShelfDefinitionId,
                        new GridPosition(5, 5),
                        FixtureOrientation.East);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.Failure,
                Is.EqualTo(FixturePlacementFailure.OutsideMap));
            Assert.That(
                result.FailedCell,
                Is.EqualTo(new GridPosition(6, 5)));
            Assert.That(state.FixtureCount, Is.EqualTo(0));
        }


        [Test]
        public void TryPlaceFixture_PartiallyOutsideConstructionArea_RejectsAtomically()
        {
            FixtureState state =
                new FixtureState();

            HashSet<GridPosition> eligibleCells =
                CreateCells();

            eligibleCells.Remove(
                new GridPosition(3, 2));

            FixturePlacementResult result =
                CreateService(
                        state,
                        eligibleCells: eligibleCells)
                    .TryPlaceFixture(
                        new FixtureInstanceId("shelf-boundary"),
                        ShelfDefinitionId,
                        new GridPosition(2, 2),
                        FixtureOrientation.East);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.Failure,
                Is.EqualTo(
                    FixturePlacementFailure
                        .OutsideConstructionArea));
            Assert.That(
                result.FailedCell,
                Is.EqualTo(new GridPosition(3, 2)));
            Assert.That(state.FixtureCount, Is.EqualTo(0));
        }


        [Test]
        public void EvaluatePlacement_UnsupportedOrientation_IsRejected()
        {
            FixtureState state =
                new FixtureState();

            FixturePlacementResult result =
                CreateService(state)
                    .EvaluatePlacement(
                        new FixtureInstanceId("shelf-invalid"),
                        ShelfDefinitionId,
                        new GridPosition(2, 2),
                        (FixtureOrientation)99);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.Failure,
                Is.EqualTo(
                    FixturePlacementFailure
                        .UnsupportedOrientation));
            Assert.That(result.Footprint, Is.Null);
            Assert.That(state.FixtureCount, Is.EqualTo(0));
        }


        [Test]
        public void TryPlaceFixture_Overlap_RejectsCompleteSecondFixture()
        {
            FixtureState state =
                new FixtureState();

            FixturePlacementService service =
                CreateService(
                    state,
                    shelfAccess: LongFaceAccess);

            Assert.That(
                service.TryPlaceFixture(
                        new FixtureInstanceId("shelf-1"),
                        ShelfDefinitionId,
                        new GridPosition(2, 2),
                        FixtureOrientation.East)
                    .Succeeded,
                Is.True);

            FixturePlacementResult result =
                service.TryPlaceFixture(
                    new FixtureInstanceId("shelf-2"),
                    ShelfDefinitionId,
                    new GridPosition(3, 2),
                    FixtureOrientation.North);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.Failure,
                Is.EqualTo(FixturePlacementFailure.OverlapsFixture));
            Assert.That(
                result.FailedCell,
                Is.EqualTo(new GridPosition(3, 2)));
            Assert.That(state.FixtureCount, Is.EqualTo(1));
            Assert.That(state.OccupiedCellCount, Is.EqualTo(2));
            Assert.That(
                state.IsOccupied(new GridPosition(3, 3)),
                Is.False);
        }


        [TestCase(
            FixtureOrientation.East,
            CellEdgeDirection.NorthEast,
            3,
            2)]
        [TestCase(
            FixtureOrientation.North,
            CellEdgeDirection.NorthWest,
            2,
            3)]
        public void TryPlaceFixture_WallBetweenOccupiedCells_IsRejected(
            FixtureOrientation orientation,
            CellEdgeDirection wallDirection,
            int expectedFailedX,
            int expectedFailedY)
        {
            FixtureState state =
                new FixtureState();

            HashSet<CellEdge> walls =
                new HashSet<CellEdge>
                {
                    new CellEdge(
                        new GridPosition(2, 2),
                        wallDirection)
                };

            FixturePlacementResult result =
                CreateService(
                        state,
                        walls: walls)
                    .TryPlaceFixture(
                        new FixtureInstanceId("shelf-wall"),
                        ShelfDefinitionId,
                        new GridPosition(2, 2),
                        orientation);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.Failure,
                Is.EqualTo(FixturePlacementFailure.CrossesWall));
            Assert.That(
                result.FailedCell,
                Is.EqualTo(
                    new GridPosition(
                        expectedFailedX,
                        expectedFailedY)));
            Assert.That(state.FixtureCount, Is.EqualTo(0));
            Assert.That(state.OccupiedCellCount, Is.EqualTo(0));
        }


        [Test]
        public void TryPlaceFixture_WallOnFootprintPerimeter_IsAllowed()
        {
            FixtureState state =
                new FixtureState();

            HashSet<CellEdge> walls =
                new HashSet<CellEdge>
                {
                    new CellEdge(
                        new GridPosition(2, 2),
                        CellEdgeDirection.SouthWest)
                };

            FixturePlacementResult result =
                CreateService(
                        state,
                        walls: walls)
                    .TryPlaceFixture(
                        new FixtureInstanceId("shelf-beside-wall"),
                        ShelfDefinitionId,
                        new GridPosition(2, 2),
                        FixtureOrientation.East);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(state.OccupiedCellCount, Is.EqualTo(2));
        }


        [Test]
        public void TryPlaceFixture_WallAgainstUnauthoredBack_IsAllowed()
        {
            FixtureState state =
                new FixtureState();

            HashSet<CellEdge> walls =
                new HashSet<CellEdge>
                {
                    new CellEdge(
                        new GridPosition(2, 3),
                        CellEdgeDirection.NorthWest)
                };

            FixturePlacementResult result =
                CreateService(
                        state,
                        walls: walls,
                        shelfAccess: SingleFaceAccess)
                    .TryPlaceFixture(
                        new FixtureInstanceId("half-shelf-wall"),
                        ShelfDefinitionId,
                        new GridPosition(2, 2),
                        FixtureOrientation.North);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(state.OccupiedCellCount, Is.EqualTo(2));
        }


        [TestCase(FixtureOrientation.North)]
        [TestCase(FixtureOrientation.East)]
        [TestCase(FixtureOrientation.South)]
        [TestCase(FixtureOrientation.West)]
        public void TryPlaceFixture_HalfShelfBackWallIsAllowedAtEveryRotation(
            FixtureOrientation orientation)
        {
            FixtureState state =
                new FixtureState();

            FixturePlacementResult result =
                CreateService(
                        state,
                        walls: CreateHalfShelfBoundaryWalls(
                            orientation,
                            authoredFront: false),
                        shelfAccess: SingleFaceAccess,
                        shelfWidthInCells: 2,
                        shelfDepthInCells: 1)
                    .TryPlaceFixture(
                        new FixtureInstanceId(
                            $"half-shelf-back-{orientation}"),
                        ShelfDefinitionId,
                        new GridPosition(2, 2),
                        orientation);

            Assert.That(
                result.Succeeded,
                Is.True,
                $"The unauthored back must accept a wall at {orientation}.");
        }


        [TestCase(FixtureOrientation.North)]
        [TestCase(FixtureOrientation.East)]
        [TestCase(FixtureOrientation.South)]
        [TestCase(FixtureOrientation.West)]
        public void TryPlaceFixture_HalfShelfFrontWallIsRejectedAtEveryRotation(
            FixtureOrientation orientation)
        {
            FixtureState state =
                new FixtureState();

            FixturePlacementResult result =
                CreateService(
                        state,
                        walls: CreateHalfShelfBoundaryWalls(
                            orientation,
                            authoredFront: true),
                        shelfAccess: SingleFaceAccess,
                        shelfWidthInCells: 2,
                        shelfDepthInCells: 1)
                    .TryPlaceFixture(
                        new FixtureInstanceId(
                            $"half-shelf-front-{orientation}"),
                        ShelfDefinitionId,
                        new GridPosition(2, 2),
                        orientation);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.Failure,
                Is.EqualTo(FixturePlacementFailure.BlockedAccess));
        }


        [Test]
        public void TryPlaceFixture_HalfShelfOwnsBothFrontCellsOnly()
        {
            FixtureState state =
                new FixtureState();

            FixturePlacementService service =
                CreateService(
                    state,
                    shelfAccess: SingleFaceAccess,
                    shelfWidthInCells: 2,
                    shelfDepthInCells: 1);

            FixturePlacementResult placement =
                service.TryPlaceFixture(
                    new FixtureInstanceId("half-shelf"),
                    ShelfDefinitionId,
                    new GridPosition(2, 2),
                    FixtureOrientation.North);

            Assert.That(placement.Succeeded, Is.True);
            Assert.That(state.ReservedAccessCellCount, Is.EqualTo(2));
            Assert.That(
                state.IsAccessCellReserved(
                    new GridPosition(2, 1)),
                Is.True);
            Assert.That(
                state.IsAccessCellReserved(
                    new GridPosition(3, 1)),
                Is.True);
            Assert.That(
                state.IsAccessCellReserved(
                    new GridPosition(2, 3)),
                Is.False);
            Assert.That(
                state.IsAccessCellReserved(
                    new GridPosition(3, 3)),
                Is.False);

            FixturePlacementResult firstFrontBlocker =
                service.EvaluatePlacement(
                    new FixtureInstanceId("front-blocker-1"),
                    BlockerDefinitionId,
                    new GridPosition(2, 1),
                    FixtureOrientation.North);

            FixturePlacementResult secondFrontBlocker =
                service.EvaluatePlacement(
                    new FixtureInstanceId("front-blocker-2"),
                    BlockerDefinitionId,
                    new GridPosition(3, 1),
                    FixtureOrientation.North);

            FixturePlacementResult backBlocker =
                service.EvaluatePlacement(
                    new FixtureInstanceId("back-blocker"),
                    BlockerDefinitionId,
                    new GridPosition(2, 3),
                    FixtureOrientation.North);

            Assert.That(
                firstFrontBlocker.Failure,
                Is.EqualTo(FixturePlacementFailure.BlockedAccess));
            Assert.That(
                secondFrontBlocker.Failure,
                Is.EqualTo(FixturePlacementFailure.BlockedAccess));
            Assert.That(backBlocker.Succeeded, Is.True);
        }


        [Test]
        public void TryPlaceFixture_WallAcrossSingleAuthoredFace_IsRejected()
        {
            FixtureState state =
                new FixtureState();

            HashSet<CellEdge> walls =
                new HashSet<CellEdge>
                {
                    new CellEdge(
                        new GridPosition(2, 2),
                        CellEdgeDirection.SouthEast)
                };

            FixturePlacementResult result =
                CreateService(
                        state,
                        walls: walls,
                        shelfAccess: SingleFaceAccess)
                    .TryPlaceFixture(
                        new FixtureInstanceId("half-shelf-face-wall"),
                        ShelfDefinitionId,
                        new GridPosition(2, 2),
                        FixtureOrientation.North);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.Failure,
                Is.EqualTo(FixturePlacementFailure.BlockedAccess));
            Assert.That(state.FixtureCount, Is.EqualTo(0));
        }


        [Test]
        public void TryPlaceFixture_WallAcrossAuthoredFace_IsRejected()
        {
            FixtureState state =
                new FixtureState();

            HashSet<CellEdge> walls =
                new HashSet<CellEdge>
                {
                    new CellEdge(
                        new GridPosition(2, 2),
                        CellEdgeDirection.NorthEast)
                };

            FixturePlacementResult result =
                CreateService(
                        state,
                        walls: walls,
                        shelfAccess: LongFaceAccess)
                    .TryPlaceFixture(
                        new FixtureInstanceId("shelf-face-wall"),
                        ShelfDefinitionId,
                        new GridPosition(2, 2),
                        FixtureOrientation.North);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.Failure,
                Is.EqualTo(FixturePlacementFailure.BlockedAccess));
            Assert.That(
                result.FailedCell,
                Is.EqualTo(new GridPosition(3, 2)));
            Assert.That(state.FixtureCount, Is.EqualTo(0));
        }


        [Test]
        public void TryPlaceFixture_FlexibleStorageAgainstWall_UsesOpenSide()
        {
            FixtureState state = new FixtureState();
            HashSet<CellEdge> walls =
                new HashSet<CellEdge>
                {
                    new CellEdge(
                        new GridPosition(2, 3),
                        CellEdgeDirection.SouthEast),
                    new CellEdge(
                        new GridPosition(3, 3),
                        CellEdgeDirection.SouthEast)
                };

            FixturePlacementResult result =
                CreateService(
                        state,
                        walls: walls,
                        shelfAccess: FlexibleStorageAccess,
                        shelfWidthInCells: 2,
                        shelfDepthInCells: 1)
                    .TryPlaceFixture(
                        new FixtureInstanceId("backstock-wall-rack"),
                        ShelfDefinitionId,
                        new GridPosition(2, 2),
                        FixtureOrientation.North);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(state.ReservedAccessCellCount, Is.EqualTo(2));
            Assert.That(state.ReservedAccessBoundaryCount, Is.EqualTo(2));
            Assert.That(
                state.IsAccessBoundaryReserved(
                    new CellEdge(
                        new GridPosition(2, 3),
                        CellEdgeDirection.SouthEast)),
                Is.False);
            Assert.That(
                state.IsAccessBoundaryReserved(
                    new CellEdge(
                        new GridPosition(2, 1),
                        CellEdgeDirection.NorthWest)),
                Is.True);
        }


        [Test]
        public void TryPlaceFixture_FlexibleStorageWithBothSidesBlocked_IsRejected()
        {
            FixtureState state = new FixtureState();
            HashSet<CellEdge> walls =
                new HashSet<CellEdge>
                {
                    new CellEdge(
                        new GridPosition(2, 3),
                        CellEdgeDirection.SouthEast),
                    new CellEdge(
                        new GridPosition(3, 3),
                        CellEdgeDirection.SouthEast),
                    new CellEdge(
                        new GridPosition(2, 1),
                        CellEdgeDirection.NorthWest),
                    new CellEdge(
                        new GridPosition(3, 1),
                        CellEdgeDirection.NorthWest)
                };

            FixturePlacementResult result =
                CreateService(
                        state,
                        walls: walls,
                        shelfAccess: FlexibleStorageAccess,
                        shelfWidthInCells: 2,
                        shelfDepthInCells: 1)
                    .TryPlaceFixture(
                        new FixtureInstanceId("backstock-trapped-rack"),
                        ShelfDefinitionId,
                        new GridPosition(2, 2),
                        FixtureOrientation.North);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.Failure,
                Is.EqualTo(FixturePlacementFailure.BlockedAccess));
            Assert.That(state.FixtureCount, Is.EqualTo(0));
        }


        [Test]
        public void TryPlaceFixture_WallAcrossUnauthoredEnd_IsAllowed()
        {
            FixtureState state =
                new FixtureState();

            HashSet<CellEdge> walls =
                new HashSet<CellEdge>
                {
                    new CellEdge(
                        new GridPosition(2, 2),
                        CellEdgeDirection.SouthEast)
                };

            FixturePlacementResult result =
                CreateService(
                        state,
                        walls: walls,
                        shelfAccess: LongFaceAccess)
                    .TryPlaceFixture(
                        new FixtureInstanceId("shelf-end-wall"),
                        ShelfDefinitionId,
                        new GridPosition(2, 2),
                        FixtureOrientation.North);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(state.FixtureCount, Is.EqualTo(1));
        }


        [Test]
        public void TryPlaceFixture_EndOnDoorPassageCell_IsRejected()
        {
            FixtureState state =
                new FixtureState();

            GridPosition doorPassageCell =
                new GridPosition(3, 2);

            FixturePlacementResult result =
                CreateService(
                        state,
                        doorPassageCells:
                            new HashSet<GridPosition>
                            {
                                doorPassageCell
                            })
                    .TryPlaceFixture(
                        new FixtureInstanceId("shelf-door-blocker"),
                        ShelfDefinitionId,
                        new GridPosition(2, 2),
                        FixtureOrientation.East);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.Failure,
                Is.EqualTo(
                    FixturePlacementFailure.BlocksDoorPassage));
            Assert.That(
                result.FailedCell,
                Is.EqualTo(doorPassageCell));
            Assert.That(state.FixtureCount, Is.EqualTo(0));
        }


        [Test]
        public void TryPlaceFixture_BesideDoorPassageCell_IsAllowed()
        {
            FixtureState state =
                new FixtureState();

            FixturePlacementResult result =
                CreateService(
                        state,
                        doorPassageCells:
                            new HashSet<GridPosition>
                            {
                                new GridPosition(2, 3)
                            })
                    .TryPlaceFixture(
                        new FixtureInstanceId("shelf-beside-door"),
                        ShelfDefinitionId,
                        new GridPosition(2, 2),
                        FixtureOrientation.East);

            Assert.That(result.Succeeded, Is.True);
        }


        [Test]
        public void TryPlaceFixture_FaceAccessOnDoorPassageCell_IsRejected()
        {
            FixtureState state =
                new FixtureState();

            GridPosition doorPassageCell =
                new GridPosition(3, 2);

            FixturePlacementResult result =
                CreateService(
                        state,
                        shelfAccess: LongFaceAccess,
                        doorPassageCells:
                            new HashSet<GridPosition>
                            {
                                doorPassageCell
                            })
                    .TryPlaceFixture(
                        new FixtureInstanceId("shelf-facing-door"),
                        ShelfDefinitionId,
                        new GridPosition(2, 2),
                        FixtureOrientation.North);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.Failure,
                Is.EqualTo(FixturePlacementFailure.BlockedAccess));
            Assert.That(
                result.FailedCell,
                Is.EqualTo(doorPassageCell));
            Assert.That(state.FixtureCount, Is.EqualTo(0));
        }


        [Test]
        public void TryPlaceFixture_DirectlyAcrossFixtureFace_IsRejected()
        {
            FixtureState state =
                new FixtureState();

            FixturePlacementService service =
                CreateService(
                    state,
                    shelfAccess: LongFaceAccess);

            Assert.That(
                service.TryPlaceFixture(
                        new FixtureInstanceId("shelf-1"),
                        ShelfDefinitionId,
                        new GridPosition(2, 2),
                        FixtureOrientation.North)
                    .Succeeded,
                Is.True);

            FixturePlacementResult result =
                service.TryPlaceFixture(
                    new FixtureInstanceId("shelf-2"),
                    ShelfDefinitionId,
                    new GridPosition(3, 2),
                    FixtureOrientation.North);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.Failure,
                Is.EqualTo(FixturePlacementFailure.BlockedAccess));
            Assert.That(
                result.FailedCell,
                Is.EqualTo(new GridPosition(3, 2)));
            Assert.That(state.FixtureCount, Is.EqualTo(1));
        }


        [Test]
        public void TryPlaceFixture_WithoutAccess_CannotBlockExistingFace()
        {
            FixtureState state =
                new FixtureState();

            FixturePlacementService service =
                CreateService(
                    state,
                    shelfAccess: LongFaceAccess);

            Assert.That(
                service.TryPlaceFixture(
                        new FixtureInstanceId("shelf-1"),
                        ShelfDefinitionId,
                        new GridPosition(2, 2),
                        FixtureOrientation.North)
                    .Succeeded,
                Is.True);

            FixturePlacementResult result =
                service.TryPlaceFixture(
                    new FixtureInstanceId("blocker-1"),
                    BlockerDefinitionId,
                    new GridPosition(3, 2),
                    FixtureOrientation.North);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.Failure,
                Is.EqualTo(FixturePlacementFailure.BlockedAccess));
            Assert.That(
                result.FailedCell,
                Is.EqualTo(new GridPosition(3, 2)));
            Assert.That(state.FixtureCount, Is.EqualTo(1));
        }


        [Test]
        public void TryPlaceFixture_EndToEnd_IsAllowed()
        {
            FixtureState state =
                new FixtureState();

            FixturePlacementService service =
                CreateService(
                    state,
                    shelfAccess: LongFaceAccess);

            Assert.That(
                service.TryPlaceFixture(
                        new FixtureInstanceId("shelf-1"),
                        ShelfDefinitionId,
                        new GridPosition(2, 1),
                        FixtureOrientation.North)
                    .Succeeded,
                Is.True);

            Assert.That(
                service.TryPlaceFixture(
                        new FixtureInstanceId("shelf-2"),
                        ShelfDefinitionId,
                        new GridPosition(2, 3),
                        FixtureOrientation.North)
                    .Succeeded,
                Is.True);

            Assert.That(state.FixtureCount, Is.EqualTo(2));
        }


        [Test]
        public void TryPlaceFixture_FaceToFaceWithOneCellAisle_IsRejected()
        {
            FixtureState state =
                new FixtureState();

            FixturePlacementService service =
                CreateService(
                    state,
                    shelfAccess: LongFaceAccess);

            Assert.That(
                service.TryPlaceFixture(
                        new FixtureInstanceId("shelf-1"),
                        ShelfDefinitionId,
                        new GridPosition(2, 2),
                        FixtureOrientation.North)
                    .Succeeded,
                Is.True);

            FixturePlacementResult result =
                service.TryPlaceFixture(
                    new FixtureInstanceId("shelf-2"),
                    ShelfDefinitionId,
                    new GridPosition(4, 2),
                    FixtureOrientation.North);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.Failure,
                Is.EqualTo(FixturePlacementFailure.BlockedAccess));
            Assert.That(
                result.FailedCell,
                Is.EqualTo(new GridPosition(3, 2)));
            Assert.That(state.FixtureCount, Is.EqualTo(1));
        }


        [Test]
        public void TryPlaceFixture_FaceToFaceWithTwoCellAisle_IsAllowed()
        {
            FixtureState state =
                new FixtureState();

            FixturePlacementService service =
                CreateService(
                    state,
                    shelfAccess: LongFaceAccess);

            Assert.That(
                service.TryPlaceFixture(
                        new FixtureInstanceId("shelf-1"),
                        ShelfDefinitionId,
                        new GridPosition(1, 2),
                        FixtureOrientation.North)
                    .Succeeded,
                Is.True);

            Assert.That(
                service.TryPlaceFixture(
                        new FixtureInstanceId("shelf-2"),
                        ShelfDefinitionId,
                        new GridPosition(4, 2),
                        FixtureOrientation.North)
                    .Succeeded,
                Is.True);

            Assert.That(state.FixtureCount, Is.EqualTo(2));
        }


        [Test]
        public void TryRemoveFixture_ReleasesCompleteFootprintBeforeEvent()
        {
            FixtureState state =
                new FixtureState();

            FixturePlacementService service =
                CreateService(state);

            FixtureInstanceId instanceId =
                new FixtureInstanceId("shelf-1");

            Assert.That(
                service.TryPlaceFixture(
                        instanceId,
                        ShelfDefinitionId,
                        new GridPosition(2, 2),
                        FixtureOrientation.East)
                    .Succeeded,
                Is.True);

            bool eventObservedCompleteState = false;

            state.FixtureRemoved += fixture =>
            {
                eventObservedCompleteState =
                    fixture.Id == instanceId
                    && state.FixtureCount == 0
                    && state.OccupiedCellCount == 0
                    && state.ReservedAccessCellCount == 0
                    && state.ReservedAccessBoundaryCount == 0;
            };

            FixturePlacementResult result =
                service.TryRemoveFixture(instanceId);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Changed, Is.True);
            Assert.That(
                result.Edit.Kind,
                Is.EqualTo(FixtureEditKind.RemoveFixture));
            Assert.That(eventObservedCompleteState, Is.True);
            Assert.That(
                state.IsOccupied(new GridPosition(2, 2)),
                Is.False);
            Assert.That(
                state.IsOccupied(new GridPosition(3, 2)),
                Is.False);
            Assert.That(
                state.ReservedAccessCellCount,
                Is.EqualTo(0));
            Assert.That(
                state.ReservedAccessBoundaryCount,
                Is.EqualTo(0));
        }


        [Test]
        public void TryRemoveFixtureAtCell_SecondFootprintCell_RemovesCompleteFixture()
        {
            FixtureState state =
                new FixtureState();

            FixturePlacementService service =
                CreateService(
                    state,
                    shelfAccess: LongFaceAccess);

            FixtureInstanceId instanceId =
                new FixtureInstanceId("shelf-1");

            Assert.That(
                service.TryPlaceFixture(
                        instanceId,
                        ShelfDefinitionId,
                        new GridPosition(2, 2),
                        FixtureOrientation.East)
                    .Succeeded,
                Is.True);

            FixturePlacementResult result =
                service.TryRemoveFixtureAtCell(
                    new GridPosition(3, 2));

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Changed, Is.True);
            Assert.That(result.InstanceId, Is.EqualTo(instanceId));
            Assert.That(
                result.Edit.Kind,
                Is.EqualTo(FixtureEditKind.RemoveFixture));
            Assert.That(state.FixtureCount, Is.EqualTo(0));
            Assert.That(state.OccupiedCellCount, Is.EqualTo(0));
            Assert.That(state.ReservedAccessCellCount, Is.EqualTo(0));
            Assert.That(
                state.ReservedAccessBoundaryCount,
                Is.EqualTo(0));
        }


        [Test]
        public void TryRemoveFixtureAtCell_EmptyCell_IsRejectedWithoutMutation()
        {
            FixtureState state =
                new FixtureState();

            FixturePlacementResult result =
                CreateService(state)
                    .TryRemoveFixtureAtCell(
                        new GridPosition(3, 2));

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.Failure,
                Is.EqualTo(FixturePlacementFailure.FixtureNotFound));
            Assert.That(
                result.FailedCell,
                Is.EqualTo(new GridPosition(3, 2)));
            Assert.That(state.FixtureCount, Is.EqualTo(0));
        }


        private static FixturePlacementService CreateService(
            FixtureState state,
            HashSet<GridPosition> floors = null,
            HashSet<GridPosition> eligibleCells = null,
            HashSet<CellEdge> walls = null,
            FixtureAccessProfile shelfAccess = null,
            HashSet<GridPosition> doorPassageCells = null,
            int shelfWidthInCells = 1,
            int shelfDepthInCells = 2)
        {
            HashSet<GridPosition> cells =
                CreateCells();

            GridMapDefinition map =
                new GridMapDefinition(
                    "fixture-placement-test",
                    cells);

            FixtureDefinitionCatalog catalog =
                new FixtureDefinitionCatalog(
                    new[]
                    {
                        new FixtureDefinition(
                            ShelfDefinitionId,
                            "Standard Shelf",
                            shelfWidthInCells,
                            shelfDepthInCells,
                            shelfAccess),
                        new FixtureDefinition(
                            BlockerDefinitionId,
                            "Blocker",
                            1,
                            1)
                    });

            return new FixturePlacementService(
                map,
                new ConstructionAreaDefinition(
                    map,
                    eligibleCells ?? cells),
                catalog,
                state,
                new TestSurfaceQuery(
                    floors ?? cells,
                    walls,
                    doorPassageCells));
        }


        private static HashSet<GridPosition> CreateCells()
        {
            HashSet<GridPosition> cells =
                new HashSet<GridPosition>();

            for (int x = 0; x <= 5; x++)
            {
                for (int y = 0; y <= 5; y++)
                {
                    cells.Add(
                        new GridPosition(x, y));
                }
            }

            return cells;
        }


        private static HashSet<CellEdge> CreateHalfShelfBoundaryWalls(
            FixtureOrientation orientation,
            bool authoredFront)
        {
            HashSet<CellEdge> walls =
                new HashSet<CellEdge>();

            switch (orientation)
            {
                case FixtureOrientation.North:
                case FixtureOrientation.South:
                {
                    int edgeY =
                        orientation == FixtureOrientation.North
                            ? authoredFront ? 1 : 2
                            : authoredFront ? 2 : 1;

                    walls.Add(
                        new CellEdge(
                            new GridPosition(2, edgeY),
                            CellEdgeDirection.NorthWest));
                    walls.Add(
                        new CellEdge(
                            new GridPosition(3, edgeY),
                            CellEdgeDirection.NorthWest));
                    break;
                }

                case FixtureOrientation.East:
                case FixtureOrientation.West:
                {
                    int edgeX =
                        orientation == FixtureOrientation.East
                            ? authoredFront ? 2 : 1
                            : authoredFront ? 1 : 2;

                    walls.Add(
                        new CellEdge(
                            new GridPosition(edgeX, 2),
                            CellEdgeDirection.NorthEast));
                    walls.Add(
                        new CellEdge(
                            new GridPosition(edgeX, 3),
                            CellEdgeDirection.NorthEast));
                    break;
                }

                default:
                    Assert.Fail(
                        $"Unsupported fixture orientation {orientation}.");
                    break;
            }

            return walls;
        }


        private sealed class TestSurfaceQuery :
            IFixturePlacementSurfaceQuery
        {
            private readonly HashSet<GridPosition> floors;

            private readonly HashSet<CellEdge> walls;

            private readonly HashSet<GridPosition>
                doorPassageCells;


            public TestSurfaceQuery(
                IEnumerable<GridPosition> floors,
                IEnumerable<CellEdge> walls = null,
                IEnumerable<GridPosition> doorPassageCells = null)
            {
                this.floors =
                    new HashSet<GridPosition>(floors);

                this.walls =
                    walls == null
                        ? new HashSet<CellEdge>()
                        : new HashSet<CellEdge>(walls);

                this.doorPassageCells =
                    doorPassageCells == null
                        ? new HashSet<GridPosition>()
                        : new HashSet<GridPosition>(
                            doorPassageCells);
            }


            public bool HasFloor(
                GridPosition cell)
            {
                return floors.Contains(cell);
            }


            public bool HasWall(
                CellEdge edge)
            {
                return walls.Contains(edge);
            }


            public bool IsReservedForDoorPassage(
                GridPosition cell)
            {
                return doorPassageCells.Contains(cell);
            }
        }
    }
}
