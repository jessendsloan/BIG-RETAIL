using System;
using System.Collections.Generic;
using BigRetail.Map.Construction;
using BigRetail.Map.Domain;

namespace BigRetail.Map.Walls
{
    /// <summary>
    /// Evaluates and performs wall placement and removal requests.
    ///
    /// Player-facing construction uses permissive ensure/clear
    /// operations. Undo and redo replay exact WallEdit records.
    /// </summary>
    public sealed class WallConstructionService
    {
        private readonly GridMapDefinition mapDefinition;
        private readonly ConstructionAreaDefinition constructionArea;
        private readonly WallState wallState;
        private readonly IFoundationSupportQuery foundationSupport;

        private readonly List<IWallPlacementConstraint>
            placementConstraints =
                new List<IWallPlacementConstraint>();


        public WallConstructionService(
            GridMapDefinition mapDefinition,
            ConstructionAreaDefinition constructionArea,
            WallState wallState,
            IFoundationSupportQuery foundationSupport)
        {
            this.mapDefinition =
                mapDefinition
                ?? throw new ArgumentNullException(
                    nameof(mapDefinition));

            this.constructionArea =
                constructionArea
                ?? throw new ArgumentNullException(
                    nameof(constructionArea));

            this.wallState =
                wallState
                ?? throw new ArgumentNullException(
                    nameof(wallState));

            this.foundationSupport =
                foundationSupport
                ?? throw new ArgumentNullException(
                    nameof(foundationSupport));
        }


        public bool HasWall(
            CellEdge edge)
        {
            return wallState.HasWall(edge);
        }


        public void RegisterPlacementConstraint(
            IWallPlacementConstraint constraint)
        {
            if (constraint == null)
            {
                throw new ArgumentNullException(
                    nameof(constraint));
            }

            if (!placementConstraints.Contains(constraint))
            {
                placementConstraints.Add(constraint);
            }
        }


        public void UnregisterPlacementConstraint(
            IWallPlacementConstraint constraint)
        {
            if (constraint != null)
            {
                placementConstraints.Remove(constraint);
            }
        }


        public WallChangeResult EvaluatePlacement(
            CellEdge edge)
        {
            if (!TouchesValidMapCell(edge))
            {
                return WallChangeResult.Rejected(
                    edge,
                    WallChangeFailure.OutsideMap);
            }

            if (!TouchesConstructionEligibleCell(edge))
            {
                return WallChangeResult.Rejected(
                    edge,
                    WallChangeFailure.OutsideConstructionArea);
            }

            if (wallState.HasWall(edge))
            {
                return WallChangeResult.Rejected(
                    edge,
                    WallChangeFailure.AlreadyExists);
            }

            WallChangeFailure constraintFailure =
                EvaluatePlacementConstraints(edge);

            if (constraintFailure != WallChangeFailure.None)
            {
                return WallChangeResult.Rejected(
                    edge,
                    constraintFailure);
            }

            if (!HasFoundationSupport(edge))
            {
                return WallChangeResult.Rejected(
                    edge,
                    WallChangeFailure.MissingFoundation);
            }

            return WallChangeResult.Success(edge);
        }


        public WallBatchChangeResult EvaluatePlacementBatch(
            IReadOnlyList<CellEdge> edges)
        {
            if (edges == null)
            {
                throw new ArgumentNullException(
                    nameof(edges));
            }

            if (edges.Count == 0)
            {
                return WallBatchChangeResult.Rejected(
                    0,
                    default,
                    WallChangeFailure.EmptyRequest);
            }

            HashSet<CellEdge> requestedEdges =
                new HashSet<CellEdge>();

            for (int index = 0;
                 index < edges.Count;
                 index++)
            {
                CellEdge edge =
                    edges[index];

                if (!requestedEdges.Add(edge))
                {
                    return WallBatchChangeResult.Rejected(
                        edges.Count,
                        edge,
                        WallChangeFailure.DuplicateRequest);
                }

                WallChangeResult evaluation =
                    EvaluatePlacement(edge);

                if (!evaluation.Succeeded)
                {
                    return WallBatchChangeResult.Rejected(
                        edges.Count,
                        edge,
                        evaluation.Failure);
                }
            }

            return WallBatchChangeResult.Success(
                edges.Count);
        }


