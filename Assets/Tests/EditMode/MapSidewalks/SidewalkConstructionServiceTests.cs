using System.Collections.Generic;
using BigRetail.Map.Construction;
using BigRetail.Map.Domain;
using BigRetail.Map.Foundations;
using NUnit.Framework;

namespace BigRetail.Map.Sidewalks.Tests
{
    public sealed class SidewalkConstructionServiceTests
    {
        private GridMapDefinition map;

        private ConstructionAreaDefinition area;

        private FoundationState foundationState;

        private FoundationConstructionService foundationService;

        private SidewalkState sidewalkState;

        private SidewalkConstructionService sidewalkService;


        [SetUp]
        public void SetUp()
        {
            List<GridPosition> cells = new List<GridPosition>();

            for (int x = 0; x < 4; x++)
            {
                for (int y = 0; y < 4; y++)
                {
                    cells.Add(Cell(x, y));
                }
            }

            map = new GridMapDefinition("sidewalk.test", cells);
            area = new ConstructionAreaDefinition(map, cells);
            foundationState = new FoundationState();
            sidewalkState = new SidewalkState();

            sidewalkService =
                new SidewalkConstructionService(
                    map,
                    area,
                    sidewalkState,
                    new FoundationStateQuery(foundationState));

            foundationService =
                new FoundationConstructionService(
                    map,
                    area,
                    foundationState,
                    new AllowRemovalValidator(),
                    sidewalkService);
        }


        [Test]
        public void SidewalkPlacement_DoesNotRequireFoundation()
        {
            GridPosition cell = Cell(1, 1);

            SidewalkEnsureResult result =
                sidewalkService.TryEnsureSidewalks(new[] { cell });

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.ChangedCount, Is.EqualTo(1));
            Assert.That(sidewalkState.HasSidewalk(cell), Is.True);
            Assert.That(foundationState.HasFoundation(cell), Is.False);
        }


        [Test]
        public void CampaignOwnership_RestrictsSidewalkPlacement()
        {
            List<GridPosition> propertyCells =
                new List<GridPosition>(
                    LandRegionCatalog.PropertyCellCount);

            for (int x = 0;
                 x < LandRegionCatalog.PropertySideLength;
                 x++)
            {
                for (int y = 0;
                     y < LandRegionCatalog.PropertySideLength;
                     y++)
                {
                    propertyCells.Add(Cell(x, y));
                }
            }

            GridMapDefinition propertyMap =
                new GridMapDefinition(
                    "sidewalk.campaign.test",
                    propertyCells);

            ConstructionAreaDefinition propertyArea =
                new ConstructionAreaDefinition(
                    propertyMap,
                    propertyCells);

            LandRegionCatalog regions =
                LandRegionCatalog.CreateFor(propertyArea);

            LandRegionOwnershipState ownership =
                new LandRegionOwnershipState(regions);

            ownership.Own(
                LandRegionCatalog.FrontCornerRegionId);

            SidewalkConstructionService campaignService =
                new SidewalkConstructionService(
                    propertyMap,
                    new LandRegionConstructionEligibility(
                        propertyArea,
                        regions,
                        ownership),
                    new SidewalkState(),
                    new FoundationStateQuery(
                        new FoundationState()));

            Assert.That(
                campaignService.EvaluatePlacement(Cell(0, 0)).Succeeded,
                Is.True);

            SidewalkChangeResult unownedResult =
                campaignService.EvaluatePlacement(Cell(32, 0));

            Assert.That(unownedResult.Succeeded, Is.False);
            Assert.That(
                unownedResult.Failure,
                Is.EqualTo(
                    SidewalkChangeFailure
                        .OutsideConstructionArea));
        }


        [Test]
        public void SidewalkPlacement_OnFoundation_IsSkipped()
        {
            GridPosition cell = Cell(1, 1);

            Assert.That(
                foundationService.TryEnsureFoundations(
                    new[] { cell }).Succeeded,
                Is.True);

            SidewalkEnsureResult result =
                sidewalkService.TryEnsureSidewalks(new[] { cell });

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.ChangedCount, Is.EqualTo(0));
            Assert.That(result.SkippedFoundationCount, Is.EqualTo(1));
            Assert.That(sidewalkState.HasSidewalk(cell), Is.False);
        }


        [Test]
        public void FoundationPlacement_OnSidewalk_IsSkipped()
        {
            GridPosition cell = Cell(1, 1);

            Assert.That(
                sidewalkService.TryEnsureSidewalks(
                    new[] { cell }).Succeeded,
                Is.True);

            FoundationEnsureResult result =
                foundationService.TryEnsureFoundations(new[] { cell });

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.ChangedCount, Is.EqualTo(0));
            Assert.That(result.SkippedSidewalkCount, Is.EqualTo(1));
            Assert.That(foundationState.HasFoundation(cell), Is.False);
        }


        [Test]
        public void EvaluateFoundationPlacement_OnSidewalk_IsRejected()
        {
            GridPosition cell = Cell(1, 1);

            sidewalkService.TryEnsureSidewalks(new[] { cell });

            FoundationChangeResult result =
                foundationService.EvaluatePlacement(cell);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.Failure,
                Is.EqualTo(FoundationChangeFailure.SidewalkOccupied));
        }


        [Test]
        public void SidewalkWalkability_TracksConstructionAndRemoval()
        {
            GridPosition cell = Cell(2, 2);

            Assert.That(
                sidewalkService.IsSidewalkWalkable(cell),
                Is.False);

            SidewalkEnsureResult placement =
                sidewalkService.TryEnsureSidewalks(new[] { cell });

            Assert.That(placement.Succeeded, Is.True);
            Assert.That(
                sidewalkService.IsSidewalkWalkable(cell),
                Is.True);

            SidewalkClearResult removal =
                sidewalkService.TryClearSidewalks(new[] { cell });

            Assert.That(removal.Succeeded, Is.True);
            Assert.That(
                sidewalkService.IsSidewalkWalkable(cell),
                Is.False);
        }


        [Test]
        public void ReversibleAction_UndoAndRedo_PreserveExactCells()
        {
            GridPosition first = Cell(1, 1);
            GridPosition second = Cell(2, 1);

            SidewalkEnsureResult placement =
                sidewalkService.TryEnsureSidewalks(
                    new[] { first, second });

            ReversibleSidewalkEditAction action =
                new ReversibleSidewalkEditAction(
                    sidewalkService,
                    placement.Edit);

            Assert.That(action.TryUndo().Succeeded, Is.True);
            Assert.That(sidewalkState.SidewalkCount, Is.EqualTo(0));
            Assert.That(action.TryRedo().Succeeded, Is.True);
            Assert.That(sidewalkState.SidewalkCount, Is.EqualTo(2));
        }


        private static GridPosition Cell(int x, int y)
        {
            return new GridPosition(x, y, 0);
        }


        private sealed class FoundationStateQuery :
            IFoundationSupportQuery
        {
            private readonly FoundationState state;


            public FoundationStateQuery(FoundationState state)
            {
                this.state = state;
            }


            public bool HasFoundation(GridPosition cell)
            {
                return state.HasFoundation(cell);
            }
        }


        private sealed class AllowRemovalValidator :
            IFoundationRemovalValidator
        {
            public FoundationRemovalValidation ValidateRemoval(
                IReadOnlyList<GridPosition> cells)
            {
                return FoundationRemovalValidation.Allowed();
            }
        }
    }
}
