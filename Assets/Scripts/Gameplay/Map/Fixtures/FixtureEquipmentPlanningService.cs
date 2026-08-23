using System;
using BigRetail.Map.Domain;

namespace BigRetail.Map.Fixtures
{
    public enum FixtureEquipmentPlanFailure
    {
        None = 0,
        PhysicalPlacementInvalid = 1,
        OverlapsPlan = 2,
        PlanNotFound = 3,
        StateConflict = 4
    }


    public readonly struct FixtureEquipmentPlanResult
    {
        public bool Succeeded { get; }
        public FixtureEquipmentPlanFailure Failure { get; }
        public FixturePlacementFailure PlacementFailure { get; }
        public FixtureEquipmentPlan Plan { get; }

        private FixtureEquipmentPlanResult(
            bool succeeded,
            FixtureEquipmentPlanFailure failure,
            FixturePlacementFailure placementFailure,
            FixtureEquipmentPlan plan)
        {
            Succeeded = succeeded;
            Failure = failure;
            PlacementFailure = placementFailure;
            Plan = plan;
        }

        internal static FixtureEquipmentPlanResult Success(
            FixtureEquipmentPlan plan)
        {
            return new FixtureEquipmentPlanResult(
                true,
                FixtureEquipmentPlanFailure.None,
                FixturePlacementFailure.None,
                plan);
        }

        internal static FixtureEquipmentPlanResult Rejected(
            FixtureEquipmentPlanFailure failure,
            FixturePlacementFailure placementFailure =
                FixturePlacementFailure.None)
        {
            return new FixtureEquipmentPlanResult(
                false,
                failure,
                placementFailure,
                null);
        }
    }


    /// <summary>
    /// Creates free fixture plans using the real placement rules.
    /// </summary>
    public sealed class FixtureEquipmentPlanningService
    {
        private readonly FixturePlacementService placement;
        private readonly FixtureEquipmentPlanState state;


        public FixtureEquipmentPlanningService(
            FixturePlacementService placement,
            FixtureEquipmentPlanState state)
        {
            this.placement = placement
                ?? throw new ArgumentNullException(nameof(placement));
            this.state = state
                ?? throw new ArgumentNullException(nameof(state));
        }


        public FixtureEquipmentPlanResult TryCreatePlan(
            FixtureInstanceId planId,
            FixtureDefinitionId fixtureDefinitionId,
            GridPosition anchorCell,
            FixtureOrientation orientation)
        {
            FixturePlacementResult evaluation =
                placement.EvaluatePlacement(
                    planId,
                    fixtureDefinitionId,
                    anchorCell,
                    orientation);

            if (!evaluation.Succeeded)
            {
                return FixtureEquipmentPlanResult.Rejected(
                    FixtureEquipmentPlanFailure.PhysicalPlacementInvalid,
                    evaluation.Failure);
            }

            for (int index = 0;
                 index < evaluation.Footprint.CellCount;
                 index++)
            {
                if (state.IsCellPlanned(
                        evaluation.Footprint.GetCell(index)))
                {
                    return FixtureEquipmentPlanResult.Rejected(
                        FixtureEquipmentPlanFailure.OverlapsPlan);
                }
            }

            FixtureEquipmentPlan plan =
                new FixtureEquipmentPlan(
                    planId,
                    fixtureDefinitionId,
                    evaluation.Footprint);

            return state.TryAdd(plan)
                ? FixtureEquipmentPlanResult.Success(plan)
                : FixtureEquipmentPlanResult.Rejected(
                    FixtureEquipmentPlanFailure.StateConflict);
        }

        public FixtureEquipmentPlanResult TryRemovePlan(
            FixtureInstanceId planId)
        {
            return state.TryRemove(planId, out FixtureEquipmentPlan plan)
                ? FixtureEquipmentPlanResult.Success(plan)
                : FixtureEquipmentPlanResult.Rejected(
                    FixtureEquipmentPlanFailure.PlanNotFound);
        }
    }
}
