using System;
using System.Collections.Generic;
using BigRetail.Map.Domain;

namespace BigRetail.Map.Fixtures
{
    public enum FixtureEquipmentInstallationFailure
    {
        None = 0,
        NoOwnedEquipment = 1,
        PlanNotFound = 2,
        PlacementRejected = 3,
        StateConflict = 4
    }


    public readonly struct FixtureEquipmentInstallationResult
    {
        public bool Succeeded { get; }
        public FixtureEquipmentInstallationFailure Failure { get; }
        public FixturePlacementResult Placement { get; }

        public FixtureEdit Edit => Placement.Edit;

        private FixtureEquipmentInstallationResult(
            bool succeeded,
            FixtureEquipmentInstallationFailure failure,
            FixturePlacementResult placement)
        {
            Succeeded = succeeded;
            Failure = failure;
            Placement = placement;
        }

        internal static FixtureEquipmentInstallationResult Success(
            FixturePlacementResult placement)
        {
            return new FixtureEquipmentInstallationResult(
                true,
                FixtureEquipmentInstallationFailure.None,
                placement);
        }

        internal static FixtureEquipmentInstallationResult Rejected(
            FixtureEquipmentInstallationFailure failure,
            FixturePlacementResult placement = default)
        {
            return new FixtureEquipmentInstallationResult(
                false,
                failure,
                placement);
        }
    }


    public readonly struct FixtureEquipmentBatchInstallationResult
    {
        public int InstalledCount { get; }
        public int WaitingForEquipmentCount { get; }
        public int BlockedCount { get; }
        public IReadOnlyList<FixtureEdit> Edits { get; }

        public FixtureEquipmentBatchInstallationResult(
            int installedCount,
            int waitingForEquipmentCount,
            int blockedCount,
            IReadOnlyList<FixtureEdit> edits)
        {
            InstalledCount = installedCount;
            WaitingForEquipmentCount = waitingForEquipmentCount;
            BlockedCount = blockedCount;
            Edits = edits ?? Array.Empty<FixtureEdit>();
        }
    }


    /// <summary>
    /// Converts owned modules into placed fixtures and returns removed
    /// fixtures to equipment storage.
    /// </summary>
    public sealed class FixtureEquipmentInstallationService
    {
        private readonly FixturePlacementService placement;
        private readonly FixtureEquipmentInventory inventory;
        private readonly FixtureEquipmentPlanState plans;


        public FixtureEquipmentInstallationService(
            FixturePlacementService placement,
            FixtureEquipmentInventory inventory,
            FixtureEquipmentPlanState plans)
        {
            this.placement = placement
                ?? throw new ArgumentNullException(nameof(placement));
            this.inventory = inventory
                ?? throw new ArgumentNullException(nameof(inventory));
            this.plans = plans
                ?? throw new ArgumentNullException(nameof(plans));
        }


        public FixtureEquipmentInstallationResult TryInstallOwnedFixture(
            FixtureInstanceId instanceId,
            FixtureDefinitionId fixtureDefinitionId,
            GridPosition anchorCell,
            FixtureOrientation orientation)
        {
            FixturePlacementResult evaluation =
                placement.EvaluatePlacement(
                    instanceId,
                    fixtureDefinitionId,
                    anchorCell,
                    orientation);

            if (!evaluation.Succeeded)
            {
                return FixtureEquipmentInstallationResult.Rejected(
                    FixtureEquipmentInstallationFailure.PlacementRejected,
                    evaluation);
            }

            if (!inventory.TryConsume(fixtureDefinitionId))
            {
                return FixtureEquipmentInstallationResult.Rejected(
                    FixtureEquipmentInstallationFailure.NoOwnedEquipment,
                    evaluation);
            }

            FixturePlacementResult result =
                placement.TryPlaceFixture(
                    instanceId,
                    fixtureDefinitionId,
                    anchorCell,
                    orientation);

            if (result.Succeeded)
            {
                RetireMatchingPlan(result.Fixture);
                return FixtureEquipmentInstallationResult.Success(result);
            }

            inventory.Add(fixtureDefinitionId);
            return FixtureEquipmentInstallationResult.Rejected(
                FixtureEquipmentInstallationFailure.StateConflict,
                result);
        }

