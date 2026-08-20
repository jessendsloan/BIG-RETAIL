using System;
using System.Collections.Generic;
using BigRetail.Map.Domain;

namespace BigRetail.Receiving.Domain
{
    /// <summary>
    /// Owns the cells designated for Receiving and the supplier orders
    /// currently occupying those cells. Physical legality belongs to
    /// ReceivingAreaService.
    /// </summary>
    public sealed class ReceivingAreaState
    {
        private readonly HashSet<GridPosition> cells =
            new HashSet<GridPosition>();
        private readonly Dictionary<long, GridPosition> reservations =
            new Dictionary<long, GridPosition>();
        private readonly Dictionary<GridPosition, long> cellReservations =
            new Dictionary<GridPosition, long>();


        public int CellCount =>
            cells.Count;

        public int ReservationCount =>
            reservations.Count;

        public int AvailableCellCount =>
            Math.Max(0, CellCount - ReservationCount);


        public event Action AreaChanged;

        public event Action ReservationsChanged;


        public bool Contains(
            GridPosition cell)
        {
            return cells.Contains(cell);
        }

        public bool IsReserved(
            GridPosition cell)
        {
            return cellReservations.ContainsKey(cell);
        }

        public bool TryGetReservation(
            long orderNumber,
            out GridPosition cell)
        {
            return reservations.TryGetValue(orderNumber, out cell);
        }

        public bool TryGetOrderAt(
            GridPosition cell,
            out long orderNumber)
        {
            return cellReservations.TryGetValue(cell, out orderNumber);
        }

        public IEnumerable<GridPosition> EnumerateCells()
        {
            foreach (GridPosition cell in cells)
            {
                yield return cell;
            }
        }


        internal int AddCells(
            IEnumerable<GridPosition> requestedCells)
        {
            int addedCount = 0;

            foreach (GridPosition cell in requestedCells)
            {
                if (cells.Add(cell))
                {
                    addedCount++;
                }
            }

            if (addedCount > 0)
            {
                AreaChanged?.Invoke();
            }

            return addedCount;
        }

        internal int RemoveCells(
            IEnumerable<GridPosition> requestedCells)
        {
            int removedCount = 0;

            foreach (GridPosition cell in requestedCells)
            {
                if (cells.Remove(cell))
                {
                    removedCount++;
                }
            }

            if (removedCount > 0)
            {
                AreaChanged?.Invoke();
            }

            return removedCount;
        }

        internal void ReplaceReservations(
            IReadOnlyDictionary<long, GridPosition> nextReservations)
        {
            if (ReservationsMatch(nextReservations))
            {
                return;
            }

            reservations.Clear();
            cellReservations.Clear();

            foreach (
                KeyValuePair<long, GridPosition> pair
                in nextReservations)
            {
                reservations.Add(pair.Key, pair.Value);
                cellReservations.Add(pair.Value, pair.Key);
            }

            ReservationsChanged?.Invoke();
        }

        private bool ReservationsMatch(
            IReadOnlyDictionary<long, GridPosition> nextReservations)
        {
            if (nextReservations == null
                || nextReservations.Count != reservations.Count)
            {
                return false;
            }

            foreach (
                KeyValuePair<long, GridPosition> pair
                in nextReservations)
            {
                if (!reservations.TryGetValue(
                        pair.Key,
                        out GridPosition existingCell)
                    || existingCell != pair.Value)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
