using BigRetail.Map.Domain;

namespace BigRetail.Map.Construction
{
    /// <summary>
    /// Answers whether a logical map cell is reserved by a sidewalk.
    ///
    /// Foundation construction depends on this neutral contract rather than
    /// depending directly on the sidewalk subsystem.
    /// </summary>
    public interface ISidewalkOccupancyQuery
    {
        bool HasSidewalk(GridPosition cell);
    }


    /// <summary>
    /// Compatibility query for scenes and tests that do not host sidewalks.
    /// </summary>
    public sealed class EmptySidewalkOccupancyQuery :
        ISidewalkOccupancyQuery
    {
        public static EmptySidewalkOccupancyQuery Instance
        {
            get;
        } = new EmptySidewalkOccupancyQuery();


        private EmptySidewalkOccupancyQuery()
        {
        }


        public bool HasSidewalk(GridPosition cell)
        {
            return false;
        }
    }
}
