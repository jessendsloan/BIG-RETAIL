using BigRetail.Map.Domain;

namespace BigRetail.Receiving.Domain
{
    /// <summary>
    /// Narrow physical-map boundary used when designating receiving space.
    /// </summary>
    public interface IReceivingAreaSurfaceQuery
    {
        bool HasFloor(GridPosition cell);

        bool IsObstructed(GridPosition cell);
    }
}
