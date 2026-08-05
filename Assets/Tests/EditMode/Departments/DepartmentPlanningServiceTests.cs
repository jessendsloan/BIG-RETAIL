using System;
using BigRetail.Departments;
using BigRetail.Map.Construction;
using BigRetail.Map.Domain;
using NUnit.Framework;

namespace BigRetail.Departments.Tests
{
    public sealed class DepartmentPlanningServiceTests
    {
        private static readonly GridPosition FirstCell =
            new GridPosition(2, 3, 0);

        private static readonly GridPosition SecondCell =
            new GridPosition(3, 3, 0);

        private static readonly GridPosition ThirdCell =
            new GridPosition(4, 3, 0);

        private static readonly GridPosition OutsideConstruction =
            new GridPosition(7, 3, 0);

        private static readonly DepartmentDefinitionId Grocery =
            new DepartmentDefinitionId("grocery");

        private static readonly DepartmentDefinitionId Produce =
            new DepartmentDefinitionId("produce");

        private static readonly DepartmentPlanId GroceryPlan =
            new DepartmentPlanId("grocery-01");

        private static readonly DepartmentPlanId ProducePlan =
            new DepartmentPlanId("produce-01");

        private DepartmentPlanningState state;
        private DepartmentPlanningService service;
        private MutableFoundationQuery foundations;


        [SetUp]
        public void SetUp()
        {
            GridMapDefinition map =
                new GridMapDefinition(
                    "departments.test",
                    new[]
                    {
                        FirstCell,
                        SecondCell,
                        ThirdCell,
                        OutsideConstruction
                    });

            state =
                new DepartmentPlanningState();

            foundations =
                new MutableFoundationQuery();

            foundations.Add(FirstCell);
            foundations.Add(SecondCell);
            foundations.Add(ThirdCell);

            service =
                new DepartmentPlanningService(
                    map,
                    new ConstructionAreaDefinition(
                        map,
                        new[]
                        {
                            FirstCell,
                            SecondCell,
                            ThirdCell
                        }),
                    new DepartmentDefinitionCatalog(
                    new[]
                    {
                        new DepartmentDefinition(Grocery, 4),
                        new DepartmentDefinition(Produce, 2)
                    }),
                    state,
                    foundations);
        }


        [Test]
        public void DefinitionId_NormalizesIdentity()
        {
            Assert.That(
                new DepartmentDefinitionId(" grocery "),
                Is.EqualTo(new DepartmentDefinitionId("GROCERY")));
        }


        [Test]
        public void Catalog_RejectsDuplicateNormalizedIdentity()
        {
            Assert.Throws<ArgumentException>(
                () => new DepartmentDefinitionCatalog(
                    new[]
                    {
                        new DepartmentDefinition(Grocery, 1),
                        new DepartmentDefinition(
                            new DepartmentDefinitionId(" GROCERY "),
                            1)
                    }));
        }


        [Test]
        public void CreatePlan_AssignsEveryRequestedCell()
        {
            DepartmentPlanChangeResult result =
                service.TryCreatePlan(
                    GroceryPlan,
                    Grocery,
                    new[]
                    {
                        FirstCell,
                        SecondCell
                    });

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Changed, Is.True);
            Assert.That(result.AddedCellCount, Is.EqualTo(2));
            Assert.That(state.PlanCount, Is.EqualTo(1));
            Assert.That(
                state.TryGetPlanAt(FirstCell, out DepartmentPlanId assignedPlan),
                Is.True);
            Assert.That(assignedPlan, Is.EqualTo(GroceryPlan));
        }


        [Test]
        public void CreatePlan_AllowsAnAreaBelowItsFutureMinimum()
        {
            DepartmentPlanChangeResult result =
                service.TryCreatePlan(
                    GroceryPlan,
                    Grocery,
                    new[]
                    {
                        FirstCell
                    });

            Assert.That(result.Succeeded, Is.True);
            Assert.That(state.PlanCount, Is.EqualTo(1));
        }


