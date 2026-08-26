using System;
using System.Collections.Generic;
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


    /// <summary>
    /// Records wall demolition together with every door or window assembly
    /// removed when its supporting wall disappeared. Undo restores the walls
    /// before their openings; redo removes the openings before their walls.
    /// </summary>
    public sealed class ReversibleWallDemolitionAction :
        IReversibleConstructionAction
    {
        private readonly WallConstructionService wallService;
        private readonly DoorConstructionService doorService;
        private readonly DoorAssembly[] removedAssemblies;


        public WallEdit Edit { get; }

        public IReadOnlyList<DoorAssembly> RemovedAssemblies =>
            removedAssemblies;

        public string Description =>
            $"Wall demolition: {Edit.Count} wall edge(s), "
            + $"{removedAssemblies.Length} opening(s)";

        public int ChangeCount =>
            Edit.Count + removedAssemblies.Length;


        public ReversibleWallDemolitionAction(
            WallConstructionService wallService,
            DoorConstructionService doorService,
            WallEdit edit,
            IReadOnlyList<DoorAssembly> removedAssemblies)
        {
            this.wallService =
                wallService
                ?? throw new ArgumentNullException(
                    nameof(wallService));

            this.doorService =
                doorService
                ?? throw new ArgumentNullException(
                    nameof(doorService));

            if (edit.IsEmpty
                || edit.Kind != WallEditKind.RemoveWalls)
            {
                throw new ArgumentException(
                    "A wall demolition action requires a non-empty "
                    + "remove-walls edit.",
                    nameof(edit));
            }

            if (removedAssemblies == null)
            {
                throw new ArgumentNullException(
                    nameof(removedAssemblies));
            }

            this.removedAssemblies =
                new DoorAssembly[removedAssemblies.Count];

            for (int index = 0;
                 index < removedAssemblies.Count;
                 index++)
            {
                this.removedAssemblies[index] =
                    removedAssemblies[index]
                    ?? throw new ArgumentException(
                        $"Removed opening {index} is null.",
                        nameof(removedAssemblies));
            }

            Edit = edit;
        }


        public ConstructionActionResult TryUndo()
        {
            WallBatchChangeResult wallResult =
                wallService.TryApplyEdit(
                    Edit.Inverse());

            if (!wallResult.Succeeded)
            {
                return WallRejected(
                    "restore",
                    wallResult);
            }

            for (int index = 0;
                 index < removedAssemblies.Length;
                 index++)
            {
                DoorAssembly assembly =
                    removedAssemblies[index];

                DoorAssemblyChangeResult doorResult =
                    doorService.TryPlaceAssembly(
                        assembly.Id,
                        assembly.DefinitionId,
                        assembly.Edges);

                if (doorResult.Succeeded)
                {
                    continue;
                }

                // Removing the restored walls also removes any openings that
                // were already replayed, returning this transaction to its
                // pre-undo state.
                wallService.TryApplyEdit(Edit);

                return DoorRejected(
                    "restore",
                    doorResult);
            }

            return ConstructionActionResult.Success();
        }


        public ConstructionActionResult TryRedo()
        {
            int removedCount = 0;

            for (int index = 0;
                 index < removedAssemblies.Length;
                 index++)
            {
                DoorAssemblyChangeResult doorResult =
                    doorService.TryRemoveAssembly(
                        removedAssemblies[index].Id);

                if (!doorResult.Succeeded)
                {
                    RestoreRemovedAssemblies(removedCount);

                    return DoorRejected(
                        "remove",
                        doorResult);
                }

                removedCount++;
            }

            WallBatchChangeResult wallResult =
                wallService.TryApplyEdit(Edit);

            if (!wallResult.Succeeded)
            {
                RestoreRemovedAssemblies(removedCount);

                return WallRejected(
                    "remove",
                    wallResult);
            }

            return ConstructionActionResult.Success();
        }


        private void RestoreRemovedAssemblies(
            int removedCount)
        {
            for (int index = 0;
                 index < removedCount;
                 index++)
            {
                DoorAssembly assembly =
                    removedAssemblies[index];

                doorService.TryPlaceAssembly(
                    assembly.Id,
                    assembly.DefinitionId,
                    assembly.Edges);
            }
        }


        private static ConstructionActionResult WallRejected(
            string operation,
            WallBatchChangeResult result)
        {
            return ConstructionActionResult.Rejected(
                $"Wall demolition {operation} failed: "
                + $"{result.Failure}. Edge: {result.FailedEdge}.");
        }


        private static ConstructionActionResult DoorRejected(
            string operation,
            DoorAssemblyChangeResult result)
        {
            return ConstructionActionResult.Rejected(
                $"Opening {operation} failed: {result.Failure}. "
                + $"Edge: {result.FailedEdge}.");
        }
    }
}
