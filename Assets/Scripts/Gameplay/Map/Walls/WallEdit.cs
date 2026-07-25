using System;
using System.Collections.Generic;
using BigRetail.Map.Domain;

namespace BigRetail.Map.Walls
{
    /// <summary>
    /// Records an exact wall-state mutation.
    ///
    /// This is not the player's original request. It contains only
    /// the edges that were actually changed by that request.
    /// </summary>
    public readonly struct WallEdit
    {
        private readonly CellEdge[] edges;


        public WallEditKind Kind { get; }

        public IReadOnlyList<CellEdge> Edges =>
            edges ?? Array.Empty<CellEdge>();

        public int Count =>
            edges?.Length ?? 0;

        public bool IsEmpty =>
            Count == 0;


        private WallEdit(
            WallEditKind kind,
            CellEdge[] edges)
        {
            Kind = kind;
            this.edges = edges;
        }


        public static WallEdit AddWalls(
            IReadOnlyList<CellEdge> edges)
        {
            return Create(
                WallEditKind.AddWalls,
                edges);
        }


        public static WallEdit RemoveWalls(
            IReadOnlyList<CellEdge> edges)
        {
            return Create(
                WallEditKind.RemoveWalls,
                edges);
        }


        public WallEdit Inverse()
        {
            if (IsEmpty)
            {
                return default;
            }

            WallEditKind inverseKind =
                Kind == WallEditKind.AddWalls
                    ? WallEditKind.RemoveWalls
                    : WallEditKind.AddWalls;

            // The edge array is private and treated as immutable,
            // so the inverse can safely share it.
            return new WallEdit(
                inverseKind,
                edges);
        }


        private static WallEdit Create(
            WallEditKind kind,
            IReadOnlyList<CellEdge> sourceEdges)
        {
            if (sourceEdges == null)
            {
                throw new ArgumentNullException(
                    nameof(sourceEdges));
            }

            if (sourceEdges.Count == 0)
            {
                return default;
            }

            CellEdge[] copiedEdges =
                new CellEdge[sourceEdges.Count];

            HashSet<CellEdge> uniqueEdges =
                new HashSet<CellEdge>();

            for (int index = 0;
                 index < sourceEdges.Count;
                 index++)
            {
                CellEdge edge =
                    sourceEdges[index];

                if (!uniqueEdges.Add(edge))
                {
                    throw new ArgumentException(
                        $"A WallEdit cannot contain duplicate edge " +
                        $"{edge}.",
                        nameof(sourceEdges));
                }

                copiedEdges[index] =
                    edge;
            }

            return new WallEdit(
                kind,
                copiedEdges);
        }


        public override string ToString()
        {
            return IsEmpty
                ? "Empty wall edit."
                : $"{Kind}: {Count} wall edge(s).";
        }
    }
}