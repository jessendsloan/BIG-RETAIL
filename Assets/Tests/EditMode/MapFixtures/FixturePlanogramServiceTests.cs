using System.Collections.Generic;
using BigRetail.Map.Construction;
using BigRetail.Map.Domain;
using BigRetail.Map.Fixtures;
using BigRetail.Merchandise.Domain;
using NUnit.Framework;

namespace BigRetail.Map.Fixtures.Tests
{
    public sealed class FixturePlanogramServiceTests
    {
        private static readonly FixtureDefinitionId ShelfDefinitionId =
            new FixtureDefinitionId("STANDARD-SHELF");

        private static readonly FixtureInstanceId ShelfInstanceId =
            new FixtureInstanceId("SHELF-ONE");

        private static readonly ProductId CerealProductId =
            new ProductId("CEREAL");

        private static readonly ProductId SoupProductId =
            new ProductId("SOUP");


        [Test]
        public void Definition_CustomerBrowseSides_CreateStableShelfFaces()
        {
            FixtureAccessProfile doubleSidedAccess =
                new FixtureAccessProfile(
                    FixtureAccessMode.CustomerBrowse,
                    FixtureAccessMode.None,
                    FixtureAccessMode.CustomerBrowse,
                    FixtureAccessMode.None);

            FixtureDefinition definition =
                new FixtureDefinition(
                    ShelfDefinitionId,
                    "Standard Shelf",
                    2,
                    1,
                    doubleSidedAccess);

            Assert.That(
                definition.MerchandisingProfile.DisplayFaceCount,
                Is.EqualTo(2));

            AssertDisplayFace(
                definition,
                FixtureSide.North);

            AssertDisplayFace(
                definition,
                FixtureSide.South);

            Assert.That(
                definition.MerchandisingProfile.TryGetDisplayFace(
                    FixtureSide.East,
                    out _),
                Is.False);
        }

        [Test]
        public void AssignAdjacentUnits_SameProduct_BecomesOneFacing()
        {
            FixturePlanogramService service =
                CreatePlacedShelfService(
                    out _,
                    out _);

            FixtureShelfRunKey shelfRun =
                CreateShelfRun(
                    FixtureSide.South,
                    shelfRunIndex: 1);

            Assert.That(
                service.TryAssignFrontage(
                    shelfRun,
                    startFrontageUnit: 0,
                    frontageUnitCount: 2,
                    CerealProductId,
                    out FixturePlanogramFailure failure),
                Is.True,
                failure.ToString());

            Assert.That(
                service.TryAssignFrontage(
                    shelfRun,
                    startFrontageUnit: 2,
                    frontageUnitCount: 1,
                    CerealProductId,
                    out failure),
                Is.True,
                failure.ToString());

            IReadOnlyList<ProductFacing> facings =
                service.State.GetFacings(shelfRun);

            Assert.That(facings.Count, Is.EqualTo(1));
            Assert.That(facings[0].ProductId, Is.EqualTo(CerealProductId));
            Assert.That(facings[0].StartFrontageUnit, Is.EqualTo(0));
            Assert.That(facings[0].FrontageUnitCount, Is.EqualTo(3));

            service.Dispose();
        }

        [Test]
        public void Facing_CanBeReplacedResizedAndCleared()
        {
            FixturePlanogramService service =
                CreatePlacedShelfService(
                    out _,
                    out _);

            FixtureShelfRunKey shelfRun =
                CreateShelfRun(
                    FixtureSide.South,
                    shelfRunIndex: 0);

            Assert.That(
                service.TryAssignFrontage(
                    shelfRun,
                    1,
                    2,
                    CerealProductId,
                    out FixturePlanogramFailure failure),
                Is.True,
                failure.ToString());

            Assert.That(
                service.TryReplaceFacingProduct(
                    shelfRun,
                    frontageUnitIndex: 1,
                    SoupProductId,
                    out failure),
                Is.True,
                failure.ToString());

            Assert.That(
                service.TryResizeFacing(
                    shelfRun,
                    frontageUnitIndex: 2,
                    newFrontageUnitCount: 3,
                    out failure),
                Is.True,
                failure.ToString());

            Assert.That(
                service.State.TryGetFacingAt(
                    shelfRun,
                    frontageUnitIndex: 3,
                    out ProductFacing resizedFacing),
                Is.True);

            Assert.That(resizedFacing.ProductId, Is.EqualTo(SoupProductId));
            Assert.That(resizedFacing.StartFrontageUnit, Is.EqualTo(1));
            Assert.That(resizedFacing.FrontageUnitCount, Is.EqualTo(3));

            Assert.That(
                service.TryClearFacing(
                    shelfRun,
                    frontageUnitIndex: 2,
                    out failure),
                Is.True,
                failure.ToString());

            Assert.That(service.State.GetFacings(shelfRun), Is.Empty);
            Assert.That(service.State.AssignedShelfRunCount, Is.EqualTo(0));

            service.Dispose();
        }

