using System.Collections.Generic;
using BigRetail.Map.Construction;
using BigRetail.Map.Domain;
using BigRetail.Map.Walls;
using NUnit.Framework;

namespace BigRetail.Map.Walls.Tests
{
    public sealed class DoorConstructionServiceTests
    {
        private static readonly DoorDefinitionId SingleDoorId =
            new DoorDefinitionId("single-door");

        private static readonly DoorDefinitionId AutomaticFrontDoorId =
            new DoorDefinitionId("automatic-front-door");


        [Test]
        public void EvaluatePlacement_ValidRun_DoesNotMutateOrAllocateAssembly()
        {
            CellEdge[] run =
                CreateRun(
                    CellEdgeDirection.NorthEast,
                    4);

            DoorAssemblyState state =
                new DoorAssemblyState();

            DoorAssemblyChangeResult result =
                CreateService(state, run)
                    .EvaluatePlacement(
                        new DoorAssemblyId("front-entrance-1"),
                        AutomaticFrontDoorId,
                        run);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Changed, Is.False);
            Assert.That(result.Assembly, Is.Null);
            Assert.That(result.SegmentCount, Is.EqualTo(4));
            Assert.That(state.AssemblyCount, Is.EqualTo(0));
            Assert.That(state.OccupiedEdgeCount, Is.EqualTo(0));
        }


        [Test]
        public void TryPlaceAssembly_FourPanelRun_OccupiesEveryEdge()
        {
            CellEdge[] run =
                CreateRun(
                    CellEdgeDirection.NorthEast,
                    4);

            DoorAssemblyState state =
                new DoorAssemblyState();

            DoorConstructionService service =
                CreateService(
                    state,
                    run);

            DoorAssemblyId assemblyId =
                new DoorAssemblyId("front-entrance-1");

            bool eventObservedCompleteState = false;

            state.AssemblyAdded += reportedAssembly =>
            {
                eventObservedCompleteState =
                    reportedAssembly.Id == assemblyId
                    && state.AssemblyCount == 1
                    && state.OccupiedEdgeCount == 4;
            };

            DoorAssemblyChangeResult result =
                service.TryPlaceAssembly(
                    assemblyId,
                    AutomaticFrontDoorId,
                    run);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Changed, Is.True);
            Assert.That(result.SegmentCount, Is.EqualTo(4));
            Assert.That(state.AssemblyCount, Is.EqualTo(1));
            Assert.That(state.OccupiedEdgeCount, Is.EqualTo(4));
            Assert.That(eventObservedCompleteState, Is.True);

            for (int index = 0;
                 index < run.Length;
                 index++)
            {
                Assert.That(
                    state.TryGetAssemblyAtEdge(
                        run[index],
                        out DoorAssembly assignedAssembly),
                    Is.True);

                Assert.That(
                    assignedAssembly,
                    Is.SameAs(result.Assembly));
            }

            Assert.That(
                result.Assembly.Definition.IsPassageSegment(0),
                Is.False);

            Assert.That(
                result.Assembly.Definition.IsPassageSegment(1),
                Is.True);

            Assert.That(
                result.Assembly.Definition.IsPassageSegment(2),
                Is.True);

            Assert.That(
                result.Assembly.Definition.IsPassageSegment(3),
                Is.False);

            Assert.That(
                result.Assembly.IsPassageEdge(run[0]),
                Is.False);

            Assert.That(
                result.Assembly.IsPassageEdge(run[1]),
                Is.True);

            Assert.That(
                result.Assembly.IsPassageEdge(run[2]),
                Is.True);

