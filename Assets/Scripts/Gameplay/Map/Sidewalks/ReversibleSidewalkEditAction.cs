using System;
using BigRetail.Map.Construction;

namespace BigRetail.Map.Sidewalks
{
    public sealed class ReversibleSidewalkEditAction :
        IReversibleConstructionAction
    {
        private readonly SidewalkConstructionService sidewalkService;


        public SidewalkEdit Edit { get; }

        public string Description =>
            $"{Edit.Kind}: {Edit.Count} sidewalk cell(s)";

        public int ChangeCount => Edit.Count;


        public ReversibleSidewalkEditAction(
            SidewalkConstructionService sidewalkService,
            SidewalkEdit edit)
        {
            this.sidewalkService =
                sidewalkService
                ?? throw new ArgumentNullException(
                    nameof(sidewalkService));

            if (edit.IsEmpty)
            {
                throw new ArgumentException(
                    "A reversible sidewalk action requires a non-empty edit.",
                    nameof(edit));
            }

            Edit = edit;
        }


        public ConstructionActionResult TryUndo()
        {
            return TryApply(Edit.Inverse());
        }


        public ConstructionActionResult TryRedo()
        {
            return TryApply(Edit);
        }


        private ConstructionActionResult TryApply(SidewalkEdit edit)
        {
            SidewalkBatchChangeResult result =
                sidewalkService.TryApplyEdit(edit);

            return result.Succeeded
                ? ConstructionActionResult.Success()
                : ConstructionActionResult.Rejected(
                    $"Sidewalk replay failed: {result.Failure}. " +
                    $"Cell: {result.FailedCell}.");
        }
    }
}
