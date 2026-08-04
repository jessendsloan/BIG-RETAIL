using System;
using BigRetail.Map.Domain;

namespace BigRetail.Map.Walls
{
    /// <summary>
    /// Expands one wall segment beneath the construction pointer into the
    /// complete straight span required by a door definition.
    ///
    /// Odd-width doors remain centered on the hovered wall segment. Even-
    /// width doors remain centered on one of that segment's two vertices.
    /// </summary>
    public static class DoorPlacementSpanPlanner
    {
        public static WallVertexRunPlanResult Plan(
            CellEdge hoveredEdge,
            GridVertex preferredCenterVertex,
            int segmentCount)
        {
            if (segmentCount < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(segmentCount),
                    segmentCount,
                    "A door placement span requires at least one segment.");
            }

            GridVertex firstVertex =
                hoveredEdge.FirstVertex;

            GridVertex secondVertex =
                hoveredEdge.SecondVertex;

            int xStep =
                secondVertex.X - firstVertex.X;

            int yStep =
                secondVertex.Y - firstVertex.Y;

            int halfWidth =
                segmentCount / 2;

            GridVertex startVertex;
            GridVertex endVertex;

            if (segmentCount % 2 == 0)
            {
                if (!hoveredEdge.TouchesVertex(
                        preferredCenterVertex))
                {
                    throw new ArgumentException(
                        "An even-width door must be centered on one of the "
                        + "hovered edge's vertices.",
                        nameof(preferredCenterVertex));
                }

                startVertex =
                    preferredCenterVertex.Offset(
                        -xStep * halfWidth,
                        -yStep * halfWidth);

                endVertex =
                    preferredCenterVertex.Offset(
                        xStep * halfWidth,
                        yStep * halfWidth);
            }
            else
            {
                startVertex =
                    firstVertex.Offset(
                        -xStep * halfWidth,
                        -yStep * halfWidth);

                endVertex =
                    secondVertex.Offset(
                        xStep * halfWidth,
                        yStep * halfWidth);
            }

            return StraightWallVertexRunPlanner.Plan(
                startVertex,
                endVertex);
        }
    }
}
