using System;
using System.Collections.Generic;
using BigRetail.Map.Domain;

namespace BigRetail.Map.View
{
    /// <summary>
    /// Derives which walls obscure constructed foundation for one isometric
    /// view orientation.
    ///
    /// Foundation cells are grouped into diagonal viewing lanes. A wall is
    /// lowered when any lane spanned by that wall contains foundation farther
    /// from the viewer than the wall. Empty ground and presentation-only apron
    /// cells never stop or participate in the scan.
    /// </summary>
    public sealed class FoundationCutawayMap
    {
        private readonly IsometricViewProjection projection;

        private readonly Dictionary<int, int>
            farthestFoundationDepthByLane;

        private readonly HashSet<CellEdge>
            cornerCapsToLower;


        private FoundationCutawayMap(
            IsometricViewProjection projection,
            Dictionary<int, int> farthestFoundationDepthByLane,
            HashSet<CellEdge> cornerCapsToLower)
        {
            this.projection = projection;

            this.farthestFoundationDepthByLane =
                farthestFoundationDepthByLane;

            this.cornerCapsToLower =
                cornerCapsToLower;
        }


        public static FoundationCutawayMap Calculate(
            IsometricViewProjection projection,
            IEnumerable<GridPosition> foundationCells)
        {
            if (projection == null)
            {
                throw new ArgumentNullException(
                    nameof(projection));
            }

            if (foundationCells == null)
            {
                throw new ArgumentNullException(
                    nameof(foundationCells));
            }

            Dictionary<int, int> farthestDepths =
                new Dictionary<int, int>();

            foreach (GridPosition foundationCell in foundationCells)
            {
                if (foundationCell.Level
                    != projection.Footprint.LogicalLevel)
                {
                    continue;
                }

                GridPosition displayCell =
                    projection.ToDisplayCell(
                        foundationCell);

                int lane =
                    GetViewLane(displayCell);

                int depth =
                    GetViewDepth(displayCell);

                if (!farthestDepths.TryGetValue(
                        lane,
                        out int farthestDepth)
                    || depth > farthestDepth)
                {
                    farthestDepths[lane] =
                        depth;
                }
            }

            return new FoundationCutawayMap(
                projection,
                farthestDepths,
                new HashSet<CellEdge>());
        }


        /// <summary>
        /// Builds the foundation scan and removes isolated full-height corner
        /// caps whose two endpoints both connect to walls lowered by that
        /// scan. The cleanup is deliberately non-propagating so it cannot
        /// erode a legitimate run of rear walls.
        /// </summary>
        public static FoundationCutawayMap Calculate(
            IsometricViewProjection projection,
            IEnumerable<GridPosition> foundationCells,
            IEnumerable<CellEdge> walls)
        {
            if (walls == null)
            {
                throw new ArgumentNullException(
                    nameof(walls));
            }

            FoundationCutawayMap baseMap =
                Calculate(
                    projection,
                    foundationCells);

            HashSet<CellEdge> uniqueWalls =
                new HashSet<CellEdge>();

            Dictionary<GridVertex, List<CellEdge>>
                wallsByVertex =
                    new Dictionary<GridVertex, List<CellEdge>>();

            HashSet<CellEdge> laneLoweredWalls =
                new HashSet<CellEdge>();

            foreach (CellEdge wall in walls)
            {
                if (wall.FirstCell.Level
                        != projection.Footprint.LogicalLevel
                    || !uniqueWalls.Add(wall))
                {
                    continue;
                }

                AddWallAtVertex(
                    wallsByVertex,
                    wall.FirstVertex,
                    wall);

                AddWallAtVertex(
                    wallsByVertex,
                    wall.SecondVertex,
                    wall);

                if (baseMap.ShouldLowerForFoundation(
                        wall))
                {
                    laneLoweredWalls.Add(wall);
                }
            }

            HashSet<CellEdge> cornerCaps =
                new HashSet<CellEdge>();

            foreach (CellEdge wall in uniqueWalls)
            {
                if (laneLoweredWalls.Contains(wall)
                    || !HasLoweredNeighbor(
                        wallsByVertex,
                        laneLoweredWalls,
                        wall.FirstVertex)
                    || !HasLoweredNeighbor(
                        wallsByVertex,
                        laneLoweredWalls,
                        wall.SecondVertex))
                {
                    continue;
                }

                cornerCaps.Add(wall);
            }

            return new FoundationCutawayMap(
                projection,
                baseMap.farthestFoundationDepthByLane,
                cornerCaps);
        }


        /// <summary>
        /// Returns true when a full-height wall would stand in front of at
        /// least one foundation tile in a viewing lane covered by the wall.
        /// </summary>
        public bool ShouldLowerWall(
            CellEdge logicalEdge)
        {
            return cornerCapsToLower.Contains(
                    logicalEdge)
                || ShouldLowerForFoundation(
                    logicalEdge);
        }


        private bool ShouldLowerForFoundation(
            CellEdge logicalEdge)
        {
            if (logicalEdge.FirstCell.Level
                != projection.Footprint.LogicalLevel)
            {
                return false;
            }

            WallPresentationSelection selection =
                WallPresentationSelector.Select(
                    logicalEdge,
                    projection);

            GridPosition viewerDisplayCell =
                projection.ToDisplayCell(
                    selection.ViewerFacingCell);

            GridPosition farCell =
                selection.ViewerFacingCell
                == logicalEdge.FirstCell
                    ? logicalEdge.SecondCell
                    : logicalEdge.FirstCell;

            GridPosition farDisplayCell =
                projection.ToDisplayCell(
                    farCell);

            int wallDepth =
                GetViewDepth(
                    viewerDisplayCell);

            return HasFoundationBeyond(
                    GetViewLane(
                        viewerDisplayCell),
                    wallDepth)
                || HasFoundationBeyond(
                    GetViewLane(
                        farDisplayCell),
                    wallDepth);
        }


        private static void AddWallAtVertex(
            Dictionary<GridVertex, List<CellEdge>> wallsByVertex,
            GridVertex vertex,
            CellEdge wall)
        {
            if (!wallsByVertex.TryGetValue(
                    vertex,
                    out List<CellEdge> touchingWalls))
            {
                touchingWalls =
                    new List<CellEdge>();

                wallsByVertex.Add(
                    vertex,
                    touchingWalls);
            }

            touchingWalls.Add(wall);
        }


        private static bool HasLoweredNeighbor(
            Dictionary<GridVertex, List<CellEdge>> wallsByVertex,
            HashSet<CellEdge> laneLoweredWalls,
            GridVertex vertex)
        {
            if (!wallsByVertex.TryGetValue(
                    vertex,
                    out List<CellEdge> touchingWalls))
            {
                return false;
            }

            for (int index = 0;
                 index < touchingWalls.Count;
                 index++)
            {
                if (laneLoweredWalls.Contains(
                        touchingWalls[index]))
                {
                    return true;
                }
            }

            return false;
        }


        private bool HasFoundationBeyond(
            int lane,
            int wallDepth)
        {
            return farthestFoundationDepthByLane.TryGetValue(
                    lane,
                    out int farthestFoundationDepth)
                && farthestFoundationDepth > wallDepth;
        }


        private static int GetViewLane(
            GridPosition displayCell)
        {
            return displayCell.X
                - displayCell.Y;
        }


        private static int GetViewDepth(
            GridPosition displayCell)
        {
            return displayCell.X
                + displayCell.Y;
        }
    }
}
