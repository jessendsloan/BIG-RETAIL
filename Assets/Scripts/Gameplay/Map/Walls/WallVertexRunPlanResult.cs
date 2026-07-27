using System;
using System.Collections.Generic;
using BigRetail.Map.Domain;

namespace BigRetail.Map.Walls
{
    /// <summary>
    /// Describes one straight wall run selected from a starting grid vertex
    /// to an ending grid vertex.
    ///
    /// Vertices include both endpoints. Edges contain the wall segments
    /// between each consecutive pair of vertices.
    /// </summary>
    public readonly struct WallVertexRunPlanResult
    {
        private readonly GridVertex[] vertices;
        private readonly CellEdge[] edges;


        public bool Succeeded { get; }

        public WallVertexRunPlanFailure Failure { get; }

        public GridVertex StartVertex { get; }

        public GridVertex EndVertex { get; }

        public int VertexCount =>
            vertices?.Length ?? 0;

        public int SegmentCount =>
            edges?.Length ?? 0;

        public IReadOnlyList<GridVertex> Vertices =>
            vertices ?? Array.Empty<GridVertex>();

        public IReadOnlyList<CellEdge> Edges =>
            edges ?? Array.Empty<CellEdge>();


        private WallVertexRunPlanResult(
            bool succeeded,
            GridVertex startVertex,
            GridVertex endVertex,
            WallVertexRunPlanFailure failure,
            GridVertex[] vertices,
            CellEdge[] edges)
        {
            Succeeded = succeeded;
            StartVertex = startVertex;
            EndVertex = endVertex;
            Failure = failure;

            this.vertices =
                vertices ?? Array.Empty<GridVertex>();

            this.edges =
                edges ?? Array.Empty<CellEdge>();
        }


        public static WallVertexRunPlanResult Success(
            GridVertex startVertex,
            GridVertex endVertex,
            GridVertex[] vertices,
            CellEdge[] edges)
        {
            if (vertices == null)
            {
                throw new ArgumentNullException(
                    nameof(vertices));
            }

            if (edges == null)
            {
                throw new ArgumentNullException(
                    nameof(edges));
            }

            if (vertices.Length < 2)
            {
                throw new ArgumentException(
                    "A successful vertex wall run requires at least "
                    + "two vertices.",
                    nameof(vertices));
            }

            if (edges.Length != vertices.Length - 1)
            {
                throw new ArgumentException(
                    "A vertex wall run requires exactly one fewer edge "
                    + "than vertices.",
                    nameof(edges));
            }

            return new WallVertexRunPlanResult(
                true,
                startVertex,
                endVertex,
                WallVertexRunPlanFailure.None,
                vertices,
                edges);
        }


        public static WallVertexRunPlanResult Rejected(
            GridVertex startVertex,
            GridVertex endVertex,
            WallVertexRunPlanFailure failure)
        {
            if (failure == WallVertexRunPlanFailure.None)
            {
                throw new ArgumentException(
                    "A rejected vertex wall run requires a failure reason.",
                    nameof(failure));
            }

            return new WallVertexRunPlanResult(
                false,
                startVertex,
                endVertex,
                failure,
                Array.Empty<GridVertex>(),
                Array.Empty<CellEdge>());
        }


        public override string ToString()
        {
            if (Succeeded)
            {
                return
                    $"Straight vertex wall run contains "
                    + $"{SegmentCount} segment(s) across "
                    + $"{VertexCount} vertices: "
                    + $"{StartVertex} to {EndVertex}.";
            }

            return
                $"Straight vertex wall run rejected: {Failure}. "
                + $"Start: {StartVertex}. End: {EndVertex}.";
        }
    }
}