        [Test]
        public void CreatePlan_RejectsUnknownDefinitionWithoutMutation()
        {
            DepartmentPlanChangeResult result =
                service.TryCreatePlan(
                    GroceryPlan,
                    new DepartmentDefinitionId("unknown"),
                    new[]
                    {
                        FirstCell
                    });

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.Failure,
                Is.EqualTo(
                    DepartmentPlanChangeFailure.UnknownDefinition));
            Assert.That(state.PlanCount, Is.Zero);
        }


        [Test]
        public void CreatePlan_RejectsOutsideConstructionAreaWithoutMutation()
        {
            DepartmentPlanChangeResult result =
                service.TryCreatePlan(
                    GroceryPlan,
                    Grocery,
                    new[]
                    {
                        OutsideConstruction
                    });

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.Failure,
                Is.EqualTo(
                    DepartmentPlanChangeFailure
                        .OutsideConstructionArea));
            Assert.That(state.PlanCount, Is.Zero);
        }


        [Test]
        public void CreatePlan_RequiresFoundationWithoutMutation()
        {
            foundations.Remove(FirstCell);

            DepartmentPlanChangeResult result =
                service.TryCreatePlan(
                    GroceryPlan,
                    Grocery,
                    new[]
                    {
                        FirstCell
                    });

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.Failure,
                Is.EqualTo(
                    DepartmentPlanChangeFailure.MissingFoundation));
            Assert.That(result.FailureCell, Is.EqualTo(FirstCell));
            Assert.That(state.PlanCount, Is.Zero);
        }


        [Test]
        public void CreatePlan_RejectsOverlapWithAnotherDepartment()
        {
            service.TryCreatePlan(
                GroceryPlan,
                Grocery,
                new[]
                {
                    FirstCell
                });

            DepartmentPlanChangeResult result =
                service.TryCreatePlan(
                    ProducePlan,
                    Produce,
                    new[]
                    {
                        FirstCell,
                        SecondCell
                    });

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.Failure,
                Is.EqualTo(
                    DepartmentPlanChangeFailure
                        .OverlapsAnotherDepartment));
            Assert.That(state.PlanCount, Is.EqualTo(1));
        }


        [Test]
        public void AddArea_ExtendsTheExistingDepartmentPlan()
        {
            service.TryCreatePlan(
                GroceryPlan,
                Grocery,
                new[]
                {
                    FirstCell
                });

            DepartmentPlanChangeResult result =
                service.TryAddArea(
                    GroceryPlan,
                    new[]
                    {
                        SecondCell,
                        ThirdCell
                    });

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.AddedCellCount, Is.EqualTo(2));
            Assert.That(
                state.TryGetPlan(GroceryPlan, out DepartmentPlan plan),
                Is.True);
            Assert.That(plan.CellCount, Is.EqualTo(3));
        }


        [Test]
        public void AddArea_RejectsOverlapWithoutPartiallyAddingOtherCells()
        {
            service.TryCreatePlan(
                GroceryPlan,
                Grocery,
                new[]
                {
                    FirstCell
                });

            service.TryCreatePlan(
                ProducePlan,
                Produce,
                new[]
                {
                    SecondCell
                });

            DepartmentPlanChangeResult result =
                service.TryAddArea(
                    GroceryPlan,
                    new[]
                    {
                        ThirdCell,
                        SecondCell
                    });

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.Failure,
                Is.EqualTo(
                    DepartmentPlanChangeFailure
                        .OverlapsAnotherDepartment));
            Assert.That(
                state.TryGetPlan(GroceryPlan, out DepartmentPlan plan),
                Is.True);
            Assert.That(plan.CellCount, Is.EqualTo(1));
            Assert.That(
                state.TryGetPlanAt(ThirdCell, out _),
                Is.False);
        }


        [Test]
        public void AddArea_AlreadyOwnedCellsAreSuccessfulNoOp()
        {
            service.TryCreatePlan(
                GroceryPlan,
                Grocery,
                new[]
                {
                    FirstCell
                });

            DepartmentPlanChangeResult result =
                service.TryAddArea(
                    GroceryPlan,
                    new[]
                    {
                        FirstCell,
                        FirstCell
                    });

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Changed, Is.False);
            Assert.That(result.AddedCellCount, Is.Zero);
        }


        private sealed class MutableFoundationQuery :
            IDepartmentFoundationQuery
        {
            private readonly System.Collections.Generic.HashSet<GridPosition>
                cells =
                    new System.Collections.Generic.HashSet<GridPosition>();


            public void Add(GridPosition cell)
            {
                cells.Add(cell);
            }


            public void Remove(GridPosition cell)
            {
                cells.Remove(cell);
            }


            public bool HasFoundation(GridPosition cell)
            {
                return cells.Contains(cell);
            }
        }
    }
}
