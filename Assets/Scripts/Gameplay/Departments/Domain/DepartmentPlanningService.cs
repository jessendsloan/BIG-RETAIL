using System;
using System.Collections.Generic;
using BigRetail.Map.Construction;
using BigRetail.Map.Domain;

namespace BigRetail.Departments
{
    /// <summary>
    /// Validates and applies player department planning. Planning may exist
    /// before its space is complete; readiness is evaluated separately.
    /// </summary>
    public sealed class DepartmentPlanningService
    {
        private readonly GridMapDefinition mapDefinition;
        private readonly ConstructionAreaDefinition constructionArea;
        private readonly DepartmentDefinitionCatalog definitionCatalog;
        private readonly DepartmentPlanningState planningState;
        private readonly IDepartmentFoundationQuery foundationQuery;


        public DepartmentPlanningService(
            GridMapDefinition mapDefinition,
            ConstructionAreaDefinition constructionArea,
            DepartmentDefinitionCatalog definitionCatalog,
            DepartmentPlanningState planningState,
            IDepartmentFoundationQuery foundationQuery)
        {
            this.mapDefinition =
                mapDefinition
                ?? throw new ArgumentNullException(
                    nameof(mapDefinition));

            this.constructionArea =
                constructionArea
                ?? throw new ArgumentNullException(
                    nameof(constructionArea));

            this.definitionCatalog =
                definitionCatalog
                ?? throw new ArgumentNullException(
                    nameof(definitionCatalog));

            this.planningState =
                planningState
                ?? throw new ArgumentNullException(
                    nameof(planningState));

            this.foundationQuery =
                foundationQuery
                ?? throw new ArgumentNullException(
                    nameof(foundationQuery));
        }


        public DepartmentPlanChangeResult TryCreatePlan(
            DepartmentPlanId planId,
            DepartmentDefinitionId definitionId,
            IReadOnlyList<GridPosition> cells)
        {
            if (cells == null)
            {
                throw new ArgumentNullException(
                    nameof(cells));
            }

            if (cells.Count == 0)
            {
                return DepartmentPlanChangeResult.Rejected(
                    planId,
                    DepartmentPlanChangeFailure.EmptyArea);
            }

            if (!definitionCatalog.TryGetDefinition(
                    definitionId,
                    out _))
            {
                return DepartmentPlanChangeResult.Rejected(
                    planId,
                    DepartmentPlanChangeFailure.UnknownDefinition);
            }

            if (planningState.TryGetPlan(planId, out _))
            {
                return DepartmentPlanChangeResult.Rejected(
                    planId,
                    DepartmentPlanChangeFailure.PlanAlreadyExists);
            }

            DepartmentPlanChangeResult validation =
                ValidateUnassignedCells(planId, cells);

            if (!validation.Succeeded)
            {
                return validation;
            }

            HashSet<GridPosition> uniqueCells =
                new HashSet<GridPosition>(cells);

            DepartmentPlan plan =
                new DepartmentPlan(
                    planId,
                    definitionId,
                    uniqueCells);

            if (!planningState.TryCreatePlan(plan))
            {
                return DepartmentPlanChangeResult.Rejected(
                    planId,
                    DepartmentPlanChangeFailure.PlanAlreadyExists);
            }

            return DepartmentPlanChangeResult.Success(
                planId,
                uniqueCells.Count);
        }


        public DepartmentPlanChangeResult TryAddArea(
            DepartmentPlanId planId,
            IReadOnlyList<GridPosition> cells)
        {
            if (cells == null)
            {
                throw new ArgumentNullException(
                    nameof(cells));
            }

            if (cells.Count == 0)
            {
                return DepartmentPlanChangeResult.Rejected(
                    planId,
                    DepartmentPlanChangeFailure.EmptyArea);
            }

            if (!planningState.TryGetPlan(planId, out DepartmentPlan plan))
            {
                return DepartmentPlanChangeResult.Rejected(
                    planId,
                    DepartmentPlanChangeFailure.PlanNotFound);
            }

            DepartmentPlanChangeResult validation =
                ValidateAssignableCells(planId, cells);

            if (!validation.Succeeded)
            {
                return validation;
            }

            HashSet<GridPosition> uniqueCells =
                new HashSet<GridPosition>(cells);

            int addedCellCount = 0;

            foreach (GridPosition cell in uniqueCells)
            {
                if (!plan.ContainsCell(cell))
                {
                    addedCellCount++;
                }
            }

            if (!planningState.TryAddCells(
                    planId,
                    new List<GridPosition>(uniqueCells)))
            {
                return DepartmentPlanChangeResult.Rejected(
                    planId,
                    DepartmentPlanChangeFailure
                        .OverlapsAnotherDepartment);
            }

            return DepartmentPlanChangeResult.Success(
                planId,
                addedCellCount);
        }


        private DepartmentPlanChangeResult ValidateUnassignedCells(
            DepartmentPlanId planId,
            IReadOnlyList<GridPosition> cells)
        {
            for (int index = 0;
                 index < cells.Count;
                 index++)
            {
                GridPosition cell =
                    cells[index];

                if (!mapDefinition.ContainsCell(cell))
                {
                    return DepartmentPlanChangeResult.Rejected(
                        planId,
                        DepartmentPlanChangeFailure.OutsideMap,
                        cell);
                }

                if (!constructionArea.IsEligible(cell))
                {
                    return DepartmentPlanChangeResult.Rejected(
                        planId,
                        DepartmentPlanChangeFailure
                            .OutsideConstructionArea,
                        cell);
                }

                if (!foundationQuery.HasFoundation(cell))
                {
                    return DepartmentPlanChangeResult.Rejected(
                        planId,
                        DepartmentPlanChangeFailure.MissingFoundation,
                        cell);
                }

                if (planningState.TryGetPlanAt(cell, out _))
                {
                    return DepartmentPlanChangeResult.Rejected(
                        planId,
                        DepartmentPlanChangeFailure
                            .OverlapsAnotherDepartment,
                        cell);
                }
            }

            return DepartmentPlanChangeResult.Success(
                planId,
                0);
        }


        private DepartmentPlanChangeResult ValidateAssignableCells(
            DepartmentPlanId planId,
            IReadOnlyList<GridPosition> cells)
        {
            for (int index = 0;
                 index < cells.Count;
                 index++)
            {
                GridPosition cell =
                    cells[index];

                if (!mapDefinition.ContainsCell(cell))
                {
                    return DepartmentPlanChangeResult.Rejected(
                        planId,
                        DepartmentPlanChangeFailure.OutsideMap,
                        cell);
                }

                if (!constructionArea.IsEligible(cell))
                {
                    return DepartmentPlanChangeResult.Rejected(
                        planId,
                        DepartmentPlanChangeFailure
                            .OutsideConstructionArea,
                        cell);
                }

                if (!foundationQuery.HasFoundation(cell))
                {
                    return DepartmentPlanChangeResult.Rejected(
                        planId,
                        DepartmentPlanChangeFailure.MissingFoundation,
                        cell);
                }

                if (planningState.TryGetPlanAt(
                        cell,
                        out DepartmentPlanId assignedPlanId)
                    && assignedPlanId != planId)
                {
                    return DepartmentPlanChangeResult.Rejected(
                        planId,
                        DepartmentPlanChangeFailure
                            .OverlapsAnotherDepartment,
                        cell);
                }
            }

            return DepartmentPlanChangeResult.Success(
                planId,
                0);
        }
    }
}
