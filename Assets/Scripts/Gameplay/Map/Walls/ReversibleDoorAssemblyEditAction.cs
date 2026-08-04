using System;
using BigRetail.Map.Construction;

namespace BigRetail.Map.Walls
{
    /// <summary>
    /// Records one complete door placement for construction history.
    /// Undo removes the assembly; redo restores the same definition and span.
    /// </summary>
    public sealed class ReversibleDoorAssemblyEditAction :
        IReversibleConstructionAction
    {
        private readonly DoorConstructionService doorService;


        public DoorAssembly Assembly { get; }

        public string Description =>
            $"Door: {Assembly.DefinitionId} across "
            + $"{Assembly.SegmentCount} wall segment(s)";

        public int ChangeCount => 1;


        public ReversibleDoorAssemblyEditAction(
            DoorConstructionService doorService,
            DoorAssembly assembly)
        {
            this.doorService =
                doorService
                ?? throw new ArgumentNullException(
                    nameof(doorService));

            Assembly =
                assembly
                ?? throw new ArgumentNullException(
                    nameof(assembly));
        }


        public ConstructionActionResult TryUndo()
        {
            DoorAssemblyChangeResult result =
                doorService.TryRemoveAssembly(
                    Assembly.Id);

            return ToConstructionResult(
                "remove",
                result);
        }


        public ConstructionActionResult TryRedo()
        {
            DoorAssemblyChangeResult result =
                doorService.TryPlaceAssembly(
                    Assembly.Id,
                    Assembly.DefinitionId,
                    Assembly.Edges);

            return ToConstructionResult(
                "restore",
                result);
        }


        private static ConstructionActionResult ToConstructionResult(
            string operation,
            DoorAssemblyChangeResult result)
        {
            if (result.Succeeded)
            {
                return ConstructionActionResult.Success();
            }

            return ConstructionActionResult.Rejected(
                $"Door {operation} failed: {result.Failure}. "
                + $"Edge: {result.FailedEdge}.");
        }
    }
}
