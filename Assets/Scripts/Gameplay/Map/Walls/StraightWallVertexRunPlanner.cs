using System;
using BigRetail.Map.Domain;

namespace BigRetail.Map.Walls
{
    /// <summary>
    /// Produces an ordered wall run between two axis-aligned grid vertices.
    ///
    /// Selecting vertices makes the player choose wall endpoints directly.
    /// The resulting CellEdges remain the structural wall identities consumed
    /// by WallState and WallConstructionService.
    /// </summary>
    public static class StraightWallVertexRunPlanner
    {
        public static WallVertexRunPlanResult Plan(
            GridVertex startVertex,
            GridVertex endVertex)
        {
            if (startVertex.Level != endVertex.Level)
            {
                return WallVertexRunPlanResult.Rejected(
                    startVertex,
                    endVertex,
                    WallVertexRunPlanFailure.DifferentLevel);
            }

            if (startVertex == endVertex)
            {
                return WallVertexRunPlanResult.Rejected(
                    startVertex,
                    endVertex,
                    WallVertexRunPlanFailure.SameVertex);
            }

            if (startVertex.X == endVertex.X)
            {
                return CreateRun(
                    startVertex,
                    endVertex,
                    changeX: false);
            }

            if (startVertex.Y == endVertex.Y)
            {
                return CreateRun(
                    startVertex,
                    endVertex,
                    changeX: true);
            }

            return WallVertexRunPlanResult.Rejected(
                startVertex,
                endVertex,
                WallVertexRunPlanFailure.NotAxisAligned);
        }


        private static WallVertexRunPlanResult CreateRun(
            GridVertex startVertex,
            GridVertex endVertex,
            bool changeX)
        {
            int startIndex =
                changeX
                    ? startVertex.X
                    : startVertex.Y;

            int endIndex =
                changeX
                    ? endVertex.X
                    : endVertex.Y;

            int difference =
                endIndex - startIndex;

            int step =
                difference < 0
                    ? -1
                    : 1;

            int segmentCount =
                Math.Abs(difference);

            GridVertex[] vertices =
                new GridVertex[segmentCount + 1];

            CellEdge[] edges =
                new CellEdge[segmentCount];

            for (int index = 0;
                 index < vertices.Length;
                 index++)
            {
                int currentIndex =
                    startIndex + index * step;

                vertices[index] =
                    changeX
                        ? new GridVertex(
                            currentIndex,
                            startVertex.Y,
                            startVertex.Level)
                        : new GridVertex(
                            startVertex.X,
                            currentIndex,
                            startVertex.Level);

                if (index == 0)
                {
                    continue;
                }

                edges[index - 1] =
                    new CellEdge(
                        vertices[index - 1],
                        vertices[index]);
            }

            return WallVertexRunPlanResult.Success(
                startVertex,
                endVertex,
                vertices,
                edges);
        }
    }
}
