using System.Collections.Generic;
using BigRetail.Map.Construction;
using BigRetail.Map.Domain;
using NUnit.Framework;

namespace BigRetail.Map.Walls.Tests
{
    /// <summary>
    /// Verifies wall placement, removal, validation,
    /// normalization, and state notifications.
    /// </summary>
    public sealed class WallConstructionServiceTests
    {
        private GridMapDefinition mapDefinition;
        private ConstructionAreaDefinition constructionArea;
        private WallState wallState;
        private WallConstructionService wallService;

        [SetUp]
        public void SetUp()
        {
            GridPosition[] validCells =
            {
                new GridPosition(0, 0),
                new GridPosition(1, 0),
                new GridPosition(0, 1),
                new GridPosition(1, 1),

                // This cell belongs to the map but is deliberately
                // excluded from the construction area.
                new GridPosition(2, 0)
            };

            GridPosition[] constructionEligibleCells =
            {
                new GridPosition(0, 0),
                new GridPosition(1, 0),
                new GridPosition(0, 1),
                new GridPosition(1, 1)
            };

            mapDefinition =
                new GridMapDefinition(
                    "test.map.wall_construction",
                    validCells);

            constructionArea =
                new ConstructionAreaDefinition(
                    mapDefinition,
                    constructionEligibleCells);

            wallState =
                new WallState();

            wallService =
                new WallConstructionService(
                    mapDefinition,
                    constructionArea,
                    wallState);
        }

        [Test]
        public void TryPlaceWall_ValidEdge_AddsWall()
        {
            CellEdge edge =
                new CellEdge(
                    new GridPosition(0, 0),
                    CellEdgeDirection.NorthEast);

            WallChangeResult result =
                wallService.TryPlaceWall(edge);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Failure, Is.EqualTo(WallChangeFailure.None));
            Assert.That(result.Edge, Is.EqualTo(edge));
            Assert.That(wallState.HasWall(edge), Is.True);
            Assert.That(wallState.WallCount, Is.EqualTo(1));
        }

        [Test]
        public void EvaluatePlacement_ValidEdge_DoesNotModifyState()
        {
            CellEdge edge =
                new CellEdge(
                    new GridPosition(0, 0),
                    CellEdgeDirection.NorthEast);

            WallChangeResult result =
                wallService.EvaluatePlacement(edge);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(wallState.HasWall(edge), Is.False);
            Assert.That(wallState.WallCount, Is.EqualTo(0));
        }

        [Test]
        public void TryPlaceWall_DuplicateEdge_IsRejected()
        {
            CellEdge edge =
                new CellEdge(
                    new GridPosition(0, 0),
                    CellEdgeDirection.NorthEast);

            WallChangeResult firstResult =
                wallService.TryPlaceWall(edge);

            WallChangeResult secondResult =
                wallService.TryPlaceWall(edge);

            Assert.That(firstResult.Succeeded, Is.True);

            Assert.That(secondResult.Succeeded, Is.False);
            Assert.That(
                secondResult.Failure,
                Is.EqualTo(WallChangeFailure.AlreadyExists));

            Assert.That(wallState.WallCount, Is.EqualTo(1));
        }

        [Test]
        public void TryPlaceWall_OppositeDescriptionOfSameEdge_IsRejected()
        {
            CellEdge fromFirstCell =
                new CellEdge(
                    new GridPosition(0, 0),
                    CellEdgeDirection.NorthEast);

            CellEdge fromNeighboringCell =
                new CellEdge(
                    new GridPosition(1, 0),
                    CellEdgeDirection.SouthWest);

            WallChangeResult firstResult =
                wallService.TryPlaceWall(fromFirstCell);

            WallChangeResult secondResult =
                wallService.TryPlaceWall(fromNeighboringCell);

            Assert.That(firstResult.Succeeded, Is.True);

            Assert.That(secondResult.Succeeded, Is.False);
            Assert.That(
                secondResult.Failure,
                Is.EqualTo(WallChangeFailure.AlreadyExists));

            Assert.That(wallState.WallCount, Is.EqualTo(1));
        }

        [Test]
        public void TryPlaceWall_CompletelyOutsideMap_IsRejected()
        {
            CellEdge edge =
                new CellEdge(
                    new GridPosition(20, 20),
                    CellEdgeDirection.NorthEast);

            WallChangeResult result =
                wallService.TryPlaceWall(edge);

            Assert.That(result.Succeeded, Is.False);

            Assert.That(
                result.Failure,
                Is.EqualTo(WallChangeFailure.OutsideMap));

            Assert.That(wallState.WallCount, Is.EqualTo(0));
        }

