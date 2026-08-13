using System;
using BigRetail.Map.Domain;
using BigRetail.Map.View;
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

        public const int DisplayDepthOrderStep = 2;

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


        /// <summary>
        /// Wall art keeps its structural depth at every presentation height.
        /// The low sprite creates the cutaway through transparent pixels, so
        /// its opaque base must retain the same front/back relationship as a
        /// full wall.
        /// </summary>
        public static int ResolveWall(
            CellEdge displayEdge,
            WallPresentationHeight presentationHeight)
        {
            int structuralOrder =
                ResolveWall(displayEdge);

            switch (presentationHeight)
            {
                case WallPresentationHeight.Full:
                    return structuralOrder;

                case WallPresentationHeight.Low:
                    return structuralOrder;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(presentationHeight),
                        presentationHeight,
                        "Unsupported wall presentation height.");
            }
        }


        public static int ResolveWallDepth(
            int displayDepth)
        {
            return WallBaseOrder
                - displayDepth * DisplayDepthOrderStep
                - 1;
        }


        /// <summary>
        /// Places a cell occupant between the wall on its viewer-facing side
        /// and the wall on its far side. The doubled depth scale reserves the
        /// odd integer between neighboring cell centers for their shared wall.
        /// </summary>
        public static int ResolveCell(
            GridPosition displayCell)
        {
            int displayDepth =
                displayCell.X + displayCell.Y;

            return WallBaseOrder
                - displayDepth * DisplayDepthOrderStep;
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
                    displayDepth
                    * DisplayDepthOrderStep);
        }
    }
}
