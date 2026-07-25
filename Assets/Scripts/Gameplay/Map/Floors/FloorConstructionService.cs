using System;
using System.Collections.Generic;
using BigRetail.Map.Construction;
using BigRetail.Map.Domain;

namespace BigRetail.Map.Floors
{
    /// <summary>
    /// Evaluates and performs floor construction and removal requests.
    ///
    /// Player-facing operations are permissive:
    /// - Existing floors satisfy construction requests.
    /// - Empty cells satisfy removal requests.
    /// - Invalid construction cells are skipped.
    ///
    /// Accepted floor changes are committed to FloorState before
    /// presentation events are published.
    /// </summary>
    public sealed class FloorConstructionService
    {
        private readonly GridMapDefinition mapDefinition;
        private readonly ConstructionAreaDefinition constructionArea;
        private readonly FloorState floorState;


        public FloorConstructionService(
            GridMapDefinition mapDefinition,
            ConstructionAreaDefinition constructionArea,
            FloorState floorState)
        {
            this.mapDefinition =
                mapDefinition
                ?? throw new ArgumentNullException(
                    nameof(mapDefinition));

            this.constructionArea =
                constructionArea
                ?? throw new ArgumentNullException(
                    nameof(constructionArea));

            this.floorState =
                floorState
                ?? throw new ArgumentNullException(
                    nameof(floorState));
        }


        public bool HasFloor(
            GridPosition cell)
        {
            return floorState.HasFloor(cell);
        }


        /// <summary>
        /// Evaluates whether one brand-new floor could be constructed.
        ///
        /// This method does not modify FloorState.
        /// </summary>
        public FloorChangeResult EvaluatePlacement(
            GridPosition cell)
        {
            if (!mapDefinition.ContainsCell(cell))
            {
                return FloorChangeResult.Rejected(
                    cell,
                    FloorChangeFailure.OutsideMap);
            }

            if (!constructionArea.IsEligible(cell))
            {
                return FloorChangeResult.Rejected(
                    cell,
                    FloorChangeFailure
                        .OutsideConstructionArea);
            }

            if (floorState.HasFloor(cell))
            {
                return FloorChangeResult.Rejected(
                    cell,
                    FloorChangeFailure.AlreadyExists);
            }

            return FloorChangeResult.Success(cell);
        }


        /// <summary>
        /// Ensures that every possible requested cell contains a floor.
        ///
        /// Existing floors are preserved.
        /// Duplicate cells are collapsed.
        /// Invalid cells are skipped.
        /// Missing legal floors are added together before events fire.
        /// </summary>
        public FloorEnsureResult TryEnsureFloors(
            IReadOnlyList<GridPosition> cells)
        {
            if (cells == null)
            {
                throw new ArgumentNullException(
                    nameof(cells));
            }

            if (cells.Count == 0)
            {
                return FloorEnsureResult.Rejected(
                    0,
                    0,
                    default,
                    FloorChangeFailure.EmptyRequest);
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

                // An existing floor already satisfies the requested
                // final state, including loaded or legacy floors.
                if (floorState.HasFloor(cell))
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
                && !floorState.TryAddFloors(
                    missingLegalCells))
            {
                return FloorEnsureResult.Rejected(
                    cells.Count,
                    uniqueCells.Count,
                    missingLegalCells[0],
                    FloorChangeFailure.AlreadyExists);
            }

            return FloorEnsureResult.Success(
                cells.Count,
                uniqueCells.Count,
                missingLegalCells,
                alreadyExistingCount,
                skippedOutsideMapCount,
                skippedOutsideConstructionAreaCount);
        }


        /// <summary>
        /// Ensures that every requested cell contains no floor.
        ///
        /// Existing floors are removed together before events fire.
        /// Empty and duplicate cells are harmlessly skipped.
        ///
        /// Removal does not require construction eligibility because
        /// loaded, scenario-authored, or legacy floors may still need
        /// to be removed.
        /// </summary>
        public FloorClearResult TryClearFloors(
            IReadOnlyList<GridPosition> cells)
        {
            if (cells == null)
            {
                throw new ArgumentNullException(
                    nameof(cells));
            }

            if (cells.Count == 0)
            {
                return FloorClearResult.Rejected(
                    0,
                    0,
                    default,
                    FloorChangeFailure.EmptyRequest);
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

                if (floorState.HasFloor(cell))
                {
                    existingCells.Add(cell);
                }
                else
                {
                    alreadyEmptyCount++;
                }
            }

            if (existingCells.Count > 0
                && !floorState.TryRemoveFloors(
                    existingCells))
            {
                return FloorClearResult.Rejected(
                    cells.Count,
                    uniqueCells.Count,
                    existingCells[0],
                    FloorChangeFailure.NotFound);
            }

            return FloorClearResult.Success(
                cells.Count,
                uniqueCells.Count,
                existingCells,
                alreadyEmptyCount);
        }
    }
}