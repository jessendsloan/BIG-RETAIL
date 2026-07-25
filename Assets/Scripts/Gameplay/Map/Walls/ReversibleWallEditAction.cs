using System;
using BigRetail.Map.Construction;

namespace BigRetail.Map.Walls
{
    /// <summary>
    /// Adapts one exact WallEdit to the neutral construction-history
    /// contract.
    /// </summary>
    public sealed class ReversibleWallEditAction :
        IReversibleConstructionAction
    {
        private readonly WallConstructionService wallService;


        public WallEdit Edit { get; }

        public string Description =>
            $"{Edit.Kind}: {Edit.Count} wall edge(s)";

        public int ChangeCount =>
            Edit.Count;


        public ReversibleWallEditAction(
            WallConstructionService wallService,
            WallEdit edit)
        {
            this.wallService =
                wallService
                ?? throw new ArgumentNullException(
                    nameof(wallService));

            if (edit.IsEmpty)
            {
                throw new ArgumentException(
                    "A reversible wall action requires a non-empty edit.",
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
            WallEdit edit)
        {
            WallBatchChangeResult result =
                wallService.TryApplyEdit(
                    edit);

            if (result.Succeeded)
            {
                return ConstructionActionResult.Success();
            }

            return ConstructionActionResult.Rejected(
                $"Wall replay failed: {result.Failure}. " +
                $"Edge: {result.FailedEdge}.");
        }
    }
}
