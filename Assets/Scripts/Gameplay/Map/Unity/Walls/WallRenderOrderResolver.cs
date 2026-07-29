using BigRetail.Map.Domain;
using UnityEngine;

namespace BigRetail.Map.Unity.Walls
{
    /// <summary>
    /// Resolves SpriteRenderer order for wall presentation objects.
    ///
    /// The authored wall panels overlap in increasing display-depth order, so
    /// larger display coordinates render later than smaller coordinates. Pylon
    /// previews occupy a separate band while preserving the same depth order.
    /// </summary>
    public static class WallRenderOrderResolver
    {
        public const int WallBaseOrder = 200;
        public const int PylonBaseOrder = 300;


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
                + displayDepth;
        }


        public static int ResolvePylon(
            float displayDepth)
        {
            return PylonBaseOrder
                + Mathf.RoundToInt(
                    displayDepth);
        }
    }
}