        public WallChangeResult TryPlaceWall(
            CellEdge edge)
        {
            WallChangeResult evaluation =
                EvaluatePlacement(edge);

            if (!evaluation.Succeeded)
            {
                return evaluation;
            }

            if (!wallState.TryAddWall(edge))
            {
                return WallChangeResult.Rejected(
                    edge,
                    WallChangeFailure.AlreadyExists);
            }

            return WallChangeResult.Success(edge);
        }


        public WallBatchChangeResult TryPlaceWalls(
            IReadOnlyList<CellEdge> edges)
        {
            WallBatchChangeResult evaluation =
                EvaluatePlacementBatch(edges);

            if (!evaluation.Succeeded)
            {
                return evaluation;
            }

            if (!wallState.TryAddWalls(edges))
            {
                WallBatchChangeResult changedEvaluation =
                    EvaluatePlacementBatch(edges);

                if (!changedEvaluation.Succeeded)
                {
                    return changedEvaluation;
                }

                return WallBatchChangeResult.Rejected(
                    edges.Count,
                    edges[0],
                    WallChangeFailure.AlreadyExists);
            }

            return WallBatchChangeResult.Success(
                edges.Count);
        }


        public WallEnsureResult TryEnsureWalls(
            IReadOnlyList<CellEdge> edges)
        {
            if (edges == null)
            {
                throw new ArgumentNullException(
                    nameof(edges));
            }

            if (edges.Count == 0)
            {
                return WallEnsureResult.Rejected(
                    0,
                    0,
                    default,
                    WallChangeFailure.EmptyRequest);
            }

            HashSet<CellEdge> uniqueEdges =
                new HashSet<CellEdge>();

            List<CellEdge> missingLegalEdges =
                new List<CellEdge>();

            int alreadyExistingCount = 0;
            int skippedOutsideMapCount = 0;
            int skippedOutsideConstructionAreaCount = 0;
            int skippedMissingFoundationCount = 0;
            int skippedPlacementConstraintCount = 0;

            for (int index = 0;
                 index < edges.Count;
                 index++)
            {
                CellEdge edge =
                    edges[index];

                if (!uniqueEdges.Add(edge))
                {
                    continue;
                }

                if (wallState.HasWall(edge))
                {
                    alreadyExistingCount++;
                    continue;
                }

                if (!TouchesValidMapCell(edge))
                {
                    skippedOutsideMapCount++;
                    continue;
                }

                if (!TouchesConstructionEligibleCell(edge))
                {
                    skippedOutsideConstructionAreaCount++;
                    continue;
                }

                if (!HasFoundationSupport(edge))
                {
                    skippedMissingFoundationCount++;
                    continue;
                }

                if (EvaluatePlacementConstraints(edge)
                    != WallChangeFailure.None)
                {
                    skippedPlacementConstraintCount++;
                    continue;
                }

                missingLegalEdges.Add(edge);
            }

            if (missingLegalEdges.Count > 0
                && !wallState.TryAddWalls(
                    missingLegalEdges))
            {
                return WallEnsureResult.Rejected(
                    edges.Count,
                    uniqueEdges.Count,
                    missingLegalEdges[0],
                    WallChangeFailure.AlreadyExists);
            }

            return WallEnsureResult.Success(
                edges.Count,
                uniqueEdges.Count,
                missingLegalEdges,
                alreadyExistingCount,
                skippedOutsideMapCount,
                skippedOutsideConstructionAreaCount,
                skippedMissingFoundationCount,
                skippedPlacementConstraintCount);
        }


        public WallChangeResult TryRemoveWall(
            CellEdge edge)
        {
            if (!wallState.TryRemoveWall(edge))
            {
                return WallChangeResult.Rejected(
                    edge,
                    WallChangeFailure.NotFound);
            }

            return WallChangeResult.Success(edge);
        }


        public WallClearResult TryClearWalls(
            IReadOnlyList<CellEdge> edges)
        {
            if (edges == null)
            {
                throw new ArgumentNullException(
                    nameof(edges));
            }

            if (edges.Count == 0)
            {
                return WallClearResult.Rejected(
                    0,
                    0,
                    default,
                    WallChangeFailure.EmptyRequest);
            }

            HashSet<CellEdge> uniqueEdges =
                new HashSet<CellEdge>();

            List<CellEdge> existingEdges =
                new List<CellEdge>();

            int alreadyEmptyCount = 0;

            for (int index = 0;
                 index < edges.Count;
                 index++)
            {
                CellEdge edge =
                    edges[index];

                if (!uniqueEdges.Add(edge))
                {
                    continue;
                }

                if (wallState.HasWall(edge))
                {
                    existingEdges.Add(edge);
                }
                else
                {
                    alreadyEmptyCount++;
                }
            }

            if (existingEdges.Count > 0
                && !wallState.TryRemoveWalls(
                    existingEdges))
            {
                return WallClearResult.Rejected(
                    edges.Count,
                    uniqueEdges.Count,
                    existingEdges[0],
                    WallChangeFailure.NotFound);
            }

            return WallClearResult.Success(
                edges.Count,
                uniqueEdges.Count,
                existingEdges,
                alreadyEmptyCount);
        }


