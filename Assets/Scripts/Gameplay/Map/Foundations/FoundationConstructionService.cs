using System;
using System.Collections.Generic;
using BigRetail.Map.Construction;
using BigRetail.Map.Domain;

namespace BigRetail.Map.Foundations
{
    /// <summary>
    /// Evaluates and performs foundation construction and removal requests.
    ///
    /// Player-facing requests are permissive: existing foundations satisfy
    /// placement requests, empty cells satisfy removal requests, and invalid
    /// placement cells are skipped. Exact edit replay is strict and atomic.
    /// </summary>
    public sealed class FoundationConstructionService :
        IFoundationSupportQuery
    {
        private readonly GridMapDefinition mapDefinition;
        private readonly IConstructionCellEligibility constructionArea;
        private readonly FoundationState foundationState;
        private readonly IFoundationRemovalValidator removalValidator;


        public FoundationConstructionService(
            GridMapDefinition mapDefinition,
            IConstructionCellEligibility constructionArea,
            FoundationState foundationState,
            IFoundationRemovalValidator removalValidator)
        {
            this.mapDefinition =
                mapDefinition
                ?? throw new ArgumentNullException(
                    nameof(mapDefinition));

            this.constructionArea =
                constructionArea
                ?? throw new ArgumentNullException(
                    nameof(constructionArea));

            this.foundationState =
                foundationState
                ?? throw new ArgumentNullException(
                    nameof(foundationState));

            this.removalValidator =
                removalValidator
                ?? throw new ArgumentNullException(
                    nameof(removalValidator));
        }


        public bool HasFoundation(
            GridPosition cell)
        {
            return foundationState.HasFoundation(cell);
        }


        /// <summary>
        /// Evaluates whether removing one existing Foundation would preserve
        /// every dependent Floor and Wall.
        /// </summary>
        public FoundationChangeResult EvaluateRemoval(
            GridPosition cell)
        {
            if (!foundationState.HasFoundation(cell))
            {
                return FoundationChangeResult.Rejected(
                    cell,
                    FoundationChangeFailure.NotFound);
            }

            FoundationRemovalValidation validation =
                removalValidator.ValidateRemoval(
                    new[] { cell });

            if (!validation.IsAllowed)
            {
                return FoundationChangeResult.Rejected(
                    validation.BlockedCell,
                    FoundationChangeFailure
                        .SupportsConstruction);
            }

            return FoundationChangeResult.Success(cell);
        }


        /// <summary>
        /// Evaluates whether one brand-new foundation could be constructed.
        /// This method does not modify FoundationState.
        /// </summary>
        public FoundationChangeResult EvaluatePlacement(
            GridPosition cell)
        {
            if (!mapDefinition.ContainsCell(cell))
            {
                return FoundationChangeResult.Rejected(
                    cell,
                    FoundationChangeFailure.OutsideMap);
            }

            if (!constructionArea.IsEligible(cell))
            {
                return FoundationChangeResult.Rejected(
                    cell,
                    FoundationChangeFailure
                        .OutsideConstructionArea);
            }

            if (foundationState.HasFoundation(cell))
            {
                return FoundationChangeResult.Rejected(
                    cell,
                    FoundationChangeFailure.AlreadyExists);
            }

            return FoundationChangeResult.Success(cell);
        }


        /// <summary>
        /// Ensures that every possible requested cell contains a foundation.
        /// Existing foundations are preserved, duplicate cells are collapsed,
        /// and invalid cells are skipped.
        /// </summary>
        public FoundationEnsureResult TryEnsureFoundations(
            IReadOnlyList<GridPosition> cells)
        {
            if (cells == null)
            {
                throw new ArgumentNullException(
                    nameof(cells));
            }

            if (cells.Count == 0)
            {
                return FoundationEnsureResult.Rejected(
                    0,
                    0,
                    default,
                    FoundationChangeFailure.EmptyRequest);
            }

            HashSet<GridPosition> uniqueCells =
                new HashSet<GridPosition>();

            List<GridPosition> missingLegalCells =
                new List<GridPosition>();

            int alreadyExistingCount = 0;
            int skippedOutsideMapCount = 0;
            int skippedOutsideConstructionAreaCount = 0;

            for (int index = 0;
                 index < cells.Count;
                 index++)
            {
                GridPosition cell =
                    cells[index];

                if (!uniqueCells.Add(cell))
                {
                    continue;
                }

                if (foundationState.HasFoundation(cell))
                {
                    alreadyExistingCount++;
                    continue;
                }

                if (!mapDefinition.ContainsCell(cell))
                {
                    skippedOutsideMapCount++;
                    continue;
                }

                if (!constructionArea.IsEligible(cell))
                {
                    skippedOutsideConstructionAreaCount++;
                    continue;
                }

                missingLegalCells.Add(cell);
            }

            if (missingLegalCells.Count > 0
                && !foundationState.TryAddFoundations(
                    missingLegalCells))
            {
                return FoundationEnsureResult.Rejected(
                    cells.Count,
                    uniqueCells.Count,
                    missingLegalCells[0],
                    FoundationChangeFailure.AlreadyExists);
            }

            return FoundationEnsureResult.Success(
                cells.Count,
                uniqueCells.Count,
                missingLegalCells,
                alreadyExistingCount,
                skippedOutsideMapCount,
                skippedOutsideConstructionAreaCount);
        }


        /// <summary>
        /// Ensures that every requested cell contains no foundation.
        /// Removal does not require current construction eligibility so loaded,
        /// authored, or legacy foundations can be removed.
        /// </summary>
        public FoundationClearResult TryClearFoundations(
            IReadOnlyList<GridPosition> cells)
        {
            if (cells == null)
            {
                throw new ArgumentNullException(
                    nameof(cells));
            }

            if (cells.Count == 0)
            {
                return FoundationClearResult.Rejected(
                    0,
                    0,
                    default,
                    FoundationChangeFailure.EmptyRequest);
            }

            HashSet<GridPosition> uniqueCells =
                new HashSet<GridPosition>();

            List<GridPosition> existingCells =
                new List<GridPosition>();

            int alreadyEmptyCount = 0;

            for (int index = 0;
                 index < cells.Count;
                 index++)
            {
                GridPosition cell =
                    cells[index];

                if (!uniqueCells.Add(cell))
                {
                    continue;
                }

                if (foundationState.HasFoundation(cell))
                {
                    existingCells.Add(cell);
                }
                else
                {
                    alreadyEmptyCount++;
                }
            }

            FoundationRemovalValidation validation =
                removalValidator.ValidateRemoval(
                    existingCells);

            if (!validation.IsAllowed)
            {
                return FoundationClearResult.Rejected(
                    cells.Count,
                    uniqueCells.Count,
                    validation.BlockedCell,
                    FoundationChangeFailure
                        .SupportsConstruction);
            }

            if (existingCells.Count > 0
                && !foundationState.TryRemoveFoundations(
                    existingCells))
            {
                return FoundationClearResult.Rejected(
                    cells.Count,
                    uniqueCells.Count,
                    existingCells[0],
                    FoundationChangeFailure.NotFound);
            }

            return FoundationClearResult.Success(
                cells.Count,
                uniqueCells.Count,
                existingCells,
                alreadyEmptyCount);
        }


        /// <summary>
        /// Replays an exact previously committed foundation edit.
        /// History replay bypasses the current construction-area mask and is
        /// strict and atomic: every requested state change must match.
        /// </summary>
        public FoundationBatchChangeResult TryApplyEdit(
            FoundationEdit edit)
        {
            if (edit.IsEmpty)
            {
                return FoundationBatchChangeResult.Success(0);
            }

            switch (edit.Kind)
            {
                case FoundationEditKind.AddFoundations:
                    return TryApplyAddedEdit(edit);

                case FoundationEditKind.RemoveFoundations:
                    return TryApplyRemovedEdit(edit);

                default:
                    throw new InvalidOperationException(
                        $"Unsupported foundation edit kind: {edit.Kind}.");
            }
        }


        private FoundationBatchChangeResult TryApplyAddedEdit(
            FoundationEdit edit)
        {
            for (int index = 0;
                 index < edit.Count;
                 index++)
            {
                GridPosition cell =
                    edit.Cells[index];

                if (foundationState.HasFoundation(cell))
                {
                    return FoundationBatchChangeResult.Rejected(
                        edit.Count,
                        cell,
                        FoundationChangeFailure.AlreadyExists);
                }
            }

            if (!foundationState.TryAddFoundations(edit.Cells))
            {
                return FoundationBatchChangeResult.Rejected(
                    edit.Count,
                    edit.Cells[0],
                    FoundationChangeFailure.AlreadyExists);
            }

            return FoundationBatchChangeResult.Success(
                edit.Count);
        }


        private FoundationBatchChangeResult TryApplyRemovedEdit(
            FoundationEdit edit)
        {
            for (int index = 0;
                 index < edit.Count;
                 index++)
            {
                GridPosition cell =
                    edit.Cells[index];

                if (!foundationState.HasFoundation(cell))
                {
                    return FoundationBatchChangeResult.Rejected(
                        edit.Count,
                        cell,
                        FoundationChangeFailure.NotFound);
                }
            }

            FoundationRemovalValidation validation =
                removalValidator.ValidateRemoval(
                    edit.Cells);

            if (!validation.IsAllowed)
            {
                return FoundationBatchChangeResult.Rejected(
                    edit.Count,
                    validation.BlockedCell,
                    FoundationChangeFailure
                        .SupportsConstruction);
            }

            if (!foundationState.TryRemoveFoundations(edit.Cells))
            {
                return FoundationBatchChangeResult.Rejected(
                    edit.Count,
                    edit.Cells[0],
                    FoundationChangeFailure.NotFound);
            }

            return FoundationBatchChangeResult.Success(
                edit.Count);
        }
    }
}
