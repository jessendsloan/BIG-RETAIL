using BigRetail.Map.Domain;

namespace BigRetail.Map.Navigation
{
    /// <summary>
    /// Narrow map boundary used by actor routing. The route planner owns
    /// search; the active location owns which cells and edges are traversable.
    /// </summary>
    public interface IGridRouteSurfaceQuery
    {
        bool CanStandAt(GridPosition cell);

        bool CanTraverse(CellEdge edge);
    }
}
