using System;
using BigRetail.Map.Construction;

namespace BigRetail.Map.Floors
{
    /// <summary>
    /// Adapts one exact FloorEdit to the neutral construction-history
    /// contract.
    /// </summary>
    public sealed class ReversibleFloorEditAction :
        IReversibleConstructionAction
    {
        private readonly FloorConstructionService floorService;


        public FloorEdit Edit { get; }

        public string Description =>
            $"{Edit.Kind}: {Edit.Count} floor cell(s)";

        public int ChangeCount =>
            Edit.Count;


        public ReversibleFloorEditAction(
            FloorConstructionService floorService,
            FloorEdit edit)
        {
            this.floorService =
                floorService
                ?? throw new ArgumentNullException(
                    nameof(floorService));

            if (edit.IsEmpty)
            {
                throw new ArgumentException(
                    "A reversible floor action requires a non-empty edit.",
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
            FloorEdit edit)
        {
            FloorBatchChangeResult result =
                floorService.TryApplyEdit(
                    edit);

            if (result.Succeeded)
            {
                return ConstructionActionResult.Success();
            }

            return ConstructionActionResult.Rejected(
                $"Floor replay failed: {result.Failure}. " +
                $"Cell: {result.FailedCell}.");
        }
    }
}
