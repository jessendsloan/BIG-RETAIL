using BigRetail.Map.Domain;

namespace BigRetail.Map.Fixtures
{
    /// <summary>
    /// Narrow physical-world boundary used to validate the floor beneath and
    /// beside a fixture, walls crossing its footprint or access faces, and
    /// cells reserved as clear passage on either side of a door.
    /// </summary>
    public interface IFixturePlacementSurfaceQuery
    {
        bool HasFloor(GridPosition cell);

        bool HasWall(CellEdge edge);

        bool IsReservedForDoorPassage(GridPosition cell);
    }
}
