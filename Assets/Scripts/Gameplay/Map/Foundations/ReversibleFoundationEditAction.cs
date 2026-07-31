using System;
using BigRetail.Map.Construction;

namespace BigRetail.Map.Foundations
{
    /// <summary>
    /// Adapts one exact FoundationEdit to the neutral construction-history
    /// contract.
    /// </summary>
    public sealed class ReversibleFoundationEditAction :
        IReversibleConstructionAction
    {
        private readonly FoundationConstructionService foundationService;


        public FoundationEdit Edit { get; }

        public string Description =>
            $"{Edit.Kind}: {Edit.Count} foundation cell(s)";

        public int ChangeCount =>
            Edit.Count;


        public ReversibleFoundationEditAction(
            FoundationConstructionService foundationService,
            FoundationEdit edit)
        {
            this.foundationService =
                foundationService
                ?? throw new ArgumentNullException(
                    nameof(foundationService));

            if (edit.IsEmpty)
            {
                throw new ArgumentException(
                    "A reversible foundation action requires a non-empty edit.",
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
            return TryApply(
                Edit);
        }


        private ConstructionActionResult TryApply(
            FoundationEdit edit)
        {
            FoundationBatchChangeResult result =
                foundationService.TryApplyEdit(
                    edit);

            if (result.Succeeded)
            {
                return ConstructionActionResult.Success();
            }

            return ConstructionActionResult.Rejected(
                $"Foundation replay failed: {result.Failure}. " +
                $"Cell: {result.FailedCell}.");
        }
    }
}
