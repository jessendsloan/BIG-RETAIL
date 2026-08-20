using System;
using System.Collections.Generic;
using BigRetail.Map.Domain;

namespace BigRetail.Map.Sidewalks
{
    /// <summary>
    /// Stores the cells currently reserved as pedestrian sidewalk.
    /// </summary>
    public sealed class SidewalkState
    {
        private readonly HashSet<GridPosition> sidewalks;

        private bool isPublishingChanges;


        public int SidewalkCount => sidewalks.Count;

        public event Action<GridPosition> SidewalkAdded;

        public event Action<GridPosition> SidewalkRemoved;


        public SidewalkState(
            IEnumerable<GridPosition> initialSidewalks = null)
        {
            sidewalks =
                initialSidewalks == null
                    ? new HashSet<GridPosition>()
                    : new HashSet<GridPosition>(initialSidewalks);
        }


        public bool HasSidewalk(GridPosition cell)
        {
            return sidewalks.Contains(cell);
        }


        public IEnumerable<GridPosition> EnumerateSidewalks()
        {
            foreach (GridPosition cell in sidewalks)
            {
                yield return cell;
            }
        }


        internal bool TryAddSidewalks(
            IReadOnlyList<GridPosition> cells)
        {
            if (!CanMutate(cells, mustExist: false))
            {
                return false;
            }

            for (int index = 0; index < cells.Count; index++)
            {
                sidewalks.Add(cells[index]);
            }

            Publish(cells, SidewalkAdded);
            return true;
        }


        internal bool TryRemoveSidewalks(
            IReadOnlyList<GridPosition> cells)
        {
            if (!CanMutate(cells, mustExist: true))
            {
                return false;
            }

            for (int index = 0; index < cells.Count; index++)
            {
                sidewalks.Remove(cells[index]);
            }

            Publish(cells, SidewalkRemoved);
            return true;
        }


        private bool CanMutate(
            IReadOnlyList<GridPosition> cells,
            bool mustExist)
        {
            if (isPublishingChanges
                || cells == null
                || cells.Count == 0)
            {
                return false;
            }

            HashSet<GridPosition> requestedCells =
                new HashSet<GridPosition>();

            for (int index = 0; index < cells.Count; index++)
            {
                GridPosition cell = cells[index];

                if (!requestedCells.Add(cell)
                    || sidewalks.Contains(cell) != mustExist)
                {
                    return false;
                }
            }

            return true;
        }


        private void Publish(
            IReadOnlyList<GridPosition> cells,
            Action<GridPosition> changed)
        {
            isPublishingChanges = true;

            try
            {
                for (int index = 0; index < cells.Count; index++)
                {
                    changed?.Invoke(cells[index]);
                }
            }
            finally
            {
                isPublishingChanges = false;
            }
        }
    }
}
