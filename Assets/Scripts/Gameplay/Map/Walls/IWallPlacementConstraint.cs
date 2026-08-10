using BigRetail.Map.Domain;

namespace BigRetail.Map.Walls
{
    /// <summary>
    /// A runtime domain boundary that may reject construction on a wall edge.
    /// Returning None leaves the edge available to later validation rules.
    /// </summary>
    public interface IWallPlacementConstraint
    {
        WallChangeFailure EvaluateWallPlacement(CellEdge edge);
    }
}