        /// <summary>
        /// Replays an exact previously committed edit.
        ///
        /// Unlike ordinary construction, history replay does not check
        /// the current construction-area mask. This allows undo to
        /// restore scenario-authored or legacy walls exactly.
        /// Added walls must still have current Foundation support.
        ///
        /// The operation remains strict: every requested state change
        /// must still match the current wall state.
        /// </summary>
        public WallBatchChangeResult TryApplyEdit(
            WallEdit edit)
        {
            if (edit.IsEmpty)
            {
                return WallBatchChangeResult.Success(0);
            }

            switch (edit.Kind)
            {
                case WallEditKind.AddWalls:
                    return TryApplyAddedEdit(edit);

                case WallEditKind.RemoveWalls:
                    return TryApplyRemovedEdit(edit);

                default:
                    throw new InvalidOperationException(
                        $"Unsupported wall edit kind: {edit.Kind}.");
            }
        }


        private WallBatchChangeResult TryApplyAddedEdit(
            WallEdit edit)
        {
            for (int index = 0;
                 index < edit.Count;
                 index++)
            {
                CellEdge edge =
                    edit.Edges[index];

                if (wallState.HasWall(edge))
                {
                    return WallBatchChangeResult.Rejected(
                        edit.Count,
                        edge,
                        WallChangeFailure.AlreadyExists);
                }

                if (!HasFoundationSupport(edge))
                {
                    return WallBatchChangeResult.Rejected(
                        edit.Count,
                        edge,
                        WallChangeFailure.MissingFoundation);
                }


                WallChangeFailure constraintFailure =
                    EvaluatePlacementConstraints(edge);

                if (constraintFailure != WallChangeFailure.None)
                {
                    return WallBatchChangeResult.Rejected(
                        edit.Count,
                        edge,
                        constraintFailure);
                }
            }

            if (!wallState.TryAddWalls(edit.Edges))
            {
                return WallBatchChangeResult.Rejected(
                    edit.Count,
                    edit.Edges[0],
                    WallChangeFailure.AlreadyExists);
            }

            return WallBatchChangeResult.Success(
                edit.Count);
        }


        private WallBatchChangeResult TryApplyRemovedEdit(
            WallEdit edit)
        {
            for (int index = 0;
                 index < edit.Count;
                 index++)
            {
                CellEdge edge =
                    edit.Edges[index];

                if (!wallState.HasWall(edge))
                {
                    return WallBatchChangeResult.Rejected(
                        edit.Count,
                        edge,
                        WallChangeFailure.NotFound);
                }
            }

            if (!wallState.TryRemoveWalls(edit.Edges))
            {
                return WallBatchChangeResult.Rejected(
                    edit.Count,
                    edit.Edges[0],
                    WallChangeFailure.NotFound);
            }

            return WallBatchChangeResult.Success(
                edit.Count);
        }


        private bool TouchesValidMapCell(
            CellEdge edge)
        {
            return mapDefinition.ContainsCell(
                       edge.FirstCell)
                || mapDefinition.ContainsCell(
                       edge.SecondCell);
        }


        private bool TouchesConstructionEligibleCell(
            CellEdge edge)
        {
            return constructionArea.IsEligible(
                       edge.FirstCell)
                || constructionArea.IsEligible(
                       edge.SecondCell);
        }


        private bool HasFoundationSupport(
            CellEdge edge)
        {
            return foundationSupport.HasFoundation(
                       edge.FirstCell)
                || foundationSupport.HasFoundation(
                       edge.SecondCell);
        }


        private WallChangeFailure EvaluatePlacementConstraints(
            CellEdge edge)
        {
            for (int index = 0;
                 index < placementConstraints.Count;
                 index++)
            {
                WallChangeFailure failure =
                    placementConstraints[index]
                        .EvaluateWallPlacement(edge);

                if (failure != WallChangeFailure.None)
                {
                    return failure;
                }
            }

            return WallChangeFailure.None;
        }
    }
}
