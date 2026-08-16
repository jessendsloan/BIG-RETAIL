using System;
using System.Collections.Generic;
using BigRetail.Map.Construction;
using BigRetail.Map.Domain;

namespace BigRetail.Map.Sidewalks
{
    /// <summary>
    /// Owns sidewalk construction rules. Sidewalks need no foundation, but
    /// they reserve their cells against future structural construction.
    /// </summary>
    public sealed class SidewalkConstructionService :
        ISidewalkOccupancyQuery,
        ISidewalkWalkabilityQuery
    {
        private readonly GridMapDefinition mapDefinition;

        private readonly ConstructionAreaDefinition constructionArea;

        private readonly SidewalkState sidewalkState;

        private readonly IFoundationSupportQuery foundationSupport;


        public SidewalkConstructionService(
            GridMapDefinition mapDefinition,
            ConstructionAreaDefinition constructionArea,
            SidewalkState sidewalkState,
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

            this.sidewalkState =
                sidewalkState
                ?? throw new ArgumentNullException(
                    nameof(sidewalkState));

            this.foundationSupport =
                foundationSupport
                ?? throw new ArgumentNullException(
                    nameof(foundationSupport));
        }


        public bool HasSidewalk(GridPosition cell)
        {
            return sidewalkState.HasSidewalk(cell);
        }


        public bool IsSidewalkWalkable(GridPosition cell)
        {
            return mapDefinition.ContainsCell(cell)
                && sidewalkState.HasSidewalk(cell);
        }


        public SidewalkChangeResult EvaluatePlacement(
            GridPosition cell)
        {
            if (!mapDefinition.ContainsCell(cell))
            {
                return SidewalkChangeResult.Rejected(
                    cell,
                    SidewalkChangeFailure.OutsideMap);
            }

            if (!constructionArea.IsEligible(cell))
            {
                return SidewalkChangeResult.Rejected(
                    cell,
                    SidewalkChangeFailure.OutsideConstructionArea);
            }

            if (sidewalkState.HasSidewalk(cell))
            {
                return SidewalkChangeResult.Rejected(
                    cell,
                    SidewalkChangeFailure.AlreadyExists);
            }

            if (foundationSupport.HasFoundation(cell))
            {
                return SidewalkChangeResult.Rejected(
                    cell,
                    SidewalkChangeFailure.FoundationOccupied);
            }

            return SidewalkChangeResult.Success(cell);
        }


        public SidewalkChangeResult EvaluateRemoval(
            GridPosition cell)
        {
            return sidewalkState.HasSidewalk(cell)
                ? SidewalkChangeResult.Success(cell)
                : SidewalkChangeResult.Rejected(
                    cell,
                    SidewalkChangeFailure.NotFound);
        }


        public SidewalkEnsureResult TryEnsureSidewalks(
            IReadOnlyList<GridPosition> cells)
        {
            if (cells == null)
            {
                throw new ArgumentNullException(nameof(cells));
            }

            if (cells.Count == 0)
            {
                return SidewalkEnsureResult.Rejected(
                    0,
                    0,
                    default,
                    SidewalkChangeFailure.EmptyRequest);
            }

            HashSet<GridPosition> uniqueCells =
                new HashSet<GridPosition>();

            List<GridPosition> legalCells =
                new List<GridPosition>();

            int alreadyExistingCount = 0;
            int skippedOutsideMapCount = 0;
            int skippedOutsideConstructionAreaCount = 0;
            int skippedFoundationCount = 0;

            for (int index = 0; index < cells.Count; index++)
            {
                GridPosition cell = cells[index];

                if (!uniqueCells.Add(cell))
                {
                    continue;
                }

                if (sidewalkState.HasSidewalk(cell))
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

                if (foundationSupport.HasFoundation(cell))
                {
                    skippedFoundationCount++;
                    continue;
                }

                legalCells.Add(cell);
            }

            if (legalCells.Count > 0
                && !sidewalkState.TryAddSidewalks(legalCells))
            {
                return SidewalkEnsureResult.Rejected(
                    cells.Count,
                    uniqueCells.Count,
                    legalCells[0],
                    SidewalkChangeFailure.AlreadyExists);
            }

            return SidewalkEnsureResult.Success(
                cells.Count,
                uniqueCells.Count,
                legalCells,
                alreadyExistingCount,
                skippedOutsideMapCount,
                skippedOutsideConstructionAreaCount,
                skippedFoundationCount);
        }


        public SidewalkClearResult TryClearSidewalks(
            IReadOnlyList<GridPosition> cells)
        {
            if (cells == null)
            {
                throw new ArgumentNullException(nameof(cells));
            }

            if (cells.Count == 0)
            {
                return SidewalkClearResult.Rejected(
                    0,
                    0,
                    default,
                    SidewalkChangeFailure.EmptyRequest);
            }

            HashSet<GridPosition> uniqueCells =
                new HashSet<GridPosition>();

            List<GridPosition> existingCells =
                new List<GridPosition>();

            int alreadyEmptyCount = 0;

            for (int index = 0; index < cells.Count; index++)
            {
                GridPosition cell = cells[index];

                if (!uniqueCells.Add(cell))
                {
                    continue;
                }

                if (sidewalkState.HasSidewalk(cell))
                {
                    existingCells.Add(cell);
                }
                else
                {
                    alreadyEmptyCount++;
                }
            }

            if (existingCells.Count > 0
                && !sidewalkState.TryRemoveSidewalks(existingCells))
            {
                return SidewalkClearResult.Rejected(
                    cells.Count,
                    uniqueCells.Count,
                    existingCells[0],
                    SidewalkChangeFailure.NotFound);
            }

            return SidewalkClearResult.Success(
                cells.Count,
                uniqueCells.Count,
                existingCells,
                alreadyEmptyCount);
        }


        public SidewalkBatchChangeResult TryApplyEdit(
            SidewalkEdit edit)
        {
            if (edit.IsEmpty)
            {
                return SidewalkBatchChangeResult.Success(0);
            }

            return edit.Kind == SidewalkEditKind.AddSidewalks
                ? TryApplyAddedEdit(edit)
                : TryApplyRemovedEdit(edit);
        }


        private SidewalkBatchChangeResult TryApplyAddedEdit(
            SidewalkEdit edit)
        {
            for (int index = 0; index < edit.Count; index++)
            {
                GridPosition cell = edit.Cells[index];

                if (sidewalkState.HasSidewalk(cell))
                {
                    return SidewalkBatchChangeResult.Rejected(
                        edit.Count,
                        cell,
                        SidewalkChangeFailure.AlreadyExists);
                }

                if (foundationSupport.HasFoundation(cell))
                {
                    return SidewalkBatchChangeResult.Rejected(
                        edit.Count,
                        cell,
                        SidewalkChangeFailure.FoundationOccupied);
                }
            }

            return sidewalkState.TryAddSidewalks(edit.Cells)
                ? SidewalkBatchChangeResult.Success(edit.Count)
                : SidewalkBatchChangeResult.Rejected(
                    edit.Count,
                    edit.Cells[0],
                    SidewalkChangeFailure.AlreadyExists);
        }


        private SidewalkBatchChangeResult TryApplyRemovedEdit(
            SidewalkEdit edit)
        {
            for (int index = 0; index < edit.Count; index++)
            {
                GridPosition cell = edit.Cells[index];

                if (!sidewalkState.HasSidewalk(cell))
                {
                    return SidewalkBatchChangeResult.Rejected(
                        edit.Count,
                        cell,
                        SidewalkChangeFailure.NotFound);
                }
            }

            return sidewalkState.TryRemoveSidewalks(edit.Cells)
                ? SidewalkBatchChangeResult.Success(edit.Count)
                : SidewalkBatchChangeResult.Rejected(
                    edit.Count,
                    edit.Cells[0],
                    SidewalkChangeFailure.NotFound);
        }
    }
}
