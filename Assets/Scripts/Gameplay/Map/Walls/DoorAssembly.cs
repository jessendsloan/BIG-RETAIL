using System;
using System.Collections.Generic;
using BigRetail.Map.Domain;

namespace BigRetail.Map.Walls
{
    /// <summary>
    /// One placed door model occupying an ordered, contiguous wall run.
    /// The assembly is immutable; DoorAssemblyState owns its placement.
    /// </summary>
    public sealed class DoorAssembly
    {
        private readonly CellEdge[] edges;


        public DoorAssemblyId Id { get; }

        public DoorDefinition Definition { get; }

        public DoorDefinitionId DefinitionId =>
            Definition.Id;

        public int SegmentCount =>
            edges.Length;

        public IReadOnlyList<CellEdge> Edges =>
            edges;

        public CellEdge StartEdge =>
            edges[0];

        public CellEdge EndEdge =>
            edges[edges.Length - 1];


        internal DoorAssembly(
            DoorAssemblyId id,
            DoorDefinition definition,
            IReadOnlyList<CellEdge> edges)
        {
            if (!id.IsValid)
            {
                throw new ArgumentException(
                    "A door assembly requires a valid ID.",
                    nameof(id));
            }

            Definition =
                definition
                ?? throw new ArgumentNullException(
                    nameof(definition));

            if (edges == null)
            {
                throw new ArgumentNullException(
                    nameof(edges));
            }

            if (edges.Count != definition.SegmentCount)
            {
                throw new ArgumentException(
                    "A door assembly must contain the number of wall "
                    + "segments required by its definition.",
                    nameof(edges));
            }

            this.edges =
                new CellEdge[edges.Count];

            for (int index = 0;
                 index < edges.Count;
                 index++)
            {
                this.edges[index] = edges[index];
            }

            Id = id;
        }


        public CellEdge GetEdge(
            int segmentIndex)
        {
            if (segmentIndex < 0
                || segmentIndex >= edges.Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(segmentIndex));
            }

            return edges[segmentIndex];
        }

        public bool ContainsEdge(
            CellEdge edge)
        {
            return TryGetSegmentIndex(
                edge,
                out _);
        }

        public bool IsPassageEdge(
            CellEdge edge)
        {
            return TryGetSegmentIndex(
                       edge,
                       out int segmentIndex)
                && Definition.IsPassageSegment(segmentIndex);
        }

        public bool TryGetSegmentIndex(
            CellEdge edge,
            out int segmentIndex)
        {
            for (int index = 0;
                 index < edges.Length;
                 index++)
            {
                if (edges[index] == edge)
                {
                    segmentIndex = index;
                    return true;
                }
            }

            segmentIndex = -1;
            return false;
        }
    }
}
