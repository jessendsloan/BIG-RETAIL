using System;
using System.Collections.Generic;
using BigRetail.Map.Domain;

namespace BigRetail.Map.Walls
{
    /// <summary>
    /// Owns placed door assemblies and their wall-edge occupancy.
    /// Geometry and wall-support validation belong to DoorConstructionService.
    /// </summary>
    public sealed class DoorAssemblyState
    {
        private readonly Dictionary<DoorAssemblyId, DoorAssembly>
            assemblies =
                new Dictionary<DoorAssemblyId, DoorAssembly>();

        private readonly Dictionary<CellEdge, DoorAssemblyId>
            edgeAssignments =
                new Dictionary<CellEdge, DoorAssemblyId>();

        private readonly Dictionary<GridPosition, int>
            passageCellReservations =
                new Dictionary<GridPosition, int>();

        private bool isPublishingChanges;


        public int AssemblyCount =>
            assemblies.Count;

        public int OccupiedEdgeCount =>
            edgeAssignments.Count;

        public int ReservedPassageCellCount =>
            passageCellReservations.Count;


        public event Action<DoorAssembly> AssemblyAdded;

        public event Action<DoorAssembly> AssemblyRemoved;


        public bool TryGetAssembly(
            DoorAssemblyId assemblyId,
            out DoorAssembly assembly)
        {
            return assemblies.TryGetValue(
                assemblyId,
                out assembly);
        }

        public bool TryGetAssemblyAtEdge(
            CellEdge edge,
            out DoorAssembly assembly)
        {
            if (!edgeAssignments.TryGetValue(
                    edge,
                    out DoorAssemblyId assemblyId))
            {
                assembly = null;
                return false;
            }

            return assemblies.TryGetValue(
                assemblyId,
                out assembly);
        }

        public IEnumerable<DoorAssembly> EnumerateAssemblies()
        {
            foreach (DoorAssembly assembly in assemblies.Values)
            {
                yield return assembly;
            }
        }

        /// <summary>
        /// Returns true when a placed door's passable opening serves this
        /// cell. Both cells touching each passage edge are reserved so
        /// fixtures cannot block either side of an entrance.
        /// </summary>
        public bool IsPassageCellReserved(
            GridPosition cell)
        {
            return passageCellReservations.ContainsKey(cell);
        }


        internal bool TryAddAssembly(
            DoorAssembly assembly)
        {
            if (isPublishingChanges
                || assembly == null
                || assemblies.ContainsKey(assembly.Id))
            {
                return false;
            }

            for (int index = 0;
                 index < assembly.SegmentCount;
                 index++)
            {
                if (edgeAssignments.ContainsKey(
                        assembly.GetEdge(index)))
                {
                    return false;
                }
            }

            assemblies.Add(
                assembly.Id,
                assembly);

            for (int index = 0;
                 index < assembly.SegmentCount;
                 index++)
            {
                edgeAssignments.Add(
                    assembly.GetEdge(index),
                    assembly.Id);
            }

            ReservePassageCells(assembly);

            PublishAssemblyAdded(assembly);
            return true;
        }

        internal bool TryRemoveAssembly(
            DoorAssemblyId assemblyId,
            out DoorAssembly removedAssembly)
        {
            if (isPublishingChanges
                || !assemblies.TryGetValue(
                    assemblyId,
                    out removedAssembly))
            {
                removedAssembly = null;
                return false;
            }

            assemblies.Remove(assemblyId);

            for (int index = 0;
                 index < removedAssembly.SegmentCount;
                 index++)
            {
                edgeAssignments.Remove(
                    removedAssembly.GetEdge(index));
            }

            ReleasePassageCells(removedAssembly);

            PublishAssemblyRemoved(removedAssembly);
            return true;
        }


        private void ReservePassageCells(
            DoorAssembly assembly)
        {
            for (int index = 0;
                 index < assembly.SegmentCount;
                 index++)
            {
                if (!assembly.Definition.IsPassageSegment(index))
                {
                    continue;
                }

                CellEdge edge = assembly.GetEdge(index);

                AddPassageCellReservation(edge.FirstCell);
                AddPassageCellReservation(edge.SecondCell);
            }
        }


        private void ReleasePassageCells(
            DoorAssembly assembly)
        {
            for (int index = 0;
                 index < assembly.SegmentCount;
                 index++)
            {
                if (!assembly.Definition.IsPassageSegment(index))
                {
                    continue;
                }

                CellEdge edge = assembly.GetEdge(index);

                RemovePassageCellReservation(edge.FirstCell);
                RemovePassageCellReservation(edge.SecondCell);
            }
        }


        private void AddPassageCellReservation(
            GridPosition cell)
        {
            passageCellReservations.TryGetValue(
                cell,
                out int reservationCount);

            passageCellReservations[cell] =
                reservationCount + 1;
        }


        private void RemovePassageCellReservation(
            GridPosition cell)
        {
            if (!passageCellReservations.TryGetValue(
                    cell,
                    out int reservationCount))
            {
                return;
            }

            if (reservationCount <= 1)
            {
                passageCellReservations.Remove(cell);
                return;
            }

            passageCellReservations[cell] =
                reservationCount - 1;
        }


        private void PublishAssemblyAdded(
            DoorAssembly assembly)
        {
            isPublishingChanges = true;

            try
            {
                AssemblyAdded?.Invoke(assembly);
            }
            finally
            {
                isPublishingChanges = false;
            }
        }

        private void PublishAssemblyRemoved(
            DoorAssembly assembly)
        {
            isPublishingChanges = true;

            try
            {
                AssemblyRemoved?.Invoke(assembly);
            }
            finally
            {
                isPublishingChanges = false;
            }
        }
    }
}
