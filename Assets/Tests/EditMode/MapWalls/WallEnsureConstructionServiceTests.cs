using System.Collections.Generic;
using BigRetail.Map.Construction;
using BigRetail.Map.Domain;
using BigRetail.Map.Walls;
using NUnit.Framework;

namespace BigRetail.Map.Walls.Tests
{
    public sealed class WallEnsureConstructionServiceTests
    {
        private GridMapDefinition map;
        private ConstructionAreaDefinition constructionArea;
        private WallState wallState;
        private WallConstructionService service;


        [SetUp]
        public void SetUp()
        {
            List<GridPosition> validCells =
                new List<GridPosition>();

            List<GridPosition> eligibleCells =
                new List<GridPosition>();

            for (int x = 0; x <= 5; x++)
            {
                for (int y = 0; y <= 5; y++)
                {
                    GridPosition cell =
                        new GridPosition(
                            x,
                            y,
                            0);

                    validCells.Add(cell);

                    if (x <= 3)
                    {
                        eligibleCells.Add(cell);
                    }
                }
            }

            map =
                new GridMapDefinition(
                    "test.map",
                    validCells);

            constructionArea =
                new ConstructionAreaDefinition(
                    map,
                    eligibleCells);

            wallState =
                new WallState();

            service =
                new WallConstructionService(
                    map,
                    constructionArea,
                    wallState,
                    UnrestrictedFoundationSupportQuery.Instance);
        }


        [Test]
        public void TryEnsureWalls_ValidEdges_AddsEveryMissingWall()
        {
            CellEdge[] edges =
                CreateNorthEastRun(
                    2,
                    1,
                    3);

            WallEnsureResult result =
                service.TryEnsureWalls(edges);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.ChangedCount, Is.EqualTo(3));
            Assert.That(result.AlreadyExistingCount, Is.EqualTo(0));
            Assert.That(result.SkippedCount, Is.EqualTo(0));
            Assert.That(wallState.WallCount, Is.EqualTo(3));
        }


        [Test]
        public void TryEnsureWalls_ExistingMiddleWall_AddsMissingWalls()
        {
            CellEdge[] edges =
                CreateNorthEastRun(
                    2,
                    1,
                    3);

            Assert.That(
                service.TryPlaceWall(edges[1]).Succeeded,
                Is.True);

            WallEnsureResult result =
                service.TryEnsureWalls(edges);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.ChangedCount, Is.EqualTo(2));
            Assert.That(result.AlreadyExistingCount, Is.EqualTo(1));
            Assert.That(result.SatisfiedCount, Is.EqualTo(3));
            Assert.That(wallState.WallCount, Is.EqualTo(3));
        }


        [Test]
        public void TryEnsureWalls_InvalidEdges_DoNotBlockValidEdges()
        {
            CellEdge validStart =
                CreateEdge(
                    2,
                    1,
                    CellEdgeDirection.NorthEast);

            CellEdge outsideConstructionArea =
                CreateEdge(
                    5,
                    2,
                    CellEdgeDirection.NorthEast);

            CellEdge outsideMap =
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
                outsideConstructionArea,
                outsideMap,
                validEnd
            };

            WallEnsureResult result =
                service.TryEnsureWalls(edges);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.ChangedCount, Is.EqualTo(2));

            Assert.That(
                result.SkippedOutsideConstructionAreaCount,
                Is.EqualTo(1));

            Assert.That(
                result.SkippedOutsideMapCount,
                Is.EqualTo(1));

            Assert.That(wallState.HasWall(validStart), Is.True);
            Assert.That(wallState.HasWall(validEnd), Is.True);
            Assert.That(wallState.WallCount, Is.EqualTo(2));
        }


        [Test]
        public void TryEnsureWalls_DuplicateEdges_AreCollapsed()
        {
            CellEdge edge =
                CreateEdge(
                    2,
                    1,
                    CellEdgeDirection.NorthEast);

            CellEdge[] edges =
            {
                edge,
                edge,
                edge
            };

            WallEnsureResult result =
                service.TryEnsureWalls(edges);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.RequestedCount, Is.EqualTo(3));
            Assert.That(result.UniqueCount, Is.EqualTo(1));
            Assert.That(result.ChangedCount, Is.EqualTo(1));
            Assert.That(wallState.WallCount, Is.EqualTo(1));
        }


        [Test]
        public void TryEnsureWalls_AllExisting_IsSuccessfulNoOp()
        {
            CellEdge[] edges =
                CreateNorthEastRun(
                    2,
                    1,
                    3);

            Assert.That(
                service.TryPlaceWalls(edges).Succeeded,
                Is.True);

            WallEnsureResult result =
                service.TryEnsureWalls(edges);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.ChangedCount, Is.EqualTo(0));
            Assert.That(result.AlreadyExistingCount, Is.EqualTo(3));
            Assert.That(result.SatisfiedCount, Is.EqualTo(3));
            Assert.That(wallState.WallCount, Is.EqualTo(3));
        }


        [Test]
        public void TryEnsureWalls_AllInvalid_IsProcessedWithoutMutation()
        {
            CellEdge[] edges =
            {
                CreateEdge(
                    5,
                    2,
                    CellEdgeDirection.NorthEast),

                CreateEdge(
                    20,
                    20,
                    CellEdgeDirection.NorthEast)
            };

            WallEnsureResult result =
                service.TryEnsureWalls(edges);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.ChangedCount, Is.EqualTo(0));
            Assert.That(result.AlreadyExistingCount, Is.EqualTo(0));
            Assert.That(result.SkippedCount, Is.EqualTo(2));
            Assert.That(wallState.WallCount, Is.EqualTo(0));
        }


        [Test]
        public void TryEnsureWalls_EmptyRequest_IsRejected()
        {
            CellEdge[] edges =
                new CellEdge[0];

            WallEnsureResult result =
                service.TryEnsureWalls(edges);

            Assert.That(result.Succeeded, Is.False);

            Assert.That(
                result.Failure,
                Is.EqualTo(
                    WallChangeFailure.EmptyRequest));

            Assert.That(wallState.WallCount, Is.EqualTo(0));
        }


        [Test]
        public void TryEnsureWalls_PublishesAfterAcceptedSubsetIsComplete()
        {
            CellEdge existing =
                CreateEdge(
                    2,
                    2,
                    CellEdgeDirection.NorthEast);

            Assert.That(
                service.TryPlaceWall(existing).Succeeded,
                Is.True);

            CellEdge firstNew =
                CreateEdge(
                    2,
                    1,
                    CellEdgeDirection.NorthEast);

            CellEdge secondNew =
                CreateEdge(
                    2,
                    3,
                    CellEdgeDirection.NorthEast);

            CellEdge invalid =
                CreateEdge(
                    5,
                    2,
                    CellEdgeDirection.NorthEast);

            CellEdge[] edges =
            {
                firstNew,
                existing,
                invalid,
                secondNew
            };

            int eventCount = 0;
            int wallCountDuringFirstEvent = -1;

            wallState.WallAdded +=
                edge =>
                {
                    eventCount++;

                    if (eventCount == 1)
                    {
                        wallCountDuringFirstEvent =
                            wallState.WallCount;
                    }
                };

            WallEnsureResult result =
                service.TryEnsureWalls(edges);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.ChangedCount, Is.EqualTo(2));
            Assert.That(eventCount, Is.EqualTo(2));

            // The pre-existing wall plus both newly accepted walls
            // already exist before the first event is published.
            Assert.That(
                wallCountDuringFirstEvent,
                Is.EqualTo(3));
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
