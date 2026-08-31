using BigRetail.Map.Domain;

namespace BigRetail.Map.Unity.View
{
    /// <summary>
    /// Central numeric depth contract for every isometric world object.
    /// Smaller display-depth values are closer to the viewer and therefore
    /// receive larger sorting orders.
    /// </summary>
    public static class IsometricRenderOrderResolver
    {
        public const int WorldCellBaseOrder = 200;
        public const int DisplayDepthOrderStep = 2;


        public static int ResolveCell(GridPosition displayCell)
        {
            return ResolveCellDepth(
                displayCell.X + displayCell.Y);
        }


        public static int ResolveCellDepth(int displayDepth)
        {
            return WorldCellBaseOrder
                - displayDepth * DisplayDepthOrderStep;
        }


        /// <summary>
        /// Resolves the odd order reserved for the wall between one cell and
        /// the next farther cell.
        /// </summary>
        public static int ResolveFarBoundaryDepth(int displayDepth)
        {
            return ResolveCellDepth(displayDepth) - 1;
        }
    }
}
