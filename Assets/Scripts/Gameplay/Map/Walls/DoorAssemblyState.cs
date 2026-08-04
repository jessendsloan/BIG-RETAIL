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

        private bool isPublishingChanges;


        public int AssemblyCount =>
            assemblies.Count;

        public int OccupiedEdgeCount =>
            edgeAssignments.Count;


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

            PublishAssemblyRemoved(removedAssembly);
            return true;
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