            Assert.That(
                result.Assembly.IsPassageEdge(run[3]),
                Is.False);
        }


        [Test]
        public void TryPlaceAssembly_SinglePanelRun_Succeeds()
        {
            CellEdge[] run =
                CreateRun(
                    CellEdgeDirection.NorthWest,
                    1);

            DoorAssemblyState state =
                new DoorAssemblyState();

            DoorAssemblyChangeResult result =
                CreateService(state, run)
                    .TryPlaceAssembly(
                        new DoorAssemblyId("staff-door-1"),
                        SingleDoorId,
                        run);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.SegmentCount, Is.EqualTo(1));
            Assert.That(
                result.Assembly.Definition.IsPassageSegment(0),
                Is.True);
        }


        [Test]
        public void TryPlaceAssembly_MissingWall_RejectsAtomically()
        {
            CellEdge[] run =
                CreateRun(
                    CellEdgeDirection.NorthEast,
                    4);

            DoorAssemblyState state =
                new DoorAssemblyState();

            List<CellEdge> incompleteWalls =
                new List<CellEdge>
                {
                    run[0],
                    run[1],
                    run[3]
                };

            DoorAssemblyChangeResult result =
                CreateService(state, incompleteWalls)
                    .TryPlaceAssembly(
                        new DoorAssemblyId("front-entrance-1"),
                        AutomaticFrontDoorId,
                        run);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.Failure,
                Is.EqualTo(DoorAssemblyChangeFailure.MissingWall));
            Assert.That(result.FailedEdge, Is.EqualTo(run[2]));
            Assert.That(state.AssemblyCount, Is.EqualTo(0));
            Assert.That(state.OccupiedEdgeCount, Is.EqualTo(0));
        }


        [Test]
        public void TryPlaceAssembly_OverlappingRun_RejectsAtomically()
        {
            CellEdge[] walls =
                CreateRun(
                    CellEdgeDirection.NorthWest,
                    5);

            DoorAssemblyState state =
                new DoorAssemblyState();

            DoorConstructionService service =
                CreateService(
                    state,
                    walls);

            CellEdge[] firstRun =
                new[]
                {
                    walls[0],
                    walls[1],
                    walls[2],
                    walls[3]
                };

            CellEdge[] overlappingRun =
                new[]
                {
                    walls[1],
                    walls[2],
                    walls[3],
                    walls[4]
                };

            Assert.That(
                service.TryPlaceAssembly(
                        new DoorAssemblyId("front-entrance-1"),
                        AutomaticFrontDoorId,
                        firstRun)
                    .Succeeded,
                Is.True);

            DoorAssemblyChangeResult result =
                service.TryPlaceAssembly(
                    new DoorAssemblyId("front-entrance-2"),
                    AutomaticFrontDoorId,
                    overlappingRun);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.Failure,
                Is.EqualTo(DoorAssemblyChangeFailure.OverlapsAssembly));
            Assert.That(result.FailedEdge, Is.EqualTo(walls[1]));
            Assert.That(state.AssemblyCount, Is.EqualTo(1));
            Assert.That(state.OccupiedEdgeCount, Is.EqualTo(4));
        }


        [Test]
        public void TryPlaceAssembly_DisconnectedSpan_IsRejected()
        {
            CellEdge[] walls =
                CreateRun(
                    CellEdgeDirection.NorthEast,
                    5);

            CellEdge[] disconnected =
                new[]
                {
                    walls[0],
                    walls[1],
                    walls[3],
                    walls[4]
                };

            DoorAssemblyState state =
                new DoorAssemblyState();

            DoorAssemblyChangeResult result =
                CreateService(state, walls)
                    .TryPlaceAssembly(
                        new DoorAssemblyId("front-entrance-1"),
                        AutomaticFrontDoorId,
                        disconnected);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.Failure,
                Is.EqualTo(DoorAssemblyChangeFailure.InvalidSpan));
            Assert.That(state.AssemblyCount, Is.EqualTo(0));
        }


        [Test]
        public void TryPlaceAssembly_IncorrectDefinitionWidth_IsRejected()
        {
            CellEdge[] run =
                CreateRun(
                    CellEdgeDirection.NorthEast,
                    3);

            DoorAssemblyState state =
                new DoorAssemblyState();

            DoorAssemblyChangeResult result =
                CreateService(state, run)
                    .TryPlaceAssembly(
                        new DoorAssemblyId("front-entrance-1"),
                        AutomaticFrontDoorId,
                        run);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.Failure,
                Is.EqualTo(
                    DoorAssemblyChangeFailure.IncorrectSegmentCount));
            Assert.That(state.AssemblyCount, Is.EqualTo(0));
        }


        [Test]
        public void TryRemoveAssembly_ReleasesCompleteSpanBeforeEvent()
        {
            CellEdge[] run =
                CreateRun(
                    CellEdgeDirection.NorthEast,
                    4);

            DoorAssemblyState state =
                new DoorAssemblyState();

            DoorConstructionService service =
                CreateService(
                    state,
                    run);

            DoorAssemblyId assemblyId =
                new DoorAssemblyId("front-entrance-1");

            Assert.That(
                service.TryPlaceAssembly(
                        assemblyId,
                        AutomaticFrontDoorId,
                        run)
                    .Succeeded,
                Is.True);

            bool eventObservedCompleteState = false;

            state.AssemblyRemoved += reportedAssembly =>
            {
                eventObservedCompleteState =
                    reportedAssembly.Id == assemblyId
                    && state.AssemblyCount == 0
                    && state.OccupiedEdgeCount == 0;
            };

            DoorAssemblyChangeResult result =
                service.TryRemoveAssembly(assemblyId);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Changed, Is.True);
            Assert.That(result.SegmentCount, Is.EqualTo(4));
            Assert.That(state.AssemblyCount, Is.EqualTo(0));
            Assert.That(state.OccupiedEdgeCount, Is.EqualTo(0));
            Assert.That(eventObservedCompleteState, Is.True);

            for (int index = 0;
                 index < run.Length;
                 index++)
            {
                Assert.That(
                    state.TryGetAssemblyAtEdge(
                        run[index],
                        out _),
                    Is.False);
            }
        }


        [Test]
        public void TryPlaceAssembly_ReversedInputSpan_UsesStablePanelOrder()
        {
            CellEdge[] run =
                CreateRun(
                    CellEdgeDirection.NorthEast,
                    4);

            CellEdge[] reversed =
                new[]
                {
                    run[3],
                    run[2],
                    run[1],
                    run[0]
                };

            DoorAssemblyState state =
                new DoorAssemblyState();

            DoorAssemblyChangeResult result =
                CreateService(state, run)
                    .TryPlaceAssembly(
                        new DoorAssemblyId("front-entrance-1"),
                        AutomaticFrontDoorId,
                        reversed);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Assembly.GetEdge(0), Is.EqualTo(run[0]));
            Assert.That(result.Assembly.GetEdge(3), Is.EqualTo(run[3]));
        }


        [Test]
        public void SupportingWallRemoval_RemovesCompleteAssembly()
        {
            CellEdge[] run =
                CreateRun(
                    CellEdgeDirection.NorthEast,
                    4);

            WallState wallState =
                new WallState(run);

            DoorAssemblyState doorState =
                new DoorAssemblyState();

            DoorConstructionService doorService =
                CreateService(
                    doorState,
                    wallState);

            Assert.That(
                doorService.TryPlaceAssembly(
                        new DoorAssemblyId("front-entrance-1"),
                        AutomaticFrontDoorId,
                        run)
                    .Succeeded,
                Is.True);

            WallConstructionService wallService =
                CreateWallConstructionService(
                    wallState);

            Assert.That(
                wallService.TryRemoveWall(run[1]).Succeeded,
                Is.True);

            Assert.That(doorState.AssemblyCount, Is.EqualTo(0));
            Assert.That(doorState.OccupiedEdgeCount, Is.EqualTo(0));

            doorService.Dispose();
        }


        [Test]
        public void ReversibleDoorAction_UndoAndRedo_PreserveExactAssembly()
        {
            CellEdge[] run =
                CreateRun(
                    CellEdgeDirection.NorthEast,
                    4);

            DoorAssemblyState state =
                new DoorAssemblyState();

            DoorConstructionService service =
                CreateService(state, run);

            DoorAssemblyChangeResult placement =
                service.TryPlaceAssembly(
                    new DoorAssemblyId("front-entrance-1"),
                    AutomaticFrontDoorId,
                    run);

            ConstructionHistory history =
                new ConstructionHistory();

            history.Record(
                new ReversibleDoorAssemblyEditAction(
                    service,
                    placement.Assembly));

            Assert.That(history.TryUndo(out _), Is.True);
            Assert.That(state.AssemblyCount, Is.EqualTo(0));

            Assert.That(history.TryRedo(out _), Is.True);
            Assert.That(state.AssemblyCount, Is.EqualTo(1));
            Assert.That(state.OccupiedEdgeCount, Is.EqualTo(4));
        }


        private static DoorConstructionService CreateService(
            DoorAssemblyState state,
            IEnumerable<CellEdge> walls)
        {
            return CreateService(
                state,
                new WallState(walls));
        }


        private static DoorConstructionService CreateService(
            DoorAssemblyState state,
            WallState wallState)
        {
            DoorDefinitionCatalog catalog =
                new DoorDefinitionCatalog(
                    new[]
                    {
                        new DoorDefinition(
                            SingleDoorId,
                            1,
                            new[] { 0 }),
                        new DoorDefinition(
                            AutomaticFrontDoorId,
                            4,
                            new[] { 1, 2 })
                    });

            return new DoorConstructionService(
                catalog,
                state,
                wallState);
        }


        private static WallConstructionService
            CreateWallConstructionService(
                WallState wallState)
        {
            List<GridPosition> cells =
                new List<GridPosition>();

            for (int x = 0; x <= 8; x++)
            {
                for (int y = 0; y <= 8; y++)
                {
                    cells.Add(
                        new GridPosition(x, y));
                }
            }

            GridMapDefinition map =
                new GridMapDefinition(
                    "door.support-removal.test",
                    cells);

            return new WallConstructionService(
                map,
                new ConstructionAreaDefinition(
                    map,
                    cells),
                wallState,
                UnrestrictedFoundationSupportQuery.Instance);
        }


        private static CellEdge[] CreateRun(
            CellEdgeDirection direction,
            int count)
        {
            CellEdge[] edges =
                new CellEdge[count];

            for (int index = 0;
                 index < count;
                 index++)
            {
                GridPosition anchor =
                    direction == CellEdgeDirection.NorthEast
                        ? new GridPosition(2, 2 + index)
                        : new GridPosition(2 + index, 2);

                edges[index] =
                    new CellEdge(
                        anchor,
                        direction);
            }

            return edges;
        }
    }
}
