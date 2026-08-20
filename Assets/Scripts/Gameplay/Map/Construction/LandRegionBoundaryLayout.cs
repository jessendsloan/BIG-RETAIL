using System;
using System.Collections.Generic;
using BigRetail.Map.Domain;

namespace BigRetail.Map.Construction
{
    /// <summary>
    /// One cell-length visual boundary shared by two neighboring Lots on
    /// Mr. Big's Property.
    /// </summary>
    public readonly struct LandRegionBoundarySegment
    {
        public CellEdge Edge { get; }

        public LandRegionId FirstRegionId { get; }

        public LandRegionId SecondRegionId { get; }


        public LandRegionBoundarySegment(
            CellEdge edge,
            LandRegionId firstRegionId,
            LandRegionId secondRegionId)
        {
            Edge = edge;
            FirstRegionId = firstRegionId;
            SecondRegionId = secondRegionId;
        }


        /// <summary>
        /// The shared fence remains until the player owns both Lots.
        /// Boundaries between unowned Lots therefore continue to show the
        /// complete nine-Lot division of the Property.
        /// </summary>
        public bool ShouldDisplay(
            LandRegionOwnershipState ownership)
        {
            if (ownership == null)
            {
                throw new ArgumentNullException(
                    nameof(ownership));
            }

            return !ownership.IsOwned(FirstRegionId)
                || !ownership.IsOwned(SecondRegionId);
        }
    }


    /// <summary>
    /// Generates only the internal boundaries of the fixed 3x3 Lot layout.
    /// The outside edge belongs to the Property presentation, not to the
    /// fences that divide one purchasable Lot from another.
    /// </summary>
    public static class LandRegionBoundaryLayout
    {
        public const int InternalBoundarySegmentCount =
            ((LandRegionCatalog.RegionColumns - 1)
                * LandRegionCatalog.RegionRows
                * LandRegionDefinition.SideLength)
            + ((LandRegionCatalog.RegionRows - 1)
                * LandRegionCatalog.RegionColumns
                * LandRegionDefinition.SideLength);


        public static IEnumerable<LandRegionBoundarySegment>
            EnumerateSegments(
                LandRegionCatalog catalog)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(
                    nameof(catalog));
            }

            GridPosition propertyMinimum =
                catalog.PropertyMinimumCell;

            for (int boundaryColumn = 1;
                 boundaryColumn < LandRegionCatalog.RegionColumns;
                 boundaryColumn++)
            {
                int anchorX =
                    propertyMinimum.X
                    + (boundaryColumn * LandRegionDefinition.SideLength)
                    - 1;

                for (int row = 0;
                     row < LandRegionCatalog.RegionRows;
                     row++)
                {
                    LandRegionId firstRegion =
                        new LandRegionId(
                            boundaryColumn - 1,
                            row);

                    LandRegionId secondRegion =
                        new LandRegionId(
                            boundaryColumn,
                            row);

                    for (int offset = 0;
                         offset < LandRegionDefinition.SideLength;
                         offset++)
                    {
                        GridPosition anchorCell =
                            new GridPosition(
                                anchorX,
                                propertyMinimum.Y
                                    + (row
                                        * LandRegionDefinition.SideLength)
                                    + offset,
                                propertyMinimum.Level);

                        yield return new LandRegionBoundarySegment(
                            new CellEdge(
                                anchorCell,
                                CellEdgeDirection.NorthEast),
                            firstRegion,
                            secondRegion);
                    }
                }
            }

            for (int boundaryRow = 1;
                 boundaryRow < LandRegionCatalog.RegionRows;
                 boundaryRow++)
            {
                int anchorY =
                    propertyMinimum.Y
                    + (boundaryRow * LandRegionDefinition.SideLength)
                    - 1;

                for (int column = 0;
                     column < LandRegionCatalog.RegionColumns;
                     column++)
                {
                    LandRegionId firstRegion =
                        new LandRegionId(
                            column,
                            boundaryRow - 1);

                    LandRegionId secondRegion =
                        new LandRegionId(
                            column,
                            boundaryRow);

                    for (int offset = 0;
                         offset < LandRegionDefinition.SideLength;
                         offset++)
                    {
                        GridPosition anchorCell =
                            new GridPosition(
                                propertyMinimum.X
                                    + (column
                                        * LandRegionDefinition.SideLength)
                                    + offset,
                                anchorY,
                                propertyMinimum.Level);

                        yield return new LandRegionBoundarySegment(
                            new CellEdge(
                                anchorCell,
                                CellEdgeDirection.NorthWest),
                            firstRegion,
                            secondRegion);
                    }
                }
            }
        }
    }
}
