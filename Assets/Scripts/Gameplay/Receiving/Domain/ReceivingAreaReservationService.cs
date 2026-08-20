using System;
using System.Collections.Generic;
using BigRetail.Map.Domain;

namespace BigRetail.Receiving.Domain
{
    /// <summary>
    /// Keeps ready supplier orders assigned to stable, usable Receiving cells.
    /// Orders that do not fit remain unassigned until capacity is available.
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

            HashSet<long> seenReadyOrders =
                new HashSet<long>();
            Dictionary<long, GridPosition> nextReservations =
                new Dictionary<long, GridPosition>();
            HashSet<GridPosition> occupiedCells =
                new HashSet<GridPosition>();

            for (int index = 0;
                 index < readyOrderNumbers.Count;
                 index++)
            {
                long orderNumber = readyOrderNumbers[index];

                if (!seenReadyOrders.Add(orderNumber)
                    || !state.TryGetReservation(
                        orderNumber,
                        out GridPosition cell)
                    || !state.Contains(cell)
                    || !isCellUsable(cell)
                    || !occupiedCells.Add(cell))
                {
                    continue;
                }

                nextReservations.Add(orderNumber, cell);
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
                 index < readyOrderNumbers.Count
                 && nextCellIndex < availableCells.Count;
                 index++)
            {
                long orderNumber = readyOrderNumbers[index];

                if (nextReservations.ContainsKey(orderNumber)
                    || !seenReadyOrders.Contains(orderNumber))
                {
                    continue;
                }

                GridPosition cell = availableCells[nextCellIndex++];
                nextReservations.Add(orderNumber, cell);
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
