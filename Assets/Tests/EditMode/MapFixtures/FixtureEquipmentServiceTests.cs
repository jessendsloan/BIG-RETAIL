using System.Collections.Generic;
using BigRetail.Economy.Domain;
using BigRetail.Map.Construction;
using BigRetail.Map.Domain;
using NUnit.Framework;

namespace BigRetail.Map.Fixtures.Tests
{
    public sealed class FixtureEquipmentServiceTests
    {
        private static readonly FixtureDefinitionId ShelfId =
            new FixtureDefinitionId("standard-shelf");
        private static readonly GridPosition FirstCell =
            new GridPosition(1, 1);
        private static readonly GridPosition SecondCell =
            new GridPosition(2, 1);


        [Test]
        public void Order_Arrival_Installation_AndStorage_TransferOwnership()
        {
            TestContext context = CreateContext(openingCashCents: 100000);
            FixtureEquipmentOrderResult orderResult =
                context.Orders.TryPlaceOrders(
                    new Dictionary<FixtureDefinitionId, int>
                    {
                        { ShelfId, 2 }
                    },
                    currentGameSeconds: 100);

            Assert.That(orderResult.Succeeded, Is.True);
            Assert.That(context.Cash.BalanceCents, Is.EqualTo(50000));
            Assert.That(context.Inventory.GetQuantity(ShelfId), Is.Zero);

            context.Orders.AdvanceTo(100 + 7200);
            FixtureEquipmentOrder order = orderResult.Orders[0];
            Assert.That(order.SupplierId, Is.EqualTo("BIG"));
            Assert.That(
                order.SupplierDisplayName,
                Is.EqualTo("BIG Wholesale"));
            Assert.That(
                order.Status,
                Is.EqualTo(FixtureEquipmentOrderStatus.ReadyToReceive));

            Assert.That(
                context.Orders.Receive(order.OrderNumber).Succeeded,
                Is.True);
            Assert.That(context.Inventory.GetQuantity(ShelfId), Is.EqualTo(2));

            FixtureEquipmentInstallationResult installed =
                context.Installation.TryInstallOwnedFixture(
                    new FixtureInstanceId("shelf-one"),
                    ShelfId,
                    FirstCell,
                    FixtureOrientation.North);
            Assert.That(installed.Succeeded, Is.True);
            Assert.That(context.Inventory.GetQuantity(ShelfId), Is.EqualTo(1));
            Assert.That(context.State.FixtureCount, Is.EqualTo(1));

            FixtureEquipmentInstallationResult stored =
                context.Installation.TryStoreFixtureAtCell(FirstCell);
            Assert.That(stored.Succeeded, Is.True);
            Assert.That(context.Inventory.GetQuantity(ShelfId), Is.EqualTo(2));
            Assert.That(context.State.FixtureCount, Is.Zero);
        }

        [Test]
        public void Planning_IsFree_AndOrderNeedSubtractsOwnedAndOutstanding()
        {
            TestContext context = CreateContext(openingCashCents: 100000);

            Assert.That(
                context.Planning.TryCreatePlan(
                    new FixtureInstanceId("plan-one"),
                    ShelfId,
                    FirstCell,
                    FixtureOrientation.North).Succeeded,
                Is.True);
            Assert.That(
                context.Planning.TryCreatePlan(
                    new FixtureInstanceId("plan-two"),
                    ShelfId,
                    SecondCell,
                    FixtureOrientation.North).Succeeded,
                Is.True);
            Assert.That(context.Cash.BalanceCents, Is.EqualTo(100000));
            Assert.That(context.Plans.CountFor(ShelfId), Is.EqualTo(2));

            context.Inventory.Add(ShelfId);
            FixtureEquipmentOrderResult result =
                context.Orders.TryPlaceOrders(
                    new Dictionary<FixtureDefinitionId, int>
                    {
                        { ShelfId, 1 }
                    },
                    0);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(context.Orders.GetOutstandingQuantity(ShelfId), Is.EqualTo(1));
            Assert.That(
                context.Plans.CountFor(ShelfId)
                    - context.Inventory.GetQuantity(ShelfId)
                    - context.Orders.GetOutstandingQuantity(ShelfId),
                Is.Zero);
        }

        [Test]
        public void DirectInstallation_ExactMatchingPlanIsRetired()
        {
            TestContext context = CreateContext(openingCashCents: 100000);

            Assert.That(
                context.Planning.TryCreatePlan(
                    new FixtureInstanceId("planned-shelf"),
                    ShelfId,
                    FirstCell,
                    FixtureOrientation.North).Succeeded,
                Is.True);
            context.Inventory.Add(ShelfId);
            int planChangeCount = 0;
            context.Plans.PlansChanged += () => planChangeCount++;

            FixtureEquipmentInstallationResult installed =
                context.Installation.TryInstallOwnedFixture(
                    new FixtureInstanceId("manually-placed-shelf"),
                    ShelfId,
                    FirstCell,
                    FixtureOrientation.North);

            Assert.That(installed.Succeeded, Is.True);
            Assert.That(context.State.FixtureCount, Is.EqualTo(1));
            Assert.That(context.Plans.Count, Is.Zero);
            Assert.That(context.Plans.IsCellPlanned(FirstCell), Is.False);
            Assert.That(planChangeCount, Is.EqualTo(1));
        }