        [Test]
        public void ResizeFacing_DoesNotConsumeNeighborProduct()
        {
            FixturePlanogramService service =
                CreatePlacedShelfService(
                    out _,
                    out _);

            FixtureShelfRunKey shelfRun =
                CreateShelfRun(
                    FixtureSide.South,
                    shelfRunIndex: 2);

            Assert.That(
                service.TryAssignFrontage(
                    shelfRun,
                    0,
                    1,
                    CerealProductId,
                    out _),
                Is.True);

            Assert.That(
                service.TryAssignFrontage(
                    shelfRun,
                    2,
                    1,
                    SoupProductId,
                    out _),
                Is.True);

            Assert.That(
                service.TryResizeFacing(
                    shelfRun,
                    frontageUnitIndex: 0,
                    newFrontageUnitCount: 3,
                    out FixturePlanogramFailure failure),
                Is.False);

            Assert.That(
                failure,
                Is.EqualTo(FixturePlanogramFailure.FrontageOccupied));

            Assert.That(
                service.State.TryGetProductAt(
                    shelfRun,
                    2,
                    out ProductId productAtNeighbor),
                Is.True);

            Assert.That(productAtNeighbor, Is.EqualTo(SoupProductId));

            service.Dispose();
        }

        [Test]
        public void AssignFrontage_DoesNotOverwriteNeighborProduct()
        {
            FixturePlanogramService service =
                CreatePlacedShelfService(
                    out _,
                    out _);

            FixtureShelfRunKey shelfRun =
                CreateShelfRun(
                    FixtureSide.South,
                    shelfRunIndex: 2);

            Assert.That(
                service.TryAssignFrontage(
                    shelfRun,
                    2,
                    1,
                    SoupProductId,
                    out _),
                Is.True);

            Assert.That(
                service.TryAssignFrontage(
                    shelfRun,
                    1,
                    2,
                    CerealProductId,
                    out FixturePlanogramFailure failure),
                Is.False);

            Assert.That(
                failure,
                Is.EqualTo(FixturePlanogramFailure.FrontageOccupied));

            Assert.That(
                service.State.TryGetProductAt(
                    shelfRun,
                    2,
                    out ProductId neighborProduct),
                Is.True);

            Assert.That(neighborProduct, Is.EqualTo(SoupProductId));
            service.Dispose();
        }

        [Test]
        public void MaximumFrontageUnitCount_CapsAtShelfEndAndNeighborProduct()
        {
            FixturePlanogramService service =
                CreatePlacedShelfService(
                    out _,
                    out _);

            FixtureShelfRunKey shelfRun =
                CreateShelfRun(
                    FixtureSide.South,
                    shelfRunIndex: 2);

            Assert.That(
                service.GetMaximumFrontageUnitCount(
                    shelfRun,
                    startFrontageUnit: 2),
                Is.EqualTo(3));

            Assert.That(
                service.TryAssignFrontage(
                    shelfRun,
                    3,
                    1,
                    SoupProductId,
                    out _),
                Is.True);

            Assert.That(
                service.GetMaximumFrontageUnitCount(
                    shelfRun,
                    startFrontageUnit: 1),
                Is.EqualTo(2));

            service.Dispose();
        }

        [Test]
        public void MaximumFrontageUnitCount_AllowsExistingFacingButStopsAtNeighbor()
        {
            FixturePlanogramService service =
                CreatePlacedShelfService(
                    out _,
                    out _);

            FixtureShelfRunKey shelfRun =
                CreateShelfRun(
                    FixtureSide.South,
                    shelfRunIndex: 2);

            Assert.That(
                service.TryAssignFrontage(
                    shelfRun,
                    0,
                    2,
                    CerealProductId,
                    out _),
                Is.True);

            Assert.That(
                service.TryAssignFrontage(
                    shelfRun,
                    3,
                    1,
                    SoupProductId,
                    out _),
                Is.True);

            Assert.That(
                service.GetMaximumFrontageUnitCount(
                    shelfRun,
                    startFrontageUnit: 0,
                    CerealProductId),
                Is.EqualTo(3));

            service.Dispose();
        }

