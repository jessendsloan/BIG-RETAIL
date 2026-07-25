using System;
using BigRetail.Map.Domain;

namespace BigRetail.Map.Walls
{
    /// <summary>
    /// Produces an ordered, inclusive sequence of canonical CellEdges
    /// between two collinear wall edges.
    ///
    /// NorthEast edges form straight runs by changing Y.
    /// NorthWest edges form straight runs by changing X.
    ///
    /// This planner does not evaluate construction rules or modify state.
    /// </summary>
    public static class StraightWallRunPlanner
    {
        public static WallRunPlanResult Plan(
            CellEdge startEdge,
            CellEdge endEdge)
        {
            if (startEdge.AnchorCell.Level
                != endEdge.AnchorCell.Level)
            {
                return WallRunPlanResult.Rejected(
                    startEdge,
                    endEdge,
                    WallRunPlanFailure.DifferentLevel);
            }

            if (startEdge.CanonicalDirection
                != endEdge.CanonicalDirection)
            {
                return WallRunPlanResult.Rejected(
                    startEdge,
                    endEdge,
                    WallRunPlanFailure.DifferentDirection);
            }

            switch (startEdge.CanonicalDirection)
            {
                case CellEdgeDirection.NorthEast:
                    return PlanNorthEastRun(
                        startEdge,
                        endEdge);

                case CellEdgeDirection.NorthWest:
                    return PlanNorthWestRun(
                        startEdge,
                        endEdge);

                default:
                    throw new InvalidOperationException(
                        "A normalized CellEdge must use " +
                        "NorthEast or NorthWest.");
            }
        }


        /// <summary>
        /// NorthEast edges run parallel to the map's Y direction.
        /// Their X coordinate must remain fixed.
        /// </summary>
        private static WallRunPlanResult PlanNorthEastRun(
            CellEdge startEdge,
            CellEdge endEdge)
        {
            if (startEdge.AnchorCell.X
                != endEdge.AnchorCell.X)
            {
                return WallRunPlanResult.Rejected(
                    startEdge,
                    endEdge,
                    WallRunPlanFailure.NotCollinear);
            }

            int startIndex =
                startEdge.AnchorCell.Y;

            int endIndex =
                endEdge.AnchorCell.Y;

            CellEdge[] edges =
                CreateRun(
                    startEdge,
                    startIndex,
                    endIndex,
                    CellEdgeDirection.NorthEast);

            return WallRunPlanResult.Success(
                startEdge,
                endEdge,
                edges);
        }


        /// <summary>
        /// NorthWest edges run parallel to the map's X direction.
        /// Their Y coordinate must remain fixed.
        /// </summary>
        private static WallRunPlanResult PlanNorthWestRun(
            CellEdge startEdge,
            CellEdge endEdge)
        {
            if (startEdge.AnchorCell.Y
                != endEdge.AnchorCell.Y)
            {
                return WallRunPlanResult.Rejected(
                    startEdge,
                    endEdge,
                    WallRunPlanFailure.NotCollinear);
            }

            int startIndex =
                startEdge.AnchorCell.X;

            int endIndex =
                endEdge.AnchorCell.X;

            CellEdge[] edges =
                CreateRun(
                    startEdge,
                    startIndex,
                    endIndex,
                    CellEdgeDirection.NorthWest);

            return WallRunPlanResult.Success(
                startEdge,
                endEdge,
                edges);
        }


        private static CellEdge[] CreateRun(
            CellEdge startEdge,
            int startIndex,
            int endIndex,
            CellEdgeDirection direction)
        {
            int difference =
                endIndex - startIndex;

            int step =
                difference < 0
                    ? -1
                    : 1;

            int segmentCount =
                Math.Abs(difference) + 1;

            CellEdge[] edges =
                new CellEdge[segmentCount];

            for (int index = 0;
                 index < segmentCount;
                 index++)
            {
                int currentIndex =
                    startIndex + index * step;

                GridPosition anchorCell;

                switch (direction)
                {
                    case CellEdgeDirection.NorthEast:
                        anchorCell =
                            new GridPosition(
                                startEdge.AnchorCell.X,
                                currentIndex,
                                startEdge.AnchorCell.Level);
                        break;

                    case CellEdgeDirection.NorthWest:
                        anchorCell =
                            new GridPosition(
                                currentIndex,
                                startEdge.AnchorCell.Y,
                                startEdge.AnchorCell.Level);
                        break;

                    default:
                        throw new InvalidOperationException(
                            "Straight wall runs require a canonical " +
                            "NorthEast or NorthWest direction.");
                }

                edges[index] =
                    new CellEdge(
                        anchorCell,
                        direction);
            }

            return edges;
        }
    }
}