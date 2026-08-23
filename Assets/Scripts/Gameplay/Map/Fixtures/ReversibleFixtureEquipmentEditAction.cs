using System;
using BigRetail.Map.Construction;

namespace BigRetail.Map.Fixtures
{
    /// <summary>
    /// Replays fixture edits together with their owned-equipment transfer.
    /// </summary>
    public sealed class ReversibleFixtureEquipmentEditAction :
        IReversibleConstructionAction
    {
        private readonly FixtureEquipmentInstallationService installation;


        public FixtureEdit Edit { get; }

        public string Description =>
            $"{Edit.Kind}: {Edit.Fixture.Definition.DisplayName}";

        public int ChangeCount => 1;


        public ReversibleFixtureEquipmentEditAction(
            FixtureEquipmentInstallationService installation,
            FixtureEdit edit)
        {
            this.installation = installation
                ?? throw new ArgumentNullException(nameof(installation));

            if (edit.IsEmpty)
            {
                throw new ArgumentException(
                    "An equipment-aware fixture action requires an edit.",
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

        private ConstructionActionResult TryApply(FixtureEdit edit)
        {
            FixtureEquipmentInstallationResult result =
                installation.TryApplyEquipmentEdit(edit);

            if (result.Succeeded)
            {
                return ConstructionActionResult.Success();
            }

            return ConstructionActionResult.Rejected(
                $"Equipment-aware fixture replay failed: {result.Failure}; "
                + $"placement {result.Placement.Failure}.");
        }
    }
}
