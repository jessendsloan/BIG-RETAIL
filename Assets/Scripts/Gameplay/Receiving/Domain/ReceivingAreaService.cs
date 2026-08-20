using System;
using System.Collections.Generic;
using BigRetail.Map.Construction;
using BigRetail.Map.Domain;

namespace BigRetail.Receiving.Domain
{
    /// <summary>
    /// Validates and applies player Receiving Area designations.
    /// </summary>
    public sealed class ReceivingAreaService
    {
        private readonly GridMapDefinition mapDefinition;
        private readonly IConstructionCellEligibility constructionEligibility;
        private readonly IReceivingAreaSurfaceQuery surfaceQuery;
        private readonly ReceivingAreaState state;


        public ReceivingAreaService(
            GridMapDefinition mapDefinition,
            IConstructionCellEligibility constructionEligibility,
            IReceivingAreaSurfaceQuery surfaceQuery,
            ReceivingAreaState state)
        {
            this.mapDefinition = mapDefinition
                ?? throw new ArgumentNullException(nameof(mapDefinition));
            this.constructionEligibility = constructionEligibility
                ?? throw new ArgumentNullException(
                    nameof(constructionEligibility));
            this.surfaceQuery = surfaceQuery
                ?? throw new ArgumentNullException(nameof(surfaceQuery));
            this.state = state
                ?? throw new ArgumentNullException(nameof(state));
        }


        public ReceivingAreaChangeResult TryAddArea(
            IReadOnlyList<GridPosition> cells)
        {
            if (cells == null)
            {
                throw new ArgumentNullException(nameof(cells));
            }

            if (cells.Count == 0)
            {
                return ReceivingAreaChangeResult.Rejected(
                    ReceivingAreaChangeFailure.EmptyArea);
            }

            HashSet<GridPosition> uniqueCells =
                new HashSet<GridPosition>(cells);

            foreach (GridPosition cell in uniqueCells)
            {
                ReceivingAreaChangeFailure failure =
                    EvaluateCell(cell);

                if (failure != ReceivingAreaChangeFailure.None)
                {
                    return ReceivingAreaChangeResult.Rejected(
                        failure,
                        cell);
                }
            }

            return ReceivingAreaChangeResult.Success(
                state.AddCells(uniqueCells));
        }

        public ReceivingAreaChangeResult TryRemoveArea(
            IReadOnlyList<GridPosition> cells)
        {
            if (cells == null)
            {
                throw new ArgumentNullException(nameof(cells));
            }

            if (cells.Count == 0)
            {
                return ReceivingAreaChangeResult.Rejected(
                    ReceivingAreaChangeFailure.EmptyArea);
            }

            HashSet<GridPosition> removableCells =
                new HashSet<GridPosition>();

            for (int index = 0; index < cells.Count; index++)
            {
                GridPosition cell = cells[index];

                if (!state.Contains(cell))
                {
                    continue;
                }

                if (state.IsReserved(cell))
                {
                    return ReceivingAreaChangeResult.Rejected(
                        ReceivingAreaChangeFailure.OccupiedByDelivery,
                        cell);
                }

                removableCells.Add(cell);
            }

            return ReceivingAreaChangeResult.Success(
                state.RemoveCells(removableCells));
        }

        public ReceivingAreaChangeFailure EvaluateCell(
            GridPosition cell)
        {
            if (!mapDefinition.ContainsCell(cell))
            {
                return ReceivingAreaChangeFailure.OutsideMap;
            }

            if (!constructionEligibility.IsEligible(cell))
            {
                return ReceivingAreaChangeFailure
                    .OutsideOwnedProperty;
            }

            if (!surfaceQuery.HasFloor(cell))
            {
                return ReceivingAreaChangeFailure.MissingFloor;
            }

            if (surfaceQuery.IsObstructed(cell))
            {
                return ReceivingAreaChangeFailure.Obstructed;
            }

            return ReceivingAreaChangeFailure.None;
        }
    }
}
