using System;
using BigRetail.Map.Domain;

namespace BigRetail.Departments
{
    /// <summary>
    /// Evaluates the built-surface prerequisites of one department plan.
    /// It does not mutate the plan or the physical map.
    /// </summary>
    public sealed class DepartmentSpatialReadinessEvaluator
    {
        private readonly DepartmentDefinitionCatalog definitionCatalog;
        private readonly DepartmentPlanningState planningState;
        private readonly IDepartmentSurfaceQuery surfaceQuery;


        public DepartmentSpatialReadinessEvaluator(
            DepartmentDefinitionCatalog definitionCatalog,
            DepartmentPlanningState planningState,
            IDepartmentSurfaceQuery surfaceQuery)
        {
            this.definitionCatalog =
                definitionCatalog
                ?? throw new ArgumentNullException(
                    nameof(definitionCatalog));

            this.planningState =
                planningState
                ?? throw new ArgumentNullException(
                    nameof(planningState));

            this.surfaceQuery =
                surfaceQuery
                ?? throw new ArgumentNullException(
                    nameof(surfaceQuery));
        }


        public DepartmentSpatialReadiness Evaluate(
            DepartmentPlanId planId)
        {
            if (!planningState.TryGetPlan(planId, out DepartmentPlan plan))
            {
                throw new ArgumentException(
                    "The requested department plan does not exist.",
                    nameof(planId));
            }

            if (!definitionCatalog.TryGetDefinition(
                    plan.DefinitionId,
                    out DepartmentDefinition definition))
            {
                throw new InvalidOperationException(
                    "The planned department references an unknown definition.");
            }

            int missingFoundationCount = 0;
            int missingFloorCount = 0;

            foreach (GridPosition cell in plan.EnumerateCells())
            {
                if (!surfaceQuery.HasFoundation(cell))
                {
                    missingFoundationCount++;
                }

                if (!surfaceQuery.HasFloor(cell))
                {
                    missingFloorCount++;
                }
            }

            return new DepartmentSpatialReadiness(
                planId,
                plan.CellCount,
                definition.MinimumCellCount,
                missingFoundationCount,
                missingFloorCount);
        }
    }
}
