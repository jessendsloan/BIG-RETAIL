using System;
using System.Collections.Generic;
using BigRetail.Map.Domain;

namespace BigRetail.Map.Walls
{
    /// <summary>
    /// Stores the walls that currently exist on a grid map.
    ///
    /// This class owns wall state only.
    /// Construction legality belongs to WallConstructionService.
    /// </summary>
    public sealed class WallState
    {
        private readonly HashSet<CellEdge> walls;

        private bool isPublishingChanges;


        public int WallCount =>
            walls.Count;


        public event Action<CellEdge> WallAdded;

        public event Action<CellEdge> WallRemoved;


        public WallState(
            IEnumerable<CellEdge> initialWalls = null)
        {
            walls =
                initialWalls == null
                    ? new HashSet<CellEdge>()
                    : new HashSet<CellEdge>(
                        initialWalls);
        }


        public bool HasWall(
            CellEdge edge)
        {
            return walls.Contains(edge);
        }


        public IEnumerable<CellEdge> EnumerateWalls()
        {
            foreach (CellEdge wall in walls)
            {
                yield return wall;
            }
        }


        internal bool TryAddWall(
            CellEdge edge)
        {
            if (isPublishingChanges
                || !walls.Add(edge))
            {
                return false;
            }

            PublishWallAdded(edge);

            return true;
        }


        /// <summary>
        /// Adds every supplied wall before publishing any events.
        ///
        /// If an edge already exists or appears twice in the request,
        /// nothing is added and no events are raised.
        /// </summary>
        internal bool TryAddWalls(
            IReadOnlyList<CellEdge> edges)
        {
            if (isPublishingChanges
                || edges == null
                || edges.Count == 0)
            {
                return false;
            }

            HashSet<CellEdge> requestedEdges =
                new HashSet<CellEdge>();

            for (int index = 0;
                 index < edges.Count;
                 index++)
            {
                CellEdge edge =
                    edges[index];

                if (walls.Contains(edge)
                    || !requestedEdges.Add(edge))
                {
                    return false;
                }
            }

            for (int index = 0;
                 index < edges.Count;
                 index++)
            {
                bool added =
                    walls.Add(
                        edges[index]);

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
                    walls.Remove(
                        edges[rollbackIndex]);
                }

                return false;
            }

            PublishWallsAdded(edges);

            return true;
        }


        internal bool TryRemoveWall(
            CellEdge edge)
        {
            if (isPublishingChanges
                || !walls.Remove(edge))
            {
                return false;
            }

            PublishWallRemoved(edge);

            return true;
        }


        /// <summary>
        /// Removes every supplied wall before publishing any events.
        ///
        /// If an edge is missing or appears twice in the request,
        /// nothing is removed and no events are raised.
        /// </summary>
        internal bool TryRemoveWalls(
            IReadOnlyList<CellEdge> edges)
        {
            if (isPublishingChanges
                || edges == null
                || edges.Count == 0)
            {
                return false;
            }

            HashSet<CellEdge> requestedEdges =
                new HashSet<CellEdge>();

            for (int index = 0;
                 index < edges.Count;
                 index++)
            {
                CellEdge edge =
                    edges[index];

                if (!walls.Contains(edge)
                    || !requestedEdges.Add(edge))
                {
                    return false;
                }
            }

            for (int index = 0;
                 index < edges.Count;
                 index++)
            {
                bool removed =
                    walls.Remove(
                        edges[index]);

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
                    walls.Add(
                        edges[rollbackIndex]);
                }

                return false;
            }

            PublishWallsRemoved(edges);

            return true;
        }


        private void PublishWallAdded(
            CellEdge edge)
        {
            isPublishingChanges = true;

            try
            {
                WallAdded?.Invoke(edge);
            }
            finally
            {
                isPublishingChanges = false;
            }
        }


        private void PublishWallsAdded(
            IReadOnlyList<CellEdge> edges)
        {
            isPublishingChanges = true;

            try
            {
                for (int index = 0;
                     index < edges.Count;
                     index++)
                {
                    WallAdded?.Invoke(
                        edges[index]);
                }
            }
            finally
            {
                isPublishingChanges = false;
            }
        }


        private void PublishWallRemoved(
            CellEdge edge)
        {
            isPublishingChanges = true;

            try
            {
                WallRemoved?.Invoke(edge);
            }
            finally
            {
                isPublishingChanges = false;
            }
        }


        private void PublishWallsRemoved(
            IReadOnlyList<CellEdge> edges)
        {
            isPublishingChanges = true;

            try
            {
                for (int index = 0;
                     index < edges.Count;
                     index++)
                {
                    WallRemoved?.Invoke(
                        edges[index]);
                }
            }
            finally
            {
                isPublishingChanges = false;
            }
        }
    }
}