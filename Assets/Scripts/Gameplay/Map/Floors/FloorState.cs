using System;
using System.Collections.Generic;
using BigRetail.Map.Domain;

namespace BigRetail.Map.Floors
{
    /// <summary>
    /// Stores the cells that currently contain constructed floors.
    ///
    /// This class owns floor state only.
    /// Construction legality belongs to FloorConstructionService.
    /// </summary>
    public sealed class FloorState
    {
        private readonly HashSet<GridPosition> floors;

        private bool isPublishingChanges;


        public int FloorCount =>
            floors.Count;


        public event Action<GridPosition> FloorAdded;

        public event Action<GridPosition> FloorRemoved;


        public FloorState(
            IEnumerable<GridPosition> initialFloors = null)
        {
            floors =
                initialFloors == null
                    ? new HashSet<GridPosition>()
                    : new HashSet<GridPosition>(
                        initialFloors);
        }


        public bool HasFloor(
            GridPosition cell)
        {
            return floors.Contains(cell);
        }


        public IEnumerable<GridPosition> EnumerateFloors()
        {
            foreach (GridPosition floor in floors)
            {
                yield return floor;
            }
        }


        /// <summary>
        /// Adds every supplied floor before publishing any events.
        ///
        /// If a requested cell already contains a floor or appears
        /// twice in the collection, nothing is added.
        /// </summary>
        internal bool TryAddFloors(
            IReadOnlyList<GridPosition> cells)
        {
            if (isPublishingChanges
                || cells == null
                || cells.Count == 0)
            {
                return false;
            }

            HashSet<GridPosition> requestedCells =
                new HashSet<GridPosition>();

            for (int index = 0;
                 index < cells.Count;
                 index++)
            {
                GridPosition cell =
                    cells[index];

                if (floors.Contains(cell)
                    || !requestedCells.Add(cell))
                {
                    return false;
                }
            }

            for (int index = 0;
                 index < cells.Count;
                 index++)
            {
                bool added =
                    floors.Add(
                        cells[index]);

                if (added)
                {
                    continue;
                }

                // Defensive rollback. Validation above should make
                // this unreachable during ordinary execution.
                for (int rollbackIndex = 0;
                     rollbackIndex < index;
                     rollbackIndex++)
                {
                    floors.Remove(
                        cells[rollbackIndex]);
                }

                return false;
            }

            PublishFloorsAdded(cells);

            return true;
        }


        /// <summary>
        /// Removes every supplied floor before publishing any events.
        ///
        /// If a requested cell is already empty or appears twice in
        /// the collection, nothing is removed.
        /// </summary>
        internal bool TryRemoveFloors(
            IReadOnlyList<GridPosition> cells)
        {
            if (isPublishingChanges
                || cells == null
                || cells.Count == 0)
            {
                return false;
            }

            HashSet<GridPosition> requestedCells =
                new HashSet<GridPosition>();

            for (int index = 0;
                 index < cells.Count;
                 index++)
            {
                GridPosition cell =
                    cells[index];

                if (!floors.Contains(cell)
                    || !requestedCells.Add(cell))
                {
                    return false;
                }
            }

            for (int index = 0;
                 index < cells.Count;
                 index++)
            {
                bool removed =
                    floors.Remove(
                        cells[index]);

                if (removed)
                {
                    continue;
                }

                // Defensive rollback. Validation above should make
                // this unreachable during ordinary execution.
                for (int rollbackIndex = 0;
                     rollbackIndex < index;
                     rollbackIndex++)
                {
                    floors.Add(
                        cells[rollbackIndex]);
                }

                return false;
            }

            PublishFloorsRemoved(cells);

            return true;
        }


        private void PublishFloorsAdded(
            IReadOnlyList<GridPosition> cells)
        {
            isPublishingChanges = true;

            try
            {
                for (int index = 0;
                     index < cells.Count;
                     index++)
                {
                    FloorAdded?.Invoke(
                        cells[index]);
                }
            }
            finally
            {
                isPublishingChanges = false;
            }
        }


        private void PublishFloorsRemoved(
            IReadOnlyList<GridPosition> cells)
        {
            isPublishingChanges = true;

            try
            {
                for (int index = 0;
                     index < cells.Count;
                     index++)
                {
                    FloorRemoved?.Invoke(
                        cells[index]);
                }
            }
            finally
            {
                isPublishingChanges = false;
            }
        }
    }
}