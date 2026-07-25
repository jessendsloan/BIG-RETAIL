using System.Collections.Generic;
using BigRetail.Map.Construction;
using BigRetail.Map.Domain;
using BigRetail.Map.Walls;
using NUnit.Framework;

namespace BigRetail.Map.Walls.Tests
{
    public sealed class WallBatchConstructionServiceTests
    {
        private GridMapDefinition map;
        private ConstructionAreaDefinition constructionArea;
        private WallState wallState;
        private WallConstructionService service;


        [SetUp]
        public void SetUp()
        {
            List<GridPosition> cells =
                new List<GridPosition>();

            for (int x = 0; x <= 5; x++)
            {
                for (int y = 0; y <= 5; y++)
                {
                    cells.Add(
                        new GridPosition(x, y, 0));
                }
            }

            map =
                new GridMapDefinition(
                    "test.map",
                    cells);

            constructionArea =
                new ConstructionAreaDefinition(
                    map,
                    cells);

            wallState =
                new WallState();

            service =
                new WallConstructionService(
                    map,
                    constructionArea,
                    wallState);
        }


        [Test]
        public void EvaluatePlacementBatch_ValidBatch_DoesNotMutateState()
        {
            CellEdge[] edges =
                CreateNorthEastRun(2, 1, 3);

            WallBatchChangeResult result =
                service.EvaluatePlacementBatch(edges);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.ChangedCount, Is.EqualTo(3));
            Assert.That(wallState.WallCount, Is.EqualTo(0));
        }


        [Test]
        public void TryPlaceWalls_ValidBatch_AddsEveryWall()
        {
            CellEdge[] edges =
                CreateNorthEastRun(2, 1, 3);

            WallBatchChangeResult result =
                service.TryPlaceWalls(edges);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.ChangedCount, Is.EqualTo(3));
            Assert.That(wallState.WallCount, Is.EqualTo(3));

            for (int index = 0;
                 index < edges.Length;
                 index++)
            {
                Assert.That(
                    wallState.HasWall(edges[index]),
                    Is.True);
            }
        }


        [Test]
        public void TryPlaceWalls_InvalidMiddleEdge_AddsNothing()
        {
            CellEdge validStart =
                CreateEdge(
                    2,
                    1,
                    CellEdgeDirection.NorthEast);

            CellEdge invalidMiddle =
                CreateEdge(
                    20,
                    20,
                    CellEdgeDirection.NorthEast);

            CellEdge validEnd =
                CreateEdge(
                    2,
                    3,
                    CellEdgeDirection.NorthEast);

            CellEdge[] edges =
            {
                validStart,
                invalidMiddle,
                validEnd
            };

            WallBatchChangeResult result =
                service.TryPlaceWalls(edges);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Failure,
                Is.EqualTo(WallChangeFailure.OutsideMap));
            Assert.That(result.ChangedCount, Is.EqualTo(0));
            Assert.That(wallState.WallCount, Is.EqualTo(0));
        }


        [Test]
        public void TryPlaceWalls_ExistingMiddleEdge_AddsNothingElse()
        {
            CellEdge[] edges =
                CreateNorthEastRun(2, 1, 3);

            WallChangeResult initialPlacement =
                service.TryPlaceWall(edges[1]);

            Assert.That(
                initialPlacement.Succeeded,
                Is.True);

            WallBatchChangeResult result =
                service.TryPlaceWalls(edges);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Failure,
                Is.EqualTo(WallChangeFailure.AlreadyExists));

            // Only the wall placed before the batch should exist.
            Assert.That(wallState.WallCount, Is.EqualTo(1));
            Assert.That(wallState.HasWall(edges[0]), Is.False);
            Assert.That(wallState.HasWall(edges[1]), Is.True);
            Assert.That(wallState.HasWall(edges[2]), Is.False);
        }


        [Test]
        public void TryPlaceWalls_DuplicateRequest_AddsNothing()
        {
            CellEdge edge =
                CreateEdge(
                    2,
                    1,
                    CellEdgeDirection.NorthEast);

            CellEdge[] edges =
            {
                edge,
                edge
            };

            WallBatchChangeResult result =
                service.TryPlaceWalls(edges);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Failure,
                Is.EqualTo(WallChangeFailure.DuplicateRequest));
            Assert.That(wallState.WallCount, Is.EqualTo(0));
        }


        [Test]
        public void TryPlaceWalls_PublishesEventsAfterCompleteStateMutation()
        {
            CellEdge[] edges =
                CreateNorthEastRun(2, 1, 3);

            int wallCountObservedDuringFirstEvent =
                -1;

            int eventCount =
                0;

            wallState.WallAdded +=
                edge =>
                {
                    eventCount++;

                    if (eventCount == 1)
                    {
                        wallCountObservedDuringFirstEvent =
                            wallState.WallCount;
                    }
                };

            WallBatchChangeResult result =
                service.TryPlaceWalls(edges);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(eventCount, Is.EqualTo(3));

            Assert.That(
                wallCountObservedDuringFirstEvent,
                Is.EqualTo(3));
        }


        [Test]
        public void TryPlaceWalls_EmptyRequest_IsRejected()
        {
            CellEdge[] edges =
                new CellEdge[0];

            WallBatchChangeResult result =
                service.TryPlaceWalls(edges);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Failure,
                Is.EqualTo(WallChangeFailure.EmptyRequest));
            Assert.That(wallState.WallCount, Is.EqualTo(0));
        }


        private static CellEdge[]
            CreateNorthEastRun(
                int x,
                int startingY,
                int count)
        {
            CellEdge[] edges =
                new CellEdge[count];

            for (int index = 0;
                 index < count;
                 index++)
            {
                edges[index] =
                    CreateEdge(
                        x,
                        startingY + index,
                        CellEdgeDirection.NorthEast);
            }

            return edges;
        }


        private static CellEdge CreateEdge(
            int x,
            int y,
            CellEdgeDirection direction)
        {
            return new CellEdge(
                new GridPosition(x, y, 0),
                direction);
        }
    }
}