using System;
using BigRetail.Map.Domain;

namespace BigRetail.Map.View
{
    /// <summary>
    /// Converts canonical logical cells and edges to one discrete
    /// isometric presentation and back again.
    ///
    /// This class contains no Unity or simulation state. It is the one
    /// mathematical authority for view rotation.
    /// </summary>
    public sealed class IsometricViewProjection
    {
        public IsometricMapFootprint Footprint { get; }

        public IsometricViewOrientation Orientation { get; }

        public int DisplayWidth =>
            Orientation.IsQuarterTurn()
                ? Footprint.Height
                : Footprint.Width;

        public int DisplayHeight =>
            Orientation.IsQuarterTurn()
                ? Footprint.Width
                : Footprint.Height;

        public int DisplayMinimumX =>
            Footprint.MinimumX;

        public int DisplayMinimumY =>
            Footprint.MinimumY;

        public int DisplayMaximumX =>
            DisplayMinimumX + DisplayWidth - 1;

        public int DisplayMaximumY =>
            DisplayMinimumY + DisplayHeight - 1;

        public IsometricViewProjection(
            IsometricMapFootprint footprint,
            IsometricViewOrientation orientation)
        {
            int orientationValue =
                (int)orientation;

            if (orientationValue
                    < (int)IsometricViewOrientation.North
                || orientationValue
                    > (int)IsometricViewOrientation.West)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(orientation),
                    orientation,
                    "Unsupported isometric-view orientation.");
            }

            Footprint = footprint;
            Orientation = orientation;
        }

        public IsometricViewProjection WithOrientation(
            IsometricViewOrientation orientation)
        {
            return new IsometricViewProjection(
                Footprint,
                orientation);
        }

        public GridPosition ToDisplayCell(
            GridPosition logicalCell)
        {
            int normalizedX =
                logicalCell.X
                - Footprint.MinimumX;

            int normalizedY =
                logicalCell.Y
                - Footprint.MinimumY;

            int displayX;
            int displayY;

            switch (Orientation)
            {
                case IsometricViewOrientation.North:
                    displayX =
                        normalizedX;

                    displayY =
                        normalizedY;
                    break;

                case IsometricViewOrientation.East:
                    displayX =
                        normalizedY;

                    displayY =
                        Footprint.Width
                        - 1
                        - normalizedX;
                    break;

                case IsometricViewOrientation.South:
                    displayX =
                        Footprint.Width
                        - 1
                        - normalizedX;

                    displayY =
                        Footprint.Height
                        - 1
                        - normalizedY;
                    break;

                case IsometricViewOrientation.West:
                    displayX =
                        Footprint.Height
                        - 1
                        - normalizedY;

                    displayY =
                        normalizedX;
                    break;

                default:
                    throw new InvalidOperationException(
                        "Unsupported isometric-view orientation.");
            }

            return new GridPosition(
                DisplayMinimumX + displayX,
                DisplayMinimumY + displayY,
                logicalCell.Level);
        }

        public GridPosition ToLogicalCell(
            GridPosition displayCell)
        {
            int displayX =
                displayCell.X
                - DisplayMinimumX;

            int displayY =
                displayCell.Y
                - DisplayMinimumY;

            int normalizedX;
            int normalizedY;

            switch (Orientation)
            {
                case IsometricViewOrientation.North:
                    normalizedX =
                        displayX;

                    normalizedY =
                        displayY;
                    break;

                case IsometricViewOrientation.East:
                    normalizedX =
                        Footprint.Width
                        - 1
                        - displayY;

                    normalizedY =
                        displayX;
                    break;

                case IsometricViewOrientation.South:
                    normalizedX =
                        Footprint.Width
                        - 1
                        - displayX;

                    normalizedY =
                        Footprint.Height
                        - 1
                        - displayY;
                    break;

                case IsometricViewOrientation.West:
                    normalizedX =
                        displayY;

                    normalizedY =
                        Footprint.Height
                        - 1
                        - displayX;
                    break;

                default:
                    throw new InvalidOperationException(
                        "Unsupported isometric-view orientation.");
            }

            return new GridPosition(
                Footprint.MinimumX + normalizedX,
                Footprint.MinimumY + normalizedY,
                displayCell.Level);
        }

        public CellEdge ToDisplayEdge(
            CellEdge logicalEdge)
        {
            GridPosition displayFirstCell =
                ToDisplayCell(
                    logicalEdge.FirstCell);

            GridPosition displaySecondCell =
                ToDisplayCell(
                    logicalEdge.SecondCell);

            return CreateEdgeBetween(
                displayFirstCell,
                displaySecondCell);
        }

        /// <summary>
        /// Returns the logical cell on the side of the edge closest to
        /// the current viewpoint. This is the stable contract future
        /// two-sided wall finishes and cutaway rules can consume.
        /// </summary>
        public GridPosition GetViewerFacingCell(
            CellEdge logicalEdge)
        {
            GridPosition displayFirstCell =
                ToDisplayCell(
                    logicalEdge.FirstCell);

            GridPosition displaySecondCell =
                ToDisplayCell(
                    logicalEdge.SecondCell);

            int firstDepth =
                displayFirstCell.X
                + displayFirstCell.Y;

            int secondDepth =
                displaySecondCell.X
                + displaySecondCell.Y;

            return firstDepth <= secondDepth
                ? logicalEdge.FirstCell
                : logicalEdge.SecondCell;
        }

        private static CellEdge CreateEdgeBetween(
            GridPosition firstCell,
            GridPosition secondCell)
        {
            if (firstCell.Level != secondCell.Level)
            {
                throw new InvalidOperationException(
                    "A displayed CellEdge cannot span logical levels.");
            }

            int xDifference =
                secondCell.X
                - firstCell.X;

            int yDifference =
                secondCell.Y
                - firstCell.Y;

            CellEdgeDirection direction;

            if (xDifference == 1
                && yDifference == 0)
            {
                direction =
                    CellEdgeDirection.NorthEast;
            }
            else if (xDifference == -1
                && yDifference == 0)
            {
                direction =
                    CellEdgeDirection.SouthWest;
            }
            else if (xDifference == 0
                && yDifference == 1)
            {
                direction =
                    CellEdgeDirection.NorthWest;
            }
            else if (xDifference == 0
                && yDifference == -1)
            {
                direction =
                    CellEdgeDirection.SouthEast;
            }
            else
            {
                throw new InvalidOperationException(
                    "A CellEdge projection must preserve adjacency.");
            }

            return new CellEdge(
                firstCell,
                direction);
        }
    }
}