        [Test]
        public void TryPlaceWall_InsideMapButOutsideConstructionArea_IsRejected()
        {
            CellEdge edge =
                new CellEdge(
                    new GridPosition(2, 0),
                    CellEdgeDirection.NorthEast);

            WallChangeResult result =
                wallService.TryPlaceWall(edge);

            Assert.That(result.Succeeded, Is.False);

            Assert.That(
                result.Failure,
                Is.EqualTo(
                    WallChangeFailure.OutsideConstructionArea));

            Assert.That(wallState.WallCount, Is.EqualTo(0));
        }

        [Test]
        public void TryPlaceWall_OnPropertyPerimeter_IsAllowed()
        {
            GridPosition propertyCell =
                new GridPosition(0, 0);

            CellEdge perimeterEdge =
                new CellEdge(
                    propertyCell,
                    CellEdgeDirection.SouthWest);

            Assert.That(
                mapDefinition.ContainsCell(perimeterEdge.FirstCell)
                && mapDefinition.ContainsCell(perimeterEdge.SecondCell),
                Is.False,
                "The test edge must have one side outside the map.");

            WallChangeResult result =
                wallService.TryPlaceWall(perimeterEdge);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(wallState.HasWall(perimeterEdge), Is.True);
        }

        [Test]
        public void TryRemoveWall_ExistingWall_RemovesWall()
        {
            CellEdge edge =
                new CellEdge(
                    new GridPosition(0, 0),
                    CellEdgeDirection.NorthEast);

            wallService.TryPlaceWall(edge);

            WallChangeResult result =
                wallService.TryRemoveWall(edge);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Failure, Is.EqualTo(WallChangeFailure.None));
            Assert.That(wallState.HasWall(edge), Is.False);
            Assert.That(wallState.WallCount, Is.EqualTo(0));
        }

        [Test]
        public void TryRemoveWall_MissingWall_IsRejected()
        {
            CellEdge edge =
                new CellEdge(
                    new GridPosition(0, 0),
                    CellEdgeDirection.NorthEast);

            WallChangeResult result =
                wallService.TryRemoveWall(edge);

            Assert.That(result.Succeeded, Is.False);

            Assert.That(
                result.Failure,
                Is.EqualTo(WallChangeFailure.NotFound));

            Assert.That(wallState.WallCount, Is.EqualTo(0));
        }

        [Test]
        public void SuccessfulPlacement_RaisesWallAddedOnce()
        {
            CellEdge edge =
                new CellEdge(
                    new GridPosition(0, 0),
                    CellEdgeDirection.NorthEast);

            int eventCount = 0;
            CellEdge reportedEdge = default;

            wallState.WallAdded += addedEdge =>
            {
                eventCount++;
                reportedEdge = addedEdge;
            };

            wallService.TryPlaceWall(edge);
            wallService.TryPlaceWall(edge);

            Assert.That(eventCount, Is.EqualTo(1));
            Assert.That(reportedEdge, Is.EqualTo(edge));
        }

        [Test]
        public void SuccessfulRemoval_RaisesWallRemovedOnce()
        {
            CellEdge edge =
                new CellEdge(
                    new GridPosition(0, 0),
                    CellEdgeDirection.NorthEast);

            wallService.TryPlaceWall(edge);

            int eventCount = 0;
            CellEdge reportedEdge = default;

            wallState.WallRemoved += removedEdge =>
            {
                eventCount++;
                reportedEdge = removedEdge;
            };

            wallService.TryRemoveWall(edge);
            wallService.TryRemoveWall(edge);

            Assert.That(eventCount, Is.EqualTo(1));
            Assert.That(reportedEdge, Is.EqualTo(edge));
        }

        [Test]
        public void InitialWalls_AreRestoredWithoutRaisingEvents()
        {
            CellEdge existingWall =
                new CellEdge(
                    new GridPosition(0, 0),
                    CellEdgeDirection.NorthEast);

            WallState restoredState =
                new WallState(
                    new List<CellEdge>
                    {
                        existingWall
                    });

            Assert.That(restoredState.HasWall(existingWall), Is.True);
            Assert.That(restoredState.WallCount, Is.EqualTo(1));
        }
    }
}