        [Test]
        public void DirectInstallation_ElsewherePreservesPlan()
        {
            TestContext context = CreateContext(openingCashCents: 100000);

            Assert.That(
                context.Planning.TryCreatePlan(
                    new FixtureInstanceId("planned-shelf"),
                    ShelfId,
                    FirstCell,
                    FixtureOrientation.North).Succeeded,
                Is.True);
            context.Inventory.Add(ShelfId);

            FixtureEquipmentInstallationResult installed =
                context.Installation.TryInstallOwnedFixture(
                    new FixtureInstanceId("elsewhere-shelf"),
                    ShelfId,
                    SecondCell,
                    FixtureOrientation.North);

            Assert.That(installed.Succeeded, Is.True);
            Assert.That(context.Plans.Count, Is.EqualTo(1));
            Assert.That(context.Plans.IsCellPlanned(FirstCell), Is.True);
        }

        [Test]
        public void PlannedInstallation_RetiresPlanAndPublishesChange()
        {
            TestContext context = CreateContext(openingCashCents: 100000);
            FixtureInstanceId planId =
                new FixtureInstanceId("planned-shelf");

            Assert.That(
                context.Planning.TryCreatePlan(
                    planId,
                    ShelfId,
                    FirstCell,
                    FixtureOrientation.North).Succeeded,
                Is.True);
            context.Inventory.Add(ShelfId);
            int planChangeCount = 0;
            context.Plans.PlansChanged += () => planChangeCount++;

            FixtureEquipmentInstallationResult installed =
                context.Installation.TryInstallPlan(planId);

            Assert.That(installed.Succeeded, Is.True);
            Assert.That(context.Plans.Count, Is.Zero);
            Assert.That(planChangeCount, Is.EqualTo(1));
        }

        [Test]
        public void EquipmentAwareHistory_ReplaysInventoryWithFixture()
        {
            TestContext context = CreateContext(openingCashCents: 100000);
            context.Inventory.Add(ShelfId);
            FixtureEquipmentInstallationResult installed =
                context.Installation.TryInstallOwnedFixture(
                    new FixtureInstanceId("shelf-history"),
                    ShelfId,
                    FirstCell,
                    FixtureOrientation.North);
            ConstructionHistory history = new ConstructionHistory();
            history.Record(
                new ReversibleFixtureEquipmentEditAction(
                    context.Installation,
                    installed.Edit));

            Assert.That(history.TryUndo(out _), Is.True);
            Assert.That(context.State.FixtureCount, Is.Zero);
            Assert.That(context.Inventory.GetQuantity(ShelfId), Is.EqualTo(1));

            Assert.That(history.TryRedo(out _), Is.True);
            Assert.That(context.State.FixtureCount, Is.EqualTo(1));
            Assert.That(context.Inventory.GetQuantity(ShelfId), Is.Zero);
        }

        private static TestContext CreateContext(long openingCashCents)
        {
            HashSet<GridPosition> cells =
                new HashSet<GridPosition>
                {
                    FirstCell,
                    SecondCell,
                    new GridPosition(3, 1)
                };
            GridMapDefinition map =
                new GridMapDefinition("equipment-test", cells);
            FixtureDefinitionCatalog fixtures =
                new FixtureDefinitionCatalog(
                    new[]
                    {
                        new FixtureDefinition(
                            ShelfId,
                            "Standard Shelf",
                            1,
                            1)
                    });
            FixtureState state = new FixtureState();
            FixturePlacementService placement =
                new FixturePlacementService(
                    map,
                    new ConstructionAreaDefinition(map, cells),
                    fixtures,
                    state,
                    new Surface(cells));
            FixtureEquipmentCatalog equipment =
                new FixtureEquipmentCatalog(
                    fixtures,
                    new[]
                    {
                        new FixtureEquipmentDefinition(
                            ShelfId,
                            "Standard Shelf",
                            25000,
                            7200)
                    });
            FixtureEquipmentInventory inventory =
                new FixtureEquipmentInventory(equipment);
            StoreCashState cash = new StoreCashState(openingCashCents);
            FixtureEquipmentOrderService orders =
                new FixtureEquipmentOrderService(
                    equipment,
                    inventory,
                    cash);
            FixtureEquipmentPlanState plans =
                new FixtureEquipmentPlanState();
            return new TestContext(
                state,
                cash,
                inventory,
                orders,
                plans,
                new FixtureEquipmentPlanningService(placement, plans),
                new FixtureEquipmentInstallationService(
                    placement,
                    inventory,
                    plans));
        }


        private sealed class Surface : IFixturePlacementSurfaceQuery
        {
            private readonly HashSet<GridPosition> floors;

            public Surface(IEnumerable<GridPosition> floors)
            {
                this.floors = new HashSet<GridPosition>(floors);
            }

            public bool HasFloor(GridPosition cell) => floors.Contains(cell);
            public bool HasWall(CellEdge edge) => false;
            public bool IsReservedForDoorPassage(GridPosition cell) => false;
        }


        private sealed class TestContext
        {
            public FixtureState State { get; }
            public StoreCashState Cash { get; }
            public FixtureEquipmentInventory Inventory { get; }
            public FixtureEquipmentOrderService Orders { get; }
            public FixtureEquipmentPlanState Plans { get; }
            public FixtureEquipmentPlanningService Planning { get; }
            public FixtureEquipmentInstallationService Installation { get; }

            public TestContext(
                FixtureState state,
                StoreCashState cash,
                FixtureEquipmentInventory inventory,
                FixtureEquipmentOrderService orders,
                FixtureEquipmentPlanState plans,
                FixtureEquipmentPlanningService planning,
                FixtureEquipmentInstallationService installation)
            {
                State = state;
                Cash = cash;
                Inventory = inventory;
                Orders = orders;
                Plans = plans;
                Planning = planning;
                Installation = installation;
            }
        }
    }
}
