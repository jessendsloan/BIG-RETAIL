using System;
using System.Collections.Generic;
using BigRetail.Departments;
using BigRetail.Map.Construction;
using BigRetail.Map.Domain;
using NUnit.Framework;

namespace BigRetail.Departments.Tests
{
    public sealed class DepartmentSpatialReadinessEvaluatorTests
    {
        private static readonly GridPosition FirstCell =
            new GridPosition(2, 3, 0);

        private static readonly GridPosition SecondCell =
            new GridPosition(3, 3, 0);

        private static readonly DepartmentDefinitionId Grocery =
            new DepartmentDefinitionId("grocery");

        private static readonly DepartmentPlanId GroceryPlan =
            new DepartmentPlanId("grocery-01");

        private MutableSurfaceQuery surface;
        private DepartmentSpatialReadinessEvaluator evaluator;


        [SetUp]
        public void SetUp()
        {
            GridMapDefinition map =
                new GridMapDefinition(
                    "department.readiness.test",
                    new[]
                    {
                        FirstCell,
                        SecondCell
                    });

            DepartmentPlanningState state =
                new DepartmentPlanningState();

            surface =
                new MutableSurfaceQuery();

            surface.AddFoundation(FirstCell);
            surface.AddFoundation(SecondCell);

            DepartmentDefinitionCatalog catalog =
                new DepartmentDefinitionCatalog(
                    new[]
                    {
                        new DepartmentDefinition(Grocery, 3)
                    });

            DepartmentPlanningService service =
                new DepartmentPlanningService(
                    map,
                    new ConstructionAreaDefinition(
                        map,
                        new[]
                        {
                            FirstCell,
                            SecondCell
                        }),
                    catalog,
                    state,
                    surface);

            service.TryCreatePlan(
                GroceryPlan,
                Grocery,
                new[]
                {
                    FirstCell,
                    SecondCell
                });

            evaluator =
                new DepartmentSpatialReadinessEvaluator(
                    catalog,
                    state,
                    surface);
        }


        [Test]
        public void Evaluate_ReportsMissingSurfacesIfSupportLaterChanges()
        {
            surface.RemoveFoundation(SecondCell);

            DepartmentSpatialReadiness readiness =
                evaluator.Evaluate(GroceryPlan);

            Assert.That(readiness.AssignedCellCount, Is.EqualTo(2));
            Assert.That(readiness.MissingFoundationCount, Is.EqualTo(1));
            Assert.That(readiness.MissingFloorCount, Is.EqualTo(2));
            Assert.That(readiness.MeetsMinimumArea, Is.False);
            Assert.That(readiness.IsSpatiallyReady, Is.False);
        }


        [Test]
        public void Evaluate_ReportsCompleteSurfacesButAnUndersizedPlan()
        {
            surface.AddFoundation(FirstCell);
            surface.AddFoundation(SecondCell);
            surface.AddFloor(FirstCell);
            surface.AddFloor(SecondCell);

            DepartmentSpatialReadiness readiness =
                evaluator.Evaluate(GroceryPlan);

            Assert.That(readiness.HasCompleteFoundation, Is.True);
            Assert.That(readiness.HasCompleteFloor, Is.True);
            Assert.That(readiness.MeetsMinimumArea, Is.False);
            Assert.That(readiness.IsSpatiallyReady, Is.False);
        }


        [Test]
        public void Evaluate_IsSpatiallyReadyWhenEveryCurrentRequirementIsMet()
        {
            surface.AddFoundation(FirstCell);
            surface.AddFoundation(SecondCell);
            surface.AddFloor(FirstCell);
            surface.AddFloor(SecondCell);

            DepartmentPlanningState readyState =
                new DepartmentPlanningState();

            GridMapDefinition map =
                new GridMapDefinition(
                    "department.ready.test",
                    new[]
                    {
                        FirstCell,
                        SecondCell,
                        new GridPosition(4, 3, 0)
                    });

            DepartmentDefinitionCatalog catalog =
                new DepartmentDefinitionCatalog(
                    new[]
                    {
                        new DepartmentDefinition(Grocery, 2)
                    });

            DepartmentPlanningService service =
                new DepartmentPlanningService(
                    map,
                    new ConstructionAreaDefinition(
                        map,
                        map.EnumerateValidCells()),
                    catalog,
                    readyState,
                    surface);

            service.TryCreatePlan(
                GroceryPlan,
                Grocery,
                new[]
                {
                    FirstCell,
                    SecondCell
                });

            DepartmentSpatialReadiness ready =
                new DepartmentSpatialReadinessEvaluator(
                    catalog,
                    readyState,
                    surface).Evaluate(GroceryPlan);

            Assert.That(ready.IsSpatiallyReady, Is.True);
        }


        [Test]
        public void Evaluate_RejectsUnknownPlan()
        {
            Assert.Throws<ArgumentException>(
                () => evaluator.Evaluate(
                    new DepartmentPlanId("unknown")));
        }


        private sealed class MutableSurfaceQuery :
            IDepartmentSurfaceQuery
        {
            private readonly HashSet<GridPosition> foundations =
                new HashSet<GridPosition>();

            private readonly HashSet<GridPosition> floors =
                new HashSet<GridPosition>();


            public void AddFoundation(
                GridPosition cell)
            {
                foundations.Add(cell);
            }


            public void AddFloor(
                GridPosition cell)
            {
                floors.Add(cell);
            }


            public void RemoveFoundation(
                GridPosition cell)
            {
                foundations.Remove(cell);
            }


            public bool HasFoundation(
                GridPosition cell)
            {
                return foundations.Contains(cell);
            }


            public bool HasFloor(
                GridPosition cell)
            {
                return floors.Contains(cell);
            }
        }
    }
}
