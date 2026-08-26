using System;
using System.Collections.Generic;
using BigRetail.Map.Domain;

namespace BigRetail.Departments
{
    /// <summary>
    /// Owns department-plan identity and cell assignments. Map legality and
    /// configuration validation belong to DepartmentPlanningService.
    /// </summary>
    public sealed class DepartmentPlanningState
    {
        private readonly Dictionary<DepartmentPlanId,
            DepartmentPlan> plans =
                new Dictionary<DepartmentPlanId,
                    DepartmentPlan>();

        private readonly Dictionary<GridPosition,
            DepartmentPlanId> cellAssignments =
                new Dictionary<GridPosition,
                    DepartmentPlanId>();

        private bool isPublishingChanges;


        public int PlanCount =>
            plans.Count;


        public event Action<DepartmentPlanId> PlanChanged;


        public bool TryGetPlan(
            DepartmentPlanId planId,
            out DepartmentPlan plan)
        {
            return plans.TryGetValue(
                planId,
                out plan);
        }


        public bool TryGetPlanAt(
            GridPosition cell,
            out DepartmentPlanId planId)
        {
            return cellAssignments.TryGetValue(
                cell,
                out planId);
        }


        public IEnumerable<DepartmentPlan> EnumeratePlans()
        {
            foreach (DepartmentPlan plan in plans.Values)
            {
                yield return plan;
            }
        }


        internal bool TryCreatePlan(
            DepartmentPlan plan)
        {
            if (isPublishingChanges
                || plan == null
                || plans.ContainsKey(plan.Id))
            {
                return false;
            }

            foreach (GridPosition cell in plan.EnumerateCells())
            {
                if (cellAssignments.ContainsKey(cell))
                {
                    return false;
                }
            }

            plans.Add(plan.Id, plan);

            foreach (GridPosition cell in plan.EnumerateCells())
            {
                cellAssignments.Add(cell, plan.Id);
            }

            PublishPlanChanged(plan.Id);
            return true;
        }


        internal bool TryAddCells(
            DepartmentPlanId planId,
            IReadOnlyList<GridPosition> cells)
        {
            if (isPublishingChanges
                || cells == null
                || !plans.TryGetValue(planId, out DepartmentPlan plan))
            {
                return false;
            }

            for (int index = 0;
                 index < cells.Count;
                 index++)
            {
                if (cellAssignments.TryGetValue(
                        cells[index],
                        out DepartmentPlanId assignedPlanId)
                    && assignedPlanId != planId)
                {
                    return false;
                }
            }

            int addedCount =
                plan.AddCells(cells);

            if (addedCount == 0)
            {
                return true;
            }

            for (int index = 0;
                 index < cells.Count;
                 index++)
            {
                cellAssignments[cells[index]] = planId;
            }

            PublishPlanChanged(planId);
            return true;
        }


        internal bool TryRemovePlan(
            DepartmentPlanId planId,
            out DepartmentPlan removedPlan)
        {
            if (isPublishingChanges
                || !plans.TryGetValue(
                    planId,
                    out removedPlan))
            {
                removedPlan = null;
                return false;
            }

            foreach (GridPosition cell in
                     removedPlan.EnumerateCells())
            {
                cellAssignments.Remove(cell);
            }

            plans.Remove(planId);
            PublishPlanChanged(planId);
            return true;
        }


        private void PublishPlanChanged(
            DepartmentPlanId planId)
        {
            isPublishingChanges = true;

            try
            {
                PlanChanged?.Invoke(planId);
            }
            finally
            {
                isPublishingChanges = false;
            }
        }
    }
}
