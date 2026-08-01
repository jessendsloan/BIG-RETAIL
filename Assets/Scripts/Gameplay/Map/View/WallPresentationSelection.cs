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
    /// Controls how structural walls are presented without changing the
    /// authoritative wall model.
    /// </summary>
    public enum WallDisplayMode
    {
        WallsUp = 0,
        Cutaway = 1,
        WallsDown = 2
    }


    /// <summary>
    /// Selects which authored height variant is used to draw a wall. Both
    /// variants represent the same structural wall and remain rendered.
    /// </summary>
    public enum WallPresentationHeight
    {
        Full = 0,
        Low = 1
    }


    /// <summary>
    /// Defines the conventional three-state wall-view cycle used by the UI.
    /// </summary>
    public static class WallDisplayModeCycle
    {
        public static WallDisplayMode Next(
            WallDisplayMode currentMode)
        {
            switch (currentMode)
            {
                case WallDisplayMode.WallsUp:
                    return WallDisplayMode.Cutaway;

                case WallDisplayMode.Cutaway:
                    return WallDisplayMode.WallsDown;

                case WallDisplayMode.WallsDown:
                    return WallDisplayMode.WallsUp;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(currentMode),
                        currentMode,
                        "Unknown wall display mode.");
            }
        }
    }


    /// <summary>
    /// Resolves which authored height variant should represent a structural
    /// wall. Cutaway lowers only the exterior wall between the viewer and a
    /// built foundation cell. Interior and far-side walls remain full height.
    /// </summary>
    public static class WallPresentationHeightResolver
    {
        public static WallPresentationHeight Resolve(
            WallDisplayMode displayMode,
            CellEdge logicalEdge,
            IsometricViewProjection projection,
            bool firstCellHasFoundation,
            bool secondCellHasFoundation)
        {
            if (projection == null)
            {
                throw new ArgumentNullException(
                    nameof(projection));
            }

            switch (displayMode)
            {
                case WallDisplayMode.WallsUp:
                    return WallPresentationHeight.Full;

                case WallDisplayMode.WallsDown:
                    return WallPresentationHeight.Low;

                case WallDisplayMode.Cutaway:
                    WallPresentationSelection selection =
                        WallPresentationSelector.Select(
                            logicalEdge,
                            projection);

                    bool viewerCellHasFoundation =
                        selection.ViewerFacingCell
                        == logicalEdge.FirstCell
                            ? firstCellHasFoundation
                            : secondCellHasFoundation;

                    bool farCellHasFoundation =
                        selection.ViewerFacingCell
                        == logicalEdge.FirstCell
                            ? secondCellHasFoundation
                            : firstCellHasFoundation;

                    return !viewerCellHasFoundation
                        && farCellHasFoundation
                            ? WallPresentationHeight.Low
                            : WallPresentationHeight.Full;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(displayMode),
                        displayMode,
                        "Unknown wall display mode.");
            }
        }
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
