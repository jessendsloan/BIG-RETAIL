using BigRetail.Map.Domain;

namespace BigRetail.Map.Sidewalks
{
    /// <summary>
    /// Narrow navigation seam for pedestrian routing over sidewalk cells.
    /// </summary>
    public interface ISidewalkWalkabilityQuery
    {
        bool IsSidewalkWalkable(GridPosition cell);
    }
}
