using System;
using BigRetail.Map.Domain;
using UnityEngine;

namespace BigRetail.Map.Unity.Walls
{
    /// <summary>
    /// Resolves SpriteRenderer depth and equal-depth priority for wall
    /// presentation objects.
    ///
    /// Smaller display-depth values are closer to the viewer and therefore
    /// render later. Renderer priority is reserved for equal-depth seams, so
    /// one directional panel can tuck its extrusion beneath the other without
    /// disturbing whole-scene depth.
    /// </summary>
    public static class WallRenderOrderResolver
    {
        public const int WallBaseOrder = 200;
        public const int PylonBaseOrder = 300;

        public const int RisingRightPriority = 0;
        public const int RisingLeftPriority = 1;
        public const int AppearancePreviewPriorityOffset = 2;


        public static int ResolveWall(
            CellEdge displayEdge)
        {
            GridPosition anchor =
                displayEdge.AnchorCell;

            return ResolveWallDepth(
                anchor.X + anchor.Y);
        }


        public static int ResolveWallDepth(
            int displayDepth)
        {
            return WallBaseOrder
                - displayDepth;
        }


        public static int ResolveWallPriority(
            CellEdge displayEdge)
        {
            switch (displayEdge.CanonicalDirection)
            {
                case CellEdgeDirection.NorthEast:
                    return RisingLeftPriority;

                case CellEdgeDirection.NorthWest:
                    return RisingRightPriority;

                default:
                    throw new InvalidOperationException(
                        "A normalized display CellEdge must use "
                        + "NorthEast or NorthWest.");
            }
        }


        public static int ResolveAppearancePreviewPriority(
            CellEdge displayEdge)
        {
            return ResolveWallPriority(displayEdge)
                + AppearancePreviewPriorityOffset;
        }


        public static int ResolvePylon(
            float displayDepth)
        {
            return PylonBaseOrder
                - Mathf.RoundToInt(
                    displayDepth);
        }
    }
}
