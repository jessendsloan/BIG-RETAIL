using System;
using BigRetail.Map.Domain;

namespace BigRetail.Map.Unity.Doors
{
    /// <summary>
    /// Resolves the open logical edge for a one-panel hinged door.
    /// The canonical second vertex is the stable physical hinge. Resolving the
    /// swing before view projection keeps that hinge and open side consistent
    /// while the camera rotates around the building.
    /// </summary>
    public static class HingedDoorSwingResolver
    {
        public static CellEdge ResolveOpenLogicalEdge(
            CellEdge closedLogicalEdge)
        {
            GridVertex hinge =
                closedLogicalEdge.SecondVertex;

            GridVertex openEnd =
                closedLogicalEdge.CanonicalDirection switch
                {
                    CellEdgeDirection.NorthEast =>
                        hinge.Offset(1, 0),

                    CellEdgeDirection.NorthWest =>
                        hinge.Offset(0, 1),

                    _ =>
                        throw new InvalidOperationException(
                            "A logical door edge must be normalized to "
                            + "NorthEast or NorthWest.")
                };

            return new CellEdge(
                hinge,
                openEnd);
        }
    }
}
