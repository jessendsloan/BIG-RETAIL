using System;
using BigRetail.Map.Domain;

namespace BigRetail.Map.View
{
    /// <summary>
    /// Describes the two presentation choices needed to render one wall:
    /// which logical face is toward the viewer and which authored slope
    /// the displayed edge uses.
    /// </summary>
    public readonly struct WallPresentationSelection
    {
        public CellEdge DisplayEdge { get; }
        public GridPosition ViewerFacingCell { get; }
        public WallDisplaySlope DisplaySlope { get; }


        internal WallPresentationSelection(
            CellEdge displayEdge,
            GridPosition viewerFacingCell,
            WallDisplaySlope displaySlope)
        {
            DisplayEdge = displayEdge;
            ViewerFacingCell = viewerFacingCell;
            DisplaySlope = displaySlope;
        }
    }


    /// <summary>
    /// Art-facing names for the two screen-space wall axes.
    /// </summary>
    public enum WallDisplaySlope
    {
        RisingLeft = 0,
        RisingRight = 1
    }


    /// <summary>
    /// Converts the mathematical isometric projection into the small wall
    /// presentation contract consumed by Unity rendering.
    /// </summary>
    public static class WallPresentationSelector
    {
        public static WallPresentationSelection Select(
            CellEdge logicalEdge,
            IsometricViewProjection projection)
        {
            if (projection == null)
            {
                throw new ArgumentNullException(
                    nameof(projection));
            }

            CellEdge displayEdge =
                projection.ToDisplayEdge(
                    logicalEdge);

            GridPosition viewerFacingCell =
                projection.GetViewerFacingCell(
                    logicalEdge);

            return new WallPresentationSelection(
                displayEdge,
                viewerFacingCell,
                GetDisplaySlope(
                    displayEdge));
        }


        public static WallDisplaySlope GetDisplaySlope(
            CellEdge displayEdge)
        {
            switch (displayEdge.CanonicalDirection)
            {
                case CellEdgeDirection.NorthEast:
                    return WallDisplaySlope.RisingLeft;

                case CellEdgeDirection.NorthWest:
                    return WallDisplaySlope.RisingRight;

                default:
                    throw new InvalidOperationException(
                        "A normalized CellEdge must use "
                        + "NorthEast or NorthWest.");
            }
        }
    }
}