        [Test]
        public void RemovingFixture_ClearsItsPlanogramAssignments()
        {
            FixturePlanogramService service =
                CreatePlacedShelfService(
                    out FixturePlacementService placementService,
                    out _);

            FixtureShelfRunKey shelfRun =
                CreateShelfRun(
                    FixtureSide.South,
                    shelfRunIndex: 0);

            Assert.That(
                service.TryAssignFrontage(
                    shelfRun,
                    0,
                    4,
                    CerealProductId,
                    out _),
                Is.True);

            Assert.That(service.State.AssignedShelfRunCount, Is.EqualTo(1));

            FixturePlacementResult removal =
                placementService.TryRemoveFixture(ShelfInstanceId);

            Assert.That(removal.Succeeded, Is.True);
            Assert.That(service.State.AssignedShelfRunCount, Is.EqualTo(0));

            service.Dispose();
        }

        [Test]
        public void SingleAssignedFixture_IsAvailableForObjectiveHighlighting()
        {
            FixturePlanogramService service =
                CreatePlacedShelfService(
                    out _,
                    out _);

            Assert.That(
                service.State.TryGetSingleAssignedFixture(out _),
                Is.False);

            Assert.That(
                service.TryAssignFrontage(
                    CreateShelfRun(
                        FixtureSide.South,
                        shelfRunIndex: 0),
                    0,
                    4,
                    CerealProductId,
                    out FixturePlanogramFailure failure),
                Is.True,
                failure.ToString());

            Assert.That(
                service.State.TryGetSingleAssignedFixture(
                    out FixtureInstanceId fixtureId),
                Is.True);
            Assert.That(fixtureId, Is.EqualTo(ShelfInstanceId));

            service.Dispose();
        }


        private static void AssertDisplayFace(
            FixtureDefinition definition,
            FixtureSide side)
        {
            Assert.That(
                definition.MerchandisingProfile.TryGetDisplayFace(
                    side,
                    out FixtureDisplayFaceDefinition displayFace),
                Is.True);

            Assert.That(
                displayFace.ShelfRunCount,
                Is.EqualTo(3));

            Assert.That(
                displayFace.FrontageUnitsPerRun,
                Is.EqualTo(5));
        }

        private static FixtureShelfRunKey CreateShelfRun(
            FixtureSide side,
            int shelfRunIndex)
        {
            return new FixtureShelfRunKey(
                ShelfInstanceId,
                side,
                shelfRunIndex);
        }

        private static FixturePlanogramService CreatePlacedShelfService(
            out FixturePlacementService placementService,
            out FixtureState fixtureState)
        {
            HashSet<GridPosition> cells =
                new HashSet<GridPosition>();

            for (int x = 0; x < 5; x++)
            {
                for (int y = 0; y < 5; y++)
                {
                    cells.Add(new GridPosition(x, y));
                }
            }

            GridMapDefinition map =
                new GridMapDefinition(
                    "fixture-planogram-test",
                    cells);

            FixtureAccessProfile access =
                new FixtureAccessProfile(
                    FixtureAccessMode.CustomerBrowse,
                    FixtureAccessMode.None,
                    FixtureAccessMode.CustomerBrowse,
                    FixtureAccessMode.None);

            FixtureDefinition definition =
                new FixtureDefinition(
                    ShelfDefinitionId,
                    "Standard Shelf",
                    2,
                    1,
                    access);

            fixtureState = new FixtureState();

            placementService =
                new FixturePlacementService(
                    map,
                    new ConstructionAreaDefinition(map, cells),
                    new FixtureDefinitionCatalog(
                        new[] { definition }),
                    fixtureState,
                    new TestSurfaceQuery(cells));

            FixturePlacementResult placement =
                placementService.TryPlaceFixture(
                    ShelfInstanceId,
                    ShelfDefinitionId,
                    new GridPosition(1, 1),
                    FixtureOrientation.North);

            Assert.That(
                placement.Succeeded,
                Is.True,
                placement.Failure.ToString());

            ProductCatalog productCatalog =
                new ProductCatalog(
                    new[]
                    {
                        CreateProduct(CerealProductId, "Cereal"),
                        CreateProduct(SoupProductId, "Soup")
                    });

            return new FixturePlanogramService(
                fixtureState,
                productCatalog);
        }

        private static ProductDefinition CreateProduct(
            ProductId productId,
            string displayName)
        {
            return new ProductDefinition(
                productId,
                displayName,
                new ProductCategoryId("GROCERY"),
                StockUnit.Each);
        }


        private sealed class TestSurfaceQuery :
            IFixturePlacementSurfaceQuery
        {
            private readonly HashSet<GridPosition> floorCells;


            public TestSurfaceQuery(
                IEnumerable<GridPosition> floorCells)
            {
                this.floorCells =
                    new HashSet<GridPosition>(floorCells);
            }


            public bool HasFloor(GridPosition cell)
            {
                return floorCells.Contains(cell);
            }

            public bool HasWall(CellEdge edge)
            {
                return false;
            }

            public bool IsReservedForDoorPassage(GridPosition cell)
            {
                return false;
            }
        }
    }
}
