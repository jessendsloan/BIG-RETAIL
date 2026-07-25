using System.Collections.Generic;
using BigRetail.Map.Construction;
using BigRetail.Map.Domain;
using BigRetail.Map.Walls;
using NUnit.Framework;

namespace BigRetail.Map.Walls.Tests
{
    public sealed class WallClearConstructionServiceTests
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
                        new GridPosition(
                            x,
                            y,
                            0));
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
        public void TryClearWalls_ExistingWalls_RemovesEveryWall()
        {
            CellEdge[] edges =
                CreateNorthEastRun(
                    2,
                    1,
                    3);

            Assert.That(
                service.TryPlaceWalls(edges).Succeeded,
                Is.True);

            WallClearResult result =
                service.TryClearWalls(edges);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.RemovedCount, Is.EqualTo(3));
            Assert.That(result.AlreadyEmptyCount, Is.EqualTo(0));
            Assert.That(wallState.WallCount, Is.EqualTo(0));
        }


        [Test]
        public void TryClearWalls_MixedRun_RemovesOnlyExistingWalls()
        {
            CellEdge[] edges =
                CreateNorthEastRun(
                    2,
                    1,
                    4);

            Assert.That(
                service.TryPlaceWall(edges[0]).Succeeded,
                Is.True);

            Assert.That(
                service.TryPlaceWall(edges[2]).Succeeded,
                Is.True);

            WallClearResult result =
                service.TryClearWalls(edges);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.RemovedCount, Is.EqualTo(2));
            Assert.That(result.AlreadyEmptyCount, Is.EqualTo(2));
            Assert.That(wallState.WallCount, Is.EqualTo(0));
        }


        [Test]
        public void TryClearWalls_DuplicateEdges_AreCollapsed()
        {
            CellEdge edge =
                CreateEdge(
                    2,
                    1,
                    CellEdgeDirection.NorthEast);

            Assert.That(
                service.TryPlaceWall(edge).Succeeded,
                Is.True);

            CellEdge[] edges =
            {
                edge,
                edge,
                edge
            };

            WallClearResult result =
                service.TryClearWalls(edges);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.RequestedCount, Is.EqualTo(3));
            Assert.That(result.UniqueCount, Is.EqualTo(1));
            Assert.That(result.RemovedCount, Is.EqualTo(1));
            Assert.That(wallState.WallCount, Is.EqualTo(0));
        }


        [Test]
        public void TryClearWalls_AllEmpty_IsSuccessfulNoOp()
        {
            CellEdge[] edges =
                CreateNorthEastRun(
                    2,
                    1,
                    3);

            WallClearResult result =
                service.TryClearWalls(edges);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.RemovedCount, Is.EqualTo(0));
            Assert.That(result.AlreadyEmptyCount, Is.EqualTo(3));
            Assert.That(wallState.WallCount, Is.EqualTo(0));
        }


        [Test]
        public void TryClearWalls_EmptyRequest_IsRejected()
        {
            CellEdge[] edges =
                new CellEdge[0];

            WallClearResult result =
                service.TryClearWalls(edges);

            Assert.That(result.Succeeded, Is.False);

            Assert.That(
                result.Failure,
                Is.EqualTo(
                    WallChangeFailure.EmptyRequest));

            Assert.That(wallState.WallCount, Is.EqualTo(0));
        }


        [Test]
        public void TryClearWalls_PublishesAfterCompleteStateMutation()
        {
            CellEdge[] edges =
                CreateNorthEastRun(
                    2,
                    1,
                    3);

            Assert.That(
                service.TryPlaceWalls(edges).Succeeded,
                Is.True);

            int eventCount = 0;
            int wallCountDuringFirstEvent = -1;

            wallState.WallRemoved +=
                edge =>
                {
                    eventCount++;

                    if (eventCount == 1)
                    {
                        wallCountDuringFirstEvent =
                            wallState.WallCount;
                    }
                };

            WallClearResult result =
                service.TryClearWalls(edges);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(eventCount, Is.EqualTo(3));

            // Every wall has already been removed before
            // the first presentation event is published.
            Assert.That(
                wallCountDuringFirstEvent,
                Is.EqualTo(0));
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
                new GridPosition(
                    x,
                    y,
                    0),
                direction);
        }
    }
}