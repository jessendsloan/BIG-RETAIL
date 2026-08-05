using System;
using System.Collections.Generic;
using BigRetail.Map.Domain;

namespace BigRetail.Map.Foundations
{
    /// <summary>
    /// Stores the cells that currently contain constructed foundations.
    /// Construction legality belongs to FoundationConstructionService.
    /// </summary>
    public sealed class FoundationState
    {
        private readonly HashSet<GridPosition> foundations;

        private bool isPublishingChanges;


        public int FoundationCount =>
            foundations.Count;

        public event Action<GridPosition> FoundationAdded;

        public event Action<GridPosition> FoundationRemoved;


        public FoundationState(
            IEnumerable<GridPosition> initialFoundations = null)
        {
            foundations =
                initialFoundations == null
                    ? new HashSet<GridPosition>()
                    : new HashSet<GridPosition>(
                        initialFoundations);
        }


        public bool HasFoundation(
            GridPosition cell)
        {
            return foundations.Contains(cell);
        }


        public IEnumerable<GridPosition> EnumerateFoundations()
        {
            foreach (GridPosition foundation in foundations)
            {
                yield return foundation;
            }
        }


        internal bool TryAddFoundations(
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

                if (foundations.Contains(cell)
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
                    foundations.Add(
                        cells[index]);

                if (added)
                {
                    continue;
                }

                for (int rollbackIndex = 0;
                     rollbackIndex < index;
                     rollbackIndex++)
                {
                    foundations.Remove(
                        cells[rollbackIndex]);
                }

                return false;
            }

            PublishFoundationsAdded(cells);
            return true;
        }


        internal bool TryRemoveFoundations(
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

                if (!foundations.Contains(cell)
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
                    foundations.Remove(
                        cells[index]);

                if (removed)
                {
                    continue;
                }

                for (int rollbackIndex = 0;
                     rollbackIndex < index;
                     rollbackIndex++)
                {
                    foundations.Add(
                        cells[rollbackIndex]);
                }

                return false;
            }

            PublishFoundationsRemoved(cells);
            return true;
        }


        private void PublishFoundationsAdded(
            IReadOnlyList<GridPosition> cells)
        {
            isPublishingChanges = true;

            try
            {
                for (int index = 0;
                     index < cells.Count;
                     index++)
                {
                    FoundationAdded?.Invoke(
                        cells[index]);
                }
            }
            finally
            {
                isPublishingChanges = false;
            }
        }


        private void PublishFoundationsRemoved(
            IReadOnlyList<GridPosition> cells)
        {
            isPublishingChanges = true;

            try
            {
                for (int index = 0;
                     index < cells.Count;
                     index++)
                {
                    FoundationRemoved?.Invoke(
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
