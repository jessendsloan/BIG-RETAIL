using System;
using BigRetail.Map.Construction;

namespace BigRetail.Map.Fixtures
{
    /// <summary>
    /// Adapts one exact FixtureEdit to the neutral construction-history
    /// contract.
    /// </summary>
    public sealed class ReversibleFixtureEditAction :
        IReversibleConstructionAction
    {
        private readonly FixturePlacementService fixtureService;


        public FixtureEdit Edit { get; }

        public string Description =>
            $"{Edit.Kind}: {Edit.Fixture.Definition.DisplayName}";

        public int ChangeCount => 1;


        public ReversibleFixtureEditAction(
            FixturePlacementService fixtureService,
            FixtureEdit edit)
        {
            this.fixtureService =
                fixtureService
                ?? throw new ArgumentNullException(
                    nameof(fixtureService));

            if (edit.IsEmpty)
            {
                throw new ArgumentException(
                    "A reversible fixture action requires a non-empty edit.",
                    nameof(edit));
            }

            Edit = edit;
        }


        public ConstructionActionResult TryUndo()
        {
            return TryApply(
                Edit.Inverse());
        }

        public ConstructionActionResult TryRedo()
        {
            return TryApply(Edit);
        }


        private ConstructionActionResult TryApply(
            FixtureEdit edit)
        {
            FixturePlacementResult result =
                fixtureService.TryApplyEdit(edit);

            if (result.Succeeded)
            {
                return ConstructionActionResult.Success();
            }

            return ConstructionActionResult.Rejected(
                $"Fixture replay failed: {result.Failure}. "
                + $"Cell: {result.FailedCell}.");
        }
    }
}
