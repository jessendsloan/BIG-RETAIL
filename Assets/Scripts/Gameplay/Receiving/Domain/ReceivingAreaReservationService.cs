using System;
using System.Collections.Generic;
using BigRetail.Map.Domain;

namespace BigRetail.Receiving.Domain
{
    /// <summary>
    /// Keeps ready inbound loads assigned to stable, usable Receiving cells.
    /// Loads that do not fit remain unassigned until capacity is available.
    /// </summary>
    public sealed class ReceivingAreaReservationService
    {
        private readonly ReceivingAreaState state;
        private readonly Func<GridPosition, bool> isCellUsable;


        public ReceivingAreaReservationService(
            ReceivingAreaState state,
            Func<GridPosition, bool> isCellUsable)
        {
            this.state = state
                ?? throw new ArgumentNullException(nameof(state));
            this.isCellUsable = isCellUsable
                ?? throw new ArgumentNullException(nameof(isCellUsable));
        }


        public int Synchronize(
            IReadOnlyList<long> readyOrderNumbers)
        {
            if (readyOrderNumbers == null)
            {
                throw new ArgumentNullException(
                    nameof(readyOrderNumbers));
            }

            ReceivingLoadId[] loadIds =
                new ReceivingLoadId[readyOrderNumbers.Count];

            for (int index = 0;
                 index < readyOrderNumbers.Count;
                 index++)
            {
                loadIds[index] = ReceivingLoadId.SupplierOrder(
                    readyOrderNumbers[index]);
            }

            return Synchronize(loadIds);
        }

        public int Synchronize(
            IReadOnlyList<ReceivingLoadId> readyLoadIds)
        {
            if (readyLoadIds == null)
            {
                throw new ArgumentNullException(
                    nameof(readyLoadIds));
            }

            HashSet<ReceivingLoadId> seenReadyLoads =
                new HashSet<ReceivingLoadId>();
            Dictionary<ReceivingLoadId, GridPosition> nextReservations =
                new Dictionary<ReceivingLoadId, GridPosition>();
            HashSet<GridPosition> occupiedCells =
                new HashSet<GridPosition>();

            for (int index = 0;
                 index < readyLoadIds.Count;
                 index++)
            {
                ReceivingLoadId loadId = readyLoadIds[index];

                if (!loadId.IsValid
                    || !seenReadyLoads.Add(loadId)
                    || !state.TryGetReservation(
                        loadId,
                        out GridPosition cell)
                    || !state.Contains(cell)
                    || !isCellUsable(cell)
                    || !occupiedCells.Add(cell))
                {
                    continue;
                }

                nextReservations.Add(loadId, cell);
            }

            List<GridPosition> availableCells =
                new List<GridPosition>();

            foreach (GridPosition cell in state.EnumerateCells())
            {
                if (isCellUsable(cell)
                    && !occupiedCells.Contains(cell))
                {
                    availableCells.Add(cell);
                }
            }

            availableCells.Sort(CompareCells);
            int nextCellIndex = 0;

            for (int index = 0;
                 index < readyLoadIds.Count
                 && nextCellIndex < availableCells.Count;
                 index++)
            {
                ReceivingLoadId loadId = readyLoadIds[index];

                if (nextReservations.ContainsKey(loadId)
                    || !seenReadyLoads.Contains(loadId))
                {
                    continue;
                }

                GridPosition cell = availableCells[nextCellIndex++];
                nextReservations.Add(loadId, cell);
                occupiedCells.Add(cell);
            }

            state.ReplaceReservations(nextReservations);
            return nextReservations.Count;
        }


        private static int CompareCells(
            GridPosition left,
            GridPosition right)
        {
            int levelComparison = left.Level.CompareTo(right.Level);

            if (levelComparison != 0)
            {
                return levelComparison;
            }

            int yComparison = left.Y.CompareTo(right.Y);

            return yComparison != 0
                ? yComparison
                : left.X.CompareTo(right.X);
        }
    }
}