        public FixtureEquipmentInstallationResult TryStoreFixtureAtCell(
            GridPosition cell)
        {
            FixturePlacementResult result =
                placement.TryRemoveFixtureAtCell(cell);

            if (!result.Succeeded)
            {
                return FixtureEquipmentInstallationResult.Rejected(
                    FixtureEquipmentInstallationFailure.PlacementRejected,
                    result);
            }

            inventory.Add(result.DefinitionId);
            return FixtureEquipmentInstallationResult.Success(result);
        }

        public FixtureEquipmentInstallationResult TryInstallPlan(
            FixtureInstanceId planId)
        {
            if (!plans.TryGet(planId, out FixtureEquipmentPlan plan))
            {
                return FixtureEquipmentInstallationResult.Rejected(
                    FixtureEquipmentInstallationFailure.PlanNotFound);
            }

            FixtureEquipmentInstallationResult result =
                TryInstallOwnedFixture(
                    plan.Id,
                    plan.FixtureDefinitionId,
                    plan.AnchorCell,
                    plan.Orientation);

            return result;
        }

        public FixtureEquipmentBatchInstallationResult
            TryInstallReadyPlans()
        {
            List<FixtureEquipmentPlan> snapshot =
                new List<FixtureEquipmentPlan>();

            foreach (FixtureEquipmentPlan plan in plans.EnumeratePlans())
            {
                snapshot.Add(plan);
            }

            snapshot.Sort(
                (left, right) => string.CompareOrdinal(
                    left.Id.Value,
                    right.Id.Value));

            List<FixtureEdit> edits = new List<FixtureEdit>();
            int waiting = 0;
            int blocked = 0;

            for (int index = 0; index < snapshot.Count; index++)
            {
                FixtureEquipmentInstallationResult result =
                    TryInstallPlan(snapshot[index].Id);

                if (result.Succeeded)
                {
                    edits.Add(result.Edit);
                }
                else if (result.Failure
                    == FixtureEquipmentInstallationFailure.NoOwnedEquipment)
                {
                    waiting++;
                }
                else
                {
                    blocked++;
                }
            }

            return new FixtureEquipmentBatchInstallationResult(
                edits.Count,
                waiting,
                blocked,
                edits);
        }

        public FixtureEquipmentInstallationResult TryApplyEquipmentEdit(
            FixtureEdit edit)
        {
            if (edit.IsEmpty)
            {
                return FixtureEquipmentInstallationResult.Rejected(
                    FixtureEquipmentInstallationFailure.StateConflict);
            }

            if (edit.Kind == FixtureEditKind.AddFixture)
            {
                return TryInstallOwnedFixture(
                    edit.Fixture.Id,
                    edit.Fixture.DefinitionId,
                    edit.Fixture.AnchorCell,
                    edit.Fixture.Orientation);
            }

            FixturePlacementResult result =
                placement.TryRemoveFixture(edit.Fixture.Id);

            if (!result.Succeeded)
            {
                return FixtureEquipmentInstallationResult.Rejected(
                    FixtureEquipmentInstallationFailure.PlacementRejected,
                    result);
            }

            inventory.Add(result.DefinitionId);
            return FixtureEquipmentInstallationResult.Success(result);
        }


        private void RetireMatchingPlan(
            FixtureInstance installedFixture)
        {
            if (installedFixture == null)
            {
                return;
            }

            FixtureEquipmentPlan matchingPlan = null;

            foreach (FixtureEquipmentPlan plan in plans.EnumeratePlans())
            {
                if (plan.FixtureDefinitionId
                        != installedFixture.DefinitionId
                    || !FootprintsMatch(
                        plan.Footprint,
                        installedFixture.Footprint))
                {
                    continue;
                }

                matchingPlan = plan;
                break;
            }

            if (matchingPlan != null)
            {
                plans.TryRemove(matchingPlan.Id, out _);
            }
        }


        private static bool FootprintsMatch(
            FixtureFootprint planned,
            FixtureFootprint installed)
        {
            if (planned == null
                || installed == null
                || planned.CellCount != installed.CellCount)
            {
                return false;
            }

            for (int index = 0;
                 index < installed.CellCount;
                 index++)
            {
                if (!planned.ContainsCell(installed.GetCell(index)))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
