using System;
using System.Collections.Generic;
using BigRetail.Map.Domain;

namespace BigRetail.Map.Fixtures
{
    /// <summary>
    /// Owns uninstalled fixture plans and their reserved plan cells.
    /// Plans do not obstruct operations until installed.
    /// </summary>
    public sealed class FixtureEquipmentPlanState
    {
        private readonly Dictionary<FixtureInstanceId, FixtureEquipmentPlan>
            plans =
                new Dictionary<FixtureInstanceId, FixtureEquipmentPlan>();
        private readonly Dictionary<GridPosition, FixtureInstanceId>
            planByCell =
                new Dictionary<GridPosition, FixtureInstanceId>();


        public int Count => plans.Count;


        public event Action PlansChanged;


        public bool TryGet(
            FixtureInstanceId planId,
            out FixtureEquipmentPlan plan)
        {
            return plans.TryGetValue(planId, out plan);
        }

        public bool IsCellPlanned(GridPosition cell)
        {
            return planByCell.ContainsKey(cell);
        }

        public bool TryGetAtCell(
            GridPosition cell,
            out FixtureEquipmentPlan plan)
        {
            if (planByCell.TryGetValue(
                    cell,
                    out FixtureInstanceId planId))
            {
                return plans.TryGetValue(planId, out plan);
            }

            plan = null;
            return false;
        }

        public int CountFor(FixtureDefinitionId fixtureDefinitionId)
        {
            int count = 0;

            foreach (FixtureEquipmentPlan plan in plans.Values)
            {
                if (plan.FixtureDefinitionId == fixtureDefinitionId)
                {
                    count++;
                }
            }

            return count;
        }

        public IEnumerable<FixtureEquipmentPlan> EnumeratePlans()
        {
            foreach (FixtureEquipmentPlan plan in plans.Values)
            {
                yield return plan;
            }
        }

        internal bool TryAdd(FixtureEquipmentPlan plan)
        {
            if (plan == null || plans.ContainsKey(plan.Id))
            {
                return false;
            }

            for (int index = 0;
                 index < plan.Footprint.CellCount;
                 index++)
            {
                if (planByCell.ContainsKey(
                        plan.Footprint.GetCell(index)))
                {
                    return false;
                }
            }

            plans.Add(plan.Id, plan);

            for (int index = 0;
                 index < plan.Footprint.CellCount;
                 index++)
            {
                planByCell.Add(
                    plan.Footprint.GetCell(index),
                    plan.Id);
            }

            PlansChanged?.Invoke();
            return true;
        }

        internal bool TryRemove(
            FixtureInstanceId planId,
            out FixtureEquipmentPlan removedPlan)
        {
            if (!plans.Remove(planId, out removedPlan))
            {
                return false;
            }

            for (int index = 0;
                 index < removedPlan.Footprint.CellCount;
                 index++)
            {
                planByCell.Remove(
                    removedPlan.Footprint.GetCell(index));
            }

            PlansChanged?.Invoke();
            return true;
        }
    }
}
