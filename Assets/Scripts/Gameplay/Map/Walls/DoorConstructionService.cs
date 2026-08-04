using System;
using System.Collections.Generic;
using BigRetail.Map.Domain;

namespace BigRetail.Map.Walls
{
    /// <summary>
    /// Validates and applies door assemblies attached to existing wall runs.
    /// A successful placement occupies its complete span atomically.
    /// </summary>
    public sealed class DoorConstructionService : IDisposable
    {
        private readonly DoorDefinitionCatalog definitionCatalog;
        private readonly DoorAssemblyState assemblyState;
        private readonly WallState wallState;

        private bool isDisposed;


        public DoorConstructionService(
            DoorDefinitionCatalog definitionCatalog,
            DoorAssemblyState assemblyState,
            WallState wallState)
        {
            this.definitionCatalog =
                definitionCatalog
                ?? throw new ArgumentNullException(
                    nameof(definitionCatalog));

            this.assemblyState =
                assemblyState
                ?? throw new ArgumentNullException(
                    nameof(assemblyState));

            this.wallState =
                wallState
                ?? throw new ArgumentNullException(
                    nameof(wallState));

            this.wallState.WallRemoved +=
                HandleSupportingWallRemoved;
        }


        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            wallState.WallRemoved -=
                HandleSupportingWallRemoved;

            isDisposed = true;
        }


        public DoorAssemblyChangeResult EvaluatePlacement(
            DoorAssemblyId assemblyId,
            DoorDefinitionId definitionId,
            IReadOnlyList<CellEdge> edges)
        {
            if (edges == null)
            {
                throw new ArgumentNullException(
                    nameof(edges));
            }

            if (!assemblyId.IsValid)
            {
                return DoorAssemblyChangeResult.Rejected(
                    assemblyId,
                    definitionId,
                    DoorAssemblyChangeFailure.InvalidAssemblyId);
            }

            if (!definitionCatalog.TryGetDefinition(
                    definitionId,
                    out DoorDefinition definition))
            {
                return DoorAssemblyChangeResult.Rejected(
                    assemblyId,
                    definitionId,
                    DoorAssemblyChangeFailure.UnknownDefinition);
            }

            if (assemblyState.TryGetAssembly(
                    assemblyId,
                    out _))
            {
                return DoorAssemblyChangeResult.Rejected(
                    assemblyId,
                    definitionId,
                    DoorAssemblyChangeFailure.AssemblyAlreadyExists);
            }

            if (edges.Count == 0)
            {
                return DoorAssemblyChangeResult.Rejected(
                    assemblyId,
                    definitionId,
                    DoorAssemblyChangeFailure.EmptySpan);
            }

            if (edges.Count != definition.SegmentCount)
            {
                return DoorAssemblyChangeResult.Rejected(
                    assemblyId,
                    definitionId,
                    DoorAssemblyChangeFailure.IncorrectSegmentCount);
            }

            if (!TryValidateStraightSpan(
                    edges,
                    out CellEdge failedSpanEdge))
            {
                return DoorAssemblyChangeResult.Rejected(
                    assemblyId,
                    definitionId,
                    DoorAssemblyChangeFailure.InvalidSpan,
                    failedSpanEdge);
            }

            for (int index = 0;
                 index < edges.Count;
                 index++)
            {
                CellEdge edge = edges[index];

                if (!wallState.HasWall(edge))
                {
                    return DoorAssemblyChangeResult.Rejected(
                        assemblyId,
                        definitionId,
                        DoorAssemblyChangeFailure.MissingWall,
                        edge);
                }

                if (assemblyState.TryGetAssemblyAtEdge(
                        edge,
                        out _))
                {
                    return DoorAssemblyChangeResult.Rejected(
                        assemblyId,
                        definitionId,
                        DoorAssemblyChangeFailure.OverlapsAssembly,
                        edge);
                }
            }

            return DoorAssemblyChangeResult.Approved(
                assemblyId,
                definitionId,
                edges.Count);
        }


        public DoorAssemblyChangeResult TryPlaceAssembly(
            DoorAssemblyId assemblyId,
            DoorDefinitionId definitionId,
            IReadOnlyList<CellEdge> edges)
        {
            DoorAssemblyChangeResult evaluation =
                EvaluatePlacement(
                    assemblyId,
                    definitionId,
                    edges);

            if (!evaluation.Succeeded)
            {
                return evaluation;
            }

            if (!definitionCatalog.TryGetDefinition(
                    definitionId,
                    out DoorDefinition definition))
            {
                return DoorAssemblyChangeResult.Rejected(
                    assemblyId,
                    definitionId,
                    DoorAssemblyChangeFailure.UnknownDefinition);
            }

            DoorAssembly assembly =
                new DoorAssembly(
                    assemblyId,
                    definition,
                    DoorAssemblySpanOrder.Normalize(
                        edges));

            if (!assemblyState.TryAddAssembly(
                    assembly))
            {
                return DoorAssemblyChangeResult.Rejected(
                    assemblyId,
                    definitionId,
                    DoorAssemblyChangeFailure.StateConflict);
            }

            return DoorAssemblyChangeResult.Success(
                assembly);
        }


        public DoorAssemblyChangeResult EvaluateRemoval(
            DoorAssemblyId assemblyId)
        {
            if (!assemblyId.IsValid)
            {
                return DoorAssemblyChangeResult.Rejected(
                    assemblyId,
                    default,
                    DoorAssemblyChangeFailure.InvalidAssemblyId);
            }

            if (!assemblyState.TryGetAssembly(
                    assemblyId,
                    out DoorAssembly assembly))
            {
                return DoorAssemblyChangeResult.Rejected(
                    assemblyId,
                    default,
                    DoorAssemblyChangeFailure.AssemblyNotFound);
            }

            return DoorAssemblyChangeResult.Approved(
                assembly);
        }


        public DoorAssemblyChangeResult TryRemoveAssembly(
            DoorAssemblyId assemblyId)
        {
            DoorAssemblyChangeResult evaluation =
                EvaluateRemoval(assemblyId);

            if (!evaluation.Succeeded)
            {
                return evaluation;
            }

            if (!assemblyState.TryRemoveAssembly(
                    assemblyId,
                    out DoorAssembly removedAssembly))
            {
                return DoorAssemblyChangeResult.Rejected(
                    assemblyId,
                    evaluation.DefinitionId,
                    DoorAssemblyChangeFailure.StateConflict);
            }

            return DoorAssemblyChangeResult.Success(
                removedAssembly);
        }


        private static bool TryValidateStraightSpan(
            IReadOnlyList<CellEdge> edges,
            out CellEdge failedEdge)
        {
            WallRunPlanResult run =
                StraightWallRunPlanner.Plan(
                    edges[0],
                    edges[edges.Count - 1]);

            if (!run.Succeeded
                || run.SegmentCount != edges.Count)
            {
                failedEdge = edges[edges.Count - 1];
                return false;
            }

            for (int index = 0;
                 index < edges.Count;
                 index++)
            {
                if (run.Edges[index] == edges[index])
                {
                    continue;
                }

                failedEdge = edges[index];
                return false;
            }

            failedEdge = default;
            return true;
        }


        private void HandleSupportingWallRemoved(
            CellEdge edge)
        {
            if (!assemblyState.TryGetAssemblyAtEdge(
                    edge,
                    out DoorAssembly assembly))
            {
                return;
            }

            assemblyState.TryRemoveAssembly(
                assembly.Id,
                out _);
        }
    }
}